/*
 * File:   newmainXC16.c
 * Author: Jeremy Siebers
 *
 * Created on April 5, 2016, 8:38 PM
 */
#define FCY 60000000

#include "xc.h"
#include "main.h"
#include "Peripherals/config.h"
#include <stdlib.h>
#include <stdio.h>
#include <libpic30.h>


int DutyCycle[4] = {0,0,0,3};
char ReceivedNumber = 0;
int Counter = 3;

long int UpdateToPutty = 0;

int I2C1InterruprEnabled = 0, I2C1Address = 0xFFFF, I2C1Mask = 0xFFFF, I2C1Stop = 1, I2C1Start = 1, I2C1ACKstat = 1, I2C1Bcl = 1, I2C1Wcol = 1,I2C1Cov = 1,
        I2C1Received = 1, I2C1RW = 1, I2C1AD = 1, I2C1BaudRatGen = 0xFFFF, TestStorage = 0;

int main(void) {
    // Initialize the device
    SYSTEM_Initialize();  
    
    unsigned int i;
    for(i = 0; i < 256; i++ )
    {
        API[i] = 0;                       //Initlize API with 0

        //in case MasterI2C device wants to read
        //before it writes to it.
    }
    
    printf("\f");                                                       //printf("\033[2J");
    //printf("x\r\n");
    printf("dsPIC33EP512GM304 started up!!!\n\r");
    
    SDC1 = PDC1 = 3000;
         
    while(1)
    {
        
        /*
        if (I2C1STATbits.RBF) {
            printf("RBF is set...\r\n");
        }
        
        if (IFS1bits.SI2C1IF) {
            printf("SI2C1IF is set...\r\n");
        }
        
        if (IEC1bits.SI2C1IE && !I2C1InterruprEnabled)
        {
            printf("SI2C1IE is set...\r\n");
            I2C1InterruprEnabled = 1;
        }
        else if(!IEC1bits.SI2C1IE && I2C1InterruprEnabled){
            printf("SI2C1IE is NOT set...\r\n");
            I2C1InterruprEnabled = 0;
        }
        
        if (I2C1Address != I2C1ADD){
            printf("I2C1 Address is %X\r\n",I2C1ADD);
            I2C1Address = I2C1ADD;
        }
        
        if (I2C1Mask != I2C1MSK){
            printf("I2C1 Mask is %X\r\n",I2C1MSK);
            I2C1Mask = I2C1MSK;
        }
        
        if (I2C1Received != I2C1RCV){
            printf("I2C1 I2C1RCV is %X\r\n",I2C1RCV);
            I2C1Received = I2C1RCV;
        }
            
        if(I2C1RW != I2C1STATbits.R_W){
            printf("I2C1 RW is %X\r\n",I2C1STATbits.R_W);
            I2C1RW = I2C1STATbits.R_W;
        }
        
        if(I2C1AD != I2C1STATbits.D_A){
            printf("I2C1 DA is %X\r\n",I2C1STATbits.D_A);
            I2C1AD = I2C1STATbits.D_A;
        }
        
        if(I2C1BaudRatGen != I2C1BRG){
            printf("I2C1 I2C1BRG is %d\r\n",I2C1BRG);
            I2C1BaudRatGen = I2C1BRG;
        }
        
        if(I2C1Stop != I2C1STATbits.P){
            //printf("I2C1 STOP is %X\r\n",I2C1STATbits.P);
            I2C1Stop = I2C1STATbits.P;
        }
        
        if(I2C1Start != I2C1STATbits.S){
            //printf("I2C1 START is %X\r\n",I2C1STATbits.S);
            I2C1Start = I2C1STATbits.S;
        }
        
        if(I2C1ACKstat != I2C1STATbits.ACKSTAT){
            printf("I2C1 ACKSTAT is %X\r\n",I2C1STATbits.ACKSTAT);
            I2C1ACKstat = I2C1STATbits.ACKSTAT;
        }
       
        if(I2C1Bcl != I2C1STATbits.BCL){
            printf("I2C1 BCL is %X\r\n",I2C1STATbits.BCL);
            I2C1Bcl = I2C1STATbits.BCL;
        }
        
        if(I2C1Wcol != I2C1STATbits.IWCOL){
            printf("I2C1 IWCOL is %X\r\n",I2C1STATbits.IWCOL);
            I2C1Wcol = I2C1STATbits.IWCOL;
        }
        
        if(I2C1Cov != I2C1STATbits.I2COV){
            printf("I2C1 COV is %X\r\n",I2C1STATbits.I2COV);
            I2C1Cov = I2C1STATbits.I2COV;
        }    
        */
        
        if(TestStorage != API[1]){            
            TestStorage = API[1];                
        }
        
        PDC1 = SDC1;
        
        UpdateToPutty++;
        
        if (UpdateToPutty > 0xA0000)
        {
            UpdateToPutty = 0;    
            Led1 ^= 1;
            
            printf("\f");                                                       //printf("\033[2J");
            
                        
            if (TPDriveBLok_1A == 0)
            {
                printf("Drive BLock 1A: Train\r\n");
            }
            else
            {
                printf("Drive BLock 1A: -----\r\n");
            }
            
            if (TPBrakeBLok_1B == 0)
            {
                printf("Brake BLock 1B: Train\r\n");
            }
            else
            {
                printf("Brake BLock 1B: -----\r\n");
            }
            
            if (TPDriveBLok_2A == 0)
            {
                printf("Drive BLock 2A: Train\r\n");
            }
            else
            {
                printf("Drive BLock 2A: -----\r\n");
            }
            
            if (TPBrakeBLok_2B == 0)
            {
                printf("Brake BLock 2B: Train\r\n");
            }
            else
            {
                printf("Brake BLock 2B: -----\r\n");
            }
            
            printf("Soll : DutyCycle = %d%d%d%d\r\n", DutyCycle[3],DutyCycle[2],DutyCycle[1],DutyCycle[0]);
            printf("Ist  : DutyCycle = %d\r\n",SDC1); 
                        
            printf("TestStorage is %d\r\n",TestStorage);
        }
        
        
        if (EUSART1_DataReady > 0)
        {
            ReceivedNumber = EUSART1_Read();
            if (ReceivedNumber == 0xD)
            {
                SDC1 = DutyCycle[3] *1000 + DutyCycle[2] * 100 + DutyCycle[1] + DutyCycle[0];
                //SDC1 = PDC1;
                Counter = 3;
            }
            else if (ReceivedNumber == 0x77)
            {
                if (SDC1 < 5900)
                {
                    SDC1 += 100;
                }
            }
            else if (ReceivedNumber == 0x73)
            {
                if (SDC1 > 100)
                {
                    SDC1 -= 100;
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
    EUSART1_Transmit_ISR();
    _U1TXIF = 0; // Clear TX Interrupt flag
}

void __attribute__((__interrupt__,no_auto_psv)) _U1RXInterrupt(void)
{
    EUSART1_Receive_ISR();
    _U1RXIF = 0; // Clear TX Interrupt flag
}

void __attribute__ ( (__interrupt__, no_auto_psv) ) _SI2C1Interrupt( void )
{
    I2C1_ISR();
    _SI2C1IF = 0;        // Clear I2C1 Slave interrupt flag
}
