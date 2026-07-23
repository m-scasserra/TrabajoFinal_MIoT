#include "SPI_Controller.h"

#include <cstring>

#include "esp_log.h"

// ------------------------------------------------------------------ //
// Lifecycle                                                          //
// ------------------------------------------------------------------ //

SPIController::~SPIController()
{
    if (mutex_ != nullptr)
    {
        vSemaphoreDelete(mutex_);
    }
}

bool SPIController::begin(spi_bus_config_t *busConfig, spi_host_device_t hostId, spi_dma_chan_t dmaChan)
{
    if (initialized_)
    {
        ESP_LOGI(TAG, "begin: the SPI has already been initialized.");
        return true;
    }

    if (mutex_ == nullptr)
    {
        mutex_ = xSemaphoreCreateMutex();
        if (mutex_ == nullptr)
        {
            ESP_LOGE(TAG, "begin: failed to create mutex.");
            return false;
        }
    }

    xSemaphoreTake(mutex_, portMAX_DELAY);

    // Initialize the SPI bus
    ESP_LOGI(TAG, "begin: starting the SPI bus.");
    esp_err_t err = spi_bus_initialize(hostId, busConfig, dmaChan);
    if (err == ESP_ERR_INVALID_STATE)
    {
        ESP_LOGE(TAG, "begin: the SPI bus has already been initialized.");
        initialized_ = true;
        xSemaphoreGive(mutex_);
        return false;
    }
    else if (err != ESP_OK)
    {
        ESP_LOGE(TAG, "begin: error initializing the SPI bus.");
        xSemaphoreGive(mutex_);
        return false;
    }

    ESP_LOGI(TAG, "begin: SPI bus initialized correctly.");
    host_id_ = hostId;
    initialized_ = true;
    xSemaphoreGive(mutex_);
    return true;
}

bool SPIController::addDevice(spi_device_interface_config_t *slaveConfig)
{
    if (mutex_ == nullptr)
    {
        // begin() was never called: nothing to protect yet.
        ESP_LOGE(TAG, "addDevice: the SPI has not been initialized.");
        return false;
    }

    xSemaphoreTake(mutex_, portMAX_DELAY);

    if (!initialized_)
    {
        ESP_LOGE(TAG, "addDevice: the SPI has not been initialized.");
        xSemaphoreGive(mutex_);
        return false;
    }

    // Attach the device to the SPI bus it was initialized with
    ESP_LOGI(TAG, "addDevice: adding the SPI device");

    if (spi_bus_add_device(host_id_, slaveConfig, &spi_handle_) != ESP_OK)
    {
        ESP_LOGE(TAG, "addDevice: error adding the slave device to the SPI bus");
        xSemaphoreGive(mutex_);
        return false;
    }

    ESP_LOGI(TAG, "addDevice: the slave device was added correctly.");

    xSemaphoreGive(mutex_);
    return true;
}

// ------------------------------------------------------------------ //
// Control API                                                        //
// ------------------------------------------------------------------ //

bool SPIController::sendMessage(uint8_t *txMsg, uint8_t txLen, uint8_t *rxMsg, uint8_t rxLen)
{
    if (mutex_ == nullptr)
    {
        // begin() was never called: nothing to protect yet.
        ESP_LOGE(TAG, "sendMessage: the SPI has not been initialized.");
        return false;
    }

    xSemaphoreTake(mutex_, portMAX_DELAY);

    if (!initialized_)
    {
        ESP_LOGE(TAG, "sendMessage: the SPI has not been initialized.");
        xSemaphoreGive(mutex_);
        return false;
    }

    if (txLen == 0)
    {
        ESP_LOGE(TAG, "sendMessage: the message size must be greater than 0.");
        xSemaphoreGive(mutex_);
        return false;
    }

    if (rxMsg == NULL && rxLen > 0)
    {
        ESP_LOGE(TAG, "sendMessage: the receive buffer cannot be null if the size is greater than 0.");
        xSemaphoreGive(mutex_);
        return false;
    }

    if (txMsg == NULL)
    {
        ESP_LOGE(TAG, "sendMessage: the transmission buffer cannot be null.");
        xSemaphoreGive(mutex_);
        return false;
    }

    spi_transaction_t message;
    memset(&message, 0, sizeof(spi_transaction_t));

    message.length = 8 * txLen;
    message.rxlength = 8 * rxLen;
    message.user = NULL;
    message.tx_buffer = txMsg;
    message.rx_buffer = rxMsg;

    if (spi_device_polling_transmit(spi_handle_, &message) != ESP_OK)
    {
        ESP_LOGE(TAG, "sendMessage: error sending the message.");
        xSemaphoreGive(mutex_);
        return false;
    }
    debugTxMessage(txMsg, txLen);
    debugRxMessage(rxMsg, rxLen);
    xSemaphoreGive(mutex_);
    return true;
}

bool SPIController::sendMessage(uint8_t *txMsg, uint8_t txLen)
{
    return sendMessage(txMsg, txLen, NULL, 0);
}

// ------------------------------------------------------------------ //
// Debug helpers                                                      //
// ------------------------------------------------------------------ //

void SPIController::debugTxMessage(uint8_t *txMsg, uint8_t txLen)
{
    if (txLen == 0 || txMsg == NULL)
    {
        return;
    }

    // Create a buffer to store the formatted string
    char formatted_string[txLen * 5 + 1]; // Allocate enough space for hex, spaces and null terminator
    memset(formatted_string, 0, sizeof(formatted_string));
    int index = 0;

    for (int i = 0; i < txLen; i++)
    {
        // Format each byte as a two-digit hex string and store it in the buffer
        sprintf(formatted_string + index, "0x%02X ", txMsg[i]);
        index += 5; // Increment index by 5 for the formatted byte + spaces
    }

    // Print the formatted string with a trailing newline
    ESP_LOGD(TAG, "TX message: %s\n", formatted_string);
}

void SPIController::debugRxMessage(uint8_t *rxMsg, uint8_t rxLen)
{
    if (rxLen == 0 || rxMsg == NULL)
    {
        return;
    }

    // Create a buffer to store the formatted string
    char formatted_string[rxLen * 5 + 1]; // Allocate enough space for hex, spaces and null terminator
    memset(formatted_string, 0, sizeof(formatted_string));
    int index = 0;

    for (int i = 0; i < rxLen; i++)
    {
        // Format each byte as a two-digit hex string and store it in the buffer
        sprintf(formatted_string + index, "0x%02X ", rxMsg[i]);
        index += 5; // Increment index by 5 for the formatted byte + spaces
    }

    // Print the formatted string with a trailing newline
    ESP_LOGD(TAG, "RX message: %s\n", formatted_string);
}