#ifndef LORAWAN_H
#define LORAWAN_H

#include <cstdint>

#include "freertos/FreeRTOS.h"
#include "freertos/semphr.h"

#include "ILoRaWAN.h"
#include "LoRaMac.h"

class LoRaWAN : public ILoRaWAN
{
public:
    typedef struct
    {
        uint8_t devEui[8];
        uint8_t joinEui[8];
        uint8_t appKey[16];
    } credentials_t;

    LoRaWAN();
    ~LoRaWAN() override;

    void setCredentials(const credentials_t &creds);
    esp_err_t begin() override;
    esp_err_t join() override;
    joinState currentJoinState() const override;
    bool isJoined() const override;
    txStatus send(uint8_t port, const uint8_t *data, uint8_t length) override;
    void setDownlinkCallback(downlinkCallback cb, void *context) override;
    void setJoinCallback(joinCallback cb, void *context) override;

private:
    void applyAu915sb2();
    esp_err_t loadCredentials();
    static void onMcpsConfirm(void *confirm);
    static void onMcpsIndication(void *indication);
    static void onMlmeConfirm(void *confirm);
    static void onMlmeIndication(void *indication);

    static LoRaWAN *s_activeInstance;

    credentials_t credentials_{};

    joinState joinState_ = joinState::idle;

    downlinkCallback downlinkCb_ = nullptr;
    void *downlinkCtx_ = nullptr;
    joinCallback joinCb_ = nullptr;
    void *joinCtx_ = nullptr;

    SemaphoreHandle_t mutex_ = nullptr;
    bool initialized_ = false;
    LoRaMacPrimitives_t macPrimitives_{};
    LoRaMacCallback_t macCallbacks_{};

    friend class LoRaWANTest;
    static constexpr const char *TAG = "LoRaWAN"; ///< ESP-IDF log tag.
};

#endif // LORAWAN_H