#include <unity.h>
#include "unity_test_runner.h"

#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_log.h"

#include "Led.h"

void force_linker_led()
{
}

static constexpr gpio_num_t TEST_LED_GPIO = GPIO_NUM_48;

class LedTest
{
public:
    static const uint8_t *colorBytes(const Led &l) { return l.led_color_; }
    static uint8_t brightness(const Led &l) { return l.brightness_; }
    static bool initialized(const Led &l) { return l.initialized_; }
};

// ---------- Construcción y lifecycle ----------

TEST_CASE("Led can be constructed and initialized", "[led]")
{
    Led led(TEST_LED_GPIO);
    TEST_ASSERT_FALSE(LedTest::initialized(led));
    TEST_ASSERT_EQUAL(ESP_OK, led.begin());
    TEST_ASSERT_TRUE(LedTest::initialized(led));
}

TEST_CASE("Led starts black and at full brightness", "[led]")
{
    Led led(TEST_LED_GPIO);
    led.begin();
    // begin() termina seteando el LED a black
    TEST_ASSERT_EQUAL(Led::colors::black, led.currentColor());
    TEST_ASSERT_EQUAL_UINT8(100, LedTest::brightness(led));
}

// ---------- Brillo ----------

TEST_CASE("setBrightness clamps values above 100", "[led]")
{
    Led led(TEST_LED_GPIO);
    led.begin();
    led.setBrightness(200);
    TEST_ASSERT_EQUAL_UINT8(100, LedTest::brightness(led));
}

TEST_CASE("setBrightness ignores zero", "[led]")
{
    Led led(TEST_LED_GPIO);
    led.begin();
    led.setBrightness(50);
    led.setBrightness(0);
    TEST_ASSERT_EQUAL_UINT8(50, LedTest::brightness(led));
}

TEST_CASE("setBrightness accepts boundary values", "[led]")
{
    Led led(TEST_LED_GPIO);
    led.begin();
    led.setBrightness(1);
    TEST_ASSERT_EQUAL_UINT8(1, LedTest::brightness(led));
    led.setBrightness(100);
    TEST_ASSERT_EQUAL_UINT8(100, LedTest::brightness(led));
}

// ---------- Orden GRB en setColor(r,g,b) ----------

TEST_CASE("setColor(r,g,b) stores bytes in GRB order", "[led]")
{
    Led led(TEST_LED_GPIO);
    led.begin();
    led.setColor(10, 20, 30); // R=10, G=20, B=30
    const uint8_t *c = LedTest::colorBytes(led);
    TEST_ASSERT_EQUAL_UINT8(20, c[0]); // G
    TEST_ASSERT_EQUAL_UINT8(10, c[1]); // R
    TEST_ASSERT_EQUAL_UINT8(30, c[2]); // B
}

TEST_CASE("setColor(r,g,b) sets currentColor to undefined", "[led]")
{
    Led led(TEST_LED_GPIO);
    led.begin();
    led.setColor(Led::colors::red);
    TEST_ASSERT_EQUAL(Led::colors::red, led.currentColor());
    led.setColor(1, 2, 3);
    TEST_ASSERT_EQUAL(Led::colors::undefined, led.currentColor());
}

// ---------- Colores predefinidos + escala de brillo ----------

TEST_CASE("black is 0,0,0 regardless of brightness", "[led]")
{
    Led led(TEST_LED_GPIO);
    led.begin();
    led.setBrightness(50);
    led.setColor(Led::colors::black);
    const uint8_t *c = LedTest::colorBytes(led);
    TEST_ASSERT_EQUAL_UINT8(0, c[0]);
    TEST_ASSERT_EQUAL_UINT8(0, c[1]);
    TEST_ASSERT_EQUAL_UINT8(0, c[2]);
}

TEST_CASE("red at 100% brightness uses 255 in R slot only", "[led]")
{
    Led led(TEST_LED_GPIO);
    led.begin();
    led.setBrightness(100);
    led.setColor(Led::colors::red);
    const uint8_t *c = LedTest::colorBytes(led); // GRB
    TEST_ASSERT_EQUAL_UINT8(0, c[0]);
    TEST_ASSERT_EQUAL_UINT8(255, c[1]);
    TEST_ASSERT_EQUAL_UINT8(0, c[2]);
}

TEST_CASE("blue at 100% brightness uses 255 in B slot only", "[led]")
{
    Led led(TEST_LED_GPIO);
    led.begin();
    led.setColor(Led::colors::blue);
    const uint8_t *c = LedTest::colorBytes(led);
    TEST_ASSERT_EQUAL_UINT8(0, c[0]);
    TEST_ASSERT_EQUAL_UINT8(0, c[1]);
    TEST_ASSERT_EQUAL_UINT8(255, c[2]);
}

TEST_CASE("green at 100% brightness uses 255 in G slot only", "[led]")
{
    Led led(TEST_LED_GPIO);
    led.begin();
    led.setColor(Led::colors::green);
    const uint8_t *c = LedTest::colorBytes(led);
    TEST_ASSERT_EQUAL_UINT8(255, c[0]);
    TEST_ASSERT_EQUAL_UINT8(0, c[1]);
    TEST_ASSERT_EQUAL_UINT8(0, c[2]);
}

TEST_CASE("white at 50% scales all channels", "[led]")
{
    Led led(TEST_LED_GPIO);
    led.begin();
    led.setBrightness(50);
    led.setColor(Led::colors::white);
    const uint8_t *c = LedTest::colorBytes(led);
    TEST_ASSERT_EQUAL_UINT8(127, c[0]);
    TEST_ASSERT_EQUAL_UINT8(127, c[1]);
    TEST_ASSERT_EQUAL_UINT8(127, c[2]);
}

// ---------- currentColor ----------

TEST_CASE("currentColor returns last named color", "[led]")
{
    Led led(TEST_LED_GPIO);
    led.begin();
    led.setColor(Led::colors::green);
    TEST_ASSERT_EQUAL(Led::colors::green, led.currentColor());
    led.setColor(Led::colors::blue);
    TEST_ASSERT_EQUAL(Led::colors::blue, led.currentColor());
}

// ---------- Polimorfismo via ILed ----------

TEST_CASE("Led is usable through ILed interface", "[led]")
{
    Led led(TEST_LED_GPIO);
    led.begin();
    ILed &i = led;
    i.setColor(ILed::colors::red);
    TEST_ASSERT_EQUAL(ILed::colors::red, i.currentColor());
}