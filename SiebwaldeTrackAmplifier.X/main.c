/*
 * File:   newmainXC16.c
 * Author: Jeremy Siebers
 *
 * Created on April 5, 2016, 8:38 PM
 */
#define FCY 60000000

#include "xc.h"
#include "Peripherals/config.h"
#include <stdlib.h>
#include <stdio.h>
#include <libpic30.h>


int main(void) {
    // Initialize the device
    SYSTEM_Initialize();    
    
    printf("dsPIC33EP512GM304 started up!!!\n\r");
    
    while(1)
    {
        Led1 ^= 1;
        if (Led1 == 1)
        {
            printf("Led1 = 1\n\r");
        }
        else
        {
            printf("Led1 = 0\n\r");
        }
        __delay_ms(1000);
    }
}

void __attribute__((__interrupt__,no_auto_psv)) _U1TXInterrupt(void)
{
    IFS0bits.U1TXIF = 0; // Clear TX Interrupt flag
    EUSART1_Transmit_ISR();
}

void __attribute__((__interrupt__,no_auto_psv)) _U1RXInterrupt(void)
{
    IFS0bits.U1RXIF = 0; // Clear TX Interrupt flag
    EUSART1_Receive_ISR();
}