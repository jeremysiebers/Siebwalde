/******************************************************************************

 ZG2100 Driver Code and declarations used by ZGConsole.c to create a 
   VT100-compatible terminal. 
 Module for Microchip TCP/IP Stack

*******************************************************************************
 FileName:		ZGConsoleMsgHandler.h
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
#ifndef __ZGCONSOLEVT100_H
#define __ZGCONSOLEVT100_H


#include "TCPIP Stack/ZGLinkMgrII.h"
#include "TCPIP Stack/ZGDriverTypes.h"
#include "TCPIP Stack/ZGDriverConstants.h"
#include "TCPIP Stack/ZGConsoleMsgs.h"

#if defined (ZG_CONFIG_CONSOLE)
typedef struct
{
	tZGS8 	 rxBuf[kConsoleMaxMsgSize];  // buf for received characters
    tZGS8    txBuf[kConsoleMaxMsgSize];  // buf for transmitted characters
	tZGU8 	 rxState;                    // current state of Rx state machine
	tZGU8 	 cursorIndex;                // cursor index
    tZGBool  firstChar;                  // false if waiting for very first character from user or PC
    tZGBool  echoOn;                     // true if human input at console, false if PC communicating

#if defined( __18CXX )
    ROM FAR tZGU8  **p_cmdStrings;       // pointer to list of app-specific command strings
#else
    ROM tZGU8  **p_cmdStrings;           // pointer to list of app-specific command strings
#endif

    tZGU8    numCmdStrings;              // number of cmd strings in p_cmdStrings
    tZGU8    appConsoleMsgRx;            // true if app received a console msg, else false

#if defined( __18CXX )
    FAR tZGS8Ptr   argv[kZGMaxTokensPerCmd];   // pointer to each token in the rxBuf
#else
    tZGS8Ptr      argv[kZGMaxTokensPerCmd];   // pointer to each token in the rxBuf
#endif

    tZGU8    argc;                       // number of tokens in rxBuf
	tZGU8 	 subState;
	tZGBool  bStateMachineLoop;
	tZGU8	 req;
} tConsoleContext;
#endif

#define SET_ECHO_ON()       g_ConsoleContext.echoOn = kZGBoolTrue
#define SET_ECHO_OFF()      g_ConsoleContext.echoOn = kZGBoolFalse
#define IS_ECHO_ON()        g_ConsoleContext.echoOn


/*---------------------*/
/* Function Prototypes */
/*---------------------*/
tZGBool convertAsciiToHexInPlace(tZGS8 *p_string, tZGU8 expectedHexBinSize);
#define ExtractandValidateWpaPSK(x) convertAsciiToHexInPlace(x, kZGMaxPmkLen)
#define ExtractandValidateWepLong(x) convertAsciiToHexInPlace(x,kZGWEPKeyLenLong)
#define ExtractandValidateWepShort(x) convertAsciiToHexInPlace(x,kZGWEPKeyLenShort)

tZGVoidReturn process_cmd(tZGVoidInput);

tZGBool ExtractandValidateRfChannelAll(tZGS8 *p_string );
tZGBool ExtractandValidateRfChannelList(tZGS8 *p_string, tZGU8 *p_rfchan);
tZGBool ExtractandValidateDomain(tZGS8 *p_domainString, tZGU8 *pDomain);
tZGBool ExtractandValidateRts(tZGS8 *p_string, tZGU16 *p_rts);
tZGBool ExtractandValidateMode(tZGS8 *p_string, tZGU8 *p_mode);
tZGBool ExtractandValidatePower(tZGS8 *p_string, tZGBool *p_pwr);
tZGBool ExtractandValidateDTIM(tZGS8 *p_string, tZGBool *p_rxDTIM);
tZGBool ExtractandValidateSSID(tZGS8 *p_string, tZGU8 *p_len);
tZGBool ExtractWpaStrStart(tZGS8 *p_string);
tZGBool ExtractWpaStrEnd(tZGS8 *p_string);
tZGBool ValidateWpaStr(tZGS8 **p_string, tZGU8 *p_len);
tZGBool ExtractandValidateEncType(tZGS8 *p_string, tZGU8 *enc_type);
tZGBool ExtractandValidateAuthType(tZGS8 *p_string, tZGU8 *auth_type);
tZGBool ExtractandValidateWepIndex(tZGS8 *p_string, tZGU8 *key_index);

tZGBool ExtractandValidateU16Range(tZGS8 *p_string, tZGU16 *p_value, tZGU16 minValue, tZGU16 maxValue);

#ifdef ZG_CONFIG_DHCP
tZGBool ExtractandValidateDHCP(tZGS8 *p_string, tZGU8 *dhcp);
#endif


#endif /* __ZGCONSOLEVT100_H */
