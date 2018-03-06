/*
 * FreeModbus Libary: BARE Port
 * Copyright (C) 2006 Christian Walter <wolti@sil.at>
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 *
 * File: $Id: portserial.c,v 1.1 2006/08/22 21:35:13 wolti Exp $
 */

#include "port.h"
#include <plib.h>					// Peripheral Library

/* ----------------------- Modbus includes ----------------------------------*/
#include "mb.h"
#include "mbport.h"

/* ----------------------- static functions ---------------------------------*/
static void prvvUARTTxReadyISR( void );
static void prvvUARTRxISR( void );

/* ----------------------- Start implementation -----------------------------*/
void
vMBPortSerialEnable( BOOL xRxEnable, BOOL xTxEnable )
{
    /* If xRXEnable enable serial receive interrupts. If xTxENable enable
     * transmitter empty interrupts.
     */
	if(xRxEnable == TRUE && xTxEnable == TRUE){
		EnableIntU2RX;
		EnableIntU2TX;
	}
	else if(xRxEnable == TRUE && xTxEnable == FALSE){
		EnableIntU2RX;
		DisableIntU2TX;
	}
	else if(xRxEnable == FALSE && xTxEnable == TRUE){
		DisableIntU2RX;
		EnableIntU2TX;
		IFS1bits.U2TXIF = 1;
	}
	else if(xRxEnable == FALSE && xTxEnable == FALSE){
		DisableIntU2RX;
		DisableIntU2TX;
	}
}

BOOL
xMBPortSerialInit( UCHAR ucPORT, ULONG ulBaudRate, UCHAR ucDataBits, eMBParity eParity )
{
	int pbClk;

	pbClk=SYSTEMConfig(SYS_FREQ, SYS_CFG_WAIT_STATES | SYS_CFG_PCACHE);
	if (ucPORT == 1){
		OpenUART1(UART_EN, 		// Module is ON
				  UART_RX_ENABLE | UART_TX_ENABLE,		// Enable TX & RX
				  pbClk/16/ulBaudRate-1);	// 
		ConfigIntUART1(UART_INT_PR2 | UART_RX_INT_EN);
	}
	else if (ucPORT == 2){
		OpenUART2(UART_EN | UART_NO_PAR_8BIT | UART_2STOPBITS, 		// Module is ON
				  UART_RX_ENABLE | UART_TX_ENABLE,		// Enable TX & RX
				  pbClk/16/ulBaudRate-1);	// 
		ConfigIntUART2(UART_INT_PR2 | UART_RX_INT_EN);
	}
	INTEnableSystemMultiVectoredInt();
    return TRUE;
}

BOOL
xMBPortSerialPutByte( CHAR ucByte )
{
    /* Put a byte in the UARTs transmit buffer. This function is called
     * by the protocol stack if pxMBFrameCBTransmitterEmpty( ) has been
     * called. */
	putcUART2(ucByte);
    return TRUE;
}

BOOL
xMBPortSerialGetByte( CHAR * pucByte )
{
    /* Return the byte in the UARTs receive buffer. This function is called
     * by the protocol stack after pxMBFrameCBByteReceived( ) has been called.
     */
	*pucByte = ReadUART2();
    return TRUE;
}

/* Create an interrupt handler for the transmit buffer empty interrupt
 * (or an equivalent) for your target processor. This function should then
 * call pxMBFrameCBTransmitterEmpty( ) which tells the protocol stack that
 * a new character can be sent. The protocol stack will then call 
 * xMBPortSerialPutByte( ) to send the character.
 */

// UART 2 interrupt handler
// it is set at priority level 2
void __ISR(_UART2_VECTOR, ipl2) IntUart2Handler(void){

	// RX interrupt
	if(mU2RXGetIntFlag())
	{
		// Clear the RX interrupt Flag
	    mU2RXClearIntFlag();
		pxMBFrameCBByteReceived(  );

	}

	// TX interrupt
	if ( mU2TXGetIntFlag() )
	{
		mU2TXClearIntFlag();
		pxMBFrameCBTransmitterEmpty(  );

	}
}
