#ifndef LED_H
#define LED_H

#include <cstdint>

#include "driver/rmt_encoder.h"
#include "driver/rmt_tx.h"
#include "freertos/FreeRTOS.h"
#include "freertos/semphr.h"
#include "hal/gpio_types.h"

#include "ILed.h"

/**
 * @defgroup Led Module
 * @brief Driver for a single WS2812 RGB LED using the ESP32 RMT peripheral.
 *
 * ### Usage
 * Call @c begin() once from a task context before using any other method.
 * All subsequent calls are thread-safe and must also originate from task context.
 * (ISR context is not supported).
 *
 * ### Thread-safety model
 * All public methods are protected by a FreeRTOS mutex created in @c begin().
 * Callers may block up to @c portMAX_DELAY waiting to acquire the mutex.
 * @{
 */

/**
 * @brief Driver for a single WS2812 RGB LED via the RMT peripheral.
 *
 * ### Lifecycle
 * Call @c begin() exactly once before using any other method.
 * Calling any method before @c begin() results in undefined behavior.
 */
class Led : public ILed
{
public:
    /**
     * @brief Construct a Led driver bound to the given GPIO pin.
     *
     * @param gpio GPIO pin number where the WS2812 data line is connected.
     */
    explicit Led(gpio_num_t gpio);
    ~Led() override;

    // ------------------------------------------------------------------ //
    // Lifecycle                                                          //
    // ------------------------------------------------------------------ //

    /**
     * @brief Initialize the RMT channel, encoder and the instance mutex.
     *
     * Must be called exactly once from a task context any other
     * method. Returns @c ESP_OK immediately if already initialzed.
     * Logs an error and returns the failing error code if any RMT initialization
     * step fails.
     *
     * @return @c ESP_OK on success, or an esp_err_t error code on failure.
     * @note Must be called from a task context only.
     */

    esp_err_t begin();

    // ------------------------------------------------------------------ //
    // Control API                                                        //
    // ------------------------------------------------------------------ //

    /**
     * @brief Set the LED color from raw RGB components.
     *
     * Sets @c currentColor() to @c undefined. The LED is updated
     * immediately.
     *
     * @note Internally the WS2812 protocol requires GRB ordering;
     *       this method accepts standard RGB and reorders automatically.
     *
     * @param r Red component (0-255).
     * @param g Green component (0-255).
     * @param b Blue component (0-255).
     * @note Must be called from a task context only.
     */
    void setColor(uint8_t R, uint8_t G, uint8_t B);

    /**
     * @brief Set the LED to a pre-defined color.
     *
     * The color is scaled by the current brightness level. Has no
     * effect if @p color already matches @c currentColor().
     *
     * @param color One of the @c colors enumerators (except @c undefined).
     * @note Must be called from a task context only.
     */
    void setColor(colors color);

    /**
     * @brief Return the currently active pre-defined color.
     *
     * Returns @c undefined when the color was set via raw RGB.
     *
     * @return Current @c colors value.
     * @note Must be called from a task context only.
     */
    colors currentColor() const;

    /**
     * @brief Set the brightness level applied to all pre-defined colors.
     *
     * Values outside the 1-100 range are clamped: values above 100 are
     * set to 100, while a value of 0 is ignored and the brightness remains
     * unchanged.
     *
     * @param brightness Brightness percentage (1-100).
     * @note Must be called from a task context only.
     */
    void setBrightness(uint8_t brightness);

    /**
     * @brief Transmit the current color to the LED via RMT.
     *
     * Transmits the GRB payload over the already-enabled RMT channel and
     * waits for completion (up to @c portMAX_DELAY).
     * Logs an error if @c rmt_transmit fails.
     *
     * @note Must be called from a task context only.
     */
    void show();

private:
    // ------------------------------------------------------------------ //
    // RMT encoder types                                                  //
    // ------------------------------------------------------------------ //

    /**
     * @brief Configuration for the WS2812 RMT encoder.
     */
    typedef struct
    {
        uint32_t resolution; ///< Encoder resolution in Hz.
    } led_strip_encoder_config_t;

    /**
     * @brief Internal state for the WS2812 RMT encoder.
     */
    typedef struct
    {
        rmt_encoder_t base;           ///< Base encoder (must be first member).
        rmt_encoder_t *bytes_encoder; ///< Encoder raw byte data.
        rmt_encoder_t *copy_encoder;  ///< Copies the reset symbol.
        uint8_t state;                ///< Encoder FSM state (0 = data, 1 = reset).
        rmt_symbol_word_t reset_code; ///< WS2812 reset symbol (>= 50µs low).
    } rmt_led_strip_encoder_t;

    // ------------------------------------------------------------------ //
    // RMT encoder callbacks                                              //
    // ------------------------------------------------------------------ //

    /**
     * @brief Encode RGB pixel data into RMT symbols.
     *
     * Implements the @c rmt_encoder_t::encode callback. Encodes the RGB
     * payload followed by the WS2812 reset code using a two-state FSM.
     *
     * @param encoder      Pointer to the base @c rmt_encoder_t.
     * @param channel      RMT channel handle.
     * @param primary_data Pointer to the GRB payload.
     * @param data_size    Size of @p primary_data in bytes.
     * @param ret_state    Output encoder state flags.
     * @return             Number of RMT symbols written.
     */
    static size_t rmt_encode_led_strip(rmt_encoder_t *encoder,
                                       rmt_channel_handle_t channel,
                                       const void *primary_data,
                                       size_t data_size,
                                       rmt_encode_state_t *ret_state);

    /**
     * @brief Release all resources held by the LED strip encoder.
     *
     * Implements the @c rmt_encoder_t::del callback.
     *
     * @param encoder Pointer to the base @c rmt_encoder_t.
     * @return        @c ESP_OK on success.
     */
    static esp_err_t rmt_del_led_strip_encoder(rmt_encoder_t *encoder);

    /**
     * @brief the LED strip encoder FSM to its initial state.
     *
     * Implements the @c rmt_encoder_t::reset callback.
     *
     * @param encoder Pointer to the base @c rmt_encoder_t.
     * @return        @c ESP_OK on success.
     */
    static esp_err_t rmt_led_strip_encoder_reset(rmt_encoder_t *encoder);

    /**
     * @brief Allocate and configure a WS2812 RMT encoder.
     *
     * @param config      Encoder configuration (resolution in Hz).
     * @param ret_encoder Output handle for the created encoder.
     * @return            @c ESP_OK on success.
     */
    static esp_err_t rmt_new_led_strip_encoder(const led_strip_encoder_config_t *config,
                                               rmt_encoder_handle_t *ret_encoder);

    // ------------------------------------------------------------------ //
    // WS2812 timing constants (10 MHz RMT clock -> 1 tick = 0.1 µs)      //
    // ------------------------------------------------------------------ //

    static constexpr uint32_t RMT_RESOLUTION_HZ = 10000000; ///< RMT clock: 10MHz (1 tick = 0.1 µs).
    static constexpr uint32_t RMT_RESOL_TO_TICKS = 1000000; ///< Divisor to convert Hz x µs -> ticks.
    static constexpr float T0H = 0.3f;                      ///< WS2812 bit-0 high time (µs).
    static constexpr float T0L = 0.9f;                      ///< WS2812 bit-0 low time (µs).
    static constexpr float T1H = 0.9f;                      ///< WS2812 bit-1 high time (µs).
    static constexpr float T1L = 0.3f;                      ///< WS2812 bit-1 low time (µs).
    static constexpr float TRST = 100.0f;                   ///< WS2812 reset pulse duration (µs, >= 50 µs).

    // ------------------------------------------------------------------ //
    // Instance state                                                     //
    // ------------------------------------------------------------------ //

    const gpio_num_t gpio_;                      ///< GPIO number where the LED is connected.
    rmt_channel_handle_t led_chan_ = nullptr;    ///< RMT TX channel handle.
    rmt_encoder_handle_t led_encoder_ = nullptr; ///< WS2812 RMT encoder handle.
    rmt_transmit_config_t tx_config_;            ///< RMT transmit configuration

    uint8_t brightness_ = 100;                 ///< Brightness percentage (1-100).
    uint8_t led_color_[3] = {0, 0, 0};         ///< GRB payload sent to the LED.
    colors current_color_ = colors::undefined; ///< Last named color set, or @c undefined.

    SemaphoreHandle_t mutex_ = nullptr; ///< Mutex protecting all instance state.

    bool initialized_ = false; ///< True if @c begin() has been called successfully.

    friend class LedTest;
    static constexpr const char *TAG = "LED"; ///< ESP-IDF log tag.
};

/** @} */ // end of Led group

#endif // LED_H