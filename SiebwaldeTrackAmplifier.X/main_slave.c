/*
 * File:   newmainXC16.c
 * Author: Jeremy Siebers
 *
 * Created on April 5, 2016, 8:38 PM
 */
#define THIS_IS_TRACK_AMPLIFIER
#define FCY 60000000

#include "xc.h"
#include <stdlib.h>
#include <stdio.h>
#include <libpic30.h>
#include <timer.h>
#include <stdbool.h>
#include "main.h"
#include "Peripherals/config.h"
#include "api.h"

bool UpdateToPutty = false;


int main(void) {
    // Initialize the device
    SYSTEMxInitialize(); 
    
    Led1 = 1;                                                                   // Led off (low active)
        
    //printf("dsPIC33EP512GM304 started up!!!\n\r");                              // Welcome message
             
    while(1)
    {		
        if (UpdateToPutty == true)
        {            
            UpdateToPutty = false;    
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

            printf("PWM1 BLock 1A: %d, PWM1 Setpoint: %d\r\n",PDC1,API[PWM1_SETPOINT] * 24);
            printf("PWM1 BLock 1B: %d, PWM1 Setpoint: %d\r\n",PDC1,API[PWM1_SETPOINT] * 24);
            printf("PWM2 BLock 2A: %d, PWM2 Setpoint: %d\r\n",SDC1,API[PWM2_SETPOINT] * 24);
            printf("PWM2 BLock 2B: %d, PWM2 Setpoint: %d\r\n",SDC1,API[PWM2_SETPOINT] * 24);
               
        } 
    }
}

void __attribute__((__interrupt__,no_auto_psv)) _T1Interrupt(void)
{
    Led1 = 1;    
    T1CONbits.TON = 0;
    WriteTimer1(0);
    IFS0bits.T1IF = 0;  /* Clear Timer interrupt flag */
}

void __attribute__((__interrupt__,no_auto_psv)) _T2Interrupt(void)
{
    //Led1 ^= 1;
    PWMxReadxTFlag();                                                           // Update: Read all Thermal Flags
    PWMxReadxOccupiedxSignals();                                                // Update: Read all occupied signals
    PWMxSETxALLxAMP();                                                          // Update: All PWM H-bridge enable signals
    PWMxSETxALLxAMPxDIRECTIONS();                                               // Update: All PWM H-bridge direction signals
    PWMxPROCESSxSETPOINTS();                                                    // Update: All PWM new setpoints
    WriteTimer2(0);
    IFS0bits.T2IF = 0;  /* Clear Timer interrupt flag */    
}

void __attribute__((__interrupt__,no_auto_psv)) _T3Interrupt(void)
{  
    UpdateToPutty = true;
    WriteTimer3(0);    
    IFS0bits.T3IF = 0;  /* Clear Timer interrupt flag */     
}

void __attribute__((__interrupt__,no_auto_psv)) _T4Interrupt(void)
{  
    I2C1xReleasexClock();                                                       // Release clock on I2C
    PWMxSTOP();                                                                 // Watchdog timer triggered, timeout on receiving C (synC) broadcast stop all PWM    
    //T4CONbits.TON = 0;
    WriteTimer4(0);
    IFS1bits.T4IF = 0;  /* Clear Timer interrupt flag */    
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