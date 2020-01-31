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
#include "port.h"
#include <plib.h>					// Peripheral Library

/* ----------------------- Modbus includes ----------------------------------*/
#include "mb.h"
#include "mbport.h"

/* ----------------------- static functions ---------------------------------*/
static void prvvTIMERExpiredISR( void );

/* ----------------------- Start implementation -----------------------------*/
BOOL xMBPortTimersInit(USHORT usTim1Timerout50us){

	WritePeriod2(usTim1Timerout50us);
	return TRUE;
}


inline void vMBPortTimersEnable(void){
    /* Enable the timer with the timeout passed to xMBPortTimersInit( ) */
	OpenTimer2(T2_OFF | T2_SOURCE_INT | T2_PS_1_256,
		65000);
	xMBPortTimersInit(65000 );		//Adjust timing for real timeouts
	ConfigIntTimer2(T2_INT_ON | T2_INT_PRIOR_2);
	T2CONbits.ON = 1; // Start Timer
	INTEnableSystemMultiVectoredInt();
}

inline void vMBPortTimersDisable(void){
    /* Disable any pending timers. */
	CloseTimer2();
}

/* Create an ISR which is called whenever the timer has expired. This function
 * must then call pxMBPortCBTimerExpired( ) to notify the protocol stack that
 * the timer has expired.
 */

void __ISR(_TIMER_2_VECTOR, ipl2) Timer2Handler(void)
{
    // clear the interrupt flag
    mT2ClearIntFlag();

//	(void)pxMBPortCBTimerExpired();
	pxMBPortCBTimerExpired();

    mPORTAToggleBits(BIT_7);
}

