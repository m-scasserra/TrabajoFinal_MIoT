#ifndef ILED_H
#define ILED_H

#include <cstdint>

#include "esp_err.h"

class ILed
{
public:
    /**
     * @brief Pre-defined colors supported by @c setColor(colors).
     */
    enum class colors
    {
        black,    ///< LED off.
        red,      ///< Red at current brightness.
        blue,     ///< Blue at current brightness.
        green,    ///< Green at current brightness.
        purple,   ///< Red and blue at current brightness.
        cyan,     ///< Green and blue at current brightness.
        yellow,   ///< Red and green at current brightness.
        white,    ///< White (all channels) at current brightness.
        undefined ///< Color set via raw RGB; no named color active.
    };
    virtual ~ILed() = default;

    virtual void setColor(uint8_t R, uint8_t G, uint8_t B) = 0;
    virtual void setColor(colors color) = 0;
    virtual void setBrightness(uint8_t b) = 0;
    virtual colors currentColor() const = 0;
};

#endif // ILED_H