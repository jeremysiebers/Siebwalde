/**
  Generated Main Source File

  Company:
    Microchip Technology Inc.

  File Name:
    main.c

  Summary:
    This is the main file generated using PIC10 / PIC12 / PIC16 / PIC18 MCUs

  Description:
    This header file provides implementations for driver APIs for all modules selected in the GUI.
    Generation Information :
        Product Revision  :  PIC10 / PIC12 / PIC16 / PIC18 MCUs - 1.65.2
        Device            :  PIC18F97J60
        Driver Version    :  2.00
*/

/*
    (c) 2018 Microchip Technology Inc. and its subsidiaries. 
    
    Subject to your compliance with these terms, you may use Microchip software and any 
    derivatives exclusively with Microchip products. It is your responsibility to comply with third party 
    license terms applicable to your use of third party software (including open source software) that 
    may accompany Microchip software.
    
    THIS SOFTWARE IS SUPPLIED BY MICROCHIP "AS IS". NO WARRANTIES, WHETHER 
    EXPRESS, IMPLIED OR STATUTORY, APPLY TO THIS SOFTWARE, INCLUDING ANY 
    IMPLIED WARRANTIES OF NON-INFRINGEMENT, MERCHANTABILITY, AND FITNESS 
    FOR A PARTICULAR PURPOSE.
    
    IN NO EVENT WILL MICROCHIP BE LIABLE FOR ANY INDIRECT, SPECIAL, PUNITIVE, 
    INCIDENTAL OR CONSEQUENTIAL LOSS, DAMAGE, COST OR EXPENSE OF ANY KIND 
    WHATSOEVER RELATED TO THE SOFTWARE, HOWEVER CAUSED, EVEN IF MICROCHIP 
    HAS BEEN ADVISED OF THE POSSIBILITY OR THE DAMAGES ARE FORESEEABLE. TO 
    THE FULLEST EXTENT ALLOWED BY LAW, MICROCHIP'S TOTAL LIABILITY ON ALL 
    CLAIMS IN ANY WAY RELATED TO THIS SOFTWARE WILL NOT EXCEED THE AMOUNT 
    OF FEES, IF ANY, THAT YOU HAVE PAID DIRECTLY TO MICROCHIP FOR THIS 
    SOFTWARE.
*/
#include "main.h"
#include "mcc_generated_files/mcc.h"
#include "spicommhandler.h"

void WriteText(unsigned char *Text, unsigned int Ln, unsigned int Col);
void WriteData(unsigned int Data, unsigned int Ln, unsigned int Col);

/*
                         Main application
 */

/*----------------------------------------------------------------------------*/
static SLAVE_INFO         SlaveInfo[NUMBER_OF_SLAVES];   

/*----------------------------------------------------------------------------*/

unsigned char UPDATExTERMINAL;
unsigned char terminal = 0;

void main(void)
{
    // Initialize the SLAVE_INFO struct with slave numbers
    for (char i = 0; i <NUMBER_OF_SLAVES; i++){
        SlaveInfo[i].SlaveNumber = i;
    }
    
    // Initialize the device
    SYSTEM_Initialize();
    
    ModbusReset_LAT = 1;                                                        // Hold the ModbusMaster in reset
    
    InitSlaveCommunication(SlaveInfo);
    
    // If using interrupts in PIC18 High/Low Priority Mode you need to enable the Global High and Low Interrupts
    // If using interrupts in PIC Mid-Range Compatibility Mode you need to enable the Global and Peripheral Interrupts
    // Use the following macros to:

    // Enable the Global Interrupts
    INTERRUPT_GlobalInterruptEnable();

    // Disable the Global Interrupts
    //INTERRUPT_GlobalInterruptDisable();

    // Disable high priority global interrupts
    //INTERRUPT_GlobalInterruptHighDisable();

    // Disable low priority global interrupts.
    //INTERRUPT_GlobalInterruptLowDisable();

    // Enable the Peripheral Interrupts
    INTERRUPT_PeripheralInterruptEnable();

    // Disable the Peripheral Interrupts
    //INTERRUPT_PeripheralInterruptDisable();
    
    /*
    //__delay_ms(1000);
    printf("\033[2J");
    __delay_ms(100);
    printf("PIC18f97j60 started up!!!\n\r");                                    // Welcome message
    __delay_ms(1000);*/
    
    
    printf("PIC18f97j60 started up!!!\n\r");
    printf("\f");                                                               // Clear terminal (printf("\033[2J");)
    printf("PIC18f97j60 started up!!!\n\r");                                    // Welcome message
    
    ModbusReset_LAT = 0;                                                        // as last release the ModbusMaster.
    
    
    while (1)
    {
        ProcessSpiData();
                
        if(UPDATExTERMINAL){
                        
            switch(terminal){
                case 0:
                    WriteText((char *)"Slave0", 2, 0 );
                    WriteText((char *)"Slave1", 2, 30);
                    WriteText((char *)"Slave2", 2, 60);
                    WriteText((char *)"Slave3", 2, 90);                    
                    terminal++;
                    break;                   
                
                case 1:
                    WriteText((char *)"HoldingReg", 3, 0 );
                    WriteText((char *)"HoldingReg", 3, 30);
                    WriteText((char *)"HoldingReg", 3, 60);
                    WriteText((char *)"HoldingReg", 3, 90);                    
                    terminal++;
                    break;
                    
                case 2:
                    WriteText((char *)"InputReg", 4, 0 );
                    WriteText((char *)"InputReg", 4, 30);
                    WriteText((char *)"InputReg", 4, 60);
                    WriteText((char *)"InputReg", 4, 90);                    
                    terminal++;
                    break;
                    
                case 3:
                    WriteText((char *)"DiagReg", 5, 0 );
                    WriteText((char *)"DiagReg", 5, 30);
                    WriteText((char *)"DiagReg", 5, 60);
                    WriteText((char *)"DiagReg", 5, 90);                    
                    terminal++;
                    break;
                    
                case 4:
                    WriteText((char *)"MbReceiveCounter", 6, 0 );
                    WriteText((char *)"MbReceiveCounter", 6, 30);
                    WriteText((char *)"MbReceiveCounter", 6, 60);
                    WriteText((char *)"MbReceiveCounter", 6, 90);                    
                    terminal++;
                    break;
                    
                case 5:
                    WriteText((char *)"MbSentCounter", 7, 0 );
                    WriteText((char *)"MbSentCounter", 7, 30);
                    WriteText((char *)"MbSentCounter", 7, 60);
                    WriteText((char *)"MbSentCounter", 7, 90);                    
                    terminal++;
                    break;
                    
                case 6:
                    WriteText((char *)"MbCommError", 8, 0 );
                    WriteText((char *)"MbCommError", 8, 30);
                    WriteText((char *)"MbCommError", 8, 60);
                    WriteText((char *)"MbCommError", 8, 90);                    
                    terminal++;
                    break;
                    
                case 7:
                    WriteText((char *)"MbExceptionCode", 9, 0 );
                    WriteText((char *)"MbExceptionCode", 9, 30);
                    WriteText((char *)"MbExceptionCode", 9, 60);
                    WriteText((char *)"MbExceptionCode", 9, 90);                    
                    terminal++;
                    break;
                    
                case 8:
                    WriteData(SlaveInfo[0].MbReceiveCounter, 6, 20 );
                    WriteData(SlaveInfo[1].MbReceiveCounter, 6, 50 );
                    WriteData(SlaveInfo[2].MbReceiveCounter, 6, 80 );
                    WriteData(SlaveInfo[3].MbReceiveCounter, 6, 110);                     
                    terminal++;
                    break;
                    
                case 9:
                    WriteData(SlaveInfo[0].MbSentCounter, 7, 20 );
                    WriteData(SlaveInfo[1].MbSentCounter, 7, 50 );
                    WriteData(SlaveInfo[2].MbSentCounter, 7, 80 );
                    WriteData(SlaveInfo[3].MbSentCounter, 7, 110);                     
                    terminal ++;
                    break;
                    
                case 10:
                    WriteData(SlaveInfo[0].MbCommError, 8, 20 );
                    WriteData(SlaveInfo[1].MbCommError, 8, 50 );
                    WriteData(SlaveInfo[2].MbCommError, 8, 80 );
                    WriteData(SlaveInfo[3].MbCommError, 8, 110);                     
                    terminal ++;
                    break;
                    
                case 11:
                    WriteData(SlaveInfo[0].MbExceptionCode, 9, 20 );
                    WriteData(SlaveInfo[1].MbExceptionCode, 9, 50 );
                    WriteData(SlaveInfo[2].MbExceptionCode, 9, 80 );
                    WriteData(SlaveInfo[3].MbExceptionCode, 9, 110);                     
                    terminal = 8;
                    break;
                    
                default :
                    break;
            } 
            
            UPDATExTERMINAL = 0;
        }        
    }
}
/**
 End of File
*/

//printf("PWM1 BLock 1A: %d, PWM1 Setpoint: %d\r\n",PDC1,API[PWM1_SETPOINT] * 24);

void WriteText(unsigned char *Text, unsigned int Ln, unsigned int Col){
    //   [{ROW};{COLUMN}f    
    printf("\033[%d;%df", Ln, Col);
    
    printf(Text); 
}

void WriteData(unsigned int Data, unsigned int Ln, unsigned int Col){
    printf("\033[%d;%df", Ln, Col);
    printf("%d", Data);
}