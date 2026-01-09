/*******************************************************************************
 System Interrupts File

  File Name:
    system_interrupt.c

  Summary:
    Raw ISR definitions.

  Description:
    This file contains a definitions of the raw ISRs required to support the
    interrupt sub-system.

  Summary:
    This file contains source code for the interrupt vector functions in the
    system.

  Description:
    This file contains source code for the interrupt vector functions in the
    system.  It implements the system and part specific vector "stub" functions
    from which the individual "Tasks" functions are called for any modules
    executing interrupt-driven in the MPLAB Harmony system.

  Remarks:
    This file requires access to the systemObjects global data structure that
    contains the object handles to all MPLAB Harmony module objects executing
    interrupt-driven in the system.  These handles are passed into the individual
    module "Tasks" functions to identify the instance of the module to maintain.
 *******************************************************************************/

// DOM-IGNORE-BEGIN
/*******************************************************************************
Copyright (c) 2011-2014 released Microchip Technology Inc.  All rights reserved.

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

#include "system/common/sys_common.h"
#include "mbus.h"
#include "ethernet.h"
#include "controller.h"
#include "system_definitions.h"
#include "../TrackController5.X/../../modbus/General.h"
#include "../TrackController5.X/../../slavefwhandler.h"

// *****************************************************************************
// *****************************************************************************
// Section: System Interrupt Vector Functions
// *****************************************************************************
// *****************************************************************************
 
 /* 9th bit helper function */
 static inline uint16_t UART2_Read9(void)
{
    return (U2RXREG & 0x01FF);
}
 
 
void __ISR(_UART2_TX_VECTOR, ipl0AUTO) _IntHandlerDrvUsartTransmitInstance0(void)
{
    DRV_USART_TasksTransmit(sysObj.drvUsart0);
}
void __ISR(_UART2_RX_VECTOR, ipl1AUTO) _IntHandlerDrvUsartReceiveInstance0(void)
{
    if(!fwData.SlaveBootloaderHandlingActive){
        
        bool gotAny = false;
        
        while (PLIB_USART_ReceiverDataIsAvailable(USART_ID_2)){
            gotAny = true;
            /* Handle received char */
            uint16_t w = UART2_Read9();        
            uint8_t data = (uint8_t)(w & 0x00FF);        
            bool isAddress = ((w & 0x0100u) != 0u);
            //LOG_Printf("Sys_interrupt\t: raw data %d address %d", w, isAddress);        
            ReceiveInterrupt(data, isAddress);
        }
        
        if (gotAny)
        {
            // Inter-character timeout: restart once after draining FIFO
            DRV_TMR_Stop(mbusData.ModbusCharacterTimeoutHandle);
            DRV_TMR_CounterClear(mbusData.ModbusCharacterTimeoutHandle);
            DRV_TMR_Start(mbusData.ModbusCharacterTimeoutHandle);

            // Message timeout: restart once after draining FIFO
            DRV_TMR_Stop(mbusData.ModbusReceiveTimeoutHandle);
            DRV_TMR_CounterClear(mbusData.ModbusReceiveTimeoutHandle);
        }

        SYS_INT_SourceStatusClear(INT_SOURCE_USART_2_RECEIVE);
    }
    else
    {
        while (PLIB_USART_ReceiverDataIsAvailable(USART_ID_2))
        {
            uint8_t b = (uint8_t)(U2RXREG & 0x00FFu);
            SLAVExBOOTLOADERxDATAxRETURN(b);
        }
        SYS_INT_SourceStatusClear(INT_SOURCE_USART_2_RECEIVE);

    }
}


void __ISR(_UART2_FAULT_VECTOR, ipl1AUTO) _IntHandlerDrvUsartErrorInstance0(void)
{
    if(PLIB_USART_ReceiverFramingErrorHasOccurred(USART_ID_2)){
        // Drain RX to clear FE condition
        while (U2STAbits.URXDA) { (void)U2RXREG; }
        LOG_Push("UART2_FAULT_VECTOR\t: PLIB_USART_ReceiverFramingErrorHasOccurred.");
    }
    if(PLIB_USART_ReceiverOverrunHasOccurred(USART_ID_2))
    {
        PLIB_USART_ReceiverOverrunErrorClear(USART_ID_2);
        LOG_Push("UART2_FAULT_VECTOR\t: PLIB_USART_ReceiverOverrunHasOccurred.");
    }
    DRV_USART_TasksError(sysObj.drvUsart0);
}

 

 

 

 
 
 
void __ISR(_TIMER_1_VECTOR, ipl1AUTO) IntHandlerDrvTmrInstance0(void)
{
    DRV_TMR_Tasks(sysObj.drvTmr0);
}
void __ISR(_TIMER_3_VECTOR, ipl1AUTO) IntHandlerDrvTmrInstance1(void)
{
    DRV_TMR_Tasks(sysObj.drvTmr1);
}
void __ISR(_TIMER_5_VECTOR, ipl1AUTO) IntHandlerDrvTmrInstance2(void)
{
    DRV_TMR_Tasks(sysObj.drvTmr2);
}
void __ISR(_TIMER_7_VECTOR, ipl1AUTO) IntHandlerDrvTmrInstance3(void)
{
    DRV_TMR_Tasks(sysObj.drvTmr3);
}
 void __ISR(_ETHERNET_VECTOR, ipl5AUTO) _IntHandler_ETHMAC(void)
{
    DRV_ETHMAC_Tasks_ISR((SYS_MODULE_OBJ)0);
}

/* This function is used by ETHMAC driver */
bool SYS_INT_SourceRestore(INT_SOURCE src, int level)
{
    if(level)
    {
        SYS_INT_SourceEnable(src);
    }

    return level;
}

/*******************************************************************************
 End of File
*/
