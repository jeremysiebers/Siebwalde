/******************************************************************************

 ZG2100 Driver Application Access Definition Header file.
 Module for Microchip TCP/IP Stack

*******************************************************************************
 FileName:		ZGDriverAccessApp.h
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
 Zero G				Sep 2008	Initial version
 KO					31 Oct 2008	Port to PIC24F and PIC32 for TCP/IP stack v4.52
******************************************************************************/
#ifndef _ZGDRIVERACCESSAPP_H_
#define _ZGDRIVERACCESSAPP_H_

#include "TCPIP Stack/ZGDriverTypes.h"
#include "TCPIP Stack/ZGDriverConstants.h"
#include "TCPIP Stack/ZGAccessApp.h"
#include "TCPIP Stack/ZGLibIface.h"


/* ZGAPP_HANDLE_MGMTCONFIRM - used by the ZG Driver to pass management confirm
 * messages to the application.
 * type - identifies the type of Management Confirm message. Must be one of kZGMgtCnf...
 *          found in ZGDriverIfaceApp.h.
 */

/* ZGAPP_HANDLE_MGMTINDICATE - used by the ZG Driver to pass management indicate
 * announcements to the application. */

#if defined ( ZG_CONFIG_LIBRARY )

#define ZGAPP_HANDLE_MGMTCONFIRM(type)         ZGLibConfirm((tZGU8)type)
#define ZGAPP_HANDLE_MGMTINDICATE(type)        ZGLibIndicate((tZGU8)type)

#else

#define ZGAPP_HANDLE_MGMTCONFIRM(type)
#define ZGAPP_HANDLE_MGMTINDICATE(type)

#endif

#endif /*_ZGDRIVERACCESSAPP_H_ */

