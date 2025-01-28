/* 
 * File:   enums.h
 * Author: jerem
 *
 * Created on 12 december 2023, 20:35
 */

#ifndef ENUMS_H
#define	ENUMS_H

#ifdef	__cplusplus
extern "C" {
#endif
    
//    #define DEBUG /* used for debugging with simulator */

    #include <stdbool.h>
    #include "debounce.h"
    #include "mcp23017.h"
    
    /* factor to be set according to set timer interrupt value to calc seconds */
    const uint16_t tFactorSec = 1000; // timer set to 1ms, 10 * 1000 = 1 second
    /* Time to wait for servo to move to new position */
    const uint16_t tSwitchPointWaitTime = (uint16_t)(1 * tFactorSec);
    /* Minimum Time that Train waits before leaving */
    const uint16_t tParkTime = (uint16_t)(1 * tFactorSec);
    /* Time that Freight Train waits before leaving */
    const uint16_t tRestoreTime = (uint16_t)(3 * tFactorSec);
    /* Boot wait time to get all IO read and debounced first */
    const uint16_t tReadIoSignalWaitTime = (uint16_t)(1 * tFactorSec);
    /* Led blink time */
    const uint16_t tLedBlinkTime = (uint16_t)(200);
    /* Idle TimeOut */
    const uint16_t tIdleTimeOut = (uint16_t)(5 * tFactorSec);
    /* IOX update time */
    const uint16_t tIOXTimeOut = (uint16_t)(10);
    /* IOX reset time */
    const uint16_t tIOXReset = (uint16_t)(1 * tFactorSec);    

    typedef enum
    {
        MAIN_LOOP                       = 01,
        MAIN_STATION_TOP                = 10,
        MAIN_STATION_BOT                = 20,
        WALDSEE                         = 30,
        SIEBWALDE                       = 40,
        WALDBERG                        = 50,
        IODATA                          = 60,
        FALLER_BUS                      = 70,
        FALLER_CARS                     = 80,
    } TASK_ID;				

    typedef enum		
    {
        HEADER                          = 0xAA, //= dec 170
        ALIVE                           = 0xFF, //= dec 255
        STNTRACK1                       = 81,
        STNTRACK2                       = 82,
        STNTRACK3                       = 83,
        MTNTRACK1                       = 84,
        MTNTRACK2                       = 85,
        MTNTRACK7                       = 86,
        MTNTRACK8                       = 87,
		VOLTAGE_DETECTED				= 88,
        
    } TASK_COMMAND;
    
    typedef enum				
    {				
        busy                            = 100,
        connected						= 101,
        done                            = 102,
        command                         = 103,
        nop                             = 104,
		INIT  							= 105,
        INIT2                           = 106,
        RUN                             = 107,
        WAIT                            = 108,
        IDLE                            = 109,
        STN_TO_SIEBWALDE           		= 110,
		SIEBWALDE_TO_STN           		= 111,
        SIEBWALDE_SWITCHT4T5_OUT        = 112,
        SIEBWALDE_SWITCHT4T5_IN         = 113,
        SIEBWALDE_SWITCHT4T5            = 114,
        SIEWALDE_SET_NEXT               = 115,
        STN_INBOUND  					= 116,
        STN_OUTBOUND                    = 117,
		STN_PASSING                		= 118,
        STN_WAIT                        = 119,
        STN_IDLE                        = 120,
        SEQ_IDLE                        = 121,
        SEQ_WAIT                  		= 122,
        SEQ_SET_OCC                     = 123,
		SEQ_CHK_TRAIN              		= 124,
        SEQ_CHK_PASSED                  = 125,
        SEQ_OUTBOUND_LEFT_STATTION      = 126,
        SEQ_INBOUND_BRAKE_TIME          = 127,
        SEQ_OUTBOUND_BRAKE_TIME         = 128,
        SIG_RED        					= 129,
        SIG_GREEN      					= 130,
        INVERT							= 131,
        TIME                            = 132,
        SET_PATH_WAY                    = 133,
        SET_SIGNAL                      = 134,
                
        INDUSTRIAL                      = 135,
        STATION                         = 136,
        FIREDEP_MID                     = 137,
        FIREDEP_RIGHT                   = 138,
        FIREDEP                         = 140,
                
        RESTORE                         = 141,
        RESTORE_NEEDED                  = 142,
        STOP_RESTORE                    = 143,
        
                
//        DRIVE_TO_INDUSTRIAL             = 135,
//        PASS_INDUSTRIAL                 = 136,
//        STOP_AT_INDUSTRIAL              = 137,
//        LEAVE_INDUSTRIAL                = 138,
//                
//        DRIVE_TO_STATION                = 139,
//        PASS_STATION                    = 140,
//        STOP_AT_STATION                 = 141,
//        LEAVE_STATION                   = 142,
//                
//        DRIVE_TO_FIREDEP_MID            = 143,
//        STOP_AT_FIREDEP_MID             = 144,
//        LEAVE_FIREDEP_MID               = 145,
//        DRIVE_TO_FIREDEP_RIGHT          = 145,
//        STOP_AT_FIREDEP_RIGHT           = 146,
//        LEAVE_FIREDEP_RIGHT             = 145,
//        PASS_FIREDEP                    = 147,
        
        
    } TASK_STATE;		

    typedef enum
    {
       NONE    							= 150,
       T1      							= 151,
       T2      							= 152,
       T3      							= 153,
       T4      							= 154,
       T5      							= 155,
       T7      							= 157,
       T8      							= 158,
       TRACK1  							= 159,
       TRACK2  							= 160,
       TRACK3  							= 161,
       TRACK10 							= 162,
       TRACK11 							= 163,
       TRACK12 							= 164, 
       HALT                             = 165,
       DRIVE                            = 166,
       PASS                             = 167,
       PARK                             = 168,
       BRAKE                            = 169,      
       
    } TASK_MESSAGES;
    
    typedef struct
    {
        volatile unsigned char      *portx_ptr;                                 // Reference to the input port used
        uint8_t                     pin_mask;                                   // Mask to point to pin used of port
    }OCC, REL, SERVO;

    typedef struct
    {
        volatile unsigned char      *port1_ptr;                                 // Reference to the input port used
        uint8_t                     pin1_mask;                                  // Mask to point to pin used of port
        volatile unsigned char      *port2_ptr;                                 // Reference to the input port used
        uint8_t                     pin2_mask;                                  // Mask to point to pin used of port
        volatile unsigned char      *port3_ptr;                                 // Reference to the input port used
        uint8_t                     pin3_mask;                                  // Mask to point to pin used of port
        volatile unsigned char      *port4_ptr;                                 // Reference to the input port used
        uint8_t                     pin4_mask;                                  // Mask to point to pin used of port        
    }VEHICLESTOP;

    typedef struct
    {
        TASK_ID                     name;
        TASK_STATE                  AppState;                                   // State of the state meachine
        TASK_STATE                  AppNextState;
        TASK_MESSAGES               LastInboundStn;
        TASK_STATE                  LastState;
        TASK_STATE                  Return;
        VEHICLESTOP                 *setParkAction;        
        DEBOUNCE                    *getVehicleAtStop1;
        bool                        stop1occupied;
        TASK_MESSAGES               parkState1;
        uint16_t                    tWaitTime1;
        uint32_t                    tCountTime1;
        DEBOUNCE                    *getVehicleAtStop2;
        bool                        stop2occupied;
        TASK_MESSAGES               parkState2;        
        uint16_t                    tWaitTime2;
        uint32_t                    tCountTime2;
        uint32_t                    tCountTime;
        uint16_t                    tWaitTime;

    }VEHICLE;
    
    VEHICLE bus;
    VEHICLE firedep;
    
    /* To control the flow of the cars */
    TASK_STATE   FLOWxCONTROLxState = busy;
    TASK_STATE   FLOWxCONTROLxOrder = STATION;
        
    
    /*Yard Functions*/
    #define ARR_SIZE(arr) (sizeof(arr) / sizeof((arr)[0]))
    #define ARR_MAX_ELEM(arr) ((sizeof(arr) / sizeof((arr)[0]))-1)
    
    #define PCB430 0x05U
    #define PCB433 0x04U
    #define PCB432 0x03U
    #define PCB431 0x02U
    #define PCB429 0x01U
    #define PCB428 0x00U
    
    MCP23017_t devices[] = {    
    //IODIRA,IODIRB,IOXRA,IOXRB,address
    [PCB428] = {0x00, 0x00, 0x00, 0x00, 0x26, }, // MCP23017 at address 0x26, PORTA/B = Output, PCB428
    [PCB429] = {0x00, 0x00, 0x00, 0x00, 0x25, }, // MCP23017 at address 0x25, PORTA/B = Output, PCB429
    [PCB431] = {0x00, 0x00, 0xFF, 0xFF, 0x24, }, // MCP23017 at address 0x24, PORTA/B = Output, PCB431 relay card inverted level
    [PCB432] = {0x00, 0x00, 0xFF, 0xFF, 0x23, }, // MCP23017 at address 0x23, PORTA/B = Output, PCB432 relay card inverted level
    [PCB433] = {0x00, 0x00, 0xFF, 0xFF, 0x22, }, // MCP23017 at address 0x22, PORTA/B = Output, PCB433 relay card inverted level
    [PCB430] = {0x00, 0x00, 0x00, 0x00, 0x21, }, // MCP23017 at address 0x21, PORTA/B = Output, PCB430
    };
    
    typedef struct {
        uint8_t IOXRA;
        bool    IOXRAupdate;
        uint8_t IOXRB;
        bool    IOXRBupdate;        
    }IOXDATA;
    
    typedef enum
    {		
        // PCB431 I2C_ADDRESS = 0x24
		BV1   	=	0x0,	
		BV2   	=	0x1,
		BV3   	=	0x2,
		BV4   	=	0x3,
		BV5   	=	0x4,
		BV6   	=	0x5,
		BV7   	=	0x6,
		BV8   	=	0x7,
		BV9   	=	0x8,
		BV10  	=	0x9,
		BV11  	=	0xA,
		BV12  	=	0xB,
		BV13  	=	0xC,
		BV14  	=	0xD,
		BV15  	=	0xE,
		BV16  	=	0xF,
		// PCB432 I2C_ADDRESS = 0x23
		BV17  	=	0x10,
		BV18  	=	0x11,
		BV19  	=	0x12,
		BV20  	=	0x13,
		BV21  	=	0x14,
		BV22  	=	0x15,
		BV23  	=	0x16,
		BV24  	=	0x17,
		BV25  	=	0x18,
		BV26    =	0x19,
        BV27    =	0x1A,
        BV28    =	0x1B,
        BV29    =	0x1C,
        BV30    =	0x1D,
        BV31    =	0x1E,
        BV32    =	0x1F,
        // PCB428 I2C_ADDRESS = 0x26
		BW1   	=	0x20,
		BW2   	=	0x21,
		BW3   	=	0x22,
		BW4   	=	0x23,
		BW5   	=	0x24,
		BW6   	=	0x25,
		BW7   	=	0x26,
		BW8   	=	0x27,
		BW9   	=	0x28,
		BW10  	=	0x29,
		BW11  	=	0x2A,
		BW12  	=	0x2B,
		BW13  	=	0x2C,
		BW14  	=	0x2D,
		BW15  	=	0x2E,
		BW16  	=	0x2F,		
        // PCB429 I2C_ADDRESS = 0x25
		BW17  	=	0x30,
		BW18  	=	0x31,
		BW19  	=	0x32,
		// PCB433 I2C_ADDRESS = 0x22 (MISC))
        BLK22   =   0x33,
        BLK23   =   0x34,
		DISKC   =	0x35,
		CRENA   =	0x36,
		CRDIR   =	0x37,
		DISKL   =	0x38,
		DISKR   =	0x39,
		MISC8   =	0x3A,
		MISC9   =	0x3B,
		MISC10  =	0x3C,
		MISC11  =	0x3D,
		MISC12  =	0x3E,
		MISC13  =	0x3F,
		MISC14  =	0x40,
		MISC15  =	0x41,
		MISC16  =	0x42,
		
    } YARD_OUTPUTS;
    
    typedef struct
    {
        YARD_OUTPUTS            nOutput         ;// The name of the output
        uint8_t                 value           ;// Actual output value
        bool                    invertedLevel   ;// When level must be inverted
        uint8_t                 *portx_ptr      ;// Reference to the Output port used
        uint8_t                 pin_mask        ;// Mask to point to pin used of port        
    }YARDOUTPUT;
    
    YARDOUTPUT yardOutputArr[] = {
        // PCB431 I2C_ADDRESS = 0x24 relay card
        BV1    , 0, true, &devices[PCB431].byteView.IOXRA, 0x1 ,
		BV2    , 0, true, &devices[PCB431].byteView.IOXRA, 0x2 ,
		BV3    , 0, true, &devices[PCB431].byteView.IOXRA, 0x4 ,
		BV4    , 0, true, &devices[PCB431].byteView.IOXRA, 0x8 ,
		BV5    , 0, true, &devices[PCB431].byteView.IOXRA, 0x10,
		BV6    , 0, true, &devices[PCB431].byteView.IOXRA, 0x20,
		BV7    , 0, true, &devices[PCB431].byteView.IOXRA, 0x40,
		BV8    , 0, true, &devices[PCB431].byteView.IOXRA, 0x80,
		BV9    , 0, true, &devices[PCB431].byteView.IOXRB, 0x1 ,
		BV10   , 0, true, &devices[PCB431].byteView.IOXRB, 0x2 ,
		BV11   , 0, true, &devices[PCB431].byteView.IOXRB, 0x4 ,
		BV12   , 0, true, &devices[PCB431].byteView.IOXRB, 0x8 ,
		BV13   , 0, true, &devices[PCB431].byteView.IOXRB, 0x10,
		BV14   , 0, true, &devices[PCB431].byteView.IOXRB, 0x20,
		BV15   , 0, true, &devices[PCB431].byteView.IOXRB, 0x40,
		BV16   , 0, true, &devices[PCB431].byteView.IOXRB, 0x80,
		// PCB432 I2C_ADDRESS = 0x23 relay card
		BV17   , 0, true, &devices[PCB432].byteView.IOXRA, 0x1 ,
		BV18   , 0, true, &devices[PCB432].byteView.IOXRA, 0x2 ,
		BV19   , 0, true, &devices[PCB432].byteView.IOXRA, 0x4 ,
		BV20   , 0, true, &devices[PCB432].byteView.IOXRA, 0x8 ,
		BV21   , 0, true, &devices[PCB432].byteView.IOXRA, 0x10,
		BV22   , 0, true, &devices[PCB432].byteView.IOXRA, 0x20,
		BV23   , 0, true, &devices[PCB432].byteView.IOXRA, 0x40,
		BV24   , 0, true, &devices[PCB432].byteView.IOXRA, 0x80,
		BV25   , 0, true, &devices[PCB432].byteView.IOXRB, 0x1 ,	
		BV26   , 0, true, &devices[PCB432].byteView.IOXRB, 0x2 ,
		BV27   , 0, true, &devices[PCB432].byteView.IOXRB, 0x4 ,
		BV28   , 0, true, &devices[PCB432].byteView.IOXRB, 0x8 ,
		BV29   , 0, true, &devices[PCB432].byteView.IOXRB, 0x10,
		BV30   , 0, true, &devices[PCB432].byteView.IOXRB, 0x20,
		BV31   , 0, true, &devices[PCB432].byteView.IOXRB, 0x40,
		BV32   , 0, true, &devices[PCB432].byteView.IOXRB, 0x80,
        // PCB428 I2C_ADDRESS = 0x26
		BW1    , 0, false, &devices[PCB428].byteView.IOXRA, 0x1 ,
		BW2    , 0, false, &devices[PCB428].byteView.IOXRA, 0x2 ,
		BW3    , 0, false, &devices[PCB428].byteView.IOXRA, 0x4 ,
		BW4    , 0, false, &devices[PCB428].byteView.IOXRA, 0x8 ,
		BW5    , 0, false, &devices[PCB428].byteView.IOXRA, 0x10,
		BW6    , 0, false, &devices[PCB428].byteView.IOXRA, 0x20,
		BW7    , 0, false, &devices[PCB428].byteView.IOXRA, 0x40,
		BW8    , 0, false, &devices[PCB428].byteView.IOXRA, 0x80,
		BW9    , 0, false, &devices[PCB428].byteView.IOXRB, 0x1 ,
		BW10   , 0, false, &devices[PCB428].byteView.IOXRB, 0x2 ,
		BW11   , 0, false, &devices[PCB428].byteView.IOXRB, 0x4 ,
		BW12   , 0, false, &devices[PCB428].byteView.IOXRB, 0x8 ,
		BW13   , 0, false, &devices[PCB428].byteView.IOXRB, 0x10,
		BW14   , 0, false, &devices[PCB428].byteView.IOXRB, 0x20,
		BW15   , 0, false, &devices[PCB428].byteView.IOXRB, 0x40,
        BW16   , 0, false, &devices[PCB428].byteView.IOXRB, 0x80,
		// PCB429 I2C_ADDRESS = 0x25	   
		BW17   , 0, false, &devices[PCB429].byteView.IOXRA, 0x1 ,
		BW18   , 0, false, &devices[PCB429].byteView.IOXRA, 0x2 ,
		BW19   , 0, false, &devices[PCB429].byteView.IOXRA, 0x4 ,	
        // PCB433 I2C_ADDRESS = 0x22 relay card
		BLK22  , 0, true,  &devices[PCB433].byteView.IOXRA, 0x1 ,
		BLK23  , 0, true,  &devices[PCB433].byteView.IOXRA, 0x2 ,
		DISKC  , 0, true,  &devices[PCB433].byteView.IOXRA, 0x4 ,
		CRENA  , 0, true,  &devices[PCB433].byteView.IOXRA, 0x8 ,
		CRDIR  , 0, true,  &devices[PCB433].byteView.IOXRA, 0x10,
		DISKL  , 0, true,  &devices[PCB433].byteView.IOXRA, 0x20,
		DISKR  , 0, true,  &devices[PCB433].byteView.IOXRA, 0x40,
		MISC8  , 0, true,  &devices[PCB433].byteView.IOXRA, 0x80,
		MISC9  , 0, true,  &devices[PCB433].byteView.IOXRB, 0x1 ,
		MISC10 , 0, true,  &devices[PCB433].byteView.IOXRB, 0x2 ,
		MISC11 , 0, true,  &devices[PCB433].byteView.IOXRB, 0x4 ,
		MISC12 , 0, true,  &devices[PCB433].byteView.IOXRB, 0x8 ,
		MISC13 , 0, true,  &devices[PCB433].byteView.IOXRB, 0x10,
		MISC14 , 0, true,  &devices[PCB433].byteView.IOXRB, 0x20,
		MISC15 , 0, true,  &devices[PCB433].byteView.IOXRB, 0x40,
        MISC16 , 0, true,  &devices[PCB433].byteView.IOXRB, 0x80,				
	};
    
    typedef enum{
        // PCB430 I2C_ADDRESS = 0x21
        BVLED1  =	0x0,
		BVLED2  =	0x1,
		BVLED3  =	0x2,
		BVLED4  =	0x3,
		BVLED5  =	0x4,
		BVLED6  =	0x5,
		BVLED7  =	0x6,
		BVLED8  =	0x7,
		BVLED9  =	0x8,
		BVLED10 =	0x9,
		BVLED11 =	0xA,
		BVLED12 =	0xB,
		BVLED13 =	0xC,		
		BVLED14 =	0xD,
		BVLED15 =	0xE,
		BVLED16 =	0xF,
                
		// PCB429 I2C_ADDRESS = 0x22
		BVLED17 =	0x10,
		BVLED18 =	0x11,
		BVLED19 =	0x12,
		BVLED20 =	0x13,
		BVLED21 =	0x14,
		BVLED22 =	0x15,
		BVLED23 =	0x16,
		BVLED24 =	0x17,
		BVLED25 =	0x18, // Rotate Disk               
		BVLED26 =	0x19, // Crane
		BVLED27 =	0x1A, // Spare
		BVLED28 =	0x1B, // Spare
		BVLED29 =	0x1C, // Spare
        BVLEDZERO = 0x1D, // Reset all
        BVLEDINIT = 0x1E, // Init certain BV's at startup
                
    }YARD_LEDS;
    
    typedef enum{
        OFF    = 3, // led is/set-to off, not used, enLed is used for this
        ON     = 4, // led is/set-to on and function was activated
        BLINK  = 5, // Led is/set-to blinking function not yet activated
        TOGGLE = 6, // Led is toggled
    }LEDSTATE;
    
    typedef struct
    {
        YARD_LEDS               nled;         // The name of the led        
        LEDSTATE                state;        // Actual led state, On/Off/Blink       
        bool                    funcActivated;// Function activated state
        //bool                    enLed;        // Enable the led according to state
        uint8_t                 *portx_ptr;   // Reference to the Output port used
        uint8_t                 pin_mask;     // Mask to point to pin used of port        
    }YARDLED;
    
    YARDLED yardLedArr[] = {
		// PCB430 I2C_ADDRESS = 0x21
        BVLED1 , BLINK, false, &devices[PCB430].byteView.IOXRA, 0x1 ,
		BVLED2 , BLINK, false, &devices[PCB430].byteView.IOXRA, 0x2 ,
		BVLED3 , BLINK, false, &devices[PCB430].byteView.IOXRA, 0x4 ,
		BVLED4 , BLINK, false, &devices[PCB430].byteView.IOXRA, 0x8 ,
		BVLED5 , BLINK, false, &devices[PCB430].byteView.IOXRA, 0x10,
		BVLED6 , BLINK, false, &devices[PCB430].byteView.IOXRA, 0x20,
		BVLED7 , BLINK, false, &devices[PCB430].byteView.IOXRA, 0x40,
		BVLED8 , BLINK, false, &devices[PCB430].byteView.IOXRA, 0x80,
		BVLED9 , BLINK, false, &devices[PCB430].byteView.IOXRB, 0x1 ,
		BVLED10, BLINK, false, &devices[PCB430].byteView.IOXRB, 0x2 ,
		BVLED11, BLINK, false, &devices[PCB430].byteView.IOXRB, 0x4 ,
		BVLED12, BLINK, false, &devices[PCB430].byteView.IOXRB, 0x8 ,
        BVLED13, BLINK, false, &devices[PCB430].byteView.IOXRB, 0x10,		
		BVLED14, BLINK, false, &devices[PCB430].byteView.IOXRB, 0x20,
		BVLED15, BLINK, false, &devices[PCB430].byteView.IOXRB, 0x40,
		BVLED16, BLINK, false, &devices[PCB430].byteView.IOXRB, 0x80,
        // PCB429 I2C_ADDRESS = 0x25
		BVLED17, BLINK, false, &devices[PCB429].byteView.IOXRA, 0x8 ,
		BVLED18, BLINK, false, &devices[PCB429].byteView.IOXRA, 0x10,
		BVLED19, BLINK, false, &devices[PCB429].byteView.IOXRA, 0x20,
		BVLED20, BLINK, false, &devices[PCB429].byteView.IOXRA, 0x40,
		BVLED21, BLINK, false, &devices[PCB429].byteView.IOXRA, 0x80,
		BVLED22, BLINK, false, &devices[PCB429].byteView.IOXRB, 0x1 ,
		BVLED23, BLINK, false, &devices[PCB429].byteView.IOXRB, 0x2 ,
		BVLED24, BLINK, false, &devices[PCB429].byteView.IOXRB, 0x4 ,
		BVLED25, BLINK, false, &devices[PCB429].byteView.IOXRB, 0x8 ,
		BVLED26, BLINK, false, &devices[PCB429].byteView.IOXRB, 0x10,
		BVLED27, BLINK, false, &devices[PCB429].byteView.IOXRB, 0x20,
		BVLED28, BLINK, false, &devices[PCB429].byteView.IOXRB, 0x40,
        BVLED29, BLINK, false, &devices[PCB429].byteView.IOXRB, 0x80,
    };

    // Enum to distinguish the array type
    typedef enum{
        OUTPUTS,
        LEDS,
    }ARRAY_TYPE;
    
    typedef enum{
        NOF = 0,
        NEXT = 3,
        PREV = 4,
        JMP = 5,
        ASSERT = 6,
    }CHANNEL;
    
    typedef struct{        
        uint32_t                    tStartBlinkTime; // The start time for blinking
        uint16_t                    tWaitBlinkTime;        
        uint32_t                    tStartIdleTime; // The start time for timeout
        uint16_t                    tWaitIdleTime;
        uint32_t                    tStartResetTime; // The start time for reset IOX
        uint16_t                    tWaitResetTime;
        uint32_t                    tStartIOXTime; // The start time for IOX update
        uint16_t                    tWaitIOXTime;
        bool                        idle;
    }TIMERS;
    
    /* Rules */
    
    typedef struct {
        int outputIndex; // Index of the output to be set (e.g., BVx, BWx)
        bool state;      // Desired state (true/false)
    } Rule;
    
    static const Rule bvled1Rules[] = {
        {BV1, true},
        
        {BW1, true},
        {BW19, false},
        {BW8, true},
        {BW7, false},
    };

    static const Rule bvled2Rules[] = {
        {BV2, true},
        
        {BW1, false},
        {BW2, false},
        {BW3, true},
        
        {BW10, true},
        {BW7, false},
        {BW8, false},
    };
    
    static const Rule bvled3Rules[] = {
        {BV3, true},
        
        {BW1, false},
        {BW2, false},
        {BW3, false},
        {BW4, false},

        {BW7, false},
        {BW8, false},
        {BW9, false},
        {BW10, false},
    };
    
    static const Rule bvled4Rules[] = {
        {BV4, true},
        
        {BW1, false},
        {BW2, false},
        {BW3, false},
        {BW4, true},
        
        {BW9, true},
        {BW7, false},
        {BW8, false},
    };
    
    static const Rule bvled5Rules[] = {
        {BV5, true},
        
        {BW1, false},
        {BW2, true},
        {BW5, false},
        
        {BW12, false},
        {BW11, false},
        {BW7, true},
    };
    
    static const Rule bvled6Rules[] = {
        {BV6, true},
        
        {BW1, false},
        {BW2, true},
        {BW5, true},
        {BW6, false},
        
        {BW12, true},
        {BW11, false},
        {BW7, true},
    };
    
    static const Rule bvled7Rules[] = {
        {BV7, true},
        {BV6, true},
        
        {BW6, true},
        
        {BW12, true},
        {BW11, false},
        {BW7, true},
    };
    
    static const Rule bvled8Rules[] = {
        {BV8, true},
        
        {BW13, false},
        {BW11, true},
        {BW7, true},
    };
    
    static const Rule bvled9Rules[] = {
        {BV9, true},
        
        {BW14, false},
        {BW13, true},
        {BW11, true},
        {BW7, true},
    };
    
    static const Rule bvled10Rules[] = {
        {BV10, true},
        
        {BW15, false},
        {BW14, true},
        {BW13, true},
        {BW11, true},
        {BW7, true},
    };
    
    static const Rule bvled11Rules[] = {
        {BV11, true},
        
        {BW16, false},
        {BW15, true},
        {BW14, true},
        {BW13, true},
        {BW11, true},
        {BW7, true},
    };
    
    static const Rule bvled12Rules[] = {
        {BV12, true},
        
        {BW17, false},
        {BW16, true},
        {BW15, true},
        {BW14, true},
        {BW13, true},
        {BW11, true},
        {BW7, true},
    };
    
    static const Rule bvled13Rules[] = {
        {BV13, true},
        
        {BW18, false},
        {BW17, true},
        {BW16, true},
        {BW15, true},
        {BW14, true},
        {BW13, true},
        {BW11, true},
        {BW7, true},
    };
    
    static const Rule bvled14Rules[] = {
        {BV14, true},
        
        {BW18, true},
        {BW17, true},
        {BW16, true},
        {BW15, true},
        {BW14, true},
        {BW13, true},
        {BW11, true},
        {BW7, true},
    };
    
    static const Rule bvled15Rules[] = {
        {BV15, true},
    };
    
    static const Rule bvled16Rules[] = {
        {BV16, true},
    };
    
    static const Rule bvled17Rules[] = {
        {BV17, true},
    };
    
    static const Rule bvled18Rules[] = {
        {BV18, true},
    };
    
    static const Rule bvled19Rules[] = {
        {BV19, true},
    };
    
    static const Rule bvled20Rules[] = {
        {BV20, true},
    };
    
    static const Rule bvled21Rules[] = {
        {BV21, true},
    };
    
    static const Rule bvled22Rules[] = {
        {BV22, true},
    };
    
    static const Rule bvled23Rules[] = {
        {BV23, true},
    };
    
    static const Rule bvled24Rules[] = {
        {BV24, true},
    };
    
    static const Rule bvled25Rules[] = {
        {BV25, true}, 
    };
    
    static const Rule bvled26Rules[] = {
        {BV26, true}, // Rotate disk power
    };
    
    static const Rule bvled27Rules[] = {
        {CRENA, true}, // Yard Crane
    };
    
    static const Rule bvled28Rules[] = {
        {BV28, true}, // Spare
    };
    
    static const Rule bvled29Rules[] = {
        {BV29, true}, // Spare
    };
    
    static const Rule bvResetZero[] = {
        {BV1, false},
        {BV2, false},
        {BV3, false},
        {BV4, false},
        {BV5, false},
        {BV6, false},
        //{BV7, false},
        {BV8, false},
        {BV9, false},
        {BV10, false},
        {BV11, false},
        {BV12, false},
        {BV13, false},
        {BV14, false},
        {BV15, false},
        {BV16, false},
        {BV17, false},
        {BV18, false},
        {BV19, false},
        {BV20, false},
        {BV21, false},
        {BV22, false},
        {BV23, false},
        {BV24, false},
        {BV25, false},
        //{BV26, false}, Rotate Disk rail power
        {BV27, false},
        {BV28, false},
        {BV29, false},        
    };
    
    static const Rule bvledInit[] = {
        {BV26, true}, // Rotate Disk rail power
        {BV7, true},  
    };
            
    typedef struct {
        const Rule *rules; // Pointer to the array of rules
        size_t ruleCount;  // Number of rules in the array
    } RuleSet;

    static const RuleSet ruleTable[] = {
        [BVLED1] = {bvled1Rules, sizeof(bvled1Rules) / sizeof(bvled1Rules[0])},
        [BVLED2] = {bvled2Rules, sizeof(bvled2Rules) / sizeof(bvled2Rules[0])},
        [BVLED3] = {bvled3Rules, sizeof(bvled3Rules) / sizeof(bvled3Rules[0])},        
        [BVLED4] = {bvled4Rules, sizeof(bvled4Rules) / sizeof(bvled4Rules[0])},
        [BVLED5] = {bvled5Rules, sizeof(bvled5Rules) / sizeof(bvled5Rules[0])},
        [BVLED6] = {bvled6Rules, sizeof(bvled6Rules) / sizeof(bvled6Rules[0])},        
        [BVLED7] = {bvled7Rules, sizeof(bvled7Rules) / sizeof(bvled7Rules[0])},
        [BVLED8] = {bvled8Rules, sizeof(bvled8Rules) / sizeof(bvled8Rules[0])},        
        [BVLED9] = {bvled9Rules, sizeof(bvled9Rules) / sizeof(bvled9Rules[0])},
        [BVLED10] = {bvled10Rules, sizeof(bvled10Rules) / sizeof(bvled10Rules[0])},
        [BVLED11] = {bvled11Rules, sizeof(bvled11Rules) / sizeof(bvled11Rules[0])},
        [BVLED12] = {bvled12Rules, sizeof(bvled12Rules) / sizeof(bvled12Rules[0])},
        [BVLED13] = {bvled13Rules, sizeof(bvled13Rules) / sizeof(bvled13Rules[0])},
        [BVLED14] = {bvled14Rules, sizeof(bvled14Rules) / sizeof(bvled14Rules[0])},        
        [BVLED15] = {bvled15Rules, sizeof(bvled15Rules) / sizeof(bvled15Rules[0])},
        [BVLED16] = {bvled16Rules, sizeof(bvled16Rules) / sizeof(bvled16Rules[0])},
        [BVLED17] = {bvled17Rules, sizeof(bvled17Rules) / sizeof(bvled17Rules[0])},
        [BVLED18] = {bvled18Rules, sizeof(bvled18Rules) / sizeof(bvled18Rules[0])},
        [BVLED19] = {bvled19Rules, sizeof(bvled19Rules) / sizeof(bvled19Rules[0])},
        [BVLED20] = {bvled20Rules, sizeof(bvled20Rules) / sizeof(bvled20Rules[0])},        
        [BVLED21] = {bvled21Rules, sizeof(bvled21Rules) / sizeof(bvled21Rules[0])},
        [BVLED22] = {bvled22Rules, sizeof(bvled22Rules) / sizeof(bvled22Rules[0])},
        [BVLED23] = {bvled23Rules, sizeof(bvled23Rules) / sizeof(bvled23Rules[0])},
        [BVLED24] = {bvled24Rules, sizeof(bvled24Rules) / sizeof(bvled24Rules[0])},
		[BVLED25] = {bvled25Rules, sizeof(bvled25Rules) / sizeof(bvled25Rules[0])},
		[BVLED26] = {bvled26Rules, sizeof(bvled26Rules) / sizeof(bvled26Rules[0])},
		[BVLED27] = {bvled27Rules, sizeof(bvled27Rules) / sizeof(bvled27Rules[0])},
		[BVLED28] = {bvled28Rules, sizeof(bvled28Rules) / sizeof(bvled28Rules[0])},
		[BVLED29] = {bvled29Rules, sizeof(bvled29Rules) / sizeof(bvled29Rules[0])},
		[BVLEDZERO] = {bvResetZero, sizeof(bvResetZero) / sizeof(bvResetZero[0])},
        [BVLEDINIT] = {bvledInit, sizeof(bvledInit) / sizeof(bvledInit[0])},
    };
     
#ifdef	__cplusplus
}
#endif

#endif	/* ENUMS_H */

