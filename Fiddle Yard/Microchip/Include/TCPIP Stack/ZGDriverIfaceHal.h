/******************************************************************************

 ZG2100 Driver Hardware Abstraction Layer (HAL) Interface header
 Module for Microchip TCP/IP Stack

*******************************************************************************
 FileName:		ZGDriverIfaceHal.h
 Dependencies:	None
 Processor:		PIC18, PIC24F, PIC24H, dsPIC30F, dsPIC33F, PIC32
 Compiler:		Microchip C32 v1.10b or higher
				Microchip C30 v3.22 or higher
				Microchip C18 v3.34 or higher
 Company:		Microchip Technology, Inc.

 Software License Agreement

 Copyright (C) 2002-2010 Microchip Technology Inc.  All rights reserved.

 Microchip licenses to you the right to use, modify, copy, and distribute:
 (i)  the Software when embedded on a Microchip microcontroller or digital 
      signal controller product ("Device") which is integrated into 
      Licensee's product; or
 (ii) ONLY the Software driver source files ENC28J60.c, ENC28J60.h,
      ENCX24J600.c and ENCX24J600.h ported to a non-Microchip device used in 
	  conjunction with a Microchip ethernet controller for the sole purpose 
	  of interfacing with the ethernet controller.

 You should refer to the license agreement accompanying this Software for 
 additional information regarding your rights and obligations.

 THE SOFTWARE AND DOCUMENTATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
 KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION, ANY WARRANTY
 OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, TITLE AND
 NON-INFRINGEMENT. IN NO EVENT SHALL MICROCHIP BE LIABLE FOR ANY INCIDENTAL,
 SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES, LOST PROFITS OR LOST DATA, COST
 OF PROCUREMENT OF SUBSTITUTE GOODS, TECHNOLOGY OR SERVICES, ANY CLAIMS BY
 THIRD PARTIES (INCLUDING BUT NOT LIMITED TO ANY DEFENSE THEREOF), ANY CLAIMS
 FOR INDEMNITY OR CONTRIBUTION, OR OTHER SIMILAR COSTS, WHETHER ASSERTED ON
 THE BASIS OF CONTRACT, TORT (INCLUDING NEGLIGENCE), BREACH OF WARRANTY, OR
 OTHERWISE.


 Author				Date		Comment
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 Zero G				Sep 2008	Initial version
 KO					31 Oct 2008	Port to PIC24F and PIC32 for TCP/IP stack v4.52
******************************************************************************/
#ifndef _ZGDRIVERIFACEHAL_H_
#define _ZGDRIVERIFACEHAL_H_

/*****************************************************************************
 * FUNCTION: zgDriverEintHandler
 *
 * RETURNS: N/A
 *
 * PARAMS:
 *      N/A
 *
 *
 *  NOTES: This function must be called once, each time an external interrupt
 *      is received from the ZeroG device.   The ZG Driver will schedule any
 *      subsequent SPI communication to process the interrupt.
 *****************************************************************************/
tZGVoidReturn zgDriverEintHandler(tZGVoidInput);

/*****************************************************************************
 * FUNCTION: zgDriverSpiTxRxDoneHandler
 *
 * RETURNS: N/A
 *
 * PARAMS:
 *      N/A
 *
 *
 *  NOTES: This function must be called once, each time the SPI driver
 *      completes an SPI operation.  The ZG Driver will, upon receiving this
 *      function call, schedule a new SPI operation to the driver if one
 *      is pending.
 *****************************************************************************/
tZGVoidReturn zgDriverSpiTxRxDoneHandler(tZGVoidInput);

#endif /*_ZGDRIVERIFACEHAL_H_ */
