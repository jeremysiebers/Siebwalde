/*
 * File:   main.c
 * Author: Jeremy
 *
 * Created on November 24, 2015, 9:46 PM
 */
#include <xc.h>
#include "Peripherals/config.h"

volatile uint8_t Update = 0;

const uint8_t ACK = 0xAA;
const uint8_t DONE = 0x55;

uint8_t Comm = 0;
uint8_t Address = 1, Data = 0, TimeOut = 0;

void main(void) {
    // Initialize the device
    SYSTEM_Initialize();

    
    PORTDbits.RD0 = On;                                                         // Release Slave micro controllers from reset.
    
//    PWM_LoadDutyValue(208);      //416 = 100%
    
    while(1)
    {
        if (Update){
            Update = 0;            
            TimeOut++;
        }
        
        switch (Comm){
            case 0 :
                TXSTAbits.TX9  = 1; // Selects 9-bit transmission
                TXSTAbits.TX9D = 1; // 9th bit of transmit data
                ausart_Write(Address);
                Comm = 1;
                break;
                
            case 1 :
                if (ausart_DataReady > 0){
                    if (ausart_Read() == ACK){
                        TXSTAbits.TX9  = 0; // Selects 8-bit transmission
                        ausart_Write(Data);
                        Comm = 2;
                    }
                }
                else if (TimeOut > 2){
                    TimeOut = 0;
                    Comm = 0;
                    Address++;
                    if (Address > 5){
                        Data = !Data;
                        Address = 1;                        
                    }                    
                }
                break;
                
            case 2 :
                if (ausart_DataReady > 0){
                    if (ausart_Read() == ACK){
                        ausart_Write(DONE);
                        Comm = 3;
                    }
                }
                break;
                
            case 3 :
                if (ausart_DataReady > 0){
                    if (ausart_Read() == ACK){
                        Address++;
                        if (Address > 5){
                            Data = !Data;
                            Address = 1;
                            Comm = 0;
                        }
                    }
                }
                break;
                
            default : break;
        }
    }
}

