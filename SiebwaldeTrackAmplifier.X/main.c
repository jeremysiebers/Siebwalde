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

int DutyCycle[4] = {0,0,0,3};
char ReceivedNumber = 0;
int Counter = 3;

long int UpdateToPutty = 0;

int main(void) {
    // Initialize the device
    SYSTEM_Initialize();    
    printf("\f");                                                       //printf("\033[2J");
    printf("dsPIC33EP512GM304 started up!!!\n\r");
    
    while(1)
    {
        
        UpdateToPutty++;
        
        if (UpdateToPutty > 0xB0000)
        {
            UpdateToPutty = 0;            
            
            printf("\f");                                                       //printf("\033[2J");
            Led1 ^= 1;
            if (Led1 == 1)
            {
                printf("Led1 = Off\r\n");
            }
            else
            {
                printf("Led1 = On\r\n");
            }
            
            if (TrainPresent == 1)
            {
                printf("Train Present!\r\n");
            }
            else
            {
                printf("No Train Detected.\r\n");
            }
            
            printf("Soll : DutyCycle = %d%d%d%d\r\n", DutyCycle[3],DutyCycle[2],DutyCycle[1],DutyCycle[0]);
            printf("Ist  : DutyCycle = %d\r\n",PDC1);
        }
        
        
        if (EUSART1_DataReady > 0)
        {
            ReceivedNumber = EUSART1_Read();
            if (ReceivedNumber == 0xD)
            {
                PDC1 = DutyCycle[3] *1000 + DutyCycle[2] * 100 + DutyCycle[1] + DutyCycle[0];
                //SDC1 = PDC1;
                Counter = 3;
            }
            else if (ReceivedNumber == 0x77)
            {
                if (PDC1 < 5900)
                {
                    PDC1 += 100;
                }
            }
            else if (ReceivedNumber == 0x73)
            {
                if (PDC1 > 100)
                {
                    PDC1 -= 100;
                }                
            }
            else
            {
                if (Counter == -1)
                {
                    Counter = 3;
                }                 
                DutyCycle[Counter] = ReceivedNumber - '0';    
                Counter--;                
            }
        }     
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