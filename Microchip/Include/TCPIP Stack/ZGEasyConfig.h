/******************************************************************************

 ZG2100 Driver Easy Config functions
 Module for Microchip TCP/IP Stack

*******************************************************************************
 FileName:		ZGEasyConfig.h
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
 SCC				09/24/09
******************************************************************************/
#ifndef _ZGEASYCONFIG_H_
#define _ZGEASYCONFIG_H_

#include "HardwareProfile.h"
#include "TCPIP Stack/TCPIP.h"
#include "TCPIPConfig.h"

#include "TCPIP Stack/ZGDriverTypes.h"

typedef enum {                                          \
    net_infra = 0,                                      \
    net_adhoc,                                          \
    net_max                                             \
} tZGAppNetType;

typedef enum {                                          \
    sec_open = 0,                                       \
    sec_wpa_passphrase,                                 \
    sec_wpa_pskcalc,                                    \
    sec_wep64,                                          \
    sec_wep128,                                         \
    sec_max                                             \
} tZGAppSecurityType;

typedef enum {                                          \
    cfg_stopped = 0,                                    \
    cfg_wait_idle_complete,                             \
    cfg_max                                             \
} tZDynamicConfigStates;

typedef struct {
    BYTE isWifiNeedToConfigure;
    BYTE isWifiDoneConfigure;
    char ssid[33];  /* 32 for ssid and one for term character */
    tZGAppNetType type;  /* Net type infrastructure vs adhoc */
    tZGAppSecurityType security;
    BYTE  defaultWepKey;
    BYTE key[65];
    tZDynamicConfigStates cfg_state;
} tZGEasyConfigCtx;

extern tZGEasyConfigCtx g_easyConfigCtx;
#define CFGCXT	(g_easyConfigCtx)

/* Easy Config Public Functions */
tZGVoidReturn ZGEasyConfigInit(void);
tZGVoidReturn ZGEasyConfigMgr(void);

extern tZGVoidReturn
ZGUserScanMgr(tZGVoidInput);

extern
tZGVoidReturn  buildScanList(tZGBssDescPtr ptr);

tZGBool
startDynamicScan();

extern
tZGVoidReturn  DisplayScanList();

/* Macros */
#define ZG_WEP_KEY_INVALID     0xff

#if !defined (ZG_CONFIG_NO_WIFIMGRII)
#define  ZG_SCAN          WiFiScanRequest
#elif !defined (ZG_CONFIG_NO_ADHOCMGRII)
#define  ZG_SCAN          adHocScanRequest
#endif

#define ZG_START_EASY_CONFIG()   CFGCXT.isWifiNeedToConfigure = 1;
#endif /* _ZGEASYCONFIG_H_ */
