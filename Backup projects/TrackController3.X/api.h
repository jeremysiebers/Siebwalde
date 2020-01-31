/* 
 * File:   
 * Author: 
 * Comments:
 * Revision history: 
 */
 
#ifndef XC_HEADER_TEMPLATE_H
#define	XC_HEADER_TEMPLATE_H
/*
extern void APIxInitialize(void);
extern unsigned char GETxAPIxRW(unsigned char index);
extern uint8_t API[];
*/

#define APISIZE 150
#define RO	0
#define RW	1
#define DSPWM 0
#define SSPWM 1

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
#define     WATCHDOG            1                                               // Variable in API will be written and read by master to verify communication on both sides
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

#define     PWM1_GAIN	        101                                              // PWM Speed setpoint
#define     PWM2_GAIN	        102
#define     PWM3_GAIN	        103
#define     PWM4_GAIN	        104
#define     PWM5_GAIN	        105
#define     PWM6_GAIN	        106
#define     PWM7_GAIN	        107
#define     PWM8_GAIN	        108
#define     PWM9_GAIN	        109
#define     PWM10_GAIN      	110
#define     PWM11_GAIN      	111
#define     PWM12_GAIN      	112

#define     PWM1_TFLAG	        121                                              // H-Bridge Thermal flag output
#define     PWM2_TFLAG	        122
#define     PWM3_TFLAG	        123
#define     PWM4_TFLAG	        124
#define     PWM5_TFLAG	        125
#define     PWM6_TFLAG	        126
#define     PWM7_TFLAG	        127
#define     PWM8_TFLAG	        128
#define     PWM9_TFLAG	        129
#define     PWM10_TFLAG      	130
#define     PWM11_TFLAG      	131
#define     PWM12_TFLAG      	132

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
 * Overview:        Here all I/o is specified that is put into the API
 *****************************************************************************/
/*
#define OCCUPIED_1           	PORTBbits.RB7                                   // OCCUPIEDupied signal input
#define OCCUPIED_2           	PORTBbits.RB8
#define OCCUPIED_3           	PORTCbits.RC3
#define OCCUPIED_4           	PORTAbits.RA9
#define OCCUPIED_5           	0 //define here I/O ports to be read
#define OCCUPIED_6           	0
#define OCCUPIED_7           	0
#define OCCUPIED_8           	0
#define OCCUPIED_9           	0
#define OCCUPIED_10          	0
#define OCCUPIED_11          	0
#define OCCUPIED_12          	0


#define AMP1_ENABLE 			LATCbits.LATC6                                // Amplifier anable output (BRAKE INPUT ON LMD18200T)
#define AMP2_ENABLE 			LATCbits.LATC7
#define AMP3_ENABLE 			0
#define AMP4_ENABLE 			0
#define AMP5_ENABLE 			0
#define AMP6_ENABLE 			0
#define AMP7_ENABLE 			0
#define AMP8_ENABLE 			0
#define AMP9_ENABLE 			0
#define AMP10_ENABLE			0
#define AMP11_ENABLE			0
#define AMP12_ENABLE			0

#define AMP1_TFLAG              PORTCbits.PORTC8                                // Amplifier 1 Thermal flag input (THERMAL FLAG OUTPUT ON LMD18200T)
#define AMP2_TFLAG              PORTCbits.PORTC9
#define AMP3_TFLAG              0
#define AMP4_TFLAG              0
#define AMP5_TFLAG              0
#define AMP6_TFLAG              0
#define AMP7_TFLAG              0
#define AMP8_TFLAG              0
#define AMP9_TFLAG              0
#define AMP10_TFLAG             0
#define AMP11_TFLAG             0
#define AMP12_TFLAG             0
*/
/* Used only during single side PWM configuration, during double side PWM config, this ouput is connected to the PWM input on the LMD18200T and must then allways be high */
/*
#define AMP1_DIRECTION			LATBbits.LATB10                                 // Amplifier 1 Direction output (DIRECTION INPUT ON LMD18200T)
#define AMP2_DIRECTION			LATBbits.LATB11
#define AMP3_DIRECTION          0
#define AMP4_DIRECTION          0
#define AMP5_DIRECTION          0
#define AMP6_DIRECTION          0
#define AMP7_DIRECTION          0
#define AMP8_DIRECTION          0
#define AMP9_DIRECTION          0
#define AMP10_DIRECTION         0
#define AMP11_DIRECTION         0
#define AMP12_DIRECTION         0
*/
#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

    // TODO If C++ is being used, regular C code needs function names to have C 
    // linkage so the functions can be used by the c code. 

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */