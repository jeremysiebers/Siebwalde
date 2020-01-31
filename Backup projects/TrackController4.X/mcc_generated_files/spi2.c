/**
  SPI2 Generated Driver File

  @Company
    Microchip Technology Inc.

  @File Name
    spi2.c

  @Summary
    This is the generated driver implementation file for the SPI2 driver using PIC10 / PIC12 / PIC16 / PIC18 MCUs

  @Description
    This source file provides APIs for SPI2.
    Generation Information :
        Product Revision  :  PIC10 / PIC12 / PIC16 / PIC18 MCUs - 1.65.2
        Device            :  PIC18F97J60
        Driver Version    :  1.01
    The generated drivers are tested against the following:
        Compiler          :  XC8 1.45
        MPLAB 	          :  MPLAB X 4.15	
*/

/*
    (c) 2018 Microchip Technology Inc. and its subsidiaries. 
    
    Subject to your compliance with these terms, you may use Microchip software and any 
    derivatives exclusively with Microchip products. It is your responsibility to comply with third party 
    license terms applicable to your use of third party software (including open source software) that 
    may accompany Microchip software.
    
    THIS SOFTWARE IS SUPPLIED BY MICROCHIP "AS IS". NO WARRANTIES, WHETHER 
    EXPRESS, IMPLIED OR STATUTORY, APPLY TO THIS SOFTWARE, INCLUDING ANY 
    IMPLIED WARRANTIES OF NON-INFRINGEMENT, MERCHANTABILITY, AND FITNESS 
    FOR A PARTICULAR PURPOSE.
    
    IN NO EVENT WILL MICROCHIP BE LIABLE FOR ANY INDIRECT, SPECIAL, PUNITIVE, 
    INCIDENTAL OR CONSEQUENTIAL LOSS, DAMAGE, COST OR EXPENSE OF ANY KIND 
    WHATSOEVER RELATED TO THE SOFTWARE, HOWEVER CAUSED, EVEN IF MICROCHIP 
    HAS BEEN ADVISED OF THE POSSIBILITY OR THE DAMAGES ARE FORESEEABLE. TO 
    THE FULLEST EXTENT ALLOWED BY LAW, MICROCHIP'S TOTAL LIABILITY ON ALL 
    CLAIMS IN ANY WAY RELATED TO THIS SOFTWARE WILL NOT EXCEED THE AMOUNT 
    OF FEES, IF ANY, THAT YOU HAVE PAID DIRECTLY TO MICROCHIP FOR THIS 
    SOFTWARE.
*/

/**
  Section: Included Files
*/

#include <xc.h>
#include "spi2.h"


uint8_t (*SPI2_xchgHandler)(uint8_t);
/**
  Section: Module APIs
*/

void SPI2_Initialize(void)
{
    // Set the SPI2 module to the options selected in the User Interface

    // R_nW write_noTX; P stopbit_notdetected; S startbit_notdetected; BF RCinprocess_TXcomplete; SMP High Speed; UA dontupdate; CKE Idle to Active; D_nA lastbyte_address; 
    SSP2STAT = 0x00;

    // SSPEN enabled; WCOL no_collision; CKP Idle:Low, Active:High; SSPM SCKx_nSSxenabled; SSPOV no_overflow; 
    SSP2CON1 = 0x24;
	
	SSP2BUF = DUMMY_DATA;
	SPI2_setExchangeHandler(SPI2_DefaultExchangeHandler);
	
	PIE3bits.SSP2IE = 1;
}


void SPI2_ISR(void)
{
        SSP2BUF = SPI2_xchgHandler(SSP2BUF);
}


void SPI2_setExchangeHandler(uint8_t (* InterruptHandler)(uint8_t)){
    SPI2_xchgHandler = InterruptHandler;
}



uint8_t SPI2_DefaultExchangeHandler(uint8_t byte){
    // add your SPI2 interrupt custom code
    // or set custom function using SPI2_setExchangeHandler()
    return byte;
}

/**
 End of File
*/