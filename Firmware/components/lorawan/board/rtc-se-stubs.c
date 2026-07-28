#include <stdint.h>
#include <string.h>

#include "esp_timer.h"
#include "esp_mac.h"

uint32_t RtcGetCalendarTime(uint16_t *milliseconds)
{
    int64_t us = esp_timer_get_time();
    uint32_t seconds = (uint32_t)(us / 1000000);
    if (milliseconds != NULL)
    {
        *milliseconds = (uint16_t)((us / 1000) % 1000);
    }

    return seconds;
}

static uint32_t s_bkupData0 = 0;
static uint32_t s_bkupData1 = 0;

void RtcBkupWrite(uint32_t data0, uint32_t data1)
{
    s_bkupData0 = data0;
    s_bkupData1 = data1;
}

void RtcBkupRead(uint32_t *data0, uint32_t *data1)
{
    if (data0 != NULL)
    {
        *data0 = s_bkupData0;
    }
    if (data1 != NULL)
    {
        *data1 = s_bkupData1;
    }
}

void SoftSeHalGetUniqueId(uint8_t *id)
{
    if (id == NULL)
    {
        return;
    }

    uint8_t mac[6] = {0};
    esp_efuse_mac_get_default(mac);

    id[0] = mac[0];
    id[1] = mac[1];
    id[2] = mac[2];
    id[3] = 0xFF;
    id[4] = 0xFE;
    id[5] = mac[3];
    id[6] = mac[4];
    id[7] = mac[5];
}