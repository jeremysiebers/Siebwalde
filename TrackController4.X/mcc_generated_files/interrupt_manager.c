/**
  Generated Interrupt Manager Header File

  @Company:
    Microchip Technology Inc.

  @File Name:
    interrupt_manager.h

  @Summary:
    This is the Interrupt Manager file generated using PIC10 / PIC12 / PIC16 / PIC18 MCUs

  @Description:
    This header file provides implementations for global interrupt handling.
    For individual peripheral handlers please see the peripheral driver for
    all modules selected in the GUI.
    Generation Information :
        Product Revision  :  PIC10 / PIC12 / PIC16 / PIC18 MCUs - 1.65.2
        Device            :  PIC18F97J60
        Driver Version    :  1.02
    The generated drivers are tested against the following:
        Compiler          :  XC8 1.45 or later
        MPLAB 	          :  MPLAB X 4.15
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

#include "../main.h"
#include "interrupt_manager.h"
#include "mcc.h"
#include "../spicommhandler.h"

static unsigned int out = 0;

void  INTERRUPT_Initialize (void)
{
    // Enable Interrupt Priority Vectors
    RCONbits.IPEN = 1;

    // Assign peripheral interrupt priority vectors

    // SSPI - high priority
    IPR3bits.SSP2IP = 1;


    // TMRI - low priority
    IPR1bits.TMR1IP = 0;    

    // TXI - low priority
    IPR1bits.TX1IP = 0;    

    // RCI - low priority
    IPR1bits.RC1IP = 0;    

}

void interrupt INTERRUPT_InterruptManagerHigh (void)
{
    // interrupt handler
    if(PIE3bits.SSP2IE == 1 && PIR3bits.SSP2IF == 1)
    {
        ProcessSpiInterrupt();
        PIR3bits.SSP2IF = 0;
        UPDATExTERMINAL = 1;
    }
    else
    {
        //Unhandled Interrupt
    }
}

void interrupt low_priority INTERRUPT_InterruptManagerLow (void)
{
    // interrupt handler
        if(PIE1bits.TMR1IE == 1 && PIR1bits.TMR1IF == 1)
        {
            //TMR1_ISR();
            //UPDATExTERMINAL = 1;            
            PIR1bits.TMR1IF = 0;            
        } 
        else if(PIE1bits.TX1IE == 1 && PIR1bits.TX1IF == 1)
        {
            EUSART1_Transmit_ISR();
        } 
        else if(PIE1bits.RC1IE == 1 && PIR1bits.RC1IF == 1)
        {
            EUSART1_Receive_ISR();
        } 
    else
    {
        //Unhandled Interrupt
    }
}
/**
 End of File
*/
