#include "main.h"
#include "enums.h"
#include "mcc_generated_files/mcc.h"
#include "milisecond_counter.h"
#include "communication.h"
#include "mainstation.h"
#include "mountaintrack.h"
#include "debounce.h"
#include "rand.h"
#include <string.h>
#include <stdbool.h>

static bool         updateTick = false;
static uint32_t     tBootWaitTimeCnt = 1;
static bool         booted = false;
static bool         justSendData = false;

static uint32_t     tSendAliveMessWaitTimeCnt = 0;
static udpTrans_t   StatusMessage;
static udpTrans_t   DataMessage, prevDataMessage;
static uint8_t      tmp = 0;

bool compareDataToSend(const uint8_t *arr1, const uint8_t *arr2, uint8_t size);

void main(void)
{
    // Initialize the device
    // <editor-fold defaultstate="collapsed">
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
    INITxSTATION();
    INITxMOUNTAINxSTATION();
    INITxRANDxNUMBER();

    // </editor-fold>
    
    while (1)
    {        
        //TP4_SetHigh();
        PROCESSxETHxDATA();
        //TP4_SetLow();
                
        /* Check if over ride key is present, if yes then disable controller. */
        // <editor-fold defaultstate="collapsed">
        if(KEY_CTRL_GetValue())
        {
            BLK_SIG_3B_GR_SetHigh();
            BLK_SIG_12B_GR_SetHigh();            
            BLK_SIG_1B_RD_SetHigh();
            BLK_SIG_2B_RD_SetHigh();
            BLK_SIG_10B_RD_SetHigh();
            BLK_SIG_11B_RD_SetHigh();
            
            WS_FR_FYRD_7_L_SetLow();
            
            OCC_TO_21B_SetLow();
            OCC_TO_9B_SetLow();            
            OCC_TO_T6_SetHigh();
            OCC_TO_T3_SetHigh();
            OCC_TO_8A_SetLow();
            OCC_TO_STN_1_SetHigh();
            OCC_TO_STN_2_SetHigh();
            OCC_TO_STN_3_SetLow();
            OCC_TO_STN_10_SetHigh();
            OCC_TO_STN_11_SetHigh();
            OCC_TO_STN_12_SetLow();
        }
        // </editor-fold>
        /* First boot up sequence, set IO, warm up debounce first */
        // <editor-fold defaultstate="collapsed">        
        else if(false == booted){
            BLK_SIG_3B_RD_SetHigh();
            BLK_SIG_12B_RD_SetHigh();            
            BLK_SIG_1B_RD_SetHigh();
            BLK_SIG_2B_RD_SetHigh();
            BLK_SIG_10B_RD_SetHigh();
            BLK_SIG_11B_RD_SetHigh();            
            OCC_TO_21B_SetHigh();
            OCC_TO_9B_SetHigh();            
            OCC_TO_T6_SetHigh();
            OCC_TO_T3_SetHigh();            
            OCC_TO_STN_1_SetHigh();
            OCC_TO_STN_2_SetHigh();
            OCC_TO_STN_3_SetHigh();
            OCC_TO_STN_10_SetHigh();
            OCC_TO_STN_11_SetHigh();
            OCC_TO_STN_12_SetHigh();
            
            if(true == updateTick){
                DebounceIO(false);
                tBootWaitTimeCnt++;
                updateTick = false;
            }
            /* 
             * After 2 seconds warm up time check if driving 
             * voltage is present, if not reset warmup time
             */
            if(tBootWaitTimeCnt > tReadIoSignalWaitTime){
                tBootWaitTimeCnt = 0;
                if(VOLTDET_GetValue()){
                    booted = true;
                }                
            }
        }
        // </editor-fold>        
        /* 
         * When EMO is pressed stop updates to state machines 
         * and debouncing of IO
         */
        // <editor-fold defaultstate="collapsed">
        else if(false == VOLTDETECT.value && booted){
            if(true == updateTick){
                DebounceIO(true);
                if(VOLTDET_GetValue()){
                    tBootWaitTimeCnt++;
                    if(tBootWaitTimeCnt > tReadIoSignalWaitTime){
                        VOLTDETECT.value = true;
                        tBootWaitTimeCnt = 0;
                        CREATExTASKxSTATUSxMESSAGE(
                            MAIN_LOOP,
                            VOLTAGE_DETECTED, 
                            RUN, 
                            NONE);
                    }
                }
                else{
                    tBootWaitTimeCnt = 0;
                }
                updateTick = false;
            }
        }
        // </editor-fold>
        
        /* 
         * When driving voltage is present and system booted, 
         * execute state machines 
         */
        // <editor-fold defaultstate="collapsed">
        else if(VOLTDETECT.value && booted)
        {
            if(true == updateTick){
                TP4_SetHigh();
                
                TP1_SetHigh();
                DebounceIO(true);
                TP1_SetLow();
                
                if(true){//if(isUdpConnected){
                    // <editor-fold defaultstate="collapsed">
                    /*
                    * MainStation methods
                    */
                    TP2_SetHigh();
                    UPDATExSTATIONxTRAINxWAIT(&top);
                    UPDATExSTATIONxTRAINxWAIT(&bot);
                    TP2_SetLow();
                    
                    TP1_SetHigh();
                    UPDATExSTATION(&top);
                    UPDATExSTATION(&bot);
                    TP1_SetLow();
 
                    /*
                     * Mountain track methods
                     */
                    TP2_SetHigh();
                    UPDATExMOUNTAINxTRAINxWAIT(&waldsee);
                    UPDATExMOUNTAINxTRAINxWAIT(&waldberg);
                    TP2_SetLow();
                    TP1_SetHigh();
                    UPDATExMOUNTAINxSTATION(&waldsee);
                    UPDATExMOUNTAINxSTATION(&waldberg);
                    TP1_SetLow();
                    // </editor-fold>
                    
                    /* Gather data to send to PC */
                    // <editor-fold defaultstate="collapsed">
                    TP2_SetHigh();
                    DataMessage.header = (uint8_t)HEADER;
                    DataMessage.command = (uint8_t)IODATA;                    
                    tmp = 0;
                    tmp = (uint8_t)(tmp << 1) | HALL_BLK_13.value;       //MSB
                    tmp = (uint8_t)(tmp << 1) | HALL_BLK_21A.value;
                    tmp = (uint8_t)(tmp << 1) | HALL_BLK_T4.value;
                    tmp = (uint8_t)(tmp << 1) | HALL_BLK_T5.value;
                    tmp = (uint8_t)(tmp << 1) | HALL_BLK_T1.value;
                    tmp = (uint8_t)(tmp << 1) | HALL_BLK_T2.value;
                    tmp = (uint8_t)(tmp << 1) | HALL_BLK_9B.value;
                    tmp = (uint8_t)(tmp << 1) | HALL_BLK_4A.value;
                    DataMessage.data[0] = tmp;
                    
                    tmp = 0;
                    tmp = (uint8_t)(tmp << 1) | HALL_BLK_T7.value;      //MSB
                    tmp = (uint8_t)(tmp << 1) | HALL_BLK_T8.value;
                    tmp = (uint8_t)(tmp << 1) | OCC_FR_BLK13.value;
                    tmp = (uint8_t)(tmp << 1) | OCC_FR_BLK4.value;
                    tmp = (uint8_t)(tmp << 1) | OCC_FR_STN_1.value;
                    tmp = (uint8_t)(tmp << 1) | OCC_FR_STN_2.value;
                    tmp = (uint8_t)(tmp << 1) | OCC_FR_STN_3.value;
                    tmp = (uint8_t)(tmp << 1) | OCC_FR_STN_10.value;
                    DataMessage.data[1] = tmp;
                    
                    tmp = 0;
                    tmp = (uint8_t)(tmp << 1) | OCC_FR_STN_11.value;    //MSB
                    tmp = (uint8_t)(tmp << 1) | OCC_FR_STN_12.value;
                    tmp = (uint8_t)(tmp << 1) | OCC_FR_T6.value;
                    tmp = (uint8_t)(tmp << 1) | OCC_FR_T3.value;
                    tmp = (uint8_t)(tmp << 1) | CTRL_OFF.value;
                    tmp = (uint8_t)(tmp << 1) | OCC_FR_23B.value;
                    tmp = (uint8_t)(tmp << 1) | OCC_FR_22B.value;
                    tmp = (uint8_t)(tmp << 1) | OCC_FR_9B.value;
                    DataMessage.data[2] = tmp;
                    
                    tmp = 0;
                    tmp = (uint8_t)(tmp << 1) | OCC_FR_21B.value;       //MSB
                    tmp = (uint8_t)(tmp << 1) | VOLTDETECT.value;                    
                    tmp = (uint8_t)(tmp << 1) | 1;
                    tmp = (uint8_t)(tmp << 1) | 1;
                    tmp = (uint8_t)(tmp << 1) | 1;
                    tmp = (uint8_t)(tmp << 1) | 1;
                    tmp = (uint8_t)(tmp << 1) | 1;
                    tmp = (uint8_t)(tmp << 1) | 1;                      // LSB
                    DataMessage.data[3] = tmp;
                    TP2_SetLow();
                    
                    TP3_SetHigh();
                    if(compareDataToSend(prevDataMessage.data, DataMessage.data, udpTrans_t_data_length)){
                        PUTxDATAxINxSENDxMAILxBOX(&DataMessage); 
                        prevDataMessage = DataMessage;
                        justSendData = true;
                    }                                       
                    TP3_SetLow();
                    // </editor-fold>
                    
                    /* Alive heartbeat and force data send every second */
                    // <editor-fold defaultstate="collapsed">
                    TP1_SetHigh();
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
                        
                        /* Force data send once per second also if not already 
                         * send 
                         */
                        if(false == justSendData){
                            PUTxDATAxINxSENDxMAILxBOX(&DataMessage);
                        }
                        else{
                            justSendData = false;
                        }
                    }
                    TP1_SetLow();
                    // </editor-fold>
                    
                    /* Check if commands are received */
                    // <editor-fold defaultstate="collapsed">
                    TP2_SetHigh();
                    if(CHECKxDATAxINxRECEIVExMAILxBOX() == true){
                        /* Do something with the data */
                        udpTrans_t *udpReceived;
                        udpReceived = GETxDATAxFROMxRECEIVExMAILxBOX();
                        if(udpReceived->command == 8){
                            //TP1_Toggle();
                        }
                    }
                    TP2_SetLow();
                    // </editor-fold>
                }
                updateTick = false;
                TP4_SetLow();
            }            
        }
        // </editor-fold>
    }
}

// Function to compare two arrays of the same type
bool compareDataToSend(const uint8_t *arr1, const uint8_t *arr2, uint8_t size) {
    for (uint8_t i = 0; i < size; ++i) {
        if (arr1[i] != arr2[i]) {
            return 1; // Arrays are not equal
        }
    }
    return 0; // Arrays are equal
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
void DebounceIO(bool trackio)
{
    /*
     * Fetch one time the actual milisecond timer value
     * debounce the EMO button(voltdetect) always.
     */
    uint32_t millis = GETxMILLIS();
    DEBOUNCExIO(&VOLTDETECT, &millis);
    /*
     * (175us) when track IO is allowed to be debounced, typically when 
     * trackvoltage is present and no emo is pressed
     */    
    if(trackio){
        //TP3_SetHigh();        
        DEBOUNCExIO(&HALL_BLK_13  , &millis); 
        DEBOUNCExIO(&HALL_BLK_21A , &millis);
        DEBOUNCExIO(&HALL_BLK_T4  , &millis);
        DEBOUNCExIO(&HALL_BLK_T5  , &millis);
        DEBOUNCExIO(&HALL_BLK_T1  , &millis);
        DEBOUNCExIO(&HALL_BLK_T2  , &millis);
        DEBOUNCExIO(&HALL_BLK_9B  , &millis);
        DEBOUNCExIO(&HALL_BLK_4A  , &millis);
        DEBOUNCExIO(&HALL_BLK_T7  , &millis);
        DEBOUNCExIO(&HALL_BLK_T8  , &millis);
        DEBOUNCExIO(&OCC_FR_BLK13 , &millis);
        DEBOUNCExIO(&OCC_FR_BLK4  , &millis);
        DEBOUNCExIO(&OCC_FR_STN_1 , &millis);
        DEBOUNCExIO(&OCC_FR_STN_2 , &millis);
        DEBOUNCExIO(&OCC_FR_STN_3 , &millis);
        DEBOUNCExIO(&OCC_FR_STN_10, &millis);
        DEBOUNCExIO(&OCC_FR_STN_11, &millis);
        DEBOUNCExIO(&OCC_FR_STN_12, &millis);
        DEBOUNCExIO(&OCC_FR_T6    , &millis);
        DEBOUNCExIO(&OCC_FR_T3    , &millis);
        DEBOUNCExIO(&CTRL_OFF     , &millis);
        DEBOUNCExIO(&OCC_FR_9B    , &millis);
        DEBOUNCExIO(&OCC_FR_21B   , &millis);
        DEBOUNCExIO(&OCC_FR_22B   , &millis);
        DEBOUNCExIO(&OCC_FR_23B   , &millis);
        //TP3_SetLow();
    }
}
/**
 End of File
*/