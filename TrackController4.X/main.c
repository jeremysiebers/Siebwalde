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
#include <string.h>
#include "main.h"
#include "mcc_generated_files/mcc.h"
#include "spicommhandler.h"

#define COLUMNS 4

#define LNxSTART 3

#define COL0	0
#define COL1	30
#define COL2	60
#define COL3	90

#define COL0D	COL0 + 20
#define COL1D	COL1 + 20
#define COL2D	COL2 + 20
#define COL3D	COL3 + 20

void WriteText(unsigned char *Text);
void WriteData(unsigned long int Data);

/*
                         Main application
 */

/*----------------------------------------------------------------------------*/
static SLAVE_INFO         SlaveInfo[NUMBER_OF_SLAVES];   

/*----------------------------------------------------------------------------*/

unsigned char UPDATExTERMINAL;
unsigned char terminal = 0;

static unsigned int Ln       = LNxSTART;
static unsigned int Col      = COL0;
static unsigned int ColCount = 0;

static unsigned int LnD  = LNxSTART;
static unsigned int ColD = COL0D;

void main(void)
{
    // Initialize the SLAVE_INFO struct with slave numbers(fixed))
    for (unsigned int i = 0; i <NUMBER_OF_SLAVES; i++){
        SlaveInfo[i].SlaveNumber = i;
        SlaveInfo[i].Header = 0xAA;
        SlaveInfo[i].Footer = 0x55;
        if (i > 0){
            SlaveInfo[i].HoldingReg[0] = 0x818F;                                    // Set all EMO an 50%
        }
    }
    
    // Initialize the device
    SYSTEM_Initialize();
    
    ModbusReset_LAT = 1;                                                        // Hold the ModbusMaster in reset
    
    InitSlaveCommunication(SlaveInfo);                                          // Init SPI slave - master registers
    
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
        
    printf("PIC18f97j60 started up!!!\n\r");
    __delay_ms(10);
    printf("\f");                                                               // Clear terminal (printf("\033[2J");)
    __delay_ms(10);
    printf("\033[?25h");
    __delay_ms(10);   
    printf("PIC18f97j60 started up!!!\n\r");                                    // Welcome message
    
    ModbusReset_LAT = 0;                                                        // as last release the ModbusMaster.
    
    
    
    while (1)
    {
        if(EUSART1_is_rx_ready()){
            switch(EUSART1_Read()){
                case 0x30:
                    ModbusReset_LAT = 1;
                    RESET();                    
                    break;
                    
                case 0x31:
                    (unsigned int)SlaveInfo[1].HoldingReg[0] &= 0x0FFF; 
                    if((SlaveInfo[1].HoldingReg[0] & 0x3FF) > 60){               // PWM On and 50% duty
                        (unsigned int)SlaveInfo[1].HoldingReg[0]-= 50;          
                    }          
                    break;
                        
                case 0x32:
                    (unsigned int)SlaveInfo[2].HoldingReg[0] &= 0x0FFF; 
                    if((SlaveInfo[2].HoldingReg[0] & 0x3FF) > 60){               // PWM On and 50% duty
                        (unsigned int)SlaveInfo[2].HoldingReg[0]-= 50;          
                    }
                    break;
                
                case 0x33:
                    (unsigned int)SlaveInfo[3].HoldingReg[0] &= 0x0FFF; 
                    if((SlaveInfo[3].HoldingReg[0] & 0x3FF) > 60){               // PWM On and 50% duty
                        (unsigned int)SlaveInfo[3].HoldingReg[0]-= 50;          
                    }
                    break;
                    
                case 0x34:
                    (unsigned int)SlaveInfo[1].HoldingReg[0] = 0x018F;          // 50%              
                    break;
                        
                case 0x35:
                    (unsigned int)SlaveInfo[2].HoldingReg[0] = 0x018F;
                    break;
                
                case 0x36:
                    (unsigned int)SlaveInfo[3].HoldingReg[0] = 0x018F;
                    break;
                    
                case 0x37:
                    (unsigned int)SlaveInfo[1].HoldingReg[0] &= 0x0FFF; 
                    if((SlaveInfo[1].HoldingReg[0] & 0x3FF) < 720){
                        (unsigned int)SlaveInfo[1].HoldingReg[0]+= 50;          
                    }
                    break;
                        
                case 0x38:
                    (unsigned int)SlaveInfo[2].HoldingReg[0] &= 0x0FFF; 
                    if((SlaveInfo[2].HoldingReg[0] & 0x3FF) < 720){
                        (unsigned int)SlaveInfo[2].HoldingReg[0]+= 50; 
                    }
                    break;
                
                case 0x39:
                    (unsigned int)SlaveInfo[3].HoldingReg[0] &= 0x0FFF; 
                    if((SlaveInfo[3].HoldingReg[0] & 0x3FF) < 720){
                        (unsigned int)SlaveInfo[3].HoldingReg[0]+= 50; 
                    }
                    break;
                    
                default:
                    break;
            }
        }
             
        if(UPDATExTERMINAL){
                        
            switch(terminal){
                case 0:             //"SpiCommErrorCount "//
                    WriteText((char *)"ModBus_Master:");
                    WriteText((char *)"ModBus_Slave:");
                    WriteText((char *)"ModBus_Slave:");
                    WriteText((char *)"ModBus_Slave:");                    
                    terminal++;
                    break;                   
                
                case 1:
                    WriteText((char *)"HoldingReg0:");
                    WriteText((char *)"HoldingReg0:");
                    WriteText((char *)"HoldingReg0:");
                    WriteText((char *)"HoldingReg0:");                    
                    terminal++;                       
                    break;                            
                                                      
                case 2:                               
                    WriteText((char *)"HoldingReg1:");
                    WriteText((char *)"HoldingReg1:");
                    WriteText((char *)"HoldingReg1:");
                    WriteText((char *)"HoldingReg1:");                    
                    terminal++;                       
                    break;                            
                                                      
                case 3:                               
                    WriteText((char *)"HoldingReg2:");
                    WriteText((char *)"HoldingReg2:");
                    WriteText((char *)"HoldingReg2:");
                    WriteText((char *)"HoldingReg2:");                    
                    terminal++;                       
                    break;                            
                                                      
                case 4:                               
                    WriteText((char *)"HoldingReg3:");
                    WriteText((char *)"HoldingReg3:");
                    WriteText((char *)"HoldingReg3:");
                    WriteText((char *)"HoldingReg3:");                    
                    terminal++;                       
                    //break;                            
                                                      
                case 5:
                    WriteText((char *)"InputReg0:");
                    WriteText((char *)"InputReg0:");
                    WriteText((char *)"InputReg0:");
                    WriteText((char *)"InputReg0:");                    
                    terminal++;
                    //break;
                    
                case 6:
                    WriteText((char *)"InputReg1:");
                    WriteText((char *)"InputReg1:");
                    WriteText((char *)"InputReg1:");
                    WriteText((char *)"InputReg1:");                    
                    terminal++;
                    //break;
                    
                case 7:
                    WriteText((char *)"InputReg2:");
                    WriteText((char *)"InputReg2:");
                    WriteText((char *)"InputReg2:");
                    WriteText((char *)"InputReg2:");                    
                    terminal++;
                    //break;
                    
                case 8:
                    WriteText((char *)"InputReg3:");
                    WriteText((char *)"InputReg3:");
                    WriteText((char *)"InputReg3:");
                    WriteText((char *)"InputReg3:");                    
                    terminal++;
                    //break;
                    
                case 9:
                    WriteText((char *)"InputReg4:");
                    WriteText((char *)"InputReg4:");
                    WriteText((char *)"InputReg4:");
                    WriteText((char *)"InputReg4:");                    
                    terminal++;
                    //break;
                    
                case 10:
                    WriteText((char *)"InputReg5:");
                    WriteText((char *)"InputReg5:");
                    WriteText((char *)"InputReg5:");
                    WriteText((char *)"InputReg5:");                    
                    terminal++;
                    //break;
                    
                case 11:
                    WriteText((char *)"DiagReg0:");
                    WriteText((char *)"DiagReg0:");
                    WriteText((char *)"DiagReg0:");
                    WriteText((char *)"DiagReg0:");                    
                    terminal++;                
                    break;                     
                                               
                case 12:                       
                    WriteText((char *)"DiagReg1:");
                    WriteText((char *)"DiagReg1:");
                    WriteText((char *)"DiagReg1:");
                    WriteText((char *)"DiagReg1:");                    
                    terminal++;                
                    //break;                     
                                               
                case 13:
                    WriteText((char *)"MbReceiveCounter:");
                    WriteText((char *)"MbReceiveCounter:");
                    WriteText((char *)"MbReceiveCounter:");
                    WriteText((char *)"MbReceiveCounter:");                    
                    terminal++;
                    break;
                    
                case 14:
                    WriteText((char *)"MbSentCounter:");
                    WriteText((char *)"MbSentCounter:");
                    WriteText((char *)"MbSentCounter:");
                    WriteText((char *)"MbSentCounter:");                    
                    terminal++;
                    //break;
                    
                case 15:
                    WriteText((char *)"MbCommError:");
                    WriteText((char *)"MbCommError:");
                    WriteText((char *)"MbCommError:");
                    WriteText((char *)"MbCommError:");                    
                    terminal++;
                    //break;
                    
                case 16:
                    WriteText((char *)"MbExceptionCode:");
                    WriteText((char *)"MbExceptionCode:");
                    WriteText((char *)"MbExceptionCode:");
                    WriteText((char *)"MbExceptionCode:");                    
                    terminal++;
                    break;
                    
                case 17:
                    WriteText((char *)"SpiCommErrorCount:");
                    WriteText((char *)"SpiCommErrorCount:");
                    WriteText((char *)"SpiCommErrorCount:");
                    WriteText((char *)"SpiCommErrorCount:");
                    terminal++;
                    //break;
                    
                case 18:
					WriteText((char *)"SpiSLCommErrCnt:"); 
					WriteText((char *)" ");
					WriteText((char *)" ");
					WriteText((char *)" ");
                    terminal++;
                    break;                    
/*------------------------------------------------------------------------------------------*/                    
                case 19:
                    LnD  = LNxSTART;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].SlaveNumber);
                    WriteData(SlaveInfo[1].SlaveNumber);
                    WriteData(SlaveInfo[2].SlaveNumber);
                    WriteData(SlaveInfo[3].SlaveNumber);                     
                    terminal++;
                    //break;
                    
                case 20:
                    LnD  = LNxSTART+1;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].HoldingReg[0]);
                    WriteData(SlaveInfo[1].HoldingReg[0]);
                    WriteData(SlaveInfo[2].HoldingReg[0]);
                    WriteData(SlaveInfo[3].HoldingReg[0]);                     
                    terminal++;
                    //break;
					
				case 21:
                    LnD  = LNxSTART+2;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].HoldingReg[1]);
                    WriteData(SlaveInfo[1].HoldingReg[1]);
                    WriteData(SlaveInfo[2].HoldingReg[1]);
                    WriteData(SlaveInfo[3].HoldingReg[1]);                     
                    terminal++;
                    //break;
					
				case 22:
                    LnD  = LNxSTART+3;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].HoldingReg[2]);
                    WriteData(SlaveInfo[1].HoldingReg[2]);
                    WriteData(SlaveInfo[2].HoldingReg[2]);
                    WriteData(SlaveInfo[3].HoldingReg[2]);                     
                    terminal++;
                    break;
					
				case 23:
                    LnD  = LNxSTART+4;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].HoldingReg[3]);
                    WriteData(SlaveInfo[1].HoldingReg[3]);
                    WriteData(SlaveInfo[2].HoldingReg[3]);
                    WriteData(SlaveInfo[3].HoldingReg[3]);                     
                    terminal++;
                    //break;
					
				case 24:
                    LnD  = LNxSTART+5;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].InputReg[0]);
                    WriteData(SlaveInfo[1].InputReg[0]);
                    WriteData(SlaveInfo[2].InputReg[0]);
                    WriteData(SlaveInfo[3].InputReg[0]);                     
                    terminal++;
                    //break;
					
				case 25:
                    LnD  = LNxSTART+6;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].InputReg[1]);
                    WriteData(SlaveInfo[1].InputReg[1]);
                    WriteData(SlaveInfo[2].InputReg[1]);
                    WriteData(SlaveInfo[3].InputReg[1]);                     
                    terminal++;
                    //break;
					
				case 26:
                    LnD  = LNxSTART+7;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].InputReg[2]);
                    WriteData(SlaveInfo[1].InputReg[2]);
                    WriteData(SlaveInfo[2].InputReg[2]);
                    WriteData(SlaveInfo[3].InputReg[2]);                     
                    terminal++;
                    break;
					
				case 27:
                    LnD  = LNxSTART+8;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].InputReg[3]);
                    WriteData(SlaveInfo[1].InputReg[3]);
                    WriteData(SlaveInfo[2].InputReg[3]);
                    WriteData(SlaveInfo[3].InputReg[3]);                     
                    terminal++;
                    //break;
					
				case 28:
                    LnD  = LNxSTART+9;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].InputReg[4]);
                    WriteData(SlaveInfo[1].InputReg[4]);
                    WriteData(SlaveInfo[2].InputReg[4]);
                    WriteData(SlaveInfo[3].InputReg[4]);                     
                    terminal++;
                    //break;
					
				case 29:
                    LnD  = LNxSTART+10;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].InputReg[5]);
                    WriteData(SlaveInfo[1].InputReg[5]);
                    WriteData(SlaveInfo[2].InputReg[5]);
                    WriteData(SlaveInfo[3].InputReg[5]);                     
                    terminal++;
                    //break;
					
				case 30:
                    LnD  = LNxSTART+11;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].DiagReg[0]);
                    WriteData(SlaveInfo[1].DiagReg[0]);
                    WriteData(SlaveInfo[2].DiagReg[0]);
                    WriteData(SlaveInfo[3].DiagReg[0]);                     
                    terminal++;
                    break;
					
				case 31:
                    LnD  = LNxSTART+12;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].DiagReg[1]);
                    WriteData(SlaveInfo[1].DiagReg[1]);
                    WriteData(SlaveInfo[2].DiagReg[1]);
                    WriteData(SlaveInfo[3].DiagReg[1]);                     
                    terminal++;
                    //break;
					
				case 32:
                    LnD  = LNxSTART+13;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].MbReceiveCounter);
                    WriteData(SlaveInfo[1].MbReceiveCounter);
                    WriteData(SlaveInfo[2].MbReceiveCounter);
                    WriteData(SlaveInfo[3].MbReceiveCounter);                     
                    terminal++;
                    //break;
                    
                case 33:
                    LnD  = LNxSTART+14;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].MbSentCounter);
                    WriteData(SlaveInfo[1].MbSentCounter);
                    WriteData(SlaveInfo[2].MbSentCounter);
                    WriteData(SlaveInfo[3].MbSentCounter);                     
                    terminal++;
                    //break;
                    
                case 34:
                    LnD  = LNxSTART+15;
                    ColD = COL0D;
                    WriteData((char)(SlaveInfo[0].MbCommError));
                    WriteData((char)(SlaveInfo[1].MbCommError));
                    WriteData((char)(SlaveInfo[2].MbCommError));
                    WriteData((char)(SlaveInfo[3].MbCommError));                     
                    terminal++;
                    break;
                    
                case 35:
                    LnD  = LNxSTART+16;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].MbExceptionCode);
                    WriteData(SlaveInfo[1].MbExceptionCode);
                    WriteData(SlaveInfo[2].MbExceptionCode);
                    WriteData(SlaveInfo[3].MbExceptionCode);                     
                    terminal++;
                    //break;
                    
                case 36:
                    LnD  = LNxSTART+17;
                    ColD = COL0D;
                    WriteData(SlaveInfo[0].SpiCommErrorCounter);
                    WriteData(SlaveInfo[1].SpiCommErrorCounter);
                    WriteData(SlaveInfo[2].SpiCommErrorCounter);
                    WriteData(SlaveInfo[3].SpiCommErrorCounter);
                    terminal++;
                    //break;
                    
                case 37:
                    LnD  = LNxSTART+18;
                    ColD = COL0D;
					WriteData(SpiSlaveCommErrorCounter);
					WriteData(0);
					WriteData(0);
					WriteData(0);
					terminal = 19;
                    break;
                    
                default :
                    terminal = 19;
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




void WriteText(unsigned char *Text){
    
    //   [{ROW};{COLUMN}f    
    printf("\033[%u;%uf", Ln, Col);    
    printf(Text);
    
    ColCount++;
    switch(ColCount){
        case 0  : Col = COL0;break;
        case 1  : Col = COL1;break;
        case 2  : Col = COL2;break;
        case 3  : Col = COL3;break;
        default : Col = COL0;break;
    }
    
    if(ColCount > (COLUMNS-1)){
       Ln++;
       Col = COL0;
       ColCount = 0;
    }
}



void WriteData(unsigned long int Data){
    
    char padding[10];
    
         if(Data < 10){
        strcpy(padding, "    ");
    }
    else if(Data <100){
        strcpy(padding, "   ");
    }
    else if(Data <1000){
        strcpy(padding, "  ");
    }
    else if(Data <10000){
        strcpy(padding, " ");
    }
    else if(Data <100000){
        strcpy(padding, "     ");
    }
    else if(Data <1000000){
        strcpy(padding, "    ");
    }
    else if(Data <10000000){
        strcpy(padding, "   ");
    }
    else if(Data <100000000){
        strcpy(padding, "  ");
    }
    else if(Data <1000000000){
        strcpy(padding, " ");
    }
    else{
        strcpy(padding, "");
    }
    
    printf("\033[%u;%uf", LnD, ColD);
    printf("%lu%s", Data, padding);
    
    ColCount++;
    switch(ColCount){
        case 0  : ColD = COL0D;break;
        case 1  : ColD = COL1D;break;
        case 2  : ColD = COL2D;break;
        case 3  : ColD = COL3D;break;
        default : ColD = COL0D;break;
    }
    
    if(ColCount > (COLUMNS-1)){
       LnD++;
       Col = COL0D;
       ColCount = 0;
       if(LnD >= Ln){
           LnD = LNxSTART;
       }
    }
}