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

unsigned int LED_TX_prev, LED_RX_prev = 0;
unsigned int LED_TX_STATE, LED_RX_STATE = 0;
unsigned int UpdateNextSlave = false;

void main(void) {
    // Initialize the device
    SYSTEM_Initialize();
    TMR1_StopTimer();                                                           // prevent timer1 from setting slave timeout to 1.
    __delay_ms(200);                                                            // Wait longer then the slaves (1000ms)
    
    LED_RUN_LAT = 0;
    LED_WAR_LAT = 0;
    LED_ERR_LAT = 1;    
    LED_TX_LAT = 0;
    LED_RX_LAT = 0;
    
    TMR0_StartTimer();
    
    InitPetitModbus(SlaveInfo);                                                 // Pass address of array of struct for data storage
    InitSlaveCommunication(SlaveInfo);                                          // Pass address of array of struct for data storage
    TX_ENA_LAT = 1;                                                             // Enable TX, master TX is always enabled.
    //SPI1_setExchangeHandler();
    
    INTCONbits.GIE = 1;
    INTCONbits.PEIE = 1;
    
    LED_RUN_LAT = 1;
    LED_WAR_LAT = 0;
    LED_ERR_LAT = 0;
    
    modbus_sync_LAT = 0;
    
    while(1)
    {
        ProcessPetitModbus();
        ProcessSlaveCommunication();
        
        if (UpdateNextSlave == true){
            ProcessNextSlave();
            UpdateNextSlave = false;
        }
        /*if(PIR4bits.TMR2IF)
        {
            ProcessNextSlave();  
            
            PIR4bits.TMR2IF = 0;
            TMR2 = 0;
        }*/
        
        if(PIR0bits.TMR0IF){
            
            switch(LED_TX_STATE){
                case 0 : 
                    if (LED_TX > 0){
                        LED_TX_LAT = 1;
                        LED_TX_prev = LED_TX;
                        LED_TX_STATE = 1;
                    }
                    break;
                    
                case 1 :
                    if (LED_TX == LED_TX_prev || LED_TX != LED_TX_prev){
                        LED_TX_LAT = 0;
                        LED_TX_prev = 0;
                        LED_TX = 0;
                        LED_TX_STATE = 0;
                    }
                    break;
                    
                default :
                    LED_TX_STATE = 0;
                    break;                       
            }
            
            switch(LED_RX_STATE){
                case 0 : 
                    if (LED_RX > 0){
                        LED_RX_LAT = 1;
                        LED_RX_prev = LED_RX;
                        LED_RX_STATE = 1;
                    }
                    break;
                    
                case 1 :
                    if (LED_RX == LED_RX_prev || LED_RX != LED_RX_prev){
                        LED_RX_LAT = 0;
                        LED_RX_prev = 0;
                        LED_RX = 0;
                        LED_RX_STATE = 0;
                    }
                    break;
                    
                default :
                    LED_RX_STATE = 0;
                    break;                       
            }
            PIR0bits.TMR0IF = 0;
            TMR0_Reload();
        }
    }
}