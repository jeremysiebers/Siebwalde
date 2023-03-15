/**
  Generated Main Source File

  Company:
    Microchip Technology Inc.

  File Name:
    main.c

  Summary:
    This is the main file generated using PIC10 / PIC12 / PIC16 / PIC18 MCUs

  Description:
    This header file provides implementations for driver APIs for all modules selected in the GUI.
    Generation Information :
        Product Revision  :  PIC10 / PIC12 / PIC16 / PIC18 MCUs - 1.81.8
        Device            :  PIC18F97J60
        Driver Version    :  2.00
*/

#include "mcc_generated_files/mcc.h"
#include "debounce.h"
#include "bus.h"
#include "firedep.h"

/*
                         Main application
 */

/* https://www.microchip.com/en-us/software-library/tcpipstack */

/* UDP Packet Initializations*/
//udpPacket.destinationAddress = MAKE_IPV4_ADDRESS(192,168,1,19);
//udpPacket.sourcePortNumber = 65533;
//udpPacket.destinationPortNumber = 65531;

/* 
 * PORTG = INPUT  CARD 1 LOW BYTE
 * PORTH = INPUT  CARD 1 HIGH BYTE
 * PORTD = OUTPUT CARD 2 LOW BYTE
 * PORTC = OUTPUT CARD 2 HIGH BYTE
 */

void main(void)
{
    // Initialize the device
    SYSTEM_Initialize();

    // If using interrupts in PIC18 High/Low Priority Mode you need to enable the Global High and Low Interrupts
    // If using interrupts in PIC Mid-Range Compatibility Mode you need to enable the Global and Peripheral Interrupts
    // Use the following macros to:

    // Enable the Global Interrupts
    //INTERRUPT_GlobalInterruptEnable();

    // Disable the Global Interrupts
    //INTERRUPT_GlobalInterruptDisable();

    // Enable the Peripheral Interrupts
    //INTERRUPT_PeripheralInterruptEnable();

    // Disable the Peripheral Interrupts
    //INTERRUPT_PeripheralInterruptDisable();

    while (1)
    {
        //Network_Manage();         
        //DEBOUNCExIO();
        
        if(TMR0_HasOverflowOccured() == 1)
        {
            TMR0_Reload();
            BUSxDRIVE();
            FIREDEPPxDRIVE();
        }
        

    }
}
/**
 End of File
*/