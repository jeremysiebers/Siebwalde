/**
  Section: Included Files
 */
#include "ptg.h"
#include "config.h"
#include <xc.h>

/* PTG Commands */
#define PTGCTRL  (0x0<<0)
#define PTGADD   (0x1<<4)
#define PTGCOPY  (0x1<<4)
#define PTGWHI   (0x4<<4)
#define PTGIRQ   (0x7<<4)
#define PTGTRIG  (0x8<<4)
#define PTGJMP   (0xA<<4)
#define PTGJMPC0 (0xC<<4)
#define PTGJMPC1 (0xE<<4)

void PTGxInitialize(void) {
    
    PTGCST = 0;                                                                 // Clear the control/status register
    PTGCON = 0;                                                                 // Clear the control register/*
    
    // Outer loop
    _STEP0 = PTGWHI | 0x1;                                                      // Wait for positive edge trigger 1
    _STEP1 = PTGCTRL | 0x8;                                                     // Start PTGT0, wait for time out
    _STEP2 = PTGIRQ | 0x1;                                                      // Generate IRQ 1
    
    // Inner loop
    _STEP3 = PTGTRIG | 0x3;                                                     // Generate output trigger 3
    _STEP4 = PTGCTRL | 0x9;                                                     // Start PTGT1, wait for time out
    _STEP5 = PTGJMPC0 | 0x3;                                                    // Jump to STEP3 if PTGC0! = PTGC0LIM, increment PTGC0 
    
    // End inner loop
    _STEP6 = PTGADD | 0x1;                                                      // Add PTGADJ to PTGT0LIM
    _STEP7 = PTGJMPC1 | 0x0;                                                    // Jump to STEP0 if PTGC1! = PTGC1LIM, increment PTGC1 
    
    // End outer loop
    _STEP8 = PTGIRQ | 0x4;                                                      // Generate IRQ 4
    _STEP9 = PTGCOPY | 0x8;                                                     // Copy PTGHOLD to PTGT0LIM (restore original value)
    _STEP10 = PTGJMP | 0x0;                                                     // Jump to start of queue
    
    PTGCSTbits.PTGEN = Off;                                                       // Enable the PTG
    PTGCSTbits.PTGSTRT = Off;                                                     // Start the PTG
}
