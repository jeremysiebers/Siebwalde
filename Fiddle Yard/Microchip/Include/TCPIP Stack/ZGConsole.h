/******************************************************************************

 ZG2100 Driver Header file implenting the console user interface.
 Module for Microchip TCP/IP Stack

*******************************************************************************
 FileName:		ZGConsole.h
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
#ifndef _ZGCONSOLE_H_
#define _ZGCONSOLE_H_

#include "TCPIP Stack/TCPIP.h"
#include "TCPIP Stack/ZGConsoleMsgHandler.h"


#if defined (ZG_CONFIG_CONSOLE)
  #if defined(__18CXX)
  #pragma varlocate 4 g_ConsoleContext
  #endif

  extern tConsoleContext g_ConsoleContext;
#endif

#define ARGC           g_ConsoleContext.argc
#define ARGV           g_ConsoleContext.argv

typedef enum
{
    kZGConsoleReqNone = 0,
    kZGConsoleReqPing
} tZGConsoleReq;

#if defined ( ZG_CONFIG_CONSOLE )

    extern tZGVoidReturn ZGConsoleInit(ROM tZGU8 **p_cmdStrings, tZGU8 numCmdStrings);
    extern tZGVoidReturn ZGConsoleProcess(tZGVoidInput);
    extern tZGVoidReturn ZGConsoleReqClear(tZGVoidInput);
    extern tZGVoidReturn ZGConsoleProcess(tZGVoidInput);
    extern tZGBool ZGConsoleIsConsoleMsgReceived(tZGVoidInput);
    extern tZGVoidReturn ZGConsoleReleaseConsoleMsg(tZGVoidInput);
    extern tZGS8 ** ZGConsoleGetCmdLineTokens(tZGU8 *p_argc);
    extern tZGVoidReturn ZGConsoleSetMsgFlag(tZGVoidInput);
	extern tZGBool ZGConsoleIsIperfAppKillRequested(tZGVoidInput);
#else

    #define ZGConsoleInit()
    #define ZGConsoleProcess()
    #define ZGConsoleReqClear()

#endif

#endif /* _ZGCONSOLE_H_ */
