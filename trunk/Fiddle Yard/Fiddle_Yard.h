#ifndef _FIDDLE_YARD_H
#define _FIDDLE_YARD_H

#define Led4						PORTBbits.RB4	//Output
#define Led3						PORTBbits.RB3	//Output
#define Led2						PORTBbits.RB2	//Output
#define Led1						PORTBbits.RB1	//Output
#define Led5						PORTBbits.RB0	//Output
#define Output_Enable				PORTBbits.RB5	//Output
#define Pwm_Brake_TOP				PORTGbits.RG1	//Output
#define Pwm_Brake_BOTTOM			PORTCbits.RC1 	//Output
#define IO_Expander_Enable			PORTEbits.RE6	//Output
#define To_Externall_WDT_Pulse		PORTEbits.RE7	//Output

#define TOP 1
#define BOTTOM 0
#define True 1
#define False 0
#define On 1
#define Off 0

#if defined(EEPROM_CS_TRIS) || defined(SPIFLASH_CS_TRIS)
	void SaveAppConfig(void);
#else
	#define SaveAppConfig()
#endif
