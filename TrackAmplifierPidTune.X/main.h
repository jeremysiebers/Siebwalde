/* 
 * File:   main.h
 * Author: Jeremy Siebers
 *
 * Created on June 27, 2018, 9:49 PM
 */

#ifndef MAIN_H
#define	MAIN_H

#ifdef	__cplusplus
extern "C" {
#endif

#define _XTAL_FREQ 32000000                                                     // Fosc  frequency for _delay()  library

void Led_Blink (void);

#define NUMBER_OF_HOLDING_PETITREGISTERS                 4                      // Petit Modbus RTU Slave Holding Registers (read/write), Have to put a number of registers here It has to be bigger than 0 (zero)!!
#define NUMBER_OF_INPUT_PETITREGISTERS                   6                      // Number of (read only) input registers)
#define NUMBER_OF_DIAGNOSTIC_PETITREGISTERS              2                      // Number of diagnostic registers (send/receive counters)

typedef struct{
            unsigned int                     ActValue;
        }PetitRegStructure;
        
PetitRegStructure   PetitHoldingRegisters[NUMBER_OF_HOLDING_PETITREGISTERS];
PetitRegStructure   PetitInputRegisters[NUMBER_OF_INPUT_PETITREGISTERS];
PetitRegStructure   PetitDiagnosticRegisters[NUMBER_OF_DIAGNOSTIC_PETITREGISTERS];

extern unsigned int LED_ERR, LED_WAR, LED_TX, LED_RX;

#ifdef	__cplusplus
}
#endif

#endif	/* MAIN_H */

