//******************************************************************************
//        Software License Agreement
//
// ©2016 Microchip Technology Inc. and its subsidiaries. You may use this
// software and any derivatives exclusively with Microchip products.
//
// THIS SOFTWARE IS SUPPLIED BY MICROCHIP "AS IS". NO WARRANTIES, WHETHER
// EXPRESS, IMPLIED OR STATUTORY, APPLY TO THIS SOFTWARE, INCLUDING ANY IMPLIED
// WARRANTIES OF NON-INFRINGEMENT, MERCHANTABILITY, AND FITNESS FOR A PARTICULAR
// PURPOSE, OR ITS INTERACTION WITH MICROCHIP PRODUCTS, COMBINATION WITH ANY
// OTHER PRODUCTS, OR USE IN ANY APPLICATION.
//
// IN NO EVENT WILL MICROCHIP BE LIABLE FOR ANY INDIRECT, SPECIAL, PUNITIVE,
// INCIDENTAL OR CONSEQUENTIAL LOSS, DAMAGE, COST OR EXPENSE OF ANY KIND
// WHATSOEVER RELATED TO THE SOFTWARE, HOWEVER CAUSED, EVEN IF MICROCHIP HAS
// BEEN ADVISED OF THE POSSIBILITY OR THE DAMAGES ARE FORESEEABLE. TO THE
// FULLEST EXTENT ALLOWED BY LAW, MICROCHIP'S TOTAL LIABILITY ON ALL CLAIMS IN
// ANY WAY RELATED TO THIS SOFTWARE WILL NOT EXCEED THE AMOUNT OF FEES, IF ANY,
// THAT YOU HAVE PAID DIRECTLY TO MICROCHIP FOR THIS SOFTWARE.
//
// MICROCHIP PROVIDES THIS SOFTWARE CONDITIONALLY UPON YOUR ACCEPTANCE OF THESE TERMS.
//******************************************************************************

#ifndef BOOTLOADER_H
#define	BOOTLOADER_H
  
#include "memory.h"

void    BOOTLOADER_Initialize(void);
void    Run_Bootloader(void);
bool    Bootload_Required (void);
uint8_t ProcessBootBuffer (void);
void 	Check_Device_Reset (void);


#define  BOOTLOADER_INDICATOR        LATAbits.LATA5
#define  BL_INDICATOR_ON         1
#define  BL_INDICATOR_OFF        0

#define  IO_PIN_ENTRY_PORT_PIN   PORTAbits.RA0
#define  IO_PIN_ENTRY_RUN_APP    1
#define  IO_PIN_ENTRY_RUN_BL     0

// *****************************************************************************
// Frame Format
//
//  [<COMMAND><DATALEN><ADDRL><ADDRH><ADDRU><...DATA...>]
// These values are negative because the FSR is set to PACKET_DATA to minimize FSR reloads.
// *****************************************************************************
typedef union
{
    struct
    {
        uint8_t     command;
        uint16_t    data_length;
        uint8_t     EE_key_1;
        uint8_t     EE_key_2;
        uint8_t     address_L;
        uint8_t     address_H;
        uint8_t     address_U;
        uint8_t     address_unused;
        uint8_t     data[WRITE_FLASH_BLOCKSIZE];
    };
    uint8_t  buffer[WRITE_FLASH_BLOCKSIZE + 9];
}frame_t;

#endif	/* BOOTLOADER_H */

