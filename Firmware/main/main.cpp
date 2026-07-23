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
#include "E22Driver.h"
extern "C" void app_main(void);

void app_main(void)
{
    Led led(GPIO_NUM_48);
    led.begin();
    led.setColor(Led::colors::black);
    vTaskDelay(1000 / portTICK_PERIOD_MS);

    E22 &e22 = E22::getInstance();
    e22.Begin();
    while (true)
    {
        vTaskDelay(1000 / portTICK_PERIOD_MS);
    }
}