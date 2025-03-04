#include "main.h"
#include "enums.h"
#include "mcc_generated_files/mcc.h"
#include "milisecond_counter.h"
#include "communication.h"
#include "debounce.h"
#include "rand.h"
#include <string.h>
#include <stdbool.h>
#include "bus.h"
#include "firedep.h"
#include "yard_functions.h"

static bool         updateTick = false;
static uint32_t     tBootWaitTimeCnt = 1;
static bool         booted = false;

static uint32_t     tSendAliveMessWaitTimeCnt = 0;
static udpTrans_t   StatusMessage;
static udpTrans_t   DataMessage;
static uint8_t      tmp = 0;

static bool toggle = false;
/*
                         Main application
 */

void main(void)
{
    // Initialize the device
    SYSTEM_Initialize();

    // If using interrupts in PIC18 High/Low Priority Mode you need to enable the Global High and Low Interrupts
    // If using interrupts in PIC Mid-Range Compatibility Mode you need to enable the Global and Peripheral Interrupts
    // Use the following macros to:

    // Enable the Global Interrupts
    INTERRUPT_GlobalInterruptEnable();

    // Disable the Global Interrupts
    //INTERRUPT_GlobalInterruptDisable();

    // Enable the Peripheral Interrupts
    INTERRUPT_PeripheralInterruptEnable();

    // Disable the Peripheral Interrupts
    //INTERRUPT_PeripheralInterruptDisable();
    
    MILLIESxINIT();
    SETxMILLISECONDxUPDATExHANDLER(UpdateTick);
    PROCESSxETHxDATAxINIT();
    INITxRANDxNUMBER();
    INITxBUSxDIRIVE();
    INITxFIREDEP();
    INITxYARDxFUNCTION();    
    /* Kick all servo controllers */
    LATE = 0xFF;
    FLOWxCONTROLxOrder = STATION;

    while (1)
    {
        EXT_WDT_Toggle();
        
        //TP4_SetHigh();
        #ifndef DEBUG
        PROCESSxETHxDATA();
        #endif        
        //TP4_SetLow();
        
        /* First boot up sequence, set IO, warm up debounce first */
        // <editor-fold defaultstate="collapsed">        
        if(false == booted){
            
            if(true == updateTick){
                DebounceIO();
                tBootWaitTimeCnt++;
                updateTick = false;
            }
            /* 
             * After 2 seconds warm up time check if driving 
             * voltage is present, if not reset warmup time
             */
            if(tBootWaitTimeCnt > tReadIoSignalWaitTime){
                tBootWaitTimeCnt = 0;
                booted = true;                
                // release all servos
                LATE = 0x00;                
            }
        }
        // </editor-fold>
        /* 
         * When driving voltage is present and system booted, 
         * execute state machines 
         */
        // <editor-fold defaultstate="collapsed">
        else if(booted)
        {
            if(true == updateTick){
                
                DebounceIO();
                UPDATExYARDxFUNCTIONxSELECTION();
                                
                if(1){ /* Here normally the udp connected check */
                    /*
                    * MainStation methods
                    */
                    
                    /* Determine order of departure to prohibit overtaking */
                    switch(FLOWxCONTROLxOrder){
                        case STATION: if(FLOWxCONTROLxState == STATION){
                            FLOWxCONTROLxOrder = FIREDEP;
                            FLOWxCONTROLxState = busy;
                        }
                        break;
                        
                        case FIREDEP: if(FLOWxCONTROLxState == FIREDEP){
                            FLOWxCONTROLxOrder = STATION;
                            FLOWxCONTROLxState = busy;
                        }
                        break;
                            
                        default:break;
                    }
                    
                    //TP1_SetHigh();
                    UPDATExBUSxDRIVExWAIT(&bus);
                    //TP1_SetLow();
                    TP2_SetHigh();
                    UPDATExBUSxDRIVE(&bus);
                    TP2_SetLow();
 
                    //TP1_SetHigh();
                    UPDATExFIREDEPxDRIVExWAIT(&firedep);
                    //TP1_SetLow();
                    TP2_SetHigh();
                    UPDATExFIREDEPxDRIVE(&firedep);
                    TP2_SetLow();                   
                    
                    // <editor-fold defaultstate="collapsed">
                    TP3_SetHigh();
                    DataMessage.header = (uint8_t)HEADER;
                    DataMessage.command = (uint8_t)IODATA;                    
                    tmp = 0;
                    tmp = (uint8_t)(tmp << 1) | HALL_BUSSTOP_STN.value;       //MSB
                    tmp = (uint8_t)(tmp << 1) | HALL_BUSSTOP_IND.value;
                    tmp = (uint8_t)(tmp << 1) | HALL_STOP_FDEP.value;
                    tmp = (uint8_t)(tmp << 1) | false;
                    tmp = (uint8_t)(tmp << 1) | false;
                    tmp = (uint8_t)(tmp << 1) | false;
                    tmp = (uint8_t)(tmp << 1) | false;
                    tmp = (uint8_t)(tmp << 1) | false;
                    DataMessage.data[0] = tmp;
                    DataMessage.data[1] = 0;
                    DataMessage.data[2] = 0;
                    DataMessage.data[3] = 0;
                    //PUTxDATAxINxSENDxMAILxBOX(&DataMessage);                    
                    TP3_SetLow();
                    // </editor-fold>
                    
                    tSendAliveMessWaitTimeCnt++;
                    if(tSendAliveMessWaitTimeCnt >= 999){
                        //TP3_SetHigh();
                        uint32_t millis = GETxMILLIS();
                        StatusMessage.header  = (uint8_t)HEADER;
                        StatusMessage.command = (uint8_t)ALIVE;
                        
                        for(size_t i=0; i < sizeof(millis); i++){
                           StatusMessage.data[i] = (millis >> (8 * i)) & 0xFF;
                        }
                        
                        PUTxDATAxINxSENDxMAILxBOX(&StatusMessage);
                        tSendAliveMessWaitTimeCnt = 0;
                        //TP3_SetLow();
                        
                    }
                    
                    if(CHECKxDATAxINxRECEIVExMAILxBOX() == true){
                        /* Do something with the data */
                        udpTrans_t *udpReceived;
                        udpReceived = GETxDATAxFROMxRECEIVExMAILxBOX();
                        if(udpReceived->command == 8){
                            TP1_Toggle();
                        }
                    }
                }
                updateTick = false;
            }            
        }
        // </editor-fold>
    }
}
/*
 * Set to true by x millisecond update 
 */
void UpdateTick(){
    updateTick = true;
}

/*
 * Debounce all I/O
 */
void DebounceIO()
{
    /*
     * Fetch one time the actual milisecond timer value
     * debounce the EMO button(voltdetect) always.
     */
    uint32_t millis = GETxMILLIS();    
    //TP3_SetHigh();        
    DEBOUNCExIO(&HALL_BUSSTOP_STN , &millis);
    DEBOUNCExIO(&HALL_BUSSTOP_IND , &millis);
    DEBOUNCExIO(&HALL_STOP_FDEP   , &millis);
    DEBOUNCExIO(&HOTRC_CH2L       , &millis);
    DEBOUNCExIO(&HOTRC_CH2R       , &millis);
    DEBOUNCExIO(&HOTRC_CH3        , &millis);
    DEBOUNCExIO(&HOTRC_CH4        , &millis);
    DEBOUNCExIO(&HOTRC_CH5        , &millis);
    DEBOUNCExIO(&HOTRC_CH6        , &millis);
    DEBOUNCExIO(&RESET_IOX        , &millis);
    //TP3_SetLow();
    
}
/**
 End of File
*/