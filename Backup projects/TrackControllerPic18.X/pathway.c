#include <xc.h>
#include "pathway.h"
#include "mcc_generated_files/mcc.h"
#include "main.h"
#include "milisecond_counter.h"

SIG*     temp1;
uint8_t tempSig1 = 0;


SIG     temp2;
uint8_t tempSig2 = 0;

void INITxPATHWAY(SIG *signal1, SIG *signal2, uint8_t path)
{
//    SETxMILLISECONDxUPDATExHANDLER2(updateSignal);
//    tempSig1 = *signal1;
//    tempSig2 = *signal2;
//    
//    temp1 = path;
//    temp2 = path;
    
    temp1 = (SIG*) malloc (sizeof(SIG));
    
    
}

void SETxSTATIONxPATHWAY(WS *pathway, SIG *signal, uint8_t *prevpath, uint8_t path)
{
    switch(path){
        case 1:
            *pathway->port1_ptr |=  pathway->pin1_mask;
            *pathway->port2_ptr &= ~pathway->pin2_mask;
            *pathway->port3_ptr |=  pathway->pin3_mask;
            *pathway->port4_ptr &= ~pathway->pin4_mask; 
            break;
            
        case 2:
            *pathway->port1_ptr &= ~pathway->pin1_mask;
            *pathway->port2_ptr |=  pathway->pin2_mask;
            *pathway->port3_ptr &= ~pathway->pin3_mask;
            *pathway->port4_ptr |=  pathway->pin4_mask;
            break;
            
        case 3:
            *pathway->port1_ptr &= ~pathway->pin1_mask;
            *pathway->port2_ptr &= ~pathway->pin2_mask;
            *pathway->port3_ptr &= ~pathway->pin3_mask;
            *pathway->port4_ptr &= ~pathway->pin4_mask;
            break;
 
        default:break;
    }
    
    if(*prevpath != path)
    {
        *prevpath = path;
        
    }
}




void updateSignal()
{
    
}