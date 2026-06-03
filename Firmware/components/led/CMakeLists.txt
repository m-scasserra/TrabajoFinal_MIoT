cmake_minimum_required(VERSION 3.16)

idf_component_register(
    SRCS "Led.cpp"
    INCLUDE_DIRS "include"
    REQUIRES esp_driver_rmt esp_driver_gpio
)