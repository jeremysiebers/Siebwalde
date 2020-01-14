/*******************************************************************************
  MPLAB Harmony Application Source File
  
  Company:
    Microchip Technology Inc.
  
  File Name:
    controller.c

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

#include "controller.h"
#include "ethernet.h"
#include "mbus.h"
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
    This structure should be initialized by the APP_Initialize function.
    
    Application strings and buffers are be defined outside this structure.
*/

        CONTROLLER_DATA controllerData;
        uint8_t         ControllerCommand;
        udpTrans_t      *EthernetRecvData;

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


// *****************************************************************************
// *****************************************************************************
// Section: Application Initialization and State Machine Functions
// *****************************************************************************
// *****************************************************************************

/*******************************************************************************
  Function:
    void CONTROLLER_Initialize ( void )

  Remarks:
    See prototype in controller.h.
 */

void CONTROLLER_Initialize ( void )
{
    /* Place the App state machine in its initial state. */
    controllerData.state = CONTROLLER_STATE_INIT;

    
    /* TODO: Initialize your application's state machine and other
     * parameters.
     */
}


/******************************************************************************
  Function:
    void CONTROLLER_Tasks ( void )

  Remarks:
    See prototype in controller.h.
 */

void CONTROLLER_Tasks ( void )
{
    uint32_t    task_id     = CONTROLLER;
    
    /* Check the application's current state. */
    switch ( controllerData.state )
    {
        /* Application's initial state. */
        case CONTROLLER_STATE_INIT:
        {
            if (GETxMBUSxSTATE() == MBUS_STATE_WAIT && GETxETHERNETxSTATE() == ETHERNET_TCPIP_WAIT_FOR_CONNECTION){
                SYS_MESSAGE("Controller\t: MBUS and ETHERNET ready, controller waiting for client.\n\r");
                controllerData.state = CONTROLLER_STATE_WAITING_FOR_CLIENT;
            }            
            break;
        }
        
        case CONTROLLER_STATE_WAITING_FOR_CLIENT:
        {
            if(CHECKxDATAxINxRECEIVExMAILxBOX()){
                EthernetRecvData = GETxDATAxFROMxRECEIVExMAILxBOX();

                if(EthernetRecvData->header == HEADER && 
                        EthernetRecvData->command == CLIENT_CONNECTION_REQUEST){
                    controllerData.state = CONTROLLER_STATE_IDLE;
                    SYS_MESSAGE("Controller\t: Client connected, controller going to Idle state.\n\r");
                    CREATExTASKxSTATUSxMESSAGE(task_id, CONNECTED, DONE, NONE);
                }
                else{
                    DISCONNECTxCLIENT();
                    controllerData.state = CONTROLLER_STATE_INIT;
                    SYS_MESSAGE("Controller\t: Received wrong handshake, resetting Ethernet sockets.\n\r");
                }
            }
            break;
        }

        case CONTROLLER_STATE_IDLE:
        {
            if(CHECKxDATAxINxRECEIVExMAILxBOX()){
                EthernetRecvData = GETxDATAxFROMxRECEIVExMAILxBOX();
                
                if(EthernetRecvData->header == HEADER){
                    controllerData.state = CONTROLLER_STATE_HANDLE_COMM_DATA;                                        
                }
            }
            break;
        }
        
        case CONTROLLER_STATE_HANDLE_COMM_DATA:
        {
            switch(EthernetRecvData->command){
                case CLIENT_CONNECTION_REQUEST:
                {
                    CREATExTASKxSTATUSxMESSAGE(task_id, CONNECTED, DONE, NONE); // assume same client is connecting again
                    SYS_MESSAGE("Controller\t: Client re-connected, controller going to Idle state.\n\r");
                    break;
                }                
                case EXEC_MBUS_STATE_SLAVES_ON:
                {
                    SYS_MESSAGE("Controller\t: EXEC_MBUS_STATE_SLAVES_ON.\n\r");
                    SETxMBUSxSTATE(MBUS_STATE_SLAVES_ON);
                    break;
                }
                case EXEC_MBUS_STATE_SLAVE_DETECT:
                {
                    SYS_MESSAGE("Controller\t: EXEC_MBUS_STATE_SLAVE_DETECT.\n\r");
                    SETxMBUSxSTATE(MBUS_STATE_SLAVE_DETECT);
                    break;
                }
                case EXEC_MBUS_STATE_SLAVE_FW_FLASH:
                {
                    SYS_MESSAGE("Controller\t: EXEC_MBUS_STATE_SLAVE_FW_FLASH.\n\r");
                    SETxMBUSxSTATE(MBUS_STATE_SLAVE_FW_FLASH);
                    break;
                }
                case EXEC_MBUS_STATE_SLAVE_INIT:
                {
                    SYS_MESSAGE("Controller\t: EXEC_MBUS_STATE_SLAVE_INIT.\n\r");
                    SETxMBUSxSTATE(MBUS_STATE_SLAVE_INIT);
                    break;
                }
                case EXEC_MBUS_STATE_SLAVE_ENABLE:
                {
                    SYS_MESSAGE("Controller\t: EXEC_MBUS_STATE_SLAVE_ENABLE.\n\r");
                    SETxMBUSxSTATE(MBUS_STATE_SLAVE_ENABLE);
                    break;
                }
                case EXEC_MBUS_STATE_START_DATA_UPLOAD:
                {
                    SYS_MESSAGE("Controller\t: EXEC_MBUS_STATE_START_DATA_UPLOAD.\n\r");
                    SETxMBUSxSTATE(MBUS_STATE_START_DATA_UPLOAD);
                    break;
                }
                case EXEC_MBUS_STATE_RESET:
                {
                    SYS_MESSAGE("Controller\t: EXEC_MBUS_STATE_RESET.\n\r");
                    SETxMBUSxSTATE(MBUS_STATE_RESET);
                    break;
                }
                
                default:
                {
                    CREATExTASKxSTATUSxMESSAGE(task_id, RECEIVED_UNKNOWN_COMMAND, ERROR, NONE);
                    SYS_MESSAGE("Controller\t: command error, received command invalid.\n\r");
                    controllerData.state = CONTROLLER_STATE_IDLE;
                    break;
                }                
            }                        
            controllerData.state = CONTROLLER_STATE_CHECK_MBUS_STATE;            
            break;
        }
        
        case CONTROLLER_STATE_CHECK_MBUS_STATE:
        {
            if (GETxMBUSxSTATE() == MBUS_STATE_WAIT || GETxMBUSxSTATE() == MBUS_STATE_SERVICE_TASKS){
                controllerData.state = CONTROLLER_STATE_IDLE;                
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
}

 

/*******************************************************************************
 End of File
 */
