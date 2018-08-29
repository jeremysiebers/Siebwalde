/*
 * File:   newmain.c
 * Author: Jeremy Siebers
 *
 * Created on April 16, 2018, 10:06 PM
 */
#include <xc.h>
#include "main.h"
#include "mcc_generated_files/mcc.h"
#include "modbus/Interrupts.h"
#include "modbus/PetitModbus.h"
#include "commhandler.h"


/*----------------------------------------------------------------------------*/

static SLAVE_INFO         SlaveInfo[NUMBER_OF_SLAVES];                          // Array of structs holding the data of all the slaves connected  

/*----------------------------------------------------------------------------*/

void main(void) {
    // Initialize the device
    SYSTEM_Initialize();
    TMR1_StopTimer();                                                           // prevent timer1 from setting slave timeout to 1.
    __delay_ms(2000);                                                           // Wait longer then the slaves (1000ms)
    
    LED_RUN_LAT = 0;
    LED_WAR_LAT = 0;
    LED_ERR_LAT = 1;    
    LED_TX_LAT = 0;
    LED_RX_LAT = 0;
    
    InitPetitModbus(SlaveInfo);                                                 // Pass address of array of struct for data storage
    InitSlaveCommunication(SlaveInfo);                                          // Pass address of array of struct for data storage
    TX_ENA_LAT = 1;                                                             // Enable TX, master TX is always enabled.
    //SPI1_setExchangeHandler();
    
    INTCONbits.GIE = 1;
    INTCONbits.PEIE = 1;
    
    LED_RUN_LAT = 1;
    LED_WAR_LAT = 0;
    LED_ERR_LAT = 0;
    
    modbus_sync_LAT = 1;
    
    while(1)
    {
        ProcessPetitModbus();
        ProcessSlaveCommunication();
            
        if(PIR4bits.TMR2IF)
        {
            ProcessNextSlave();  
            
            PIR4bits.TMR2IF = 0;
            TMR2 = 0;
        }
    }
}