#ifndef _FIDDLE_YARD_H
#define _FIDDLE_YARD_H


#define Led3						PORTBbits.RB2	//Output
#define Led2						PORTAbits.RA4	//Output
#define Led1						PORTAbits.RA5	//Output
#define Output_Enable				PORTBbits.RB4	//Output
#define Pwm_Brake_TOP   			PORTGbits.RG2	//Output
#define Pwm_Brake_BOTTOM			PORTCbits.RC0 	//Output
#define IO_Expander_Enable			PORTBbits.RB3	//Output
#define To_Externall_WDT_Pulse		PORTBbits.RB5	//Output

#define TOP 1
#define BOTTOM 0
#define True 1
#define False 0
#define On 1
#define Off 0
#define True 1
#define False 0

#if defined(EEPROM_CS_TRIS) || defined(SPIFLASH_CS_TRIS)
	void SaveAppConfig(void);
#else
	#define SaveAppConfig()
#endif
