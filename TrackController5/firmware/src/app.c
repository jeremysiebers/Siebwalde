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

void ModbusCommCycleCallBack(uintptr_t context, uint32_t alarmCount);
void ModbusCharacterTimeoutCallBack(uintptr_t context, uint32_t alarmCount);
void ModbusReceiveTimeoutCallBack(uintptr_t context, uint32_t alarmCount);

void ModbusCommCycleCallBack(uintptr_t context, uint32_t alarmCount){         // ModbusCommCycle(){
    //Led1Toggle();
    UpdateNextSlave = true;
}

void ModbusCharacterTimeoutCallBack(uintptr_t context, uint32_t alarmCount){  // ModbusCharacterTimeout(){
    PetitModbusTimerValue = 3;                                                  // Between receive interrupts it took to long --> message done
    DRV_TMR_Stop(appData.ModbusCharacterTimeoutHandle);
}

void ModbusReceiveTimeoutCallBack(uintptr_t context, uint32_t alarmCount){    // ModbusReceiveTimeout(){
    SlaveAnswerTimeoutCounter   = 1;                                            // Data received answer timeout timer
    //DRV_TMR_CounterClear(appData.ModbusReceiveTimeoutHandle);
}

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
    //DRV_TMR0_Start();       // used for ethernet
    //DRV_TMR1_Stop();        // used for cyclic modbus communication
    
    
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

    SYS_MESSAGE("\033[2J\033[1;1H");//clear terminal
    SYS_MESSAGE("Starting Siebwalde TrackController...\n\r");
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
                
                uint32_t actualFrequency;
                uint32_t divider;
                
                DRV_USART1_Initialize();
                
                appData.ModbusCommCycleHandle = DRV_TMR_Open(DRV_TMR_INDEX_1 , DRV_IO_INTENT_EXCLUSIVE);
                appData.ModbusCharacterTimeoutHandle = DRV_TMR_Open(DRV_TMR_INDEX_2 , DRV_IO_INTENT_EXCLUSIVE);
                appData.ModbusReceiveTimeoutHandle = DRV_TMR_Open(DRV_TMR_INDEX_3 , DRV_IO_INTENT_EXCLUSIVE);
                
                if ( DRV_HANDLE_INVALID == appData.ModbusCommCycleHandle )
                {
                    // Unable to open the driver
                    SYS_MESSAGE("Failed to open appData.ModbusCommCycleHandle.\n\r");
                }
                if ( DRV_HANDLE_INVALID == appData.ModbusCharacterTimeoutHandle )
                {
                    // Unable to open the driver
                    SYS_MESSAGE("Failed to open appData.ModbusCharacterTimeoutHandle.\n\r");
                }
                if ( DRV_HANDLE_INVALID == appData.ModbusReceiveTimeoutHandle )
                {
                    // Unable to open the driver
                    SYS_MESSAGE("Failed to open appData.ModbusReceiveTimeoutHandle.\n\r");
                }
                
                
                actualFrequency = DRV_TMR_CounterFrequencyGet(appData.ModbusCommCycleHandle);
                divider = actualFrequency/(uint32_t)832; // cacluate divider value
                //DRV_TMR_AlarmRegister(appData.ModbusCommCycleHandle, 394,true, (uintptr_t)&appData, appData.ModbusCommCycleCallBack);
                if (DRV_TMR_AlarmRegister(appData.ModbusCommCycleHandle, 394,true, 0, ModbusCommCycleCallBack) == true){
                    _nop();
                    //DRV_TMR_AlarmEnable(appData.ModbusCommCycleHandle, true);
                    SYS_MESSAGE("Register appData.ModbusCommCycleCallBack success.\n\r");
                }
                
                actualFrequency = DRV_TMR_CounterFrequencyGet(appData.ModbusCharacterTimeoutHandle);
                divider = actualFrequency/(uint32_t)41015; // cacluate divider value
                if (DRV_TMR_AlarmRegister(appData.ModbusCharacterTimeoutHandle, 8,true, 0, ModbusCharacterTimeoutCallBack) == true){
                    _nop();
                    //DRV_TMR_AlarmEnable(appData.ModbusCharacterTimeoutHandle, true);
                    SYS_MESSAGE("Register appData.ModbusCharacterTimeoutCallBack success.\n\r");
                }
                
                actualFrequency = DRV_TMR_CounterFrequencyGet(appData.ModbusReceiveTimeoutHandle);
                divider = actualFrequency/(uint32_t)4002; // cacluate divider value
                if (DRV_TMR_AlarmRegister(appData.ModbusReceiveTimeoutHandle, 82,true, 0, ModbusReceiveTimeoutCallBack) == true){
                    _nop();
                    //DRV_TMR_AlarmEnable(appData.ModbusReceiveTimeoutHandle, true);
                    SYS_MESSAGE("Register appData.ModbusReceiveTimeoutCallBack success.\n\r");
                }
                
                //DRV_TMR_Stop(appData.ModbusCommCycleHandle);
                //DRV_TMR_Stop(appData.ModbusCharacterTimeoutHandle);
                //DRV_TMR_Stop(appData.ModbusReceiveTimeoutHandle);
                
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
                DRV_TMR_Start(appData.ModbusCommCycleHandle);//DRV_TMR1_Start();
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
