/******************************************************************************

 ZG2100 Driver Access Header file to the System Services.
 Module for Microchip TCP/IP Stack

*******************************************************************************
 FileName:		ZGModuleAccessSys.h
 Dependencies:	TCP/IP Stack header files
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
 ZeroG				2008
******************************************************************************/
#ifndef _ZGMODULEACCESSSYS_H_
#define _ZGMODULEACCESSSYS_H_

/* Use the same set of access functions as driver does.
 * Particularly, buffer management functions such as
 * ZGSYS_READBUF_GET() and ZGSYS_READBUF_CLEAN()
 */
#include "TCPIPConfig.h"

#include "TCPIP Stack/ZGDriverTypes.h"
#include "TCPIP Stack/ZGDriverConstants.h"
#include "TCPIP Stack/ZGAccessSys.h"

#define ZGSYS_MODULE_GET_MSEC_TICK_COUNT  TickGet

#define ZGSYS_UART_INIT(port,rate,flag)
#if defined( STACK_USE_UART )
	#define ZGSYS_UART_PUTC(port,c)          	putcUART(c)
	#define ZGSYS_UART_GETC()            		ReadUART()
	#define ZGSYS_UART_GETC_COUNT()      		DataRdyUART()
#else
	#define ZGSYS_UART_PUTC(port,c)
	#define ZGSYS_UART_GETC()            		(0)
	#define ZGSYS_UART_GETC_COUNT()      		(0)
#endif

#endif /*_ZGMODULEACCESSSYS_H_ */
