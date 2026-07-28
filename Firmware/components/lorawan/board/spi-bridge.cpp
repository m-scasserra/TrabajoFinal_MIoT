#include "SPI_Controller.h"
#include "board-config.h"
#include "esp_log.h"
extern "C"
{
    static SPIController s_lorawanSpi;
    static bool s_busReady = false;

    bool spi_bridge_init(void)
    {
        spi_bus_config_t busCfg = {};

        busCfg.miso_io_num = E22_PIN_MISO;
        busCfg.mosi_io_num = E22_PIN_MOSI;
        busCfg.sclk_io_num = E22_PIN_SCLK;
        busCfg.quadwp_io_num = -1;
        busCfg.quadhd_io_num = -1;
        busCfg.max_transfer_sz = 256;

        if (!s_lorawanSpi.begin(&busCfg, E22_SPI_HOST, SPI_DMA_DISABLED))
        {
            return false;
        }

        spi_device_interface_config_t devCfg = {};
        devCfg.clock_speed_hz = E22_SPI_CLOCK_HZ;
        devCfg.mode = 0;
        devCfg.spics_io_num = E22_PIN_NSS;
        devCfg.queue_size = 1;
        devCfg.flags = SPI_DEVICE_NO_DUMMY;

        if (!s_lorawanSpi.addDevice(&devCfg))
        {
            return false;
        }

        s_busReady = true;
        return true;
    }

    bool spi_bridge_xfer(uint8_t *tx, uint8_t *rx, uint8_t len)
    {
        if (!s_busReady || len == 0)
        {
            ESP_LOGE("SPI_BRIDGE", "SPI bus not ready or length is zero.");
            return false;
        }
        return s_lorawanSpi.sendMessage(tx, len, rx, len);
    }
}