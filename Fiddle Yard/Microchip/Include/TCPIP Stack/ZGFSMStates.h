/******************************************************************************

 ZG2100 Driver defines all possible states referenced in FSM tables defined
   in ZGWiFiMgrII.c, ZGAdhocMgrII.c, etc.  This header file eliminates a
   cross dependency between other .h files, and subsequent forward references
   that may or may not port well, based on the host MCU toolchain.
 Module for Microchip TCP/IP Stack

*******************************************************************************
 FileName:		ZGFSMStates.h
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
 SG					12/12/08
******************************************************************************/
#ifndef _ZGFSMSTATES_H_
#define _ZGFSMSTATES_H_

/* Different C runtime libraries seem to define NULL differently ways.  */
/* And the toolchains seem to expand NULL before or after macro expansions MACRO(A) */
/* So far I have seen null defined these ways (proprietary & standardized): */
/*  #define NULL             */
/*  #define NULL (0)         */
/*  #define NULL 0           */
/*  #define NULL (void *) 0  */
/*  #define NULL '\0'        */
/* To decouple these different definitions NULL from the link manager & link library code */
/* one definition of kNULL is used throughout the code.   The side effect is that each FSM   */
/* table burns one entry with doing nothing.   If you want to reduce the ROM (FSM table) code size, you could */
/* attempt to useone system definition of NULL, and the one that seems to universally work with ZGLib and LinkMgr is */
/* #define NULL 0 .   The use of kNULL is a tradeoff for portability and reduces issues with macro expansion */

enum tFSMValidStates
{
    kNULL=0,
    kSTIdle,
    kSTGetChipVersion,
    kSTEnThrottleTable,
    kSTSetConnLostCondition,
    kSTSetMacAddr,
    kSTGetMacAddr,
    kSTSetRegDom,
    kSTCalcPSK,
    kSTInstallWEPKey,
    kSTInstallPSK,
    kSTScan,
    kSTStart,
    kSTConnect,
    kSTJoin,
    kSTAuth,
    kSTAssoc,
    kSTMaintainConnect,
    kSTDisconnect,

    /* End of Valid States */
    ZG_MAX_STATES

};

enum tFSMStateStatus
{
    kFAILURE=0,
    kSUCCESS,
    kPENDING,
    kRETRY
};


#endif /* _ZGFSMSTATES_H_ */
