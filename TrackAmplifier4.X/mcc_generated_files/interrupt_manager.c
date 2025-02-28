/**
  Generated Interrupt Manager Source File

  @Company:
    Microchip Technology Inc.

  @File Name:
    interrupt_manager.c

  @Summary:
    This is the Interrupt Manager file generated using PIC10 / PIC12 / PIC16 / PIC18 MCUs

  @Description:
    This header file provides implementations for global interrupt handling.
    For individual peripheral handlers please see the peripheral driver for
    all modules selected in the GUI.
    Generation Information :
        Product Revision  :  PIC10 / PIC12 / PIC16 / PIC18 MCUs - 1.65.2
        Device            :  PIC18F25K40
        Driver Version    :  2.03
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

#include "interrupt_manager.h"
#include "mcc.h"
#include "../main.h"
#include "../modbus/General.h"

void  INTERRUPT_Initialize (void)
{
    // Disable Interrupt Priority Vectors (16CXXX Compatibility Mode)
    INTCONbits.IPEN = 0;
}

void __interrupt() INTERRUPT_InterruptManager (void)
{
    // interrupt handler
    if(INTCONbits.PEIE == 1)
    {
        if(PIE3bits.RCIE == 1 && PIR3bits.RCIF == 1)
        {
            ReceiveInterrupt(RCREG);                                            // Read first read character from buffer
            
            T4CONbits.TMR4ON = 1;                                               // Enable Timer4 for PID sync to modbus messages
            
            if(1 == RC1STAbits.OERR)
            {
                // EUSART error - restart

                RC1STAbits.CREN = 0;
                RC1STAbits.CREN = 1;
            }
            TMR3_Reload();
            PIR4bits.TMR3IF = 0;
            PIE4bits.TMR3IE = 1;

            if (PIR3bits.RCIF == 1){                                            // If the buffer contains more characters do read again
                ReceiveInterrupt(RCREG);
            }
        } 
        else if(PIE4bits.TMR3IE == 1 && PIR4bits.TMR3IF == 1)
        {
            PetitModbusTimerValue = 3;                                          // Between receive interrupts it took to long --> message done
            PIE4bits.TMR3IE = 0;
            PIR4bits.TMR3IF = 0;
            
        }
        else if(PIE4bits.TMR4IE == 1 && PIR4bits.TMR4IF == 1)
        {
            T4CONbits.TMR4ON = 0;
            T4TMR = 0x00;
            PIR4bits.TMR4IF = 0;
            Update_Amplifier = true;                                            // Start the other tasks like the regulator etc
        }
        else
        {
            //Unhandled Interrupt
        }
    }      
    else
    {
        //Unhandled Interrupt
    }
}
/**
 End of File
*/
