#include <stdio.h>
#include <string.h>
#include "unity.h"

void force_linker_led();

extern "C"
{
    void app_main(void)
    {
        force_linker_led();
        unity_run_menu();
    }
}
