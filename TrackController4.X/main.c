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
    // Initialize the SLAVE_INFO struct with slave numbers(fixed))
    for (unsigned int i = 0; i <NUMBER_OF_SLAVES; i++){
        SlaveInfo[i].SlaveNumber = i;
        SlaveInfo[i].Header = 0xAA;
        SlaveInfo[i].Footer = 0x55;
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
        //CheckSpiStart();
        
        if(EUSART1_is_rx_ready()){
            switch(EUSART1_Read()){
                case 0x30:
                    ModbusReset_LAT = 1;
                    RESET();
                    SpiSlaveCommErrorCounter = 0;
                    printf("\f");                                               // Clear terminal (printf("\033[2J");)
                    __delay_ms(10);
                    terminal = 0;                                               // Re-print whole table to terminal
                    ModbusReset_LAT = 0;
                    break;
                    
                case 0x31:
                    (unsigned int)SlaveInfo[1].HoldingRegWrSl[0] = 1;
                    break;
                        
                case 0x32:
                    (unsigned int)SlaveInfo[2].HoldingRegWrSl[0] = 1;
                    break;
                
                case 0x33:
                    (unsigned int)SlaveInfo[3].HoldingRegWrSl[0] = 1;
                    break;
                    
                case 0x34:
                    (unsigned int)SlaveInfo[1].HoldingRegWrSl[0] = 0;
                    break;
                        
                case 0x35:
                    (unsigned int)SlaveInfo[2].HoldingRegWrSl[0] = 0;
                    break;
                
                case 0x36:
                    (unsigned int)SlaveInfo[3].HoldingRegWrSl[0] = 0;
                    break;
                    
                default:
                    break;
            }
        }
             
        if(UPDATExTERMINAL){
                        
            switch(terminal){
                case 0:             //"SpiCommErrorCount "//
                    WriteText((char *)"ModBus_Master:", 2, 0 );
                    WriteText((char *)"ModBus_Slave1:", 2, 30);
                    WriteText((char *)"ModBus_Slave2:", 2, 60);
                    WriteText((char *)"ModBus_Slave3:", 2, 90);                    
                    terminal++;
                    //break;                   
                
                case 1:
                    WriteText((char *)"HoldingRegRdSl0:", 3, 0 );
                    WriteText((char *)"HoldingRegRdSl0:", 3, 30);
                    WriteText((char *)"HoldingRegRdSl0:", 3, 60);
                    WriteText((char *)"HoldingRegRdSl0:", 3, 90);                    
                    terminal++;                       
                    //break;                            
                                                      
                case 2:                               
                    WriteText((char *)"HoldingRegRdSl1:", 4, 0 );
                    WriteText((char *)"HoldingRegRdSl1:", 4, 30);
                    WriteText((char *)"HoldingRegRdSl1:", 4, 60);
                    WriteText((char *)"HoldingRegRdSl1:", 4, 90);                    
                    terminal++;                       
                    //break;                            
                                                      
                case 3:                               
                    WriteText((char *)"HoldingRegRdSl2:", 5, 0 );
                    WriteText((char *)"HoldingRegRdSl2:", 5, 30);
                    WriteText((char *)"HoldingRegRdSl2:", 5, 60);
                    WriteText((char *)"HoldingRegRdSl2:", 5, 90);                    
                    terminal++;                       
                    break;                            
                                                      
                case 4:                               
                    WriteText((char *)"HoldingRegRdSl3:", 6, 0 );
                    WriteText((char *)"HoldingRegRdSl3:", 6, 30);
                    WriteText((char *)"HoldingRegRdSl3:", 6, 60);
                    WriteText((char *)"HoldingRegRdSl3:", 6, 90);                    
                    terminal++;                       
                    //break;                            
                                                      
                case 5:                               
                    WriteText((char *)"HoldingRegWrSl0:", 7, 0 );
                    WriteText((char *)"HoldingRegWrSl0:", 7, 30);
                    WriteText((char *)"HoldingRegWrSl0:", 7, 60);
                    WriteText((char *)"HoldingRegWrSl0:", 7, 90);                    
                    terminal++;                       
                    //break;                           
                                                      
                case 6:                               
                    WriteText((char *)"HoldingRegWrSl1:", 8, 0 );
                    WriteText((char *)"HoldingRegWrSl1:", 8, 30);
                    WriteText((char *)"HoldingRegWrSl1:", 8, 60);
                    WriteText((char *)"HoldingRegWrSl1:", 8, 90);                    
                    terminal++;                       
                    //break;                            
                                                      
                case 7:                               
                    WriteText((char *)"HoldingRegWrSl2:", 9, 0 );
                    WriteText((char *)"HoldingRegWrSl2:", 9, 30);
                    WriteText((char *)"HoldingRegWrSl2:", 9, 60);
                    WriteText((char *)"HoldingRegWrSl2:", 9, 90);                    
                    terminal++;                       
                    break;                            
                                                      
                case 8:                               
                    WriteText((char *)"HoldingRegWrSl3:", 10, 0 );
                    WriteText((char *)"HoldingRegWrSl3:", 10, 30);
                    WriteText((char *)"HoldingRegWrSl3:", 10, 60);
                    WriteText((char *)"HoldingRegWrSl3:", 10, 90);                    
                    terminal++;
                    //break;
                    
                case 9:
                    WriteText((char *)"InputReg0:", 11, 0 );
                    WriteText((char *)"InputReg0:", 11, 30);
                    WriteText((char *)"InputReg0:", 11, 60);
                    WriteText((char *)"InputReg0:", 11, 90);                    
                    terminal++;
                    //break;
                    
                case 10:
                    WriteText((char *)"InputReg1:", 12, 0 );
                    WriteText((char *)"InputReg1:", 12, 30);
                    WriteText((char *)"InputReg1:", 12, 60);
                    WriteText((char *)"InputReg1:", 12, 90);                    
                    terminal++;
                    //break;
                    
                case 11:
                    WriteText((char *)"DiagReg0:", 13, 0 );
                    WriteText((char *)"DiagReg0:", 13, 30);
                    WriteText((char *)"DiagReg0:", 13, 60);
                    WriteText((char *)"DiagReg0:", 13, 90);                    
                    terminal++;                
                    break;                     
                                               
                case 12:                       
                    WriteText((char *)"DiagReg1:", 14, 0 );
                    WriteText((char *)"DiagReg1:", 14, 30);
                    WriteText((char *)"DiagReg1:", 14, 60);
                    WriteText((char *)"DiagReg1:", 14, 90);                    
                    terminal++;                
                    //break;                     
                                               
                case 13:                       
                    WriteText((char *)"DiagReg2:", 15, 0 );
                    WriteText((char *)"DiagReg2:", 15, 30);
                    WriteText((char *)"DiagReg2:", 15, 60);
                    WriteText((char *)"DiagReg2:", 15, 90);                    
                    terminal++;
                    //break;
                    
                case 14:
                    WriteText((char *)"DiagReg3:", 16, 0 );
                    WriteText((char *)"DiagReg3:", 16, 30);
                    WriteText((char *)"DiagReg3:", 16, 60);
                    WriteText((char *)"DiagReg3:", 16, 90);                    
                    terminal++;
                    //break;
                    
                case 15:
                    WriteText((char *)"MbReceiveCounter:", 17, 0 );
                    WriteText((char *)"MbReceiveCounter:", 17, 30);
                    WriteText((char *)"MbReceiveCounter:", 17, 60);
                    WriteText((char *)"MbReceiveCounter:", 17, 90);                    
                    terminal++;
                    break;
                    
                case 16:
                    WriteText((char *)"MbSentCounter:", 18, 0 );
                    WriteText((char *)"MbSentCounter:", 18, 30);
                    WriteText((char *)"MbSentCounter:", 18, 60);
                    WriteText((char *)"MbSentCounter:", 18, 90);                    
                    terminal++;
                    //break;
                    
                case 17:
                    WriteText((char *)"MbCommError:", 19, 0 );
                    WriteText((char *)"MbCommError:", 19, 30);
                    WriteText((char *)"MbCommError:", 19, 60);
                    WriteText((char *)"MbCommError:", 19, 90);                    
                    terminal++;
                    //break;
                    
                case 18:
                    WriteText((char *)"MbExceptionCode:", 20, 0 );
                    WriteText((char *)"MbExceptionCode:", 20, 30);
                    WriteText((char *)"MbExceptionCode:", 20, 60);
                    WriteText((char *)"MbExceptionCode:", 20, 90);                    
                    terminal++;
                    //break;
                    
                case 19:
                    WriteText((char *)"SpiCommErrorCount:", 21, 0 );
                    WriteText((char *)"SpiCommErrorCount:", 21, 30);
                    WriteText((char *)"SpiCommErrorCount:", 21, 60);
                    WriteText((char *)"SpiCommErrorCount:", 21, 90);
                    WriteText((char *)"Ethernet Target SPI Slave:", 23, 0 );
                    WriteText((char *)"SpiSLCommErrCnt:", 24, 0 );                    
                    terminal++;
                    break;
/*------------------------------------------------------------------------------------------*/                    
                case 20:
                    WriteData(SlaveInfo[0].SlaveNumber, 2, 20 );
                    WriteData(SlaveInfo[1].SlaveNumber, 2, 50 );
                    WriteData(SlaveInfo[2].SlaveNumber, 2, 80 );
                    WriteData(SlaveInfo[3].SlaveNumber, 2, 110);                     
                    terminal++;
                    //break;
                    
                case 21:
                    WriteData(SlaveInfo[0].HoldingRegRdSl[0], 3, 20 );
                    WriteData(SlaveInfo[1].HoldingRegRdSl[0], 3, 50 );
                    WriteData(SlaveInfo[2].HoldingRegRdSl[0], 3, 80 );
                    WriteData(SlaveInfo[3].HoldingRegRdSl[0], 3, 110);                     
                    terminal++;
                    //break;
					
				case 22:
                    WriteData(SlaveInfo[0].HoldingRegRdSl[1], 4, 20 );
                    WriteData(SlaveInfo[1].HoldingRegRdSl[1], 4, 50 );
                    WriteData(SlaveInfo[2].HoldingRegRdSl[1], 4, 80 );
                    WriteData(SlaveInfo[3].HoldingRegRdSl[1], 4, 110);                     
                    terminal++;
                    //break;
					
				case 23:
                    WriteData(SlaveInfo[0].HoldingRegRdSl[2], 5, 20 );
                    WriteData(SlaveInfo[1].HoldingRegRdSl[2], 5, 50 );
                    WriteData(SlaveInfo[2].HoldingRegRdSl[2], 5, 80 );
                    WriteData(SlaveInfo[3].HoldingRegRdSl[2], 5, 110);                     
                    terminal++;
                    break;
					
				case 24:
                    WriteData(SlaveInfo[0].HoldingRegRdSl[3], 6, 20 );
                    WriteData(SlaveInfo[1].HoldingRegRdSl[3], 6, 50 );
                    WriteData(SlaveInfo[2].HoldingRegRdSl[3], 6, 80 );
                    WriteData(SlaveInfo[3].HoldingRegRdSl[3], 6, 110);                     
                    terminal++;
                    //break;
					
				case 25:
                    WriteData(SlaveInfo[0].HoldingRegWrSl[0], 7, 20 );
                    WriteData(SlaveInfo[1].HoldingRegWrSl[0], 7, 50 );
                    WriteData(SlaveInfo[2].HoldingRegWrSl[0], 7, 80 );
                    WriteData(SlaveInfo[3].HoldingRegWrSl[0], 7, 110);                     
                    terminal++;
                    //break;
					
				case 26:
                    WriteData(SlaveInfo[0].HoldingRegWrSl[1], 8, 20 );
                    WriteData(SlaveInfo[1].HoldingRegWrSl[1], 8, 50 );
                    WriteData(SlaveInfo[2].HoldingRegWrSl[1], 8, 80 );
                    WriteData(SlaveInfo[3].HoldingRegWrSl[1], 8, 110);                     
                    terminal++;
                    //break;
					
				case 27:
                    WriteData(SlaveInfo[0].HoldingRegWrSl[2], 9, 20 );
                    WriteData(SlaveInfo[1].HoldingRegWrSl[2], 9, 50 );
                    WriteData(SlaveInfo[2].HoldingRegWrSl[2], 9, 80 );
                    WriteData(SlaveInfo[3].HoldingRegWrSl[2], 9, 110);                     
                    terminal++;
                    break;
					
				case 28:
                    WriteData(SlaveInfo[0].HoldingRegWrSl[3], 10, 20 );
                    WriteData(SlaveInfo[1].HoldingRegWrSl[3], 10, 50 );
                    WriteData(SlaveInfo[2].HoldingRegWrSl[3], 10, 80 );
                    WriteData(SlaveInfo[3].HoldingRegWrSl[3], 10, 110);                     
                    terminal++;
                    //break;
					
				case 29:
                    WriteData(SlaveInfo[0].InputReg[0], 11, 20 );
                    WriteData(SlaveInfo[1].InputReg[0], 11, 50 );
                    WriteData(SlaveInfo[2].InputReg[0], 11, 80 );
                    WriteData(SlaveInfo[3].InputReg[0], 11, 110);                     
                    terminal++;
                    //break;
					
				case 30:
                    WriteData(SlaveInfo[0].InputReg[1], 12, 20 );
                    WriteData(SlaveInfo[1].InputReg[1], 12, 50 );
                    WriteData(SlaveInfo[2].InputReg[1], 12, 80 );
                    WriteData(SlaveInfo[3].InputReg[1], 12, 110);                     
                    terminal++;
                    //break;
					
				case 31:
                    WriteData(SlaveInfo[0].DiagReg[0], 13, 20 );
                    WriteData(SlaveInfo[1].DiagReg[0], 13, 50 );
                    WriteData(SlaveInfo[2].DiagReg[0], 13, 80 );
                    WriteData(SlaveInfo[3].DiagReg[0], 13, 110);                     
                    terminal++;
                    break;
					
				case 32:
                    WriteData(SlaveInfo[0].DiagReg[1], 14, 20 );
                    WriteData(SlaveInfo[1].DiagReg[1], 14, 50 );
                    WriteData(SlaveInfo[2].DiagReg[1], 14, 80 );
                    WriteData(SlaveInfo[3].DiagReg[1], 14, 110);                     
                    terminal++;
                    //break;
					
				case 33:
                    WriteData(SlaveInfo[0].DiagReg[2], 15, 20 );
                    WriteData(SlaveInfo[1].DiagReg[2], 15, 50 );
                    WriteData(SlaveInfo[2].DiagReg[2], 15, 80 );
                    WriteData(SlaveInfo[3].DiagReg[2], 15, 110);                     
                    terminal++;
                    //break;
					
				case 34:
                    WriteData(SlaveInfo[0].DiagReg[3], 16, 20 );
                    WriteData(SlaveInfo[1].DiagReg[3], 16, 50 );
                    WriteData(SlaveInfo[2].DiagReg[3], 16, 80 );
                    WriteData(SlaveInfo[3].DiagReg[3], 16, 110);                     
                    terminal++;
                    //break;
					
				case 35:
                    WriteData(SlaveInfo[0].MbReceiveCounter, 17, 20 );
                    WriteData(SlaveInfo[1].MbReceiveCounter, 17, 50 );
                    WriteData(SlaveInfo[2].MbReceiveCounter, 17, 80 );
                    WriteData(SlaveInfo[3].MbReceiveCounter, 17, 110);                     
                    terminal++;
                    break;
                    
                case 36:
                    WriteData(SlaveInfo[0].MbSentCounter, 18, 20 );
                    WriteData(SlaveInfo[1].MbSentCounter, 18, 50 );
                    WriteData(SlaveInfo[2].MbSentCounter, 18, 80 );
                    WriteData(SlaveInfo[3].MbSentCounter, 18, 110);                     
                    terminal ++;
                    //break;
                    
                case 37:
                    WriteData((char)(SlaveInfo[0].MbCommError), 19, 20 );
                    WriteData((char)(SlaveInfo[1].MbCommError), 19, 50 );
                    WriteData((char)(SlaveInfo[2].MbCommError), 19, 80 );
                    WriteData((char)(SlaveInfo[3].MbCommError), 19, 110);                     
                    terminal ++;
                    //break;
                    
                case 38:
                    WriteData(SlaveInfo[0].MbExceptionCode, 20, 20 );
                    WriteData(SlaveInfo[1].MbExceptionCode, 20, 50 );
                    WriteData(SlaveInfo[2].MbExceptionCode, 20, 80 );
                    WriteData(SlaveInfo[3].MbExceptionCode, 20, 110);                     
                    terminal++;
                    //break;
                    
                case 39:
                    WriteData(SlaveInfo[0].SpiCommErrorCounter, 21, 20 );
                    WriteData(SlaveInfo[1].SpiCommErrorCounter, 21, 50 );
                    WriteData(SlaveInfo[2].SpiCommErrorCounter, 21, 80 );
                    WriteData(SlaveInfo[3].SpiCommErrorCounter, 21, 110);   
                    WriteData(SpiSlaveCommErrorCounter, 24, 20);
                    terminal = 20;
                    break;
                    
                default :
                    terminal = 20;
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
    printf("\033[%u;%uf", Ln, Col);
    
    printf(Text); 
}

void WriteData(unsigned int Data, unsigned int Ln, unsigned int Col){
    
    char padding[6];
    
         if(Data <10000){
        strcpy(padding, "  ");
    }
    else if(Data <1000){
        strcpy(padding, "   ");
    }
    else if(Data <100){
        strcpy(padding, "    ");
    }
    else if(Data < 10){
        strcpy(padding, "     ");
    }
    else{
        strcpy(padding, "");
    }
    
    printf("\033[%u;%uf", Ln, Col);
    printf("%u%s", Data, padding);
}