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
#include "system_definitions.h"
#include "../TrackController5.X/../../modbus/General.h"
#include "../TrackController5.X/../../mbus.h"
#include "../TrackController5.X/../../slavefwhandler.h"

// *****************************************************************************
// *****************************************************************************
// Section: System Interrupt Vector Functions
// *****************************************************************************
// *****************************************************************************
 
void __ISR(_UART1_TX_VECTOR, ipl2AUTO) _IntHandlerDrvUsartTransmitInstance0(void)
{
    DRV_USART_TasksTransmit(sysObj.drvUsart0);
}
void __ISR(_UART1_RX_VECTOR, ipl2AUTO) _IntHandlerDrvUsartReceiveInstance0(void)
{
    DRV_USART_TasksReceive(sysObj.drvUsart0);
}
void __ISR(_UART1_FAULT_VECTOR, ipl2AUTO) _IntHandlerDrvUsartErrorInstance0(void)
{
    DRV_USART_TasksError(sysObj.drvUsart0);
}
 
 

 
void __ISR(_UART2_TX_VECTOR, ipl0AUTO) _IntHandlerDrvUsartTransmitInstance1(void)
{
    DRV_USART_TasksTransmit(sysObj.drvUsart1);
}
void __ISR(_UART2_RX_VECTOR, ipl1AUTO) _IntHandlerDrvUsartReceiveInstance1(void)
{
    if(!fwData.SlaveBootloaderHandlingActive){
        /* Handle received char */
        ReceiveInterrupt(DRV_USART1_ReadByte());                                    // read received byte into modbus buffer;
        DRV_USART_TasksReceive(sysObj.drvUsart1);
        
        /* Reset and start TMR2 (timer_6) for inter character timeout 25us */
        DRV_TMR_Stop(mbusData.ModbusCharacterTimeoutHandle);
        DRV_TMR_CounterClear(mbusData.ModbusCharacterTimeoutHandle);
        DRV_TMR_Start(mbusData.ModbusCharacterTimeoutHandle);

        /* Stop and reset TMR3 (timer_8) for message receive timeout 250us */
        DRV_TMR_Stop(mbusData.ModbusReceiveTimeoutHandle);
        DRV_TMR_CounterClear(mbusData.ModbusReceiveTimeoutHandle);
    }
    else{
        SLAVExBOOTLOADERxDATAxRETURN(DRV_USART1_ReadByte());
        DRV_USART_TasksReceive(sysObj.drvUsart1);
    }
    
}
void __ISR(_UART2_FAULT_VECTOR, ipl1AUTO) _IntHandlerDrvUsartErrorInstance1(void)
{
    if(PLIB_USART_ReceiverFramingErrorHasOccurred(USART_ID_2)){
        SYS_MESSAGE("UART2_FAULT_VECTOR\t: PLIB_USART_ReceiverFramingErrorHasOccurred.\n\r");
    }
    if(PLIB_USART_ReceiverOverrunHasOccurred(USART_ID_2))
    {
        PLIB_USART_ReceiverOverrunErrorClear(USART_ID_2);
        SYS_MESSAGE("UART2_FAULT_VECTOR\t: PLIB_USART_ReceiverOverrunHasOccurred.\n\r");
    }
    DRV_USART_TasksError(sysObj.drvUsart1);
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
