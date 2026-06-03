#include "Led.h"

#include <cstring>

#include "esp_log.h"

// ------------------------------------------------------------------ //
// Lifecycle                                                          //
// ------------------------------------------------------------------ //

Led::Led(gpio_num_t gpio) : gpio_(gpio) {}

Led::~Led()
{
    if (led_chan_ != nullptr)
    {
        rmt_disable(led_chan_);
        rmt_del_channel(led_chan_);
    }
    if (led_encoder_ != nullptr)
    {
        rmt_del_encoder(led_encoder_);
    }
    if (mutex_ != nullptr)
    {
        vSemaphoreDelete(mutex_);
    }
}

esp_err_t Led::begin()
{
    if (initialized_)
    {
        return ESP_OK;
    }
    mutex_ = xSemaphoreCreateMutex();
    if (mutex_ == nullptr)
    {
        ESP_LOGE(TAG, "begin: Failed to create mutex.");
        return ESP_ERR_NO_MEM;
    }

    rmt_tx_channel_config_t tx_chan_config = {};
    led_strip_encoder_config_t enc_config = {};

    tx_chan_config.gpio_num = gpio_;
    tx_chan_config.clk_src = RMT_CLK_SRC_DEFAULT;
    tx_chan_config.resolution_hz = RMT_RESOLUTION_HZ;
    tx_chan_config.mem_block_symbols = 64;
    tx_chan_config.trans_queue_depth = 4;

    esp_err_t err = rmt_new_tx_channel(&tx_chan_config, &led_chan_);
    if (err != ESP_OK)
    {
        ESP_LOGE(TAG, "begin: rmt_new_tx_channel failed (%d).", err);
        return err;
    }

    enc_config.resolution = RMT_RESOLUTION_HZ;
    err = rmt_new_led_strip_encoder(&enc_config, &led_encoder_);
    if (err != ESP_OK)
    {
        ESP_LOGE(TAG, "begin: rmt_new_led_strip_encoder failed (%d).", err);
        return err;
    }

    tx_config_.loop_count = 0;

    err = rmt_enable(led_chan_);
    if (err != ESP_OK)
    {
        ESP_LOGE(TAG, "begin: rmt_enable failed (%d).", err);
        return err;
    }

    initialized_ = true;

    setColor(colors::white);
    setColor(colors::black);

    return ESP_OK;
}

// ------------------------------------------------------------------ //
// Control API                                                        //
// ------------------------------------------------------------------ //

Led::colors Led::currentColor() const
{
    xSemaphoreTake(mutex_, portMAX_DELAY);
    colors c = current_color_;
    xSemaphoreGive(mutex_);
    return c;
}

void Led::setColor(uint8_t R, uint8_t G, uint8_t B)
{
    xSemaphoreTake(mutex_, portMAX_DELAY);
    // WS2812 expects GRB order
    led_color_[0] = G;
    led_color_[1] = R;
    led_color_[2] = B;
    current_color_ = colors::undefined;
    xSemaphoreGive(mutex_);
    show();
}

void Led::setColor(colors color)
{
    if (color == currentColor())
    {
        return;
    }

    const auto scaled = static_cast<uint8_t>(255 * (brightness_ / 100.0f));

    xSemaphoreTake(mutex_, portMAX_DELAY);
    switch (color)
    {
    case colors::black:
        led_color_[0] = 0;
        led_color_[1] = 0;
        led_color_[2] = 0;
        break;
    case colors::red:
        led_color_[0] = 0;
        led_color_[1] = scaled;
        led_color_[2] = 0;
        break;
    case colors::blue:
        led_color_[0] = 0;
        led_color_[1] = 0;
        led_color_[2] = scaled;
        break;
    case colors::green:
        led_color_[0] = scaled;
        led_color_[1] = 0;
        led_color_[2] = 0;
        break;
    case colors::purple:
        led_color_[0] = 0;
        led_color_[1] = scaled;
        led_color_[2] = scaled;
        break;
    case colors::cyan:
        led_color_[0] = scaled;
        led_color_[1] = 0;
        led_color_[2] = scaled;
        break;
    case colors::yellow:
        led_color_[0] = scaled;
        led_color_[1] = scaled;
        led_color_[2] = 0;
        break;
    case colors::white:
        led_color_[0] = scaled;
        led_color_[1] = scaled;
        led_color_[2] = scaled;
        break;
    default:
        xSemaphoreGive(mutex_);
        return;
        break;
    }
    current_color_ = color;
    xSemaphoreGive(mutex_);
    show();
}

void Led::setBrightness(uint8_t brightness)
{
    xSemaphoreTake(mutex_, portMAX_DELAY);
    if (brightness == 0)
    {
        // Silently ignored - zero would turn the LED off, unexpectedly.
    }
    else
    {
        brightness_ = (brightness > 100) ? 100 : brightness;
    }
    xSemaphoreGive(mutex_);
}

void Led::show()
{
    xSemaphoreTake(mutex_, portMAX_DELAY);

    esp_err_t err = rmt_transmit(led_chan_, led_encoder_, led_color_, sizeof(led_color_), &tx_config_);
    if (err == ESP_OK)
    {
        rmt_tx_wait_all_done(led_chan_, portMAX_DELAY);
    }
    else
    {
        ESP_LOGE(TAG, "show: rmt_transmit failed (%d).", err);
    }
    xSemaphoreGive(mutex_);
}

// ------------------------------------------------------------------ //
// RMT encoder callbacks                                              //
// ------------------------------------------------------------------ //

size_t Led::rmt_encode_led_strip(rmt_encoder_t *encoder,
                                 rmt_channel_handle_t channel,
                                 const void *primary_data,
                                 size_t data_size,
                                 rmt_encode_state_t *ret_state)
{
    rmt_led_strip_encoder_t *led_encoder = __containerof(encoder, rmt_led_strip_encoder_t, base);
    rmt_encoder_handle_t bytes_encoder = led_encoder->bytes_encoder;
    rmt_encoder_handle_t copy_encoder = led_encoder->copy_encoder;
    rmt_encode_state_t session_state = RMT_ENCODING_RESET;
    rmt_encode_state_t state = RMT_ENCODING_RESET;
    size_t encoded_symbols = 0;

    switch (led_encoder->state)
    {
    case 0: // Encode RGB payload
        encoded_symbols += bytes_encoder->encode(bytes_encoder,
                                                 channel,
                                                 primary_data,
                                                 data_size,
                                                 &session_state);

        if (session_state & RMT_ENCODING_COMPLETE)
        {
            led_encoder->state = 1;
        }
        if (session_state & RMT_ENCODING_MEM_FULL)
        {
            state = RMT_ENCODING_MEM_FULL;
            goto out;
        }
        // fall-through
    case 1: // Encode reset pulse
        encoded_symbols += copy_encoder->encode(copy_encoder,
                                                channel,
                                                &led_encoder->reset_code,
                                                sizeof(led_encoder->reset_code),
                                                &session_state);

        if (session_state & RMT_ENCODING_COMPLETE)
        {
            led_encoder->state = RMT_ENCODING_RESET;
            state = RMT_ENCODING_COMPLETE;
        }
        if (session_state & RMT_ENCODING_MEM_FULL)
        {
            state = RMT_ENCODING_MEM_FULL;
            goto out;
        }
    }
out:
    *ret_state = state;
    return encoded_symbols;
}

esp_err_t Led::rmt_del_led_strip_encoder(rmt_encoder_t *encoder)
{
    rmt_led_strip_encoder_t *led_encoder = __containerof(encoder, rmt_led_strip_encoder_t, base);
    rmt_del_encoder(led_encoder->bytes_encoder);
    rmt_del_encoder(led_encoder->copy_encoder);
    free(led_encoder);
    return ESP_OK;
}

esp_err_t Led::rmt_led_strip_encoder_reset(rmt_encoder_t *encoder)
{
    rmt_led_strip_encoder_t *led_encoder = __containerof(encoder, rmt_led_strip_encoder_t, base);
    rmt_encoder_reset(led_encoder->bytes_encoder);
    rmt_encoder_reset(led_encoder->copy_encoder);
    led_encoder->state = RMT_ENCODING_RESET;
    return ESP_OK;
}

esp_err_t Led::rmt_new_led_strip_encoder(const led_strip_encoder_config_t *config, rmt_encoder_handle_t *ret_encoder)
{
    rmt_led_strip_encoder_t *led_encoder = NULL;

    led_encoder = (rmt_led_strip_encoder_t *)calloc(1, sizeof(rmt_led_strip_encoder_t));

    led_encoder->base.encode = rmt_encode_led_strip;
    led_encoder->base.del = rmt_del_led_strip_encoder;
    led_encoder->base.reset = rmt_led_strip_encoder_reset;
    // different led strip might have its own timing requirements, following parameter is for WS2812
    rmt_bytes_encoder_config_t bytes_encoder_config;

    bytes_encoder_config.bit0.level0 = 1;
    bytes_encoder_config.bit0.duration0 = T0H * config->resolution / RMT_RESOL_TO_TICKS; // T0H=0.3us 3 ticks
    bytes_encoder_config.bit0.level1 = 0;
    bytes_encoder_config.bit0.duration1 = T0L * config->resolution / RMT_RESOL_TO_TICKS; // T0L=0.9us 9 ticks
    bytes_encoder_config.bit1.level0 = 1;
    bytes_encoder_config.bit1.duration0 = T1H * config->resolution / RMT_RESOL_TO_TICKS; // T1H=0.9us 9 ticks
    bytes_encoder_config.bit1.level1 = 0;
    bytes_encoder_config.bit1.duration1 = T1L * config->resolution / RMT_RESOL_TO_TICKS; // T1L=0.3us 3 ticks
    bytes_encoder_config.flags.msb_first = 1;                                            // WS2812 transfer bit order: G7...G0R7...R0B7...B0

    rmt_new_bytes_encoder(&bytes_encoder_config, &led_encoder->bytes_encoder);
    rmt_copy_encoder_config_t copy_encoder_config = {};
    rmt_new_copy_encoder(&copy_encoder_config, &led_encoder->copy_encoder);

    uint32_t reset_ticks = config->resolution / RMT_RESOL_TO_TICKS * TRST / 2; // reset code duration defaults to 100us 1000 ticks

    led_encoder->reset_code.level0 = 0;
    led_encoder->reset_code.duration0 = reset_ticks;
    led_encoder->reset_code.level1 = 0;
    led_encoder->reset_code.duration1 = reset_ticks;

    *ret_encoder = &led_encoder->base;
    return ESP_OK;
}
