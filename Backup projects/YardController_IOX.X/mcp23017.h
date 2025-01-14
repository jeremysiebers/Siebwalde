/* Microchip Technology Inc. and its subsidiaries.  You may use this software 
 * and any derivatives exclusively with Microchip products. 
 * 
 * THIS SOFTWARE IS SUPPLIED BY MICROCHIP "AS IS".  NO WARRANTIES, WHETHER 
 * EXPRESS, IMPLIED OR STATUTORY, APPLY TO THIS SOFTWARE, INCLUDING ANY IMPLIED 
 * WARRANTIES OF NON-INFRINGEMENT, MERCHANTABILITY, AND FITNESS FOR A 
 * PARTICULAR PURPOSE, OR ITS INTERACTION WITH MICROCHIP PRODUCTS, COMBINATION 
 * WITH ANY OTHER PRODUCTS, OR USE IN ANY APPLICATION. 
 *
 * IN NO EVENT WILL MICROCHIP BE LIABLE FOR ANY INDIRECT, SPECIAL, PUNITIVE, 
 * INCIDENTAL OR CONSEQUENTIAL LOSS, DAMAGE, COST OR EXPENSE OF ANY KIND 
 * WHATSOEVER RELATED TO THE SOFTWARE, HOWEVER CAUSED, EVEN IF MICROCHIP HAS 
 * BEEN ADVISED OF THE POSSIBILITY OR THE DAMAGES ARE FORESEEABLE.  TO THE 
 * FULLEST EXTENT ALLOWED BY LAW, MICROCHIP'S TOTAL LIABILITY ON ALL CLAIMS 
 * IN ANY WAY RELATED TO THIS SOFTWARE WILL NOT EXCEED THE AMOUNT OF FEES, IF 
 * ANY, THAT YOU HAVE PAID DIRECTLY TO MICROCHIP FOR THIS SOFTWARE.
 *
 * MICROCHIP PROVIDES THIS SOFTWARE CONDITIONALLY UPON YOUR ACCEPTANCE OF THESE 
 * TERMS. 
 */

/* 
 * File:   
 * Author: 
 * Comments:
 * Revision history: 
 */

// This is a guard condition so that contents of this file are not included
// more than once.  
#ifndef MCP23017_H
#define	MCP23017_H

#include <xc.h> // include processor files - each processor file is guarded. 

// MCP23017 register addresses 
#define MCP23017_IODIRA 0x00
#define MCP23017_IODIRB 0x01
#define MCP23017_IOCON  0x0A  // IOCON shared between PORT A and B!
#define MCP23017_GPIOA  0x12
#define MCP23017_GPIOB  0x13
#define MCP23017_OLATA  0x14
#define MCP23017_OLATB  0x15
// Maximum number of MCP23017 devices (0 - 7))
#define MAX_MCP23017_DEVICES 7


// MCP23017 Structure
typedef struct {
    uint8_t IODIRA;
    uint8_t IODIRB;
    uint8_t IOXRA;
    uint8_t IOXRB;
    uint8_t address; // I2C address of the MCP23017
    
} ByteFieldStruct;

typedef struct {
    uint8_t   IODIRA0 :1;
    uint8_t   IODIRA1 :1;
    uint8_t   IODIRA2 :1;
    uint8_t   IODIRA3 :1;
    uint8_t   IODIRA4 :1;
    uint8_t   IODIRA5 :1;
    uint8_t   IODIRA6 :1;
    uint8_t   IODIRA7 :1;

    uint8_t   IODIRB0 :1;
    uint8_t   IODIRB1 :1;
    uint8_t   IODIRB2 :1;
    uint8_t   IODIRB3 :1;
    uint8_t   IODIRB4 :1;
    uint8_t   IODIRB5 :1;
    uint8_t   IODIRB6 :1;
    uint8_t   IODIRB7 :1;
    
    uint8_t   IOXRA0 :1;
    uint8_t   IOXRA1 :1;
    uint8_t   IOXRA2 :1;
    uint8_t   IOXRA3 :1;
    uint8_t   IOXRA4 :1;
    uint8_t   IOXRA5 :1;
    uint8_t   IOXRA6 :1;
    uint8_t   IOXRA7 :1;
    
    uint8_t   IOXRB0 :1;
    uint8_t   IOXRB1 :1;
    uint8_t   IOXRB2 :1;
    uint8_t   IOXRB3 :1;
    uint8_t   IOXRB4 :1;
    uint8_t   IOXRB5 :1;
    uint8_t   IOXRB6 :1;
    uint8_t   IOXRB7 :1;
    
    uint8_t   ADDR0 :1;
    uint8_t   ADDR1 :1;
    uint8_t   ADDR2 :1;
    uint8_t   ADDR3 :1;
    uint8_t   ADDR4 :1;
    uint8_t   ADDR5 :1;
    uint8_t   ADDR6 :1;
    uint8_t   ADDR7 :1;
} BitFieldStruct;

typedef union {
    ByteFieldStruct byteView;   // Access the whole byte
    BitFieldStruct  bits;       // Access individual bits
} MCP23017_t;

// MCP23017 Functions
    void MCP23017xInit(MCP23017_t *devices, uint8_t num_devices);
    //void MCP23017_SetPinDirection(MCP23017_t *device, uint8_t pin, uint8_t direction);
    //void MCP23017_WritePin(MCP23017_t *device, uint8_t pin, uint8_t value);
    //uint8_t MCP23017_ReadPin(MCP23017_t *device, uint8_t pin);
    void MCP23017xWritePort(MCP23017_t *device, uint8_t port, uint8_t value);
    //uint8_t MCP23017_ReadPort(MCP23017_t *device, char port);

#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

    // TODO If C++ is being used, regular C code needs function names to have C 
    // linkage so the functions can be used by the c code. 

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

