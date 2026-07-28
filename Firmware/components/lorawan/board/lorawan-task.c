#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_log.h"

#include "radio.h"
#include "sx126x-board.h"
#include "LoRaMac.h"
#include "board-config.h"
#include "driver/gpio.h"

#define TAG "LORAWAN-TASK"

TaskHandle_t s_radioTask = NULL;
extern DioIrqHandler *SX126xGetDioIrqHandler(void);

void notify_radio_irq_from_isr()
{
    if (s_radioTask)
    {
        BaseType_t hpw = pdFALSE;
        vTaskNotifyGiveFromISR(s_radioTask, &hpw);
        portYIELD_FROM_ISR(hpw);
    }
}

void notify_radio_irq(void)
{
    if (s_radioTask)
    {
        xTaskNotifyGive(s_radioTask);
    }
}

static void radio_task(void *arg)
{
    (void)arg;
    while (true)
    {
        // Distinguir "hubo interrupcion" de "timeout para pump periodico"
        uint32_t notified = ulTaskNotifyTake(pdTRUE, pdMS_TO_TICKS(10));

        if (notified > 0)
        {
            // Hubo flanco DIO1 real: procesar el evento del radio y rearmar
            DioIrqHandler *h = SX126xGetDioIrqHandler();
            if (h)
                h(NULL);
            Radio.IrqProcess();
            gpio_intr_enable(E22_PIN_DIO1);
        }

        // El pump de la MAC corre siempre (maneja timers, transiciones)
        LoRaMacProcess();
    }
}

void lorawan_task_start(void)
{
    if (s_radioTask)
    {
        return;
    }

    xTaskCreate(radio_task, "lorawan_radio_task", 4096, NULL, configMAX_PRIORITIES - 2, &s_radioTask);
    ESP_LOGI(TAG, "LoRaWAN radio task started.");
}