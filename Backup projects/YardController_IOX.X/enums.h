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

    #include <stdbool.h>
    #include "debounce.h"
    #include "mcp23017.h"
    
    /* factor to be set according to set timer interrupt value to calc seconds */
    const uint32_t tFactorSec = 1000; // timer set to 1ms, 10 * 1000 = 1 second
    /* To shift the max result of 32 sec to 131 seconds if val = 2 */
    const uint8_t  tRandomShift = 0;
    /* Time to wait for servo to move to new position */
    const uint32_t tSwitchPointWaitTime = (uint32_t)(1 * tFactorSec);
    /* Minimum Time that Train waits before leaving */
    const uint32_t tParkTime = (uint32_t)(1 * tFactorSec);
    /* Time that Freight Train waits before leaving */
    const uint32_t tRestoreTime = (uint32_t)(3 * tFactorSec);
    /* Boot wait time to get all IO read and debounced first */
    const uint32_t tReadIoSignalWaitTime = (uint32_t)(1 * tFactorSec);
    

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
        uint32_t                    tWaitTime1;
        uint32_t                    tCountTime1;
        DEBOUNCE                    *getVehicleAtStop2;
        bool                        stop2occupied;
        TASK_MESSAGES               parkState2;        
        uint32_t                    tWaitTime2;
        uint32_t                    tCountTime2;
        uint32_t                    tCountTime;
        uint32_t                    tWaitTime;

    }VEHICLE;
    
    VEHICLE bus;
    VEHICLE firedep;
    
    /* To control the flow of the cars */
    TASK_STATE   FLOWxCONTROLxState = busy;
    TASK_STATE   FLOWxCONTROLxOrder = STATION;
        
    
    /*Yard Functions*/
    #define ARRAY_SIZE(arr) (sizeof(arr) / sizeof((arr)[0]))
    
    MCP23017_t devices[] = {
    {0x00, 0x00, 0x00, 0x00, 0x21, }, // MCP23017 at address 0x21, PORTA/B = Output
    {0x00, 0x00, 0x00, 0x00, 0x22, }, // MCP23017 at address 0x22, PORTA/B = Output
    {0x00, 0x00, 0x00, 0x00, 0x23, }, // MCP23017 at address 0x23, PORTA/B = Output
    {0x00, 0x00, 0x00, 0x00, 0x24, }, // MCP23017 at address 0x24, PORTA/B = Output
    {0x00, 0x00, 0x00, 0x00, 0x25, }, // MCP23017 at address 0x25, PORTA/B = Output
    {0x00, 0x00, 0x00, 0x00, 0x26, }, // MCP23017 at address 0x26, PORTA/B = Output
    };
    
    typedef enum
    {
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
		
		BV17  	=	0x10,
		BV18  	=	0x11,
		BV19  	=	0x12,
		BV20  	=	0x13,
		BV21  	=	0x14,
		BV22  	=	0x15,
		BV23  	=	0x16,
		BV24  	=	0x17,
		BV25  	=	0x18,
		BVSP1   =	0x19,
        BVSP2   =	0x1A,
        BVSP3   =	0x1B,
        BVSP4   =	0x1C,
        BVSP5   =	0x1D,
        BVSP6   =	0x1E,
        BVSP7   =	0x1F,                
                
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
		
		BW17  	=	0x30,
		BW18  	=	0x31,
		BW19  	=	0x32,		
		
        BV22AB  =   0x33,
        BV23AB  =   0x34,
		MISC3   =	0x35,
		MISC4   =	0x36,
		MISC5   =	0x37,
		MISC6   =	0x38,
		MISC7   =	0x39,
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
        YARD_OUTPUTS            output          ;// The name of the output
        uint8_t                 lastOutputState ;// Previous output value
        uint8_t                 value           ;// Actual output value
        bool                    invertedLevel   ;// When level must be inverted
        bool                    valueUpdated    ;// Indicates a change in the value    
        uint8_t                 *portx_ptr      ;// Reference to the Output port used
        uint8_t                 pin_mask        ;// Mask to point to pin used of port        
    }YARDOUTPUT;
    
    YARDOUTPUT yardOutputArr[] = {
        
        BV1    , 0, 0, 0, 0, &devices[3].byteView.IOXRA, 0x1 ,
		BV2    , 0, 0, 0, 0, &devices[3].byteView.IOXRA, 0x2 ,
		BV3    , 0, 0, 0, 0, &devices[3].byteView.IOXRA, 0x4 ,
		BV4    , 0, 0, 0, 0, &devices[3].byteView.IOXRA, 0x8 ,
		BV5    , 0, 0, 0, 0, &devices[3].byteView.IOXRA, 0x10,
		BV6    , 0, 0, 0, 0, &devices[3].byteView.IOXRA, 0x20,
		BV7    , 0, 0, 0, 0, &devices[3].byteView.IOXRA, 0x40,
		BV8    , 0, 0, 0, 0, &devices[3].byteView.IOXRA, 0x80,
		BV9    , 0, 0, 0, 0, &devices[3].byteView.IOXRB, 0x1 ,
		BV10   , 0, 0, 0, 0, &devices[3].byteView.IOXRB, 0x2 ,
		BV11   , 0, 0, 0, 0, &devices[3].byteView.IOXRB, 0x4 ,
		BV12   , 0, 0, 0, 0, &devices[3].byteView.IOXRB, 0x8 ,
		BV13   , 0, 0, 0, 0, &devices[3].byteView.IOXRB, 0x10,
		BV14   , 0, 0, 0, 0, &devices[3].byteView.IOXRB, 0x20,
		BV15   , 0, 0, 0, 0, &devices[3].byteView.IOXRB, 0x40,
		BV16   , 0, 0, 0, 0, &devices[3].byteView.IOXRB, 0x80,
		
		BV17   , 0, 0, 0, 0, &devices[4].byteView.IOXRA, 0x1 ,
		BV18   , 0, 0, 0, 0, &devices[4].byteView.IOXRA, 0x2 ,
		BV19   , 0, 0, 0, 0, &devices[4].byteView.IOXRA, 0x4 ,
		BV20   , 0, 0, 0, 0, &devices[4].byteView.IOXRA, 0x8 ,
		BV21   , 0, 0, 0, 0, &devices[4].byteView.IOXRA, 0x10,
		BV22   , 0, 0, 0, 0, &devices[4].byteView.IOXRA, 0x20,
		BV23   , 0, 0, 0, 0, &devices[4].byteView.IOXRA, 0x40,
		BV24   , 0, 0, 0, 0, &devices[4].byteView.IOXRA, 0x80,
		BV25   , 0, 0, 0, 0, &devices[4].byteView.IOXRB, 0x1 ,	
		BVSP1  , 0, 0, 0, 0, &devices[4].byteView.IOXRB, 0x2 ,
		BVSP2  , 0, 0, 0, 0, &devices[4].byteView.IOXRB, 0x4 ,
		BVSP3  , 0, 0, 0, 0, &devices[4].byteView.IOXRB, 0x8 ,
		BVSP4  , 0, 0, 0, 0, &devices[4].byteView.IOXRB, 0x10,
		BVSP5  , 0, 0, 0, 0, &devices[4].byteView.IOXRB, 0x20,
		BVSP6  , 0, 0, 0, 0, &devices[4].byteView.IOXRB, 0x40,
		BVSP7  , 0, 0, 0, 0, &devices[4].byteView.IOXRB, 0x80,
        
		BW1    , 0, 0, 0, 0, &devices[0].byteView.IOXRA, 0x1 ,
		BW2    , 0, 0, 0, 0, &devices[0].byteView.IOXRA, 0x2 ,
		BW3    , 0, 0, 0, 0, &devices[0].byteView.IOXRA, 0x4 ,
		BW4    , 0, 0, 0, 0, &devices[0].byteView.IOXRA, 0x8 ,
		BW5    , 0, 0, 0, 0, &devices[0].byteView.IOXRA, 0x10,
		BW6    , 0, 0, 0, 0, &devices[0].byteView.IOXRA, 0x20,
		BW7    , 0, 0, 0, 0, &devices[0].byteView.IOXRA, 0x40,
		BW8    , 0, 0, 0, 0, &devices[0].byteView.IOXRA, 0x80,
		BW9    , 0, 0, 0, 0, &devices[0].byteView.IOXRB, 0x1 ,
		BW10   , 0, 0, 0, 0, &devices[0].byteView.IOXRB, 0x2 ,
		BW11   , 0, 0, 0, 0, &devices[0].byteView.IOXRB, 0x4 ,
		BW12   , 0, 0, 0, 0, &devices[0].byteView.IOXRB, 0x8 ,
		BW13   , 0, 0, 0, 0, &devices[0].byteView.IOXRB, 0x10,
		BW14   , 0, 0, 0, 0, &devices[0].byteView.IOXRB, 0x20,
		BW15   , 0, 0, 0, 0, &devices[0].byteView.IOXRB, 0x40,
        BW16   , 0, 0, 0, 0, &devices[0].byteView.IOXRB, 0x80,
			   
		BW17   , 0, 0, 0, 0, &devices[1].byteView.IOXRA, 0x1 ,
		BW18   , 0, 0, 0, 0, &devices[1].byteView.IOXRA, 0x2 ,
		BW19   , 0, 0, 0, 0, &devices[1].byteView.IOXRA, 0x4 ,	
		
		BV22AB , 0, 0, 0, 0, &devices[5].byteView.IOXRA, 0x1 ,
		BV23AB , 0, 0, 0, 0, &devices[5].byteView.IOXRA, 0x2 ,
		MISC3  , 0, 0, 0, 0, &devices[5].byteView.IOXRA, 0x4 ,
		MISC4  , 0, 0, 0, 0, &devices[5].byteView.IOXRA, 0x8 ,
		MISC5  , 0, 0, 0, 0, &devices[5].byteView.IOXRA, 0x10,
		MISC6  , 0, 0, 0, 0, &devices[5].byteView.IOXRA, 0x20,
		MISC7  , 0, 0, 0, 0, &devices[5].byteView.IOXRA, 0x40,
		MISC8  , 0, 0, 0, 0, &devices[5].byteView.IOXRA, 0x80,
		MISC9  , 0, 0, 0, 0, &devices[5].byteView.IOXRB, 0x1 ,
		MISC10 , 0, 0, 0, 0, &devices[5].byteView.IOXRB, 0x2 ,
		MISC11 , 0, 0, 0, 0, &devices[5].byteView.IOXRB, 0x4 ,
		MISC12 , 0, 0, 0, 0, &devices[5].byteView.IOXRB, 0x8 ,
		MISC13 , 0, 0, 0, 0, &devices[5].byteView.IOXRB, 0x10,
		MISC14 , 0, 0, 0, 0, &devices[5].byteView.IOXRB, 0x20,
		MISC15 , 0, 0, 0, 0, &devices[5].byteView.IOXRB, 0x40,
        MISC16 , 0, 0, 0, 0, &devices[5].byteView.IOXRB, 0x80,				
	};
    
    typedef enum{
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
		BVLED17 =	0x10,
		BVLED18 =	0x11,
		BVLED19 =	0x12,
		BVLED20 =	0x13,
		BVLED21 =	0x14,
		BVLED22 =	0x15,
		BVLED23 =	0x16,
		BVLED24 =	0x17,
		BVLED25 =	0x18,
		BVLED26 =	0x19,
		BVLED27 =	0x1A,
		BVLED28 =	0x1B,
		BVLED29 =	0x1C,
    }YARD_LEDS;
    
    typedef struct
    {
        YARD_LEDS               function;     // The name of the function        
        uint8_t                 value;        // Actual output value        
        bool                    valueUpdated; // Indicates a change in the value    
        uint8_t                 *portx_ptr;   // Reference to the Output port used
        uint8_t                 pin_mask;     // Mask to point to pin used of port        
    }YARDLED;
    
    YARDLED yardLedArr[] = {
        BVLED1 , 0, 0, &devices[1].byteView.IOXRA, 0x8 ,
		BVLED2 , 0, 0, &devices[1].byteView.IOXRA, 0x10,
		BVLED3 , 0, 0, &devices[1].byteView.IOXRA, 0x20,
		BVLED4 , 0, 0, &devices[1].byteView.IOXRA, 0x40,
		BVLED5 , 0, 0, &devices[1].byteView.IOXRA, 0x80,
		BVLED6 , 0, 0, &devices[1].byteView.IOXRB, 0x1 ,
		BVLED7 , 0, 0, &devices[1].byteView.IOXRB, 0x2 ,
		BVLED8 , 0, 0, &devices[1].byteView.IOXRB, 0x4 ,
		BVLED9 , 0, 0, &devices[1].byteView.IOXRB, 0x8 ,
		BVLED10, 0, 0, &devices[1].byteView.IOXRB, 0x10,
		BVLED11, 0, 0, &devices[1].byteView.IOXRB, 0x20,
		BVLED12, 0, 0, &devices[1].byteView.IOXRB, 0x40,
        BVLED13, 0, 0, &devices[1].byteView.IOXRB, 0x80,
		
		BVLED14, 0, 0, &devices[2].byteView.IOXRA, 0x1 ,
		BVLED15, 0, 0, &devices[2].byteView.IOXRA, 0x2 ,
		BVLED16, 0, 0, &devices[2].byteView.IOXRA, 0x4 ,
		BVLED17, 0, 0, &devices[2].byteView.IOXRA, 0x8 ,
		BVLED18, 0, 0, &devices[2].byteView.IOXRA, 0x10,
		BVLED19, 0, 0, &devices[2].byteView.IOXRA, 0x20,
		BVLED20, 0, 0, &devices[2].byteView.IOXRA, 0x40,
		BVLED21, 0, 0, &devices[2].byteView.IOXRA, 0x80,
		BVLED22, 0, 0, &devices[2].byteView.IOXRB, 0x1 ,
		BVLED23, 0, 0, &devices[2].byteView.IOXRB, 0x2 ,
		BVLED24, 0, 0, &devices[2].byteView.IOXRB, 0x4 ,
		BVLED25, 0, 0, &devices[2].byteView.IOXRB, 0x8 ,
		BVLED26, 0, 0, &devices[2].byteView.IOXRB, 0x10,
		BVLED27, 0, 0, &devices[2].byteView.IOXRB, 0x20,
		BVLED28, 0, 0, &devices[2].byteView.IOXRB, 0x40,
        BVLED29, 0, 0, &devices[2].byteView.IOXRB, 0x80,
    };

    // Enum to distinguish the array type
    typedef enum{
        OUTPUTS,
        LEDS,
    }ARRAY_TYPE;
     
#ifdef	__cplusplus
}
#endif

#endif	/* ENUMS_H */

