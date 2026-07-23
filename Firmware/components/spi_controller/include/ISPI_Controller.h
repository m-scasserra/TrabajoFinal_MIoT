#ifndef ISPI_CONTROLLER_H
#define ISPI_CONTROLLER_H

#include <cstdint>

#include "driver/spi_master.h"

/**
 * @brief Interface for a single SPI/bus device driver.
 *
 * Abstracts the SPI bus initialization, device attachment and message
 * transfer so that consumers can be unit tested against a mock
 * implementation instead of the real ESP-IDF SPI driver.
 */
class ISPIController
{
public:
    virtual ~ISPIController() = default;

    virtual bool begin(spi_bus_config_t *busConfig, spi_host_device_t hostId, spi_dma_chan_t dmaChan) = 0;
    virtual bool addDevice(spi_device_interface_config_t *slaveConfig) = 0;
    virtual bool sendMessage(uint8_t *txMsg, uint8_t txLen, uint8_t *rxMsg, uint8_t rxLen) = 0;
    virtual bool sendMessage(uint8_t *txMsg, uint8_t txLen) = 0;
};

#endif // ISPI_CONTROLLER_H