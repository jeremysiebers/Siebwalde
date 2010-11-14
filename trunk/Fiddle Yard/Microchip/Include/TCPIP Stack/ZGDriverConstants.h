/******************************************************************************

 ZG2100 Driver defined Constants header file.
 Module for Microchip TCP/IP Stack

*******************************************************************************
 FileName:		ZGDriverConstants.h
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
#ifndef _ZGDRIVERCONSTANTS_H_
#define _ZGDRIVERCONSTANTS_H_

/* if NULL is not already defined by the system it is defined here
 * for future use. */
#ifndef NULL
#define NULL (0)
#endif

/* Because some systems require different types for ROM vs
 * RAM memory pointers there are several NULL types below
 * that must be defined when the driver is ported. */
#define kZGDataPtrNULL  ((tZGDataPtr)NULL)
#define kZGU8PtrNULL    ((tZGU8Ptr)NULL)
#define kZGTextPtrNULL  ((tZGTextPtr)NULL)
/* The driver uses the following definitions for testing
 * boolean state. */
#define kZGBoolTrue     ((tZGBool)1u)
#define kZGBoolFalse    ((tZGBool)0u)
/* the driver uses the following definitions for testing
 * the signal state. */
#define kZGSignalExit   ((tZGSignal)0)
#define kZGSignalContinue ((tZGSignal)1)

//#define ROM rom

/* tZGReturnStatus - The return value returned by most of the
 *  Driver API functions. */
typedef enum
{
    kZGFailure = 0,
    kZGSuccess = 1
}tZGReturnStatus;


#endif /* _ZGDRIVERCONSTANTS_H_ */
