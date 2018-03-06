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
 * File: $Id: porttimer.c,v 1.1 2006/08/22 21:35:13 wolti Exp $
 */

/* ----------------------- Platform includes --------------------------------*/
#include <xc.h>
#include "port.h"

/* ----------------------- Modbus includes ----------------------------------*/
#include "mb.h"
#include "mbport.h"

/* ----------------------- static functions ---------------------------------*/
static void prvvTIMERExpiredISR( void );

/* ----------------------- Start implementation -----------------------------*/
BOOL xMBPortTimersInit(USHORT usTim1Timerout50us){

	TMR0 = usTim1Timerout50us;
	return TRUE;
}


void vMBPortTimersEnable(void){
    /* Enable the timer with the timeout passed to xMBPortTimersInit( ) */
	
    // TMR0 Prescaler (default assigned)
    OPTION_REGbits.PS = 0b110; //1:128 to get 50 us timer interrupts
    
    OPTION_REGbits.T0CS = 0;
    
    OPTION_REGbits.T0SE = 0;
    
    OPTION_REGbits.PSA = 0;
    
    // TMR0 0x0; 
    TMR0 = 0x00;

    // Clearing IF flag.
    INTCONbits.TMR0IF = 0;
    
    // Enable interrupt
    INTCONbits.TMR0IE = 1;
    
    
	//xMBPortTimersInit(65000 );		//Adjust timing for real timeouts
	
}

void vMBPortTimersDisable(void){
    /* Disable any pending timers. */
	INTCONbits.TMR0IE = 0;
}

/* Create an ISR which is called whenever the timer has expired. This function
 * must then call pxMBPortCBTimerExpired( ) to notify the protocol stack that
 * the timer has expired.
 */

void MBIntTimerHandler(void)
{
    if (INTCONbits.TMR0IE == 1 && INTCONbits.TMR0IF == 1){
        // clear the interrupt flag
        INTCONbits.TMR0IF = 0;

        //	(void)pxMBPortCBTimerExpired();
        pxMBPortCBTimerExpired();

        PORTCbits.RC3 = !PORTCbits.RC3;
    }
    
}

