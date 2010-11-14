/******************************************************************************

 ZG2100 Text Control Console: Describes messages and conversion functions
 Module for Microchip TCP/IP Stack

*******************************************************************************
 FileName:		ZGConsoleMsgs.h
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
 SAG				12/12/08	Ported from eval SDK
******************************************************************************/
#ifndef __ZGCONSOLEMSGS_H
#define __ZGCONSOLEMSGS_H

#ifdef __cplusplus
 extern "C" {
#endif

//----------------------------------------------------------------------------
//                                 Defines
//----------------------------------------------------------------------------

// !!! These defines MUST match the g_consoleCmd structure  !!!
enum validConsoleCmds
{
    HELP_MSG = 0,                   // only used by humans
    GET_ZG2100_VERSION_MSG,
    RESET_HOST,
    CLEAR_SCREEN_MSG,
    IFCONFIG_MSG,
    IWCONFIG_MSG,
    IWPRIV_MSG,
    INVALID_CMD = 0xFF
};


#define kZGMaxTokensPerCmd          (16)  /* max tokens, including cmd and parameters */
#define kConsoleMaxMsgSize	        (80)


//----------------------------------------------------------------------------
//                                 Typedefs
//----------------------------------------------------------------------------

// structure definition to define all ASCII messages
typedef struct msg_struct
{
    ROM tZGS8   *p_cmdName;       // pointer to cmd name
    ROM tZGS8   *p_cmdHelp;       // pointer to cmd help string
    tZGU8   maxTokens;            // max tokens for this command
} tZGCmd;

// parsed version numbers put in this structure
typedef struct version_struct
{
    tZGU8 major;
    tZGU8 minor;
    tZGU8 revision;
} tZGVersion;


//----------------------------------------------------------------------------
//                                 External Globals
//----------------------------------------------------------------------------
extern  const tZGCmd    g_consoleCmd[];
extern  const tZGU8     g_numCmds;


//----------------------------------------------------------------------------
//                                 Function Prototypes
//----------------------------------------------------------------------------
tZGVoidReturn   TokenizeCmdLine(tZGS8 *p_line);
tZGU8           GetCmdId(tZGVoidInput);
tZGVoidReturn   Output_Monitor_Hdr(tZGVoidInput);
tZGBool         ConvertASCIIHexToBinary(tZGS8 *p_ascii, tZGU16 *p_binary);
tZGBool         ConvertASCIIUnsignedDecimalToBinary(tZGS8 *p_ascii, tZGU16 *p_binary);
tZGBool         ConvertASCIISignedDecimalToBinary(tZGS8 *p_ascii, tZGS16 *p_binary);
tZGU8           HexToBin(tZGU8 hexChar);
tZGVoidReturn   ZGConsoleSetMsgFlag(tZGVoidInput);



#ifdef __cplusplus
 }
#endif


#endif /* __ZGCONSOLEMSGS_H */
