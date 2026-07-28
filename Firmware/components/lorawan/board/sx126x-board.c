#include <stdint.h>
#include <stdbool.h>
#include <string.h>

#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/gpio.h"
#include "esp_rom_sys.h"
#include "esp_timer.h"
#include "esp_log.h"

#include "sx126x-board.h"
#include "radio.h"
#include "board-config.h"

#define TAG "SX126X-BOARD"

bool spi_bridge_init(void);
bool spi_bridge_xfer(uint8_t *tx, uint8_t *rx, uint8_t len);

extern TaskHandle_t s_radioTask;

#define XFER_MAX 260
static uint8_t s_txbuf[XFER_MAX];
static uint8_t s_rxbuf[XFER_MAX];

static DioIrqHandler *s_dioIrq;

extern SX126x_t SX126x;

static RadioOperatingModes_t s_operatingMode = MODE_STDBY_RC;

void SX126xWaitOnBusy(void)
{
    int guard = 0;
    while (gpio_get_level(E22_PIN_BUSY) == 1)
    {
        esp_rom_delay_us(1);
        if (++guard > 30000)
        {
            ESP_LOGW(TAG, "SX126xWaitOnBusy: timeout");
            break;
        }
    }
}

void SX126xReset(void)
{
    vTaskDelay(pdMS_TO_TICKS(2));
    gpio_set_direction(E22_PIN_NRST, GPIO_MODE_OUTPUT);
    gpio_set_level(E22_PIN_NRST, 0);
    vTaskDelay(pdMS_TO_TICKS(2));
    gpio_set_level(E22_PIN_NRST, 1);
    vTaskDelay(pdMS_TO_TICKS(6));
    SX126xWaitOnBusy();
}

void SX126xWakeup(void)
{
    s_txbuf[0] = RADIO_GET_STATUS;
    s_txbuf[1] = 0x00;
    spi_bridge_xfer(s_txbuf, s_rxbuf, 2);
    SX126xWaitOnBusy();
    SX126xSetOperatingMode(MODE_STDBY_RC);
}

void SX126xWriteCommand(RadioCommands_t command, uint8_t *buffer, uint16_t size)
{
    SX126xCheckDeviceReady();

    uint16_t total = 1 + size;
    if (total > XFER_MAX)
    {
        total = XFER_MAX;
    }
    s_txbuf[0] = (uint8_t)command;
    if (size)
    {
        memcpy(&s_txbuf[1], buffer, total - 1);
    }
    spi_bridge_xfer(s_txbuf, s_rxbuf, (uint8_t)total);

    if (command != RADIO_SET_SLEEP)
    {
        SX126xWaitOnBusy();
    }
}

uint8_t SX126xReadCommand(RadioCommands_t command, uint8_t *buffer, uint16_t size)
{
    SX126xCheckDeviceReady();

    uint16_t total = 2 + size;
    if (total > XFER_MAX)
    {
        total = XFER_MAX;
    }
    memset(s_txbuf, 0, total);
    s_txbuf[0] = (uint8_t)command;
    spi_bridge_xfer(s_txbuf, s_rxbuf, (uint8_t)total);

    uint8_t status = s_rxbuf[1];

    if (size)
    {
        memcpy(buffer, &s_rxbuf[2], size);
    }

    SX126xWaitOnBusy();
    return status;
}

void SX126xWriteRegisters(uint16_t address, uint8_t *buffer, uint16_t size)
{
    SX126xCheckDeviceReady();

    uint16_t total = 3 + size;
    if (total > XFER_MAX)
    {
        total = XFER_MAX;
    }
    s_txbuf[0] = RADIO_WRITE_REGISTER;
    s_txbuf[1] = (uint8_t)((address & 0xFF00) >> 8);
    s_txbuf[2] = (uint8_t)(address & 0x00FF);
    if (size)
    {
        memcpy(&s_txbuf[3], buffer, total - 3);
    }
    spi_bridge_xfer(s_txbuf, s_rxbuf, (uint8_t)total);

    SX126xWaitOnBusy();
}

void SX126xWriteRegister(uint16_t address, uint8_t value)
{
    SX126xWriteRegisters(address, &value, 1);
}

void SX126xReadRegisters(uint16_t address, uint8_t *buffer, uint16_t size)
{
    SX126xCheckDeviceReady();

    uint16_t total = 4 + size;
    if (total > XFER_MAX)
    {
        total = XFER_MAX;
    }
    memset(s_txbuf, 0, total);
    s_txbuf[0] = RADIO_READ_REGISTER;
    s_txbuf[1] = (uint8_t)((address & 0xFF00) >> 8);
    s_txbuf[2] = (uint8_t)(address & 0x00FF);
    spi_bridge_xfer(s_txbuf, s_rxbuf, (uint8_t)total);

    if (size)
    {
        memcpy(buffer, &s_rxbuf[4], size);
    }

    SX126xWaitOnBusy();
}

uint8_t SX126xReadRegister(uint16_t address)
{
    uint8_t data;
    SX126xReadRegisters(address, &data, 1);
    return data;
}

void SX126xWriteBuffer(uint8_t offset, uint8_t *buffer, uint8_t size)
{
    SX126xCheckDeviceReady();

    uint16_t total = 2 + size;
    if (total > XFER_MAX)
    {
        total = XFER_MAX;
    }
    s_txbuf[0] = RADIO_WRITE_BUFFER;
    s_txbuf[1] = offset;
    if (size)
    {
        memcpy(&s_txbuf[2], buffer, total - 2);
    }
    spi_bridge_xfer(s_txbuf, s_rxbuf, (uint8_t)total);

    SX126xWaitOnBusy();
}

void SX126xReadBuffer(uint8_t offset, uint8_t *buffer, uint8_t size)
{
    SX126xCheckDeviceReady();

    uint16_t total = 3 + size;
    if (total > XFER_MAX)
    {
        total = XFER_MAX;
    }
    memset(s_txbuf, 0, total);
    s_txbuf[0] = RADIO_READ_BUFFER;
    s_txbuf[1] = offset;
    spi_bridge_xfer(s_txbuf, s_rxbuf, (uint8_t)total);
    if (size)
    {
        memcpy(buffer, &s_rxbuf[3], size);
    }

    SX126xWaitOnBusy();
}

uint32_t SX126xGetBoardTcxoWakeupTime(void)
{
    return TCXO_WAKEUP_TIME_MS;
}

void SX126xIoTcxoInit(void)
{
#if USE_TCXO
    CalibrationParams_t calibParam;

    SX126xSetDio3AsTcxoCtrl(TCXO_CTRL_VOLTAGE, SX126xGetBoardTcxoWakeupTime() << 6);
    calibParam.Value = 0x7F;
    SX126xCalibrate(calibParam);
#endif
}

void SX126xIoRfSwitchInit(void)
{
#if USE_DIO2_RF_SWITCH
    SX126xSetDio2AsRfSwitchCtrl(true);
#endif
}

void SX126xSetAndtSw(RadioOperatingModes_t mode)
{
#if !USE_DIO2_RF_SWITCH
    switch (mode)
    {
    case MODE_TX:
        gpio_set_level(E22_PIN_TXEN, 1);
        gpio_set_level(E22_PIN_RXEN, 0);
        break;
    case MODE_RX:
    case MODE_RX_DC:
        gpio_set_level(E22_PIN_TXEN, 0);
        gpio_set_level(E22_PIN_RXEN, 1);
        break;
    default:
        gpio_set_level(E22_PIN_TXEN, 0);
        gpio_set_level(E22_PIN_RXEN, 0);
        break;
    }
#endif
}

uint8_t SX126xGetPaSelect(uint32_t channel)
{
    (void)channel;
    return SX1262;
}

void SX126xAntSwOn(void) {}
void SX126xAntSwOff(void) {}

bool SX126xCheckRfFrequency(uint32_t frequency)
{
    (void)frequency;
    return true;
}

static void IRAM_ATTR dio1_isr(void *arg)
{
    (void)arg;
    gpio_intr_disable(E22_PIN_DIO1);
    BaseType_t hpw = pdFALSE;
    vTaskNotifyGiveFromISR(s_radioTask, &hpw); // necesitás s_radioTask visible aca
    portYIELD_FROM_ISR(hpw);
}

void SX126xIoInit(void)
{
    gpio_config_t io = {};
    io.pin_bit_mask = (1ULL << E22_PIN_BUSY);
    io.mode = GPIO_MODE_INPUT;
    io.pull_up_en = GPIO_PULLUP_DISABLE;
    io.pull_down_en = GPIO_PULLDOWN_DISABLE;
    io.intr_type = GPIO_INTR_DISABLE;
    gpio_config(&io);

    gpio_set_direction(E22_PIN_NRST, GPIO_MODE_OUTPUT);
    gpio_set_level(E22_PIN_NRST, 1);

#if !USE_DIO2_RF_SWITCH
    gpio_set_direction(E22_PIN_RXEN, GPIO_MODE_OUTPUT);
    gpio_set_direction(E22_PIN_TXEN, GPIO_MODE_OUTPUT);
    gpio_set_level(E22_PIN_RXEN, 0);
    gpio_set_level(E22_PIN_TXEN, 0);
#endif

    if (!spi_bridge_init())
    {
        ESP_LOGE(TAG, "SX126xIoInit: spi_bridge_init failed.");
    }
}

void SX126xIoIrqInit(DioIrqHandler dioIrq)
{
    ESP_LOGI(TAG, "SX126xIoIrqInit llamado, handler=%p", dioIrq);
    s_dioIrq = dioIrq;

    gpio_config_t io = {};
    io.pin_bit_mask = (1ULL << E22_PIN_DIO1);
    io.mode = GPIO_MODE_INPUT;
    io.pull_up_en = GPIO_PULLUP_DISABLE;
    io.pull_down_en = GPIO_PULLDOWN_DISABLE;
    io.intr_type = GPIO_INTR_POSEDGE;
    gpio_config(&io);

    static bool isr_service_installed = false;
    if (!isr_service_installed)
    {
        gpio_install_isr_service(0);
        isr_service_installed = true;
    }
    gpio_isr_handler_add(E22_PIN_DIO1, dio1_isr, NULL);
}

DioIrqHandler *SX126xGetDioIrqHandler(void)
{
    return s_dioIrq;
}

void SX126xIoDeInit(void)
{
    gpio_isr_handler_remove(E22_PIN_DIO1);
}

void SX126xIoDbgInit(void) {}
void SX126xDbgPinTxWrite(bool state) { (void)state; }
void SX126xDbgPinRxWrite(bool state) { (void)state; }

uint32_t SX126xGetDio1PinState(void)
{
    return gpio_get_level(E22_PIN_DIO1);
}

RadioOperatingModes_t SX126xGetOperatingMode(void)
{
    return s_operatingMode;
}

void SX126xSetOperatingMode(RadioOperatingModes_t mode)
{
    s_operatingMode = mode;

#if !USE_DIO2_RF_SWITCH
    SX126xSetAndtSw(mode);
#endif
}

void SX126xSetRfTxPower(int8_t power)
{
    if (power > 22)
    {
        power = 22;
    }
    if (power < -3)
    {
        power = -3;
    }
    SX126xSetTxParams(10, RADIO_RAMP_40_US);
}

uint8_t SX126xGetDeviceId(void)
{
    return SX1262;
}