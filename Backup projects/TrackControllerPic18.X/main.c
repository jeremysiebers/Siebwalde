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
#include "main.h"
#include "milisecond_counter.h"
#include "debounce.h"
#include "station.h"
#include "pathway.h"

void DebounceIO(void);

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
    INTERRUPT_GlobalInterruptEnable();

    // Disable the Global Interrupts
    //INTERRUPT_GlobalInterruptDisable();

    // Enable the Peripheral Interrupts
    INTERRUPT_PeripheralInterruptEnable();

    // Disable the Peripheral Interrupts
    //INTERRUPT_PeripheralInterruptDisable();
    
    MILLIESxINIT();
    SETxMILLISECONDxUPDATExHANDLER(DebounceIO);
    SETxMILLISECONDxUPDATExHANDLER2(UPDATExSIGNAL);
    INITxSTATION();
    
    while (1)
    {
        /* Manage TCP/IP Stack */
        //Network_Manage();
        
        /* Check if over ride key is present, if yes then disable controller. */
        if(CTRL_OFF_GetValue())
        {
            LATB = 0;
            LATC = 0;
            LATD = 0;
            LATE = 0;
            LATJ = 0;  
            BLK_SIG_3B_GR_SetHigh();
            BLK_SIG_12B_GR_SetHigh();
        }
        else
        {
            UPDATExSTATION(&top);
            UPDATExSTATION(&bot);
        }  
                
    }
}

void DebounceIO(void)
{
    TP2_SetHigh();
    DEBOUNCExIO(&HALL_BLK_13  );        
    DEBOUNCExIO(&HALL_BLK_21A );
    DEBOUNCExIO(&HALL_BLK_T4  );
    DEBOUNCExIO(&HALL_BLK_T5  );
    DEBOUNCExIO(&HALL_BLK_T1  );
    DEBOUNCExIO(&HALL_BLK_T2  );
    DEBOUNCExIO(&HALL_BLK_9B  );
    DEBOUNCExIO(&HALL_BLK_4A  );
    DEBOUNCExIO(&HALL_BLK_T7  );
    DEBOUNCExIO(&HALL_BLK_T8  );
    DEBOUNCExIO(&OCC_FR_BLK13 );
    DEBOUNCExIO(&OCC_FR_BLK4  );
    DEBOUNCExIO(&OCC_FR_STN_1 );
    DEBOUNCExIO(&OCC_FR_STN_2 );
    DEBOUNCExIO(&OCC_FR_STN_3 );
    DEBOUNCExIO(&OCC_FR_STN_10);
    DEBOUNCExIO(&OCC_FR_STN_11);
    DEBOUNCExIO(&OCC_FR_STN_12);
    DEBOUNCExIO(&OCC_FR_STN_T6);
    DEBOUNCExIO(&OCC_FR_STN_T3);
    TP2_SetLow();
}
/**
 End of File
*/