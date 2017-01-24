/*
 * File:   newmainXC16.c
 * Author: Jeremy Siebers
 *
 * Created on April 5, 2016, 8:38 PM
 */
#define FCY 60000000

#include "xc.h"
#include <stdlib.h>
#include <stdio.h>
#include <libpic30.h>
#include "main.h"
#include "Peripherals/config.h"

long int UpdateToPutty = 0;


int main(void) {
    // Initialize the device
    SYSTEMxInitialize(); 
    
    printf("\f");                                                               // Clear terminal (printf("\033[2J");)
    printf("dsPIC33EP512GM304 started up!!!\n\r");                              // Welcome message
             
    while(1)
    {
		PWMxReadxOccupiedxSignals();                                            // Update: Read all occupied signals
        PWMxSETxALLxAMP();                                                      // Update: All PWM H-bridge enable signals
        PWMxSETxALLxAMPxDIRECTIONS();                                           // Update: All PWM H-bridge direction signals
		
        UpdateToPutty++;
        
        if (UpdateToPutty > 0x30000)
        {
            UpdateToPutty = 0;    
            //Led1 ^= 1;
            
            printf("\f");                                                       //printf("\033[2J");
            
                        
            if (API[PWM1_OCCUPIED] == 1)
            {
                printf("Drive BLock 1A: Train\r\n");
            }
            else
            {
                printf("Drive BLock 1A: -----\r\n");
            }
            
            if (API[PWM2_OCCUPIED] == 1)
            {
                printf("Brake BLock 1B: Train\r\n");
            }
            else
            {
                printf("Brake BLock 1B: -----\r\n");
            }
            
            if (API[PWM3_OCCUPIED] == 1)
            {
                printf("Drive BLock 2A: Train\r\n");
            }
            else
            {
                printf("Drive BLock 2A: -----\r\n");
            }
            
            if (API[PWM4_OCCUPIED] == 1)
            {
                printf("Brake BLock 2B: Train\r\n");
            }
            else
            {
                printf("Brake BLock 2B: -----\r\n");
            }
            
            printf("PWM1 BLock 1A: %d\r\n",API[PWM1_SETPOINT]);
            printf("PWM2 BLock 1B: %d\r\n",API[PWM2_SETPOINT]);
            printf("PWM3 BLock 2A: %d\r\n",API[PWM3_SETPOINT]);
            printf("PWM4 BLock 2B: %d\r\n",API[PWM4_SETPOINT]);
            
            
            
            if (Led1 == 0){
                Led1 = 1;
            }
        } 
    }
}

void __attribute__((__interrupt__,no_auto_psv)) _U1TXInterrupt(void)
{    
    EUSART1xTransmitxISR();
    _U1TXIF = 0; // Clear TX Interrupt flag
}

void __attribute__((__interrupt__,no_auto_psv)) _U1RXInterrupt(void)
{
    EUSART1xReceivexISR();
    _U1RXIF = 0; // Clear TX Interrupt flag
}

void __attribute__ ( (__interrupt__, no_auto_psv) ) _SI2C1Interrupt( void )
{
    I2C1xISR();
    _SI2C1IF = 0; // Clear I2C1 Slave interrupt flag
}
