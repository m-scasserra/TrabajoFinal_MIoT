#include <stdint.h>
#include "esp_timer.h"
#include "timer.h"
#include "esp_log.h"

TimerTime_t TimerGetCurrentTime()
{
    return (TimerTime_t)(esp_timer_get_time() / 1000);
}

TimerTime_t TimerGetElapsedTime(TimerTime_t past)
{
    TimerTime_t now = TimerGetCurrentTime();
    return now - past;
}

static void timer_cb(void *arg)
{
    TimerEvent_t *obj = (TimerEvent_t *)arg;
    if (obj && obj->Callback)
    {
        obj->Callback(obj->Context);
    }
}

void TimerInit(TimerEvent_t *obj, void (*callback)(void *context))
{
    obj->IsStarted = false;
    obj->Callback = callback;
    obj->Context = NULL;
    obj->ReloadValue = 0;

    esp_timer_create_args_t timer_args = {
        .callback = timer_cb,
        .arg = obj,
        .dispatch_method = ESP_TIMER_TASK,
        .name = "loramac_timer",
        .skip_unhandled_events = true,
    };
    esp_timer_handle_t h = NULL;
    esp_timer_create(&timer_args, &h);
    esp_timer_create(&timer_args, &h);
    obj->Handle = h;
}

void TimerSetContext(TimerEvent_t *obj, void *context)
{
    obj->Context = context;
}

void TimerSetValue(TimerEvent_t *obj, uint32_t value_ms)
{
    obj->ReloadValue = value_ms;
}

void TimerStart(TimerEvent_t *obj)
{
    if (!obj->Handle)
    {
        return;
    }
    esp_timer_stop((esp_timer_handle_t)obj->Handle);
    uint64_t us = (uint64_t)obj->ReloadValue * 1000;
    if (us == 0)
    {
        us = 1;
    }
    esp_timer_start_once(obj->Handle, us);
    obj->IsStarted = true;
}

void TimerStop(TimerEvent_t *obj)
{
    if (!obj->Handle)
    {
        return;
    }
    esp_timer_stop((esp_timer_handle_t)obj->Handle);
    obj->IsStarted = false;
}

void TimerReset(TimerEvent_t *obj)
{
    TimerStop(obj);
    TimerStart(obj);
}

TimerTime_t TimerTempCompensation(TimerTime_t period, float temperature)
{
    (void)temperature;
    return period;
}