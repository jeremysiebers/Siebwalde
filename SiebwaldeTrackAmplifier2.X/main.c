/*
 * File:   main.c
 * Author: Jeremy
 *
 * Created on November 24, 2015, 9:46 PM
 */
#include <xc.h>
#include "Peripherals/config.h"
#include <stdlib.h>
#include <stdio.h>

const uint8_t ADDRESS = 0x3;
const uint8_t DONE = 0x55;
const uint8_t ACK = 0xAA;

uint8_t Comm = 0;
uint8_t Data_Tmp = 0;

void main(void) {
    // Initialize the device
    SYSTEM_Initialize();

    PORTCbits.RC1 = 0;
    PORTCbits.RC3 = 0;
    
    PWM_LoadDutyValue(208);      //416 = 100%
    
    while(1)
    {
        PORTCbits.RC1 = !PORTCbits.RC1;
        
        
        switch (Comm){
            case 0 : 
                if (ausart_AddressDetect > 0){
                    ausart_AddressDetect = 0;
                    if (ausart_DataReady > 0){
                        if (ausart_Read() == ADDRESS){
                             Comm = 1;
                        }
                        else{
                            RCSTAbits.ADDEN = 1;
                        }
                    }
                }
                break;
                
            case 1 :
                ausart_Write(ACK);
                Comm = 2;
                break;
                
            case 2 :
                if (ausart_DataReady > 0){
                    Data_Tmp = ausart_Read();
                    if (Data_Tmp == DONE)
                    {
                        ausart_Write(ACK);
                        Comm = 0;
                        RCSTAbits.ADDEN = 1;
                    }
                    else{
                        PORTCbits.RC3 = Data_Tmp;
                        ausart_Write(ACK);
                    }
                }
                break;
                
            default : break;
        }
        
        
            
        
        
        
        
        
    }
}
