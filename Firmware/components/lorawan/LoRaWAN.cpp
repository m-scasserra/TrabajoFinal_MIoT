#include "LoRaWAN.h"

#include <cstring>

#include <esp_log.h>

extern "C"
{
#include "LoRaMac.h"
#include "Region.h"
#include "radio.h"
#include "sx126x.h"
    void SX126xIoInit(void);
    void SX126xReset(void);
    void SX126xIoRfSwitchInit(void);
    void RadioOnDioIrq(void *context);
    void SX126xIoIrqInit(DioIrqHandler dioirq);
}

extern "C" void lorawan_task_start(void);
extern "C" void notify_radio_irq(void);
namespace
{
    constexpr LoRaMacRegion_t kActiveRegion = LORAMAC_REGION_AU915;
    constexpr int8_t kDefaultDatarate = DR_2;
}

LoRaWAN *LoRaWAN::s_activeInstance = nullptr;

static void onMacProcessNotify(void)
{
    notify_radio_irq();
}

LoRaWAN::LoRaWAN() {}

LoRaWAN::~LoRaWAN()
{
    if (s_activeInstance == this)
    {
        LoRaMacStop();
        s_activeInstance = nullptr;
    }

    if (mutex_ != nullptr)
    {
        vSemaphoreDelete(mutex_);
    }
}

void LoRaWAN::setCredentials(const credentials_t &creds)
{
    credentials_ = creds;
}

esp_err_t LoRaWAN::begin()
{
    if (initialized_)
    {
        return ESP_OK;
    }

    if (s_activeInstance != nullptr)
    {
        ESP_LOGE(TAG, "begin: another instance of LoRaWAN is already active.");
        return ESP_ERR_INVALID_STATE;
    }

    mutex_ = xSemaphoreCreateMutex();
    if (mutex_ == nullptr)
    {
        ESP_LOGE(TAG, "begin: failed to create mutex.");
        return ESP_ERR_NO_MEM;
    }

    SX126xIoInit();
    SX126xReset();
    SX126xIoIrqInit(RadioOnDioIrq);

    macCallbacks_.MacProcessNotify = onMacProcessNotify;

    macPrimitives_.MacMcpsConfirm = reinterpret_cast<void (*)(McpsConfirm_t *)>(onMcpsConfirm);
    macPrimitives_.MacMcpsIndication = reinterpret_cast<void (*)(McpsIndication_t *)>(onMcpsIndication);
    macPrimitives_.MacMlmeConfirm = reinterpret_cast<void (*)(MlmeConfirm_t *)>(onMlmeConfirm);
    macPrimitives_.MacMlmeIndication = reinterpret_cast<void (*)(MlmeIndication_t *)>(onMlmeIndication);
    ESP_LOGI(TAG, "primitives: mcpsC=%p mcpsI=%p mlmeC=%p mlmeI=%p",
             macPrimitives_.MacMcpsConfirm, macPrimitives_.MacMcpsIndication,
             macPrimitives_.MacMlmeConfirm, macPrimitives_.MacMlmeIndication);
    LoRaMacStatus_t initSt = LoRaMacInitialization(&macPrimitives_, &macCallbacks_, kActiveRegion);
    ESP_LOGI(TAG, "LoRaMacInitialization -> %d", initSt);
    if (initSt != LORAMAC_STATUS_OK)
    {
        ESP_LOGE(TAG, "begin: LoRaMacInitialization failed.");
        vSemaphoreDelete(mutex_);
        mutex_ = nullptr;
        return ESP_FAIL;
    }

    s_activeInstance = this;

    esp_err_t err = loadCredentials();
    if (err != ESP_OK)
    {
        ESP_LOGE(TAG, "begin: loadCredentials failed (%d).", err);
        s_activeInstance = nullptr;
        vSemaphoreDelete(mutex_);
        mutex_ = nullptr;
        return err;
    }

    MibRequestConfirm_t mib = {};
    mib.Type = MIB_ADR;
    mib.Param.AdrEnable = true;
    LoRaMacStatus_t st = LoRaMacMibSetRequestConfirm(&mib);
    ESP_LOGI(TAG, "MIB_CHANNELS_MASK -> %d", st);
    applyAu915sb2();

    LoRaMacStatus_t startSt = LoRaMacStart();
    ESP_LOGI(TAG, "begin: LoRaMacStart returned %d.", startSt);
    lorawan_task_start();

    initialized_ = true;
    return ESP_OK;
}

esp_err_t LoRaWAN::join()
{
    xSemaphoreTake(mutex_, portMAX_DELAY);
    joinState_ = joinState::joining;
    joinCallback cb = joinCb_;
    void *ctx = joinCtx_;
    xSemaphoreGive(mutex_);

    MlmeReq_t req = {};
    req.Type = MLME_JOIN;
    req.Req.Join.Datarate = kDefaultDatarate;
    req.Req.Join.NetworkActivation = ACTIVATION_TYPE_OTAA;

    ESP_LOGI(TAG, "pre-join: IsBusy=%d", LoRaMacIsBusy());
    LoRaMacStatus_t st = LoRaMacMlmeRequest(&req);
    ESP_LOGI(TAG, "post-join: st=%d IsBusy=%d", st, LoRaMacIsBusy());
    if (st != LORAMAC_STATUS_OK)
    {
        ESP_LOGW(TAG, "join: MLME_JOIN request rejected (%d).", st);
        xSemaphoreTake(mutex_, portMAX_DELAY);
        joinState_ = joinState::failed;
        xSemaphoreGive(mutex_);
        if (cb)
        {
            cb(joinState::failed, ctx);
        }
        return ESP_FAIL;
    }

    if (cb)
    {
        cb(joinState::joining, ctx);
    }
    return ESP_OK;
}

ILoRaWAN::joinState LoRaWAN::currentJoinState() const
{
    xSemaphoreTake(mutex_, portMAX_DELAY);
    joinState s = joinState_;
    xSemaphoreGive(mutex_);
    return s;
}

bool LoRaWAN::isJoined() const
{
    return currentJoinState() == joinState::joined;
}

ILoRaWAN::txStatus LoRaWAN::send(uint8_t port, const uint8_t *data, uint8_t length)
{
    if (!isJoined())
    {
        return txStatus::notJoined;
    }

    LoRaMacTxInfo_t txInfo = {};
    McpsReq_t req = {};

    if (LoRaMacQueryTxPossible(length, &txInfo) != LORAMAC_STATUS_OK)
    {
        req.Type = MCPS_UNCONFIRMED;
        req.Req.Unconfirmed.fBuffer = nullptr;
        req.Req.Unconfirmed.fBufferSize = 0;
        req.Req.Unconfirmed.Datarate = kDefaultDatarate;
        LoRaMacMcpsRequest(&req);
        return txStatus::payloadTooBig;
    }

    req.Type = MCPS_UNCONFIRMED;
    req.Req.Unconfirmed.fPort = port;
    req.Req.Unconfirmed.fBuffer = const_cast<uint8_t *>(data);
    req.Req.Unconfirmed.fBufferSize = length;
    req.Req.Unconfirmed.Datarate = kDefaultDatarate;

    switch (LoRaMacMcpsRequest(&req))
    {
    case LORAMAC_STATUS_OK:
        return txStatus::ok;
    case LORAMAC_STATUS_BUSY:
        return txStatus::busy;
    case LORAMAC_STATUS_LENGTH_ERROR:
        return txStatus::payloadTooBig;
    default:
        return txStatus::error;
    }
}

void LoRaWAN::setDownlinkCallback(downlinkCallback cb, void *context)
{
    if (mutex_ != nullptr)
    {
        xSemaphoreTake(mutex_, portMAX_DELAY);
    }
    downlinkCb_ = cb;
    downlinkCtx_ = context;
    if (mutex_ != nullptr)
    {
        xSemaphoreGive(mutex_);
    }
}

void LoRaWAN::setJoinCallback(joinCallback cb, void *context)
{
    if (mutex_ != nullptr)
    {
        xSemaphoreTake(mutex_, portMAX_DELAY);
    }
    joinCb_ = cb;
    joinCtx_ = context;
    if (mutex_ != nullptr)
    {
        xSemaphoreGive(mutex_);
    }
}

esp_err_t LoRaWAN::loadCredentials()
{
    MibRequestConfirm_t mib = {};

    mib.Type = MIB_DEV_EUI;
    mib.Param.DevEui = credentials_.devEui;
    LoRaMacStatus_t st = LoRaMacMibSetRequestConfirm(&mib);
    ESP_LOGI(TAG, "MIB_DEV_EUI -> %d", st);
    if (st != LORAMAC_STATUS_OK)
    {
        return ESP_FAIL;
    }

    mib.Type = MIB_JOIN_EUI;
    mib.Param.JoinEui = credentials_.joinEui;
    st = LoRaMacMibSetRequestConfirm(&mib);
    ESP_LOGI(TAG, "MIB_JOIN_EUI -> %d", st);
    if (st != LORAMAC_STATUS_OK)
    {
        return ESP_FAIL;
    }

    mib.Type = MIB_APP_KEY;
    mib.Param.AppKey = credentials_.appKey;
    st = LoRaMacMibSetRequestConfirm(&mib);
    ESP_LOGI(TAG, "MIB_APP_KEY -> %d", st);
    if (st != LORAMAC_STATUS_OK)
    {
        return ESP_FAIL;
    }

    mib.Type = MIB_NWK_KEY;
    mib.Param.NwkKey = credentials_.appKey;
    st = LoRaMacMibSetRequestConfirm(&mib);
    ESP_LOGI(TAG, "MIB_NWK_KEY -> %d", st);
    if (st != LORAMAC_STATUS_OK)
    {
        return ESP_FAIL;
    }

    return ESP_OK;
}

void LoRaWAN::applyAu915sb2()
{
    static uint16_t mask[6] = {0x00FF, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000};
    mask[0] = 0xFF00;
    mask[4] = 0x0002;

    MibRequestConfirm_t mib = {};
    mib.Type = MIB_CHANNELS_MASK;
    mib.Param.ChannelsMask = mask;
    LoRaMacMibSetRequestConfirm(&mib);

    mib.Type = MIB_CHANNELS_DEFAULT_MASK;
    mib.Param.ChannelsDefaultMask = mask;
    LoRaMacMibSetRequestConfirm(&mib);
}

void LoRaWAN::onMcpsConfirm(void *confirm)
{
    McpsConfirm_t *c = static_cast<McpsConfirm_t *>(confirm);
    if (c->Status == LORAMAC_EVENT_INFO_STATUS_OK)
    {
        ESP_LOGI(TAG, "onMcpsConfirm: Uplink confirmed (DR%d).", c->Datarate);
    }
    else
    {
        ESP_LOGW(TAG, "onMcpsConfirm: Uplink status=%d).", c->Status);
    }
}

void LoRaWAN::onMcpsIndication(void *indication)
{
    McpsIndication_t *ind = static_cast<McpsIndication_t *>(indication);
    if (ind->Status != LORAMAC_EVENT_INFO_STATUS_OK)
    {
        return;
    }

    LoRaWAN *self = s_activeInstance;
    if (self == nullptr)
    {
        return;
    }

    if (ind->RxData)
    {
        ESP_LOGI(TAG, "onMcpsIndication: downlink port %d, %d bytes, RSSI %d SNR %d.",
                 ind->Port, ind->BufferSize, ind->Rssi, ind->Snr);

        xSemaphoreTake(self->mutex_, portMAX_DELAY);
        downlinkCallback cb = self->downlinkCb_;
        void *ctx = self->downlinkCtx_;
        xSemaphoreGive(self->mutex_);

        if (cb)
        {
            cb(ind->Port, ind->Buffer, ind->BufferSize, ctx);
        }
    }
}

void LoRaWAN::onMlmeConfirm(void *confirm)
{
    MlmeConfirm_t *c = static_cast<MlmeConfirm_t *>(confirm);
    if (c->MlmeRequest != MLME_JOIN)
    {
        return;
    }

    LoRaWAN *self = s_activeInstance;
    if (self == nullptr)
    {
        return;
    }

    joinState newState;
    if (c->Status == LORAMAC_EVENT_INFO_STATUS_OK)
    {
        newState = joinState::joined;
        ESP_LOGI(TAG, "onMlmeConfirm: OTAA join suceeded.");
    }
    else
    {
        newState = joinState::failed;
        ESP_LOGW(TAG, "onMlmeConfirm: OTAA join failed status=%d.", c->Status);
    }

    xSemaphoreTake(self->mutex_, portMAX_DELAY);
    self->joinState_ = newState;
    joinCallback cb = self->joinCb_;
    void *ctx = self->joinCtx_;
    xSemaphoreGive(self->mutex_);

    if (cb)
    {
        cb(newState, ctx);
    }
}

void LoRaWAN::onMlmeIndication(void *indication)
{
    (void)indication;
}