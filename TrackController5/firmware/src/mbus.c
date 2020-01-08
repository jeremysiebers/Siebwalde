/*******************************************************************************
  MPLAB Harmony Application Source File
  
  Company:
    Microchip Technology Inc.
  
  File Name:
    mbus.c

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

#include "mbus.h"
#include "modbus/PetitModbus.h"
#include "modbus/PetitModbusPort.h"
#include "slavecommhandler.h"
#include "slavestartup.h"
#include "slavehandler.h"
#include "slavefwhandler.h"
#include "ethernet.h"
#include "enums.h"

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
    This structure should be initialized by the MBUS_Initialize function.
    
    Application strings and buffers are be defined outside this structure.
*/

/* 
 * Array of structs holding the data of all the slaves connected  
 */
static SLAVE_INFO         SlaveInfo[NUMBER_OF_SLAVES_SIZE];
static SLAVE_INFO         SlaveDump[1];

            uint32_t    DelayCount = 0;
static      uint16_t    LED_TX_prev, LED_RX_prev, LED_ERR_prev, LED_WAR_prev = 0;
static      uint16_t    LED_TX_STATE, LED_RX_STATE, LED_ERR_STATE, LED_WAR_STATE = 0;
            uint16_t    LED_ERR = 0;
static      uint16_t    LED_WAR = 0;
volatile    uint16_t    UpdateNextSlave = false;
volatile    uint16_t    UploadSlaveData = false;
static      uint16_t    NextSlaveCounter = 1;
static      uint16_t    MaxSlaveUploadCount = 55;
static      uint32_t    SizeOfSlaveInfo = sizeof(SlaveInfo[0]);

            udpTrans_t  EthernetSendData;


//uint16_t AllSlavesReadAllDataCounter = 1;
//uint16_t InitDone = false;
//uint16_t stop = false;

// *****************************************************************************
// *****************************************************************************
// Section: Application Callback Functions
// *****************************************************************************
// *****************************************************************************

void ModbusCommCycleCallBack(uintptr_t context, uint32_t alarmCount);
void ModbusCharacterTimeoutCallBack(uintptr_t context, uint32_t alarmCount);
void ModbusReceiveTimeoutCallBack(uintptr_t context, uint32_t alarmCount);

void ModbusCommCycleCallBack(uintptr_t context, uint32_t alarmCount){           // ModbusCommCycle(){
    //Led1Toggle();
    UpdateNextSlave = true;
    UploadSlaveData = true;
}

void ModbusCharacterTimeoutCallBack(uintptr_t context, uint32_t alarmCount){    // ModbusCharacterTimeout(){
    PetitModbusTimerValue = 3;                                                  // Between receive interrupts it took to long --> message done
    DRV_TMR_Stop(mbusData.ModbusCharacterTimeoutHandle);
}

void ModbusReceiveTimeoutCallBack(uintptr_t context, uint32_t alarmCount){      // ModbusReceiveTimeout(){
    SlaveAnswerTimeoutCounter   = 1;                                            // Data received answer timeout timer
    //DRV_TMR_CounterClear(mbusData.ModbusReceiveTimeoutHandle);
}

// *****************************************************************************
// *****************************************************************************
// Section: Application Local Functions
// *****************************************************************************
// *****************************************************************************



// *****************************************************************************
// *****************************************************************************
// Section: Application Initialization and State Machine Functions
// *****************************************************************************
// *****************************************************************************

/*******************************************************************************
  Function:
    void MBUS_Initialize ( void )

  Remarks:
    See prototype in mbus.h.
 */

void MBUS_Initialize ( void )
{
    /* Place the App state machine in its initial state. */
    mbusData.state = MBUS_STATE_INIT;
    
    /* Pass address of array of struct for data storage. */
    INITxPETITXMODBUS(SlaveInfo, SlaveDump, NUMBER_OF_SLAVES_SIZE);
    /* Pass address of array of struct for data storage. */
    INITxCOMMxHANDLER(SlaveInfo, SlaveDump);
    INITxSLAVExSTARTUP(SlaveInfo, SlaveDump);
    INITxSLAVExHANDLER(SlaveInfo, SlaveDump);
    INITxSLAVExFWxHANDLER(SlaveInfo, SlaveDump);
    
    /* Initialize the SLAVE_INFO struct with slave numbers. */
    uint8_t i = 0;
    for (i = 0; i <NUMBER_OF_SLAVES_SIZE; i++){
        SlaveInfo[i].SlaveNumber = i;
        SlaveInfo[i].Header = 0xAA;
        SlaveInfo[i].Footer = 0x55;
    }
    
    Slaves_Disable_On();
}


/******************************************************************************
  Function:
    void MBUS_Tasks ( void )

  Remarks:
    See prototype in mbus.h.
 */

void MBUS_Tasks ( void )
{

    /* Check the application's current state. */
    switch ( mbusData.state )
    {
        /* Application's initial state. */
        case MBUS_STATE_INIT:
        {
            uint32_t actualFrequency;
            uint32_t divider;

            DRV_USART1_Initialize();

            mbusData.ModbusCommCycleHandle = DRV_TMR_Open(DRV_TMR_INDEX_1 , DRV_IO_INTENT_EXCLUSIVE);
            mbusData.ModbusCharacterTimeoutHandle = DRV_TMR_Open(DRV_TMR_INDEX_2 , DRV_IO_INTENT_EXCLUSIVE);
            mbusData.ModbusReceiveTimeoutHandle = DRV_TMR_Open(DRV_TMR_INDEX_3 , DRV_IO_INTENT_EXCLUSIVE);

            if ( DRV_HANDLE_INVALID == mbusData.ModbusCommCycleHandle )
            {
                // Unable to open the driver
                SYS_MESSAGE("Mbus handler\t: Failed to open mbusData.ModbusCommCycleHandle.\n\r");
            }
            if ( DRV_HANDLE_INVALID == mbusData.ModbusCharacterTimeoutHandle )
            {
                // Unable to open the driver
                SYS_MESSAGE("Mbus handler\t: Failed to open mbusData.ModbusCharacterTimeoutHandle.\n\r");
            }
            if ( DRV_HANDLE_INVALID == mbusData.ModbusReceiveTimeoutHandle )
            {
                // Unable to open the driver
                SYS_MESSAGE("Mbus handler\t: Failed to open mbusData.ModbusReceiveTimeoutHandle.\n\r");
            }


            actualFrequency = DRV_TMR_CounterFrequencyGet(mbusData.ModbusCommCycleHandle);
            divider = actualFrequency/(uint32_t)832; // cacluate divider value
            //DRV_TMR_AlarmRegister(mbusData.ModbusCommCycleHandle, 394,true, (uintptr_t)&mbusData, mbusData.ModbusCommCycleCallBack);
            if (DRV_TMR_AlarmRegister(mbusData.ModbusCommCycleHandle, 394,true, 0, ModbusCommCycleCallBack) == true){
                _nop();
                
            }
            else{
                SYS_MESSAGE("Mbus handler\t: Register mbusData.ModbusCommCycleCallBack failed.\n\r");
            }

            actualFrequency = DRV_TMR_CounterFrequencyGet(mbusData.ModbusCharacterTimeoutHandle);
            divider = actualFrequency/(uint32_t)41015; // cacluate divider value
            if (DRV_TMR_AlarmRegister(mbusData.ModbusCharacterTimeoutHandle, 8,true, 0, ModbusCharacterTimeoutCallBack) == true){
                _nop();
                
            }
            else{
                SYS_MESSAGE("Mbus handler\t: Register mbusData.ModbusCharacterTimeoutCallBack failed.\n\r");
            }

            actualFrequency = DRV_TMR_CounterFrequencyGet(mbusData.ModbusReceiveTimeoutHandle);
            divider = actualFrequency/(uint32_t)4002; // cacluate divider value
            if (DRV_TMR_AlarmRegister(mbusData.ModbusReceiveTimeoutHandle, 82,true, 0, ModbusReceiveTimeoutCallBack) == true){
                _nop();
                
            }
            else{
                SYS_MESSAGE("Mbus handler\t: Register mbusData.ModbusReceiveTimeoutCallBack failed.\n\r");
            }
            
            mbusData.state = MBUS_STATE_WAIT;            
            break;
        }
        
        case MBUS_STATE_WAIT:
        {            
            break;
        }
        
        case MBUS_STATE_SLAVES_ON:
        {
            Slaves_Disable_Off();
            DelayCount = READxCORExTIMER();
            mbusData.state = MBUS_STATE_SLAVES_BOOT_WAIT;
            break;
        }
        
        case MBUS_STATE_SLAVES_BOOT_WAIT:
        {
            if((READxCORExTIMER() - DelayCount) > 200000000){
                mbusData.state = MBUS_STATE_WAIT;
                CREATExTASKxSTATUSxMESSAGE((uint8_t)MBUS, (uint8_t)MBUS_STATE_SLAVES_ON, (uint8_t)DONE);
                SYS_MESSAGE("Mbus handler\t: MBUS_STATE_SLAVES_BOOT_WAIT done.\n\r");
            }            
            break;
        }
        
        case MBUS_STATE_SLAVE_DETECT:
        {
            if (SLAVExDETECT()){
                mbusData.state = MBUS_STATE_WAIT;
                CREATExTASKxSTATUSxMESSAGE((uint8_t)MBUS, (uint8_t)MBUS_STATE_SLAVE_DETECT, (uint8_t)DONE);
                SYS_MESSAGE("Mbus handler\t: MBUS_STATE_SLAVE_DETECT done.\n\r");
            }
            PROCESSxPETITxMODBUS();
            break;
        }
        
        case MBUS_STATE_SLAVE_FW_FLASH:
        {
            if (SLAVExFWxHANDLER()){
                mbusData.state = MBUS_STATE_WAIT;
                CREATExTASKxSTATUSxMESSAGE((uint8_t)MBUS, (uint8_t)MBUS_STATE_SLAVE_FW_FLASH, (uint8_t)DONE);
                SYS_MESSAGE("Mbus handler\t: MBUS_STATE_SLAVE_FW_FLASH done.\n\r");
            }
            if(fwData.SlaveBootloaderHandlingActive == false){
                PROCESSxPETITxMODBUS();
            }            
            break;
        }
        
        case MBUS_STATE_SLAVE_INIT:
        {              
            if (SLAVExINITxANDxCONFIG())
            {                
                mbusData.state = MBUS_STATE_WAIT;
                CREATExTASKxSTATUSxMESSAGE((uint8_t)MBUS, (uint8_t)MBUS_STATE_SLAVE_INIT, (uint8_t)DONE);
                SYS_MESSAGE("Mbus handler\t: MBUS_STATE_SLAVE_INIT done.\n\r");
            }
            PROCESSxPETITxMODBUS();
            break;
        }
        
        case MBUS_STATE_SLAVE_ENABLE:
        {            
            if (ENABLExAMPLIFIER()){                
                mbusData.state = MBUS_STATE_SERVICE_TASKS;
                MaxSlaveUploadCount = 50;                                       // limit the upload to slave data only (cyclic))
                CREATExTASKxSTATUSxMESSAGE((uint8_t)MBUS, (uint8_t)MBUS_STATE_SLAVE_ENABLE, (uint8_t)DONE);
                SYS_MESSAGE("Mbus handler\t: MBUS_STATE_SLAVE_ENABLE done.\n\r");
            }
            PROCESSxPETITxMODBUS();
            break;
        }
        
        case MBUS_STATE_START_DATA_UPLOAD:
        {
            DRV_TMR_Start(mbusData.ModbusCommCycleHandle);
            mbusData.state  = MBUS_STATE_WAIT;
            mbusData.upload = UPLOAD_STATE_ALL;
            CREATExTASKxSTATUSxMESSAGE((uint8_t)MBUS, (uint8_t)MBUS_STATE_START_DATA_UPLOAD, (uint8_t)DONE);
            SYS_MESSAGE("Mbus handler\t: MBUS_STATE_START_DATA_UPLOAD done.\n\r");
            break;
        }

        case MBUS_STATE_SERVICE_TASKS:
        {
            if(UpdateNextSlave == true){
                if (PROCESSxNEXTxSLAVE()){
                    UpdateNextSlave = false;
                }                
            }
            PROCESSxSLAVExCOMMUNICATION();
            PROCESSxPETITxMODBUS();
            break;
        }
        
        case MBUS_STATE_RESET:
        {
            DRV_TMR_Stop(mbusData.ModbusCommCycleHandle);
            Slaves_Disable_On();
            mbusData.state  = MBUS_STATE_RESET_WAIT;
            mbusData.upload = UPLOAD_STATE_WAIT;
            MaxSlaveUploadCount = 55;
            DelayCount = READxCORExTIMER();            
            break;
        }
        
        case MBUS_STATE_RESET_WAIT:
        {
            if((READxCORExTIMER() - DelayCount) > 100000000){
                mbusData.state = MBUS_STATE_WAIT;
                CREATExTASKxSTATUSxMESSAGE((uint8_t)MBUS, (uint8_t)MBUS_STATE_RESET, (uint8_t)DONE);
                SYS_MESSAGE("Mbus handler\t: MBUS_STATE_RESET_WAIT done.\n\r");
            }            
            break;
        }

        /* The default state should never be executed. */
        default:
        {
            /* TODO: Handle error in application's state machine. */
            break;
        }
    }
    
    
    switch ( mbusData.upload )
    {
        /* Application's initial state. */
        case UPLOAD_STATE_WAIT:
        {
            break;
        }
        
        case UPLOAD_STATE_ALL:
        {
            udpTrans_t data;
            
            /* Every timer interrupt data is send to Ethernet PC client if available*/
            if (UploadSlaveData){
                UploadSlaveData = false;
                data.header = HEADER;
                data.command = SLAVEINFO;
                memcpy(&data.data, &(SlaveInfo[NextSlaveCounter].Header), sizeof(SLAVE_INFO));
                //data.data[0] &= SlaveInfo[NextSlaveCounter].Header;
                PUTxDATAxINxSENDxMAILxBOX(&data);                
                NextSlaveCounter++;
                if(NextSlaveCounter > MaxSlaveUploadCount){
                    NextSlaveCounter = 1;
                }
            }
            break;
        }
        
        case UPLOAD_STATE_SLAVES:
        {
            uint16_t loopcount = 0;
            udpTrans_t data;
            /* Every timer interrupt data is send to Ethernet PC client if available*/
            if (UploadSlaveData){
                UploadSlaveData = false;
                
                while (SlaveInfo[NextSlaveCounter].SlaveDetected == false){
                    NextSlaveCounter++; 
                    if (NextSlaveCounter > (MaxSlaveUploadCount)){
                        NextSlaveCounter = 1;
                        loopcount++;
                    }
                    if(loopcount > 2){
                        loopcount = 0;
                        NextSlaveCounter = 1;
                        break;
                    }
                }                                
                memcpy(&data, &(SlaveInfo[NextSlaveCounter].Header), sizeof(SLAVE_INFO));
                PUTxDATAxINxSENDxMAILxBOX(&data);
                
                NextSlaveCounter++;
                if(NextSlaveCounter > MaxSlaveUploadCount){
                    NextSlaveCounter = 1;
                }
            }
            break;
        }
        
        default:
        {
            break;
        }
    }
}

/******************************************************************************
  Function:
    uint32_t GETxMBUSxSTATE (void)

  Remarks:
    See prototype in mbus.h.
 */

uint32_t GETxMBUSxSTATE (void){
    return (mbusData.state);
}

/******************************************************************************
  Function:
    void SETxMBUSxSTATE (uint32_t state)

  Remarks:
    See prototype in mbus.h.
 */

void SETxMBUSxSTATE (MBUS_STATES state){
    mbusData.state = state;
}

/******************************************************************************
  Function:
    uint32_t READxCORExTIMER(void)

  Remarks:
    See prototype in mbus.h.
 */
uint32_t READxCORExTIMER(void)
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
