/*
 * File:   mcp23017.c
 * Author: Jeremy Siebers
 *
 * Created on January 5, 2025, 15:32
 */
#include <xc.h>
#include "mcc_generated_files/mcc.h"
#include "mcc_generated_files/examples/i2c2_master_example.h"
#include "mcp23017.h"
/**
 * Using mcc generated I2C files and example files 
 */

// Initialize MCP23017 Devices
void MCP23017_Init(MCP23017_t *devices, uint8_t num_devices) {
    for (uint8_t i = 0; i < num_devices; i++) {
        if(i > MAX_MCP23017_DEVICES){
            break;
        }
        // disable auto addressing
        //I2C2_Write1ByteRegister(devices[i]->address, MCP23017_IOCON,  0b00100000);
        // set IO direction of Port A
        //I2C2_Write1ByteRegister(devices[i].byteView.address, MCP23017_IODIRA, devices[i].byteView.IODIRA);
        // set IO direction of Port B
        //I2C2_Write1ByteRegister(devices[i].byteView.address, MCP23017_IODIRB, devices[i].byteView.IODIRB);
    }
}

// Set the Direction of a Specific Port
//void MCP23017_SetPinDirection(MCP23017_t *device, uint8_t pin, uint8_t direction) {
//    uint8_t reg = (pin < 8) ? MCP23017_IODIRA : MCP23017_IODIRB;
//    uint8_t mask = (1 << (pin % 8));
//
//    // Read current IODIR value
//    I2C_Start();
//    I2C_Write((device->address << 1) | 0); // Write to the MCP23017
//    I2C_Write(reg);                        // Select IODIR register
//    I2C_Start();
//    I2C_Write((device->address << 1) | 1); // Read from MCP23017
//    uint8_t iodir = I2C_Read(0);          // Read IODIR value
//    I2C_Stop();
//
//    // Modify the specific pin direction
//    if (direction == 0) {
//        iodir &= ~mask; // Set as output
//    } else {
//        iodir |= mask;  // Set as input
//    }
//
//    // Write back the updated IODIR value
//    I2C_Start();
//    I2C_Write((device->address << 1) | 0); // Write to the MCP23017
//    I2C_Write(reg);                        // Select IODIR register
//    I2C_Write(iodir);                      // Write updated IODIR value
//    I2C_Stop();
//}

// Set the Direction of a Specific Pin
//void MCP23017_SetPinDirection(MCP23017_t *device, uint8_t pin, uint8_t direction) {
//    uint8_t reg = (pin < 8) ? MCP23017_IODIRA : MCP23017_IODIRB;
//    uint8_t mask = (1 << (pin % 8));
//
//    // Read current IODIR value
//    I2C_Start();
//    I2C_Write((device->address << 1) | 0); // Write to the MCP23017
//    I2C_Write(reg);                        // Select IODIR register
//    I2C_Start();
//    I2C_Write((device->address << 1) | 1); // Read from MCP23017
//    uint8_t iodir = I2C_Read(0);          // Read IODIR value
//    I2C_Stop();
//
//    // Modify the specific pin direction
//    if (direction == 0) {
//        iodir &= ~mask; // Set as output
//    } else {
//        iodir |= mask;  // Set as input
//    }
//
//    // Write back the updated IODIR value
//    I2C_Start();
//    I2C_Write((device->address << 1) | 0); // Write to the MCP23017
//    I2C_Write(reg);                        // Select IODIR register
//    I2C_Write(iodir);                      // Write updated IODIR value
//    I2C_Stop();
//}

// Write to a Specific Pin
//void MCP23017_WritePin(MCP23017_t *device, uint8_t pin, uint8_t value) {
//    uint8_t reg = (pin < 8) ? MCP23017_OLATA : MCP23017_OLATB;
//    uint8_t mask = (1 << (pin % 8));
//
//    // Read current OLAT value
//    I2C_Start();
//    I2C_Write((device->address << 1) | 0); // Write to the MCP23017
//    I2C_Write(reg);                        // Select OLAT register
//    I2C_Start();
//    I2C_Write((device->address << 1) | 1); // Read from MCP23017
//    uint8_t olat = I2C_Read(0);           // Read OLAT value
//    I2C_Stop();
//
//    // Modify the specific pin value
//    if (value == 0) {
//        olat &= ~mask; // Clear the bit
//    } else {
//        olat |= mask;  // Set the bit
//    }
//
//    // Write back the updated OLAT value
//    I2C_Start();
//    I2C_Write((device->address << 1) | 0); // Write to the MCP23017
//    I2C_Write(reg);                        // Select OLAT register
//    I2C_Write(olat);                       // Write updated OLAT value
//    I2C_Stop();
//}

// Read the Value of a Specific Pin
//uint8_t MCP23017_ReadPin(MCP23017_t *device, uint8_t pin) {
//    uint8_t reg = (pin < 8) ? MCP23017_GPIOA : MCP23017_GPIOB;
//    uint8_t mask = (1 << (pin % 8));
//
//    // Read current GPIO value
//    I2C_Start();
//    I2C_Write((device->address << 1) | 0); // Write to the MCP23017
//    I2C_Write(reg);                        // Select GPIO register
//    I2C_Start();
//    I2C_Write((device->address << 1) | 1); // Read from MCP23017
//    uint8_t gpio = I2C_Read(0);           // Read GPIO value
//    I2C_Stop();
//
//    // Return the value of the specific pin
//    return (gpio & mask) ? 1 : 0;
//}

// Write to an Entire Port
void MCP23017_WritePort(MCP23017_t *device, uint8_t port, uint8_t value) {
    uint8_t reg = (port == 0xA) ? MCP23017_OLATA : MCP23017_OLATB;

    uint8_t data[3] = {reg, value};
    I2C2_WriteNBytes(device->byteView.address, data, 2);
}

// Read an Entire Port
//uint8_t MCP23017_ReadPort(MCP23017_t *device, char port) {
//    uint8_t reg = (port == 'A') ? MCP23017_GPIOA : MCP23017_GPIOB;
//
//    I2C_Start();
//    I2C_Write((device->address << 1) | 0); // Write to the MCP23017
//    I2C_Write(reg);                        // Select GPIO register
//    I2C_Start();
//    I2C_Write((device->address << 1) | 1); // Read from MCP23017
//    uint8_t value = I2C_Read(0);          // Read GPIO value
//    I2C_Stop();
//
//    return value;
//}

