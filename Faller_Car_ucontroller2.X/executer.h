/* Microchip Technology Inc. and its subsidiaries.  You may use this software 
 * and any derivatives exclusively with Microchip products. 
 * 
 * THIS SOFTWARE IS SUPPLIED BY MICROCHIP "AS IS".  NO WARRANTIES, WHETHER 
 * EXPRESS, IMPLIED OR STATUTORY, APPLY TO THIS SOFTWARE, INCLUDING ANY IMPLIED 
 * WARRANTIES OF NON-INFRINGEMENT, MERCHANTABILITY, AND FITNESS FOR A 
 * PARTICULAR PURPOSE, OR ITS INTERACTION WITH MICROCHIP PRODUCTS, COMBINATION 
 * WITH ANY OTHER PRODUCTS, OR USE IN ANY APPLICATION. 
 *
 * IN NO EVENT WILL MICROCHIP BE LIABLE FOR ANY INDIRECT, SPECIAL, PUNITIVE, 
 * INCIDENTAL OR CONSEQUENTIAL LOSS, DAMAGE, COST OR EXPENSE OF ANY KIND 
 * WHATSOEVER RELATED TO THE SOFTWARE, HOWEVER CAUSED, EVEN IF MICROCHIP HAS 
 * BEEN ADVISED OF THE POSSIBILITY OR THE DAMAGES ARE FORESEEABLE.  TO THE 
 * FULLEST EXTENT ALLOWED BY LAW, MICROCHIP'S TOTAL LIABILITY ON ALL CLAIMS 
 * IN ANY WAY RELATED TO THIS SOFTWARE WILL NOT EXCEED THE AMOUNT OF FEES, IF 
 * ANY, THAT YOU HAVE PAID DIRECTLY TO MICROCHIP FOR THIS SOFTWARE.
 *
 * MICROCHIP PROVIDES THIS SOFTWARE CONDITIONALLY UPON YOUR ACCEPTANCE OF THESE 
 * TERMS. 
 */

/* 
 * File:   
 * Author: 
 * Comments:
 * Revision history: 
 */

// This is a guard condition so that contents of this file are not included
// more than once.  
#ifndef __EXECUTER_H
#define	__EXECUTER_H

#include <xc.h> // include processor files - each processor file is guarded. 
#include "Main.h"
#include "mcc_generated_files/adc.h"

// TODO Insert appropriate #include <>

// TODO Insert C++ class definitions if appropriate

// TODO Insert declarations

// Comment a function and leverage automatic documentation with slash star star
typedef struct
{
    uint8_t Name;            // Name of the Led
    uint8_t Led;             // Holds the duty cycle value for a Led
    uint8_t Prog;            // Program to run on a selected Led
    uint8_t Speed;           // setting on how fast to change a led on time, should be lower than 254
    uint8_t nominal;         // Free to use nominal value for intensity 
    uint8_t Prog_State;      // Memory to hold a actual program status
    uint8_t Iteration;       // Counter to flow over the Speed setting
    uint8_t StepSize;        // Multiplier for PWM calc
    uint8_t PWM;             // Pointer to method for setting a linked PWM duty cycle

}LEDBIT;

enum{
    LedFlashLeft  = 0,
    LedFlashRight = 1,
    LedBackLeft   = 2,
    LedBackRight  = 3,
    LedFrontLeft  = 4,
    LedFrontRight = 5,
    
};

enum{
    MAX = 254,
    FRONT = 80,
    FRONT_HIGH = 255,
    BACK = 25,
    BRAKE = 127,
    MIN = 1,
    SLOW_FLASH_LOW = 30
};

enum{
    Led_Off,
    Led_Nom,
    Led_Max,
    Led_Brake,
    Led_SlFl,
    Led_Flash
};

enum{
    PWM1,
    PWM2,
    PWM3,
    PWM4,
    PWM5,
    PWM6    
};

uint8_t Effect_Prog(uint8_t Prog, uint8_t Led);
uint8_t LedOff(uint8_t Led);
uint8_t LedNom(uint8_t Led);
uint8_t LedMax(uint8_t Led);
uint8_t LedBrake(uint8_t Led);
uint8_t LedSlFl(uint8_t Led);
uint8_t LedFlash(uint8_t Led);
void CalcPwm(uint8_t Led);

extern void INITxEXECUTER(void);
extern uint8_t EXECUTExEFFECT(void);
extern LEDBIT LedBit[6];
extern void RCSxLED(void);
extern void BATTxPROTECT(void);

// TODO Insert declarations or function prototypes (right here) to leverage 
// live documentation

#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

    // TODO If C++ is being used, regular C code needs function names to have C 
    // linkage so the functions can be used by the c code. 

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

