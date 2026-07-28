#ifndef ILORAWAN_H
#define ILORAWAN_H

#include <cstdint>

#include "esp_err.h"

class ILoRaWAN
{
public:
    enum class joinState
    {
        idle,
        joining,
        joined,
        failed
    };

    enum class txStatus
    {
        ok,
        notJoined,
        busy,
        payloadTooBig,
        error
    };

    using downlinkCallback = void (*)(uint8_t port, const uint8_t *data,
                                      uint8_t length, void *context);

    using joinCallback = void (*)(joinState state, void *context);

    virtual ~ILoRaWAN() = default;

    virtual esp_err_t begin() = 0;
    virtual esp_err_t join() = 0;
    virtual joinState currentJoinState() const = 0;
    virtual bool isJoined() const = 0;

    virtual txStatus send(uint8_t port, const uint8_t *data, uint8_t length) = 0;
    virtual void setDownlinkCallback(downlinkCallback cb, void *context) = 0;
    virtual void setJoinCallback(joinCallback cb, void *context) = 0;
};

#endif