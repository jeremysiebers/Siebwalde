/*******************************************************************************
  MPLAB Harmony Application Source File
  
  Company:
    Microchip Technology Inc.
  
  File Name:
    app.c

  Summary:
    This file contains the source code for the MPLAB Harmony application.

  Description:
    This file contains the source code for the MPLAB Harmony application.  It 
    implements the logic of the application's state machine and it may call 
    API routines of other MPLAB Harmony modules in the system, such as drivers,
    system services, and middleware.  However, it does not call any of the
    system interfaces (such as the "Initialize" and "Tasks" functions) of any of
    the modules in the system or make any assumptions about when those functions
    are called.  That is the responsibility of the configuration-specific system
    files.
 *******************************************************************************/

// DOM-IGNORE-BEGIN
/*******************************************************************************
Copyright (c) 2013-2014 released Microchip Technology Inc.  All rights reserved.

Microchip licenses to you the right to use, modify, copy and distribute
Software only when embedded on a Microchip microcontroller or digital signal
controller that is integrated into your product or third party product
(pursuant to the sublicense terms in the accompanying license agreement).

You should refer to the license agreement accompanying this Software for
additional information regarding your rights and obligations.

SOFTWARE AND DOCUMENTATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND,
EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION, ANY WARRANTY OF
MERCHANTABILITY, TITLE, NON-INFRINGEMENT AND FITNESS FOR A PARTICULAR PURPOSE.
IN NO EVENT SHALL MICROCHIP OR ITS LICENSORS BE LIABLE OR OBLIGATED UNDER
CONTRACT, NEGLIGENCE, STRICT LIABILITY, CONTRIBUTION, BREACH OF WARRANTY, OR
OTHER LEGAL EQUITABLE THEORY ANY DIRECT OR INDIRECT DAMAGES OR EXPENSES
INCLUDING BUT NOT LIMITED TO ANY INCIDENTAL, SPECIAL, INDIRECT, PUNITIVE OR
CONSEQUENTIAL DAMAGES, LOST PROFITS OR LOST DATA, COST OF PROCUREMENT OF
SUBSTITUTE GOODS, TECHNOLOGY, SERVICES, OR ANY CLAIMS BY THIRD PARTIES
(INCLUDING BUT NOT LIMITED TO ANY DEFENSE THEREOF), OR OTHER SIMILAR COSTS.
 *******************************************************************************/
// DOM-IGNORE-END


// *****************************************************************************
// *****************************************************************************
// Section: Included Files 
// *****************************************************************************
// *****************************************************************************

#include "app.h"
#include "modbus/PetitModbus.h"
#include "modbus/PetitModbusPort.h"
#include "commhandler.h"
#include "slavestartup.h"
#include "slavehandler.h"
#include "slavefwhandler.h"

// *****************************************************************************
// *****************************************************************************
// Section: Global Data Definitions
// *****************************************************************************
// *****************************************************************************

// *****************************************************************************
/* Application Data

  Summary:
    Holds application data

  Description:
    This structure holds the application's data.

  Remarks:
    This structure should be initialized by the APP_Initialize function.
    
    Application strings and buffers are be defined outside this structure.
*/

APP_DATA appData;

/* 
 * Array of structs holding the data of all the slaves connected  
 */
static SLAVE_INFO         SlaveInfo[NUMBER_OF_SLAVES_SIZE];
static SLAVE_INFO         SlaveDump[1];

static uint16_t LED_TX_prev, LED_RX_prev, LED_ERR_prev, LED_WAR_prev = 0;
static uint16_t LED_TX_STATE, LED_RX_STATE, LED_ERR_STATE, LED_WAR_STATE = 0;
uint16_t LED_ERR = 0;
static uint16_t LED_WAR = 0;
uint16_t UpdateNextSlave = false;
uint16_t AllSlavesReadAllDataCounter = 1;
uint16_t InitDone = false;
uint16_t stop = false;
uint32_t DelayCount = 0;

// *****************************************************************************
// *****************************************************************************
// Section: Application Callback Functions
// *****************************************************************************
// *****************************************************************************

/* TODO:  Add any necessary callback functions.
*/

// *****************************************************************************
// *****************************************************************************
// Section: Application Local Functions
// *****************************************************************************
// *****************************************************************************


/* TODO:  Add any necessary local functions.
*/
uint32_t ReadCoreTimer(void);


// *****************************************************************************
// *****************************************************************************
// Section: Application Initialization and State Machine Functions
// *****************************************************************************
// *****************************************************************************

/*******************************************************************************
  Function:
    void APP_Initialize ( void )

  Remarks:
    See prototype in app.h.
 */

void APP_Initialize ( void )
{
    /* Place the App state machine in its initial state. */
    appData.state = APP_STATE_INIT;
    DRV_TMR0_Start();       // used for ethernet
    DRV_TMR1_Stop();        // used for cyclic modbus communication
    
    
    /* Pass address of array of struct for data storage. */
    INITxPETITXMODBUS(SlaveInfo, SlaveDump, NUMBER_OF_SLAVES_SIZE);
    /* Pass address of array of struct for data storage. */
    INITxCOMMxHANDLER(SlaveInfo, SlaveDump);
    INITxSLAVExSTARTUP(SlaveInfo, SlaveDump);
    INITxSLAVExHANDLER(SlaveInfo, SlaveDump);
    
    /* Initialize the SLAVE_INFO struct with slave numbers. */
    uint8_t i = 0;
    for (i = 0; i <NUMBER_OF_SLAVES_SIZE; i++){
        SlaveInfo[i].SlaveNumber = i;
        SlaveInfo[i].Header = 0xAA;
        SlaveInfo[i].Footer = 0x55;
    }
    
    Slaves_Disable_Off();
    DelayCount = ReadCoreTimer();
    /*
    DRV_USART0_WriteByte('A');
    DRV_USART0_WriteByte('P');
    DRV_USART0_WriteByte('P');
    DRV_USART0_WriteByte('_');
    DRV_USART0_WriteByte('I');
    DRV_USART0_WriteByte('N');
    DRV_USART0_WriteByte('I');
    DRV_USART0_WriteByte('T');
    DRV_USART0_WriteByte('!');
    DRV_USART0_WriteByte(0xa);
    DRV_USART0_WriteByte(0xd);
     */
}


/******************************************************************************
  Function:
    void APP_Tasks ( void )

  Remarks:
    See prototype in app.h.
 */

void APP_Tasks ( void )
{
    /* Check the application's current state. */
    switch ( appData.state )
    {
        /* Application's initial state. */
        case APP_STATE_INIT:
        {
            if ((ReadCoreTimer() - DelayCount) > 150000000 ){
                DRV_USART1_Initialize();
                appData.state = APP_STATE_SLAVE_DETECT;
            }
            break;
        }
        
        case APP_STATE_SLAVE_DETECT:
        {
            if (SLAVExDETECT()){
                appData.state = APP_STATE_SLAVE_FW_DOWNLOAD;
            }
            break;
        }
        
        case APP_STATE_SLAVE_FW_DOWNLOAD:
        {
            if (SLAVExFWxHANDLER()){
                appData.state = APP_STATE_SLAVE_INIT;
            }
            break;
        }
        
        case APP_STATE_SLAVE_INIT:
        {              
            if (SLAVExINITxANDxCONFIG())
            {                
                appData.state = APP_STATE_SLAVE_ENABLE;
            }
            break;
        }
        
        case APP_STATE_SLAVE_ENABLE:
        {            
            if (ENABLExAMPLIFIER()){
                DRV_TMR1_Start();
                appData.state = APP_STATE_SERVICE_TASKS;
            }            
            break;
        }

        case APP_STATE_SERVICE_TASKS:
        {
            if(UpdateNextSlave == true){
                if (PROCESSxNEXTxSLAVE()){
                    UpdateNextSlave = false;
                }                
            }
            PROCESSxSLAVExCOMMUNICATION();
            break;
        }

        

        /* The default state should never be executed. */
        default:
        {
            /* TODO: Handle error in application's state machine. */
            break;
        }
    }
    
    PROCESSxPETITxMODBUS();
    
}

void ModbusCommCycle(){
    //Led1Toggle();
    UpdateNextSlave = true;
}

void ModbusCharacterTimeout(){
    PetitModbusTimerValue = 3;                                                  // Between receive interrupts it took to long --> message done
}

void ModbusReceiveTimeout(){
    SlaveAnswerTimeoutCounter   = 1;                                            // Data received answer timeout timer
    DRV_TMR3_CounterClear();
}

uint32_t ReadCoreTimer(void)
{
    uint32_t count;
    asm volatile("mfc0 %0, $9" : "=r"(count));
    return(count);
}

/*
void Led_Blink (){
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
        
        switch(LED_ERR_STATE){
            case 0 : 
                if (LED_ERR > 0){
                    LED_ERR_LAT = 1;
                    LED_ERR_prev = LED_ERR;
                    LED_ERR_STATE = 1;                        
                }
                break;

            case 1 :
                if (LED_ERR == LED_ERR_prev || LED_ERR != LED_ERR_prev){
                    LED_ERR_LAT = 0;
                    LED_ERR_prev = 0;
                    LED_ERR = 0;
                    LED_ERR_STATE = 0;                        
                }
                break;

            default :
                LED_ERR_STATE = 0;
                break;                       
        }
        
        switch(LED_WAR_STATE){
            case 0 : 
                if (LED_WAR > 0){
                    LED_WAR_LAT = 1;
                    LED_WAR_prev = LED_WAR;
                    LED_WAR_STATE = 1;                        
                }
                break;

            case 1 :
                if (LED_WAR == LED_WAR_prev || LED_WAR != LED_WAR_prev){
                    LED_WAR_LAT = 0;
                    LED_WAR_prev = 0;
                    LED_WAR = 0;
                    LED_WAR_STATE = 0;                        
                }
                break;

            default :
                LED_WAR_STATE = 0;
                break;                       
        }
        
        PIR0bits.TMR0IF = 0;
        TMR0_Reload();
    }
}*/
/*******************************************************************************
 End of File
 */
