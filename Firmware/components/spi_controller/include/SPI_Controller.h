#ifndef SPI_CONTROLLER_H
#define SPI_CONTROLLER_H

#include <cstdint>

#include "driver/spi_master.h"
#include "freertos/FreeRTOS.h"
#include "freertos/semphr.h"

#include "ISPI_Controller.h"

/**
 * @defgroup SPI Module
 * @brief Thin wrapper around the ESP-IDF SPI master driver for a single bus/device pair.
 *
 * ### Usage
 * Call @c begin() once to initalize the SPI bus, then call @c addDevice() once to attach
 * the slave device before issuing any @c sendMessage() call.
 *
 * ### Thread-safety model
 * All public methods are protected by a FreeRTOS mutex created on the first
 * call to @c begin(). Callers may block up to @c portMAX_DELAY waiting to
 * acquire the mutex.
 * @{
 */

/**
 * @brief Driver for a single SPI bus with one attached slave device.
 *
 * ### Lifecycle
 * Call @c begin() exactly once, then @c addDevice() exactly once, before
 * issuing any @c sendMessage() call. Calling @c addDevice() or @c sendMessage()
 * before the bus has been initialized returns @c false and logs an error.
 */
class SPIController : public ISPIController
{
public:
    SPIController() = default;
    ~SPIController() override;

    // ------------------------------------------------------------------ //
    // Lifecycle                                                          //
    // ------------------------------------------------------------------ //

    /**
     * @brief Initializes the SPI bus.
     *
     * Creates the instance mutex on the first call. Returns @c true
     * immediately if the bus was already initialized by this instance.
     * Logs an error and returns @c false if the mutex cannot be created,
     * if the underlying ESP-IDF bus was already initialized by someone
     * else, or if initialization otherwise fails.
     *
     * The host device passed here is remembered internally and reused by
     * @c addDevice(), so a given @c SPI instance always attaches its
     * device to the same host it was initialized with.
     *
     * @param busConfig SPI bus configuration.
     * @param hostId    SPI host device to initialize.
     * @param dmaChan   SPI DMA channel to use (defaults to @c SPI_DMA_DISABLED).
     * @return @c true if the SPI bus was initialized correctly, @c false otherwise.
     * @note Must be called from a task context only.
     */

    bool begin(spi_bus_config_t *busConfig, spi_host_device_t hostId, spi_dma_chan_t dmaChan = SPI_DMA_DISABLED) override;

    /**
     * @brief Attach a slave device to the already-initialized SPI bus.
     *
     * Attaches the device to the host device passed to @c begin(). Logs
     * an error and returns @c false if @c begin() has not been called
     * successfully yet, or if the ESP-IDF driver fails to add the device.
     *
     * @param slaveConfig SPI slave (device) configuration.
     * @return @c true if the slave device was added correcly, @c false otherwise.
     * @note Must be called from a task context only.
     */
    bool addDevice(spi_device_interface_config_t *slaveConfig);

    // ------------------------------------------------------------------ //
    // Control API                                                        //
    // ------------------------------------------------------------------ //

    /**
     * @brief Send a message to the slave device and optionally read a response.
     *
     * Performs a blocking (polling) SPI transaction. Logs an error and
     * returns @c false if the bus has not been initialized, if @p txLen
     * is zero, if @p txMsg is @c nullptr, or if @p rxMsg is @c nullptr while
     * @p rxLen is greater than zero.
     *
     * @param txMsg Buffer containing the message to send.
     * @param txLen Length of @p txMsg in bytes.
     * @param rxMsg Buffer to store the received message (may be @c nullptr if @p rxLen is 0).
     * @param rxLen Length of @p rxMsg in bytes.
     * @return @c true if the message was sent (and received) correctly, @c false otherwise.
     * @note Must be called from a task context only.
     */
    bool sendMessage(uint8_t *txMsg, uint8_t txLen, uint8_t *rxMsg, uint8_t rxLen) override;

    /**
     * @brief Send a message to the slave device without reading a response.
     *
     * Equivalent to calling @c sendMessage(txMsg, txLen, nullptr, 0).
     *
     * @param txMsg Buffer containing the message to send.
     * @param txLen Length of @p txMsg in bytes.
     * @return @c true if the message was sent correctly, @c false otherwise.
     * @note Must be called from a task context only.
     */
    bool sendMessage(uint8_t *txMsg, uint8_t txLen) override;

private:
    // ------------------------------------------------------------------ //
    // Debug helpers                                                      //
    // ------------------------------------------------------------------ //

    /**
     * @brief Log the contents of a transmitted message as hex bytes.
     *
     * No-op if @p txLen is zero or @p txMsg is nullptr. Emitted at
     * @c ESP_LOGD level.
     *
     * @param txMsg Message that was sent.
     * @param txLen Length of @p txMsg in bytes.
     */
    void debugTxMessage(uint8_t *txMsg, uint8_t txLen);

    /**
     * @brief Log the contents of a received message as hex bytes.
     *
     * No-op if @p rxLen is zero or @p rxMsg is nullptr. Emitted at
     * @c ESP_LOGD level.
     *
     * @param rxMsg Message that was received.
     * @param rxLen Length of @p rxMsg in bytes.
     */
    void debugRxMessage(uint8_t *rxMsg, uint8_t rxLen);

    // ------------------------------------------------------------------ //
    // Constants                                                          //
    // ------------------------------------------------------------------ //

    static constexpr uint32_t MAX_TRANSFER = 1024; ///< Maximum transfer size supported by the bus (bytes).
    static constexpr uint32_t INTR_BUS_FLAG = 0;   ///< Interrupt allocation flags used when initializing the bus.

    // ------------------------------------------------------------------ //
    // Instance state                                                     //
    // ------------------------------------------------------------------ //

    bool initialized_ = false;       ///< True once @c begin() has completed successfully.
    spi_host_device_t host_id_;      ///< Host device this instance was initialized with, reused by @c addDevice().
    spi_device_handle_t spi_handle_; ///< Handle tot he attached slave device, set by @c addDevice().

    SemaphoreHandle_t mutex_ = nullptr; ///< Mutex protecting all instance state, created by @c begin().

    friend class SPITest;
    static constexpr const char *TAG = "SPI Controller"; ///< ESP-IDF log tag.
};

/** @} */ // end of SPI group

#endif // SPI_CONTROLLER_H