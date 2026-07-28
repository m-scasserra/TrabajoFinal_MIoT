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

#include "LoRaWAN.h"

static LoRaWAN radio;

static void onDownlink(uint8_t port, const uint8_t *data, uint8_t length, void *context)
{
    ESP_LOGI("LoRaWAN", "Downlink received on port %d, length %d", port, length);
    for (int i = 0; i < length; ++i)
    {
        ESP_LOGI("LoRaWAN", "Data[%d]: 0x%02X", i, data[i]);
    }
}

static void onJoin(ILoRaWAN::joinState state, void *context)
{
    ESP_LOGI("LoRaWAN", "Join state changed: %d", static_cast<int>(state));
    if (state == ILoRaWAN::joinState::joined)
    {
        ESP_LOGI("LoRaWAN", "Successfully joined the network.");
    }
    else if (state == ILoRaWAN::joinState::failed)
    {
        ESP_LOGE("LoRaWAN", "Failed to join the network.");
    }
}

extern "C" void app_main(void)
{
    LoRaWAN::credentials_t creds = {
        .devEui = {0x70, 0xB3, 0xD5, 0x7E, 0xD0, 0x07, 0x88, 0x0B},
        .joinEui = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
        .appKey = {0xA0, 0x80, 0x9D, 0x29, 0xC8, 0x0C, 0xC9, 0xAD, 0x7B, 0x4D, 0x99, 0x6D, 0x69, 0x90, 0xB1, 0xD2}};

    radio.setCredentials(creds);

    radio.setDownlinkCallback(onDownlink, nullptr);
    radio.setJoinCallback(onJoin, nullptr);

    radio.begin();
    vTaskDelay(pdMS_TO_TICKS(500));
    radio.join();
    while (!radio.isJoined())
    {
        ESP_LOGI("LoRaWAN", "Waiting to join the network...");
        vTaskDelay(pdMS_TO_TICKS(1000));
        if (radio.currentJoinState() == ILoRaWAN::joinState::failed)
        {
            radio.join();
        }
    }

    uint8_t payload[] = {0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x20, 0x57, 0x6f, 0x72, 0x6c, 0x64, 0x21, 0x00};
    while (true)
    {
        ESP_LOGI("LoRaWAN", "Sending payload: %s", payload);
        radio.send(1, payload, sizeof(payload));
        vTaskDelay(pdMS_TO_TICKS(60000));
        payload[12]++;
    }
}