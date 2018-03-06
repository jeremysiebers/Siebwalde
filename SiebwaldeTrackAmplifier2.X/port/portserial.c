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
#include <xc.h>
#include "port.h"

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
		PIE1bits.RCIE = 1;
		PIE1bits.TXIE = 1;
	}
	else if(xRxEnable == TRUE && xTxEnable == FALSE){
		PIE1bits.RCIE = 1;
		PIE1bits.TXIE = 0;
	}
	else if(xRxEnable == FALSE && xTxEnable == TRUE){
		PIE1bits.RCIE = 0;
		PIE1bits.TXIE = 1;
		PIR1bits.TXIF = 1;
	}
	else if(xRxEnable == FALSE && xTxEnable == FALSE){
		PIE1bits.RCIE = 0;
		PIE1bits.TXIE = 0;
	}
}

BOOL
xMBPortSerialInit( UCHAR ucPORT, ULONG ulBaudRate, UCHAR ucDataBits, eMBParity eParity )
{
    BOOL _return = FALSE;
    
    if (ucPORT == 1){
        // disable interrupts before changing states
        PIE1bits.RCIE = 0;
        PIE1bits.TXIE = 0;

        // Baud Rate = 
        SPBRG = (SYS_FREQ/ulBaudRate/16)-1;


        RCSTAbits.SPEN = 1; // 1 = Serial port enabled (configures RC7/RX/DT and RC6/TX/CK pins as serial port pins)
        RCSTAbits.RX9  = 1; // 0 = Selects 8-bit reception
        RCSTAbits.SREN = 0; // 0 = Disables single receive
        RCSTAbits.CREN = 1; // 1 = Enables continuous receive until enable bit CREN is cleared (CREN overrides SREN)
        RCSTAbits.ADDEN= 1; // 1 = Enables address detection, enables interrupt and load of the receive buffer when RSR<8> is set

        TXSTAbits.CSRC = 0; // Master mode
        TXSTAbits.TXEN = 1; // Transmit Enable bit (SREN/CREN overrides TXEN in Sync mode)
        TXSTAbits.SYNC = 0; // Asynchronous mode
        TXSTAbits.BRGH = 1; // Used in sync mode  
        
        PIE1bits.RCIE = 1;
        PIE1bits.TXIE = 1;
       
        _return =  TRUE;
	}
	else {
        _return = FALSE;
	}
	   
    return _return;
}

BOOL
xMBPortSerialPutByte( CHAR ucByte )
{
    /* Put a byte in the UARTs transmit buffer. This function is called
     * by the protocol stack if pxMBFrameCBTransmitterEmpty( ) has been
     * called. */
	TXREG = ucByte;
    return TRUE;
}

BOOL
xMBPortSerialGetByte( CHAR * pucByte )
{
    /* Return the byte in the UARTs receive buffer. This function is called
     * by the protocol stack after pxMBFrameCBByteReceived( ) has been called.
     */
	*pucByte = RCREG;
    return TRUE;
}

/* Create an interrupt handler for the transmit buffer empty interrupt
 * (or an equivalent) for your target processor. This function should then
 * call pxMBFrameCBTransmitterEmpty( ) which tells the protocol stack that
 * a new character can be sent. The protocol stack will then call 
 * xMBPortSerialPutByte( ) to send the character.
 */

// UART interrupt handler
void MBIntUartHandler(void){

	// RX interrupt
	if( PIR1bits.RCIF == 1 )
	{
		// Clear the RX interrupt Flag
	    PIR1bits.RCIF = 0;
		pxMBFrameCBByteReceived(  );

	}

	// TX interrupt
	if ( PIR1bits.TXIF == 1 )
	{
		PIR1bits.TXIF = 0;
		pxMBFrameCBTransmitterEmpty(  );

	}
}
