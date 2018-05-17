/*
 * File:   newmain.c
 * Author: Jeremy Siebers
 *
 * Created on April 16, 2018, 10:06 PM
 */


#include <xc.h>
#include "mcc_generated_files/mcc.h"
#include "modbus/General.h"

#define MODBUSxADDRESS 1

void main(void) {
    
    SYSTEM_Initialize();
    
    INTCONbits.GIE = 1;
    INTCONbits.PEIE = 1;
    
    InitPetitModbus(MODBUSxADDRESS);
    
    while(1){
        ProcessPetitModbus();
        LED_WAR_LAT = ((unsigned int)PetitRegisters[0].ActValue);
    }
}
