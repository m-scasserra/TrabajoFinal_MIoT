cmake_minimum_required(VERSION 3.16)

idf_component_register(
    SRCS 
        "SPI_Controller.cpp"
    INCLUDE_DIRS 
        "include"
    REQUIRES 
        driver
        esp_driver_spi
)