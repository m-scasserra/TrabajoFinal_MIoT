#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_random.h"

void DelayMs(uint32_t ms)
{
    vTaskDelay(pdMS_TO_TICKS(ms));
}

void DelayMsMcu(uint32_t ms)
{
    vTaskDelay(pdMS_TO_TICKS(ms));
}

void BoardCriticalSectionBegin(uint32_t *mask)
{
    (void)mask;
    portDISABLE_INTERRUPTS();
}

void BoardCriticalSectionEnd(uint32_t *mask)
{
    (void)mask;
    portENABLE_INTERRUPTS();
}

uint32_t BoardGetRandomSeed(void)
{
    return esp_random();
}

uint8_t BoardGetBatteryLevel(void)
{
    return 0;
}

void BoardLowPowerHandler(void) {}

void BoardResetMcu(void)
{
    esp_restart();
}