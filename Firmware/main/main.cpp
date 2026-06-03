/*
 * SPDX-FileCopyrightText: 2021-2024 Espressif Systems (Shanghai) CO LTD
 *
 * SPDX-License-Identifier: Unlicense OR CC0-1.0
 */
#include <string.h>
#include <math.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_log.h"
#include "driver/rmt_tx.h"

#include "Led.h"
extern "C" void app_main(void);

void app_main(void)
{
    Led led(GPIO_NUM_48);
    led.begin();
    led.setColor(Led::colors::green);
    vTaskDelay(1000 / portTICK_PERIOD_MS);
    while (true)
    {
        led.setColor(Led::colors::red);
        vTaskDelay(1000 / portTICK_PERIOD_MS);
        led.setColor(Led::colors::green);
        vTaskDelay(1000 / portTICK_PERIOD_MS);
        led.setColor(Led::colors::blue);
        vTaskDelay(1000 / portTICK_PERIOD_MS);
        led.setColor(Led::colors::purple);
        vTaskDelay(1000 / portTICK_PERIOD_MS);
        led.setColor(Led::colors::cyan);
        vTaskDelay(1000 / portTICK_PERIOD_MS);
        led.setColor(Led::colors::yellow);
        vTaskDelay(1000 / portTICK_PERIOD_MS);
        led.setColor(Led::colors::white);
        vTaskDelay(1000 / portTICK_PERIOD_MS);
        led.setColor(102, 0, 204);
        vTaskDelay(1000 / portTICK_PERIOD_MS);
    }
}