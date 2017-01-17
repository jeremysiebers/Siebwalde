/* 
 * File:   
 * Author: 
 * Comments:
 * Revision history: 
 */
 
#ifndef XC_HEADER_TEMPLATE_H
#define	XC_HEADER_TEMPLATE_H

#include <xc.h> // include processor files - each processor file is guarded.  
#include "Peripherals/config.h"

/******************************************************************************
 * Function:        API
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        Here all API addresses are declared
 *****************************************************************************/

#define     PWM1        11
#define     PWM2        12
#define     PWM3        13
#define     PWM4        14
#define     PWM5        15
#define     PWM6        16
#define     PWM7        17
#define     PWM8        18
#define     PWM9        19
#define     PWM10       10
#define     PWM11       21
#define     PWM12       22

#define     PWM1_ENA    31
#define     PWM2_ENA    32
#define     PWM3_ENA    33
#define     PWM4_ENA    34
#define     PWM5_ENA    35
#define     PWM6_ENA    36
#define     PWM7_ENA    37
#define     PWM8_ENA    38
#define     PWM9_ENA    39
#define     PWM10_ENA   30
#define     PWM11_ENA   31
#define     PWM12_ENA   32

#define     PWM1_OCC    41
#define     PWM2_OCC    42
#define     PWM3_OCC    43
#define     PWM4_OCC    44
#define     PWM5_OCC    45
#define     PWM6_OCC    46
#define     PWM7_OCC    47
#define     PWM8_OCC    48
#define     PWM9_OCC    49
#define     PWM10_OCC   50
#define     PWM11_OCC   51
#define     PWM12_OCC   52

/******************************************************************************
 * Function:        I/O to API 
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        Here all I/o that is put into API is specified
 *****************************************************************************/

#define OCC_1           PORTBbits.RB7
#define OCC_2           PORTBbits.RB8
#define OCC_3           PORTCbits.RC3
#define OCC_4           PORTAbits.RA9
#define OCC_5           0 //define here I/O ports to be read
#define OCC_6           0
#define OCC_7           0
#define OCC_8           0
#define OCC_9           0
#define OCC_10          0
#define OCC_11          0
#define OCC_12          0

#define AMP_1 			0 //define here I/O Lats to be written
#define AMP_2 			0
#define AMP_3 			0
#define AMP_4 			0
#define AMP_5 			0
#define AMP_6 			0
#define AMP_7 			0
#define AMP_8 			0
#define AMP_9 			0
#define AMP_10			0
#define AMP_11			0
#define AMP_12			0



#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

    // TODO If C++ is being used, regular C code needs function names to have C 
    // linkage so the functions can be used by the c code. 

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

/*
 * PDC1 = ( unsigned char )API[10];
    SDC1 = ( unsigned char )API[11];
    PDC2 = ( unsigned char )API[12];
    SDC2 = ( unsigned char )API[13];
    PDC3 = ( unsigned char )API[14];
    SDC3 = ( unsigned char )API[15];
    PDC4 = ( unsigned char )API[16];
    SDC4 = ( unsigned char )API[17];
    PDC5 = ( unsigned char )API[18];
    SDC5 = ( unsigned char )API[19];
    PDC6 = ( unsigned char )API[20];
    SDC6 = ( unsigned char )API[21];
 */