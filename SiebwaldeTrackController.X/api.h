/* 
 * File:   api.h
 * Author: Jeremy Siebers
 *
 * Created on January 20, 2017, 11:56 PM
 */

#ifndef API_H
#define	API_H

#ifdef	__cplusplus
extern "C" {
#endif

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
#define     API_SIZE            0                                               // Size of the API
#define     PWM_OUTPUT_MODE     10		                                        // PWM output mode, 0 is dual sided PWM, 1 is single sided PWM

#define     PWM1_SETPOINT       11                                              // PWM Speed setpoint
#define     PWM2_SETPOINT       12
#define     PWM3_SETPOINT       13
#define     PWM4_SETPOINT       14
#define     PWM5_SETPOINT       15
#define     PWM6_SETPOINT       16
#define     PWM7_SETPOINT       17
#define     PWM8_SETPOINT       18
#define     PWM9_SETPOINT       19
#define     PWM10_SETPOINT      10
#define     PWM11_SETPOINT      21
#define     PWM12_SETPOINT      22

/* Used only during single side PWM configuration, during double side PWM config, this ouput is connected to the PWM input on the LMD18200T and must then allways be high */
#define		PWM1_DIRECTION      31
#define		PWM2_DIRECTION      32
#define		PWM3_DIRECTION      33
#define		PWM4_DIRECTION      34
#define		PWM5_DIRECTION      35
#define		PWM6_DIRECTION      36
#define		PWM7_DIRECTION      37
#define		PWM8_DIRECTION      38
#define		PWM9_DIRECTION      39
#define		PWM10_DIRECTION		30
#define		PWM11_DIRECTION		31
#define		PWM12_DIRECTION		32
                                
                                
#define     PWM1_ENABLE    		41                                              // ENABLEble H-Brdige amplifier
#define     PWM2_ENABLE    		42
#define     PWM3_ENABLE    		43
#define     PWM4_ENABLE    		44
#define     PWM5_ENABLE    		45
#define     PWM6_ENABLE    		46
#define     PWM7_ENABLE    		47
#define     PWM8_ENABLE    		48
#define     PWM9_ENABLE    		49
#define     PWM10_ENABLE   		50
#define     PWM11_ENABLE   		51
#define     PWM12_ENABLE   		52

#define     PWM1_OCCUPIED    	61	                                              // Occupied signal per PWM output section (read continuousely from input OCC_1)
#define     PWM2_OCCUPIED    	62	
#define     PWM3_OCCUPIED    	63	
#define     PWM4_OCCUPIED    	64	
#define     PWM5_OCCUPIED    	65	
#define     PWM6_OCCUPIED    	66	
#define     PWM7_OCCUPIED    	67	
#define     PWM8_OCCUPIED    	68	
#define     PWM9_OCCUPIED    	69	
#define     PWM10_OCCUPIED   	70	
#define     PWM11_OCCUPIED   	71	
#define     PWM12_OCCUPIED   	72	

#define     PWM1_CURRENT        81
#define     PWM2_CURRENT        82
#define     PWM3_CURRENT        83
#define     PWM4_CURRENT        84
#define     PWM5_CURRENT        85
#define     PWM6_CURRENT        86
#define     PWM7_CURRENT        87
#define     PWM8_CURRENT        88
#define     PWM9_CURRENT        89
#define     PWM10_CURRENT       90
#define     PWM11_CURRENT       91
#define     PWM12_CURRENT       92


#ifdef	__cplusplus
}
#endif

#endif	/* API_H */

