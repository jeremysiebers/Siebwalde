/*
 * File:   newmain.c
 * Author: Jeremy Siebers
 *
 * Created on April 16, 2018, 10:06 PM
 */


#include <xc.h>
#include "mcc_generated_files/mcc.h"

void main(void) {
    
    SYSTEM_Initialize();
    
    INTCONbits.GIE = 1;
    INTCONbits.PEIE = 1;
    
    while(1){
        
    }
}
