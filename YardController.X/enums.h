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
    
    /* factor to be set according to set timer interrupt value to calc seconds */
    const uint32_t tFactorSec = 1000; // timer set to 1ms, 10 * 1000 = 1 second
    /* To shift the max result of 32 sec to 131 seconds if val = 2 */
    const uint8_t  tRandomShift = 0;
    /* Time to wait for servo to move to new position */
    const uint32_t tSwitchPointWaitTime = (uint32_t)(1 * tFactorSec);
    /* Minimum Time that Train waits before leaving */
    const uint32_t tParkTime = (uint32_t)(10 * tFactorSec);
    /* Time that Freight Train waits before leaving */
    const uint32_t tRestoreTime = (uint32_t)(2 * tFactorSec);
    /* Boot wait time to get all IO read and debounced first */
    const uint32_t tReadIoSignalWaitTime = (uint32_t)(10 * tFactorSec);
    

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
    TASK_STATE   state = busy;
    TASK_STATE   order = STATION;
    
#ifdef	__cplusplus
}
#endif

#endif	/* ENUMS_H */

