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
#include "mcc_generated_files/examples/i2c2_master_example.h"

// Address of thee MCP23017 (0x20 - 0x27 depending on A0-A2 pins first 4 bits are 0100!)
i2c2_address_t MCP23017_ADDR1 = 0x21;
i2c2_address_t MCP23017_ADDR2 = 0x22;
i2c2_address_t MCP23017_ADDR3 = 0x23;
i2c2_address_t MCP23017_ADDR4 = 0x24;
i2c2_address_t MCP23017_ADDR5 = 0x25;
i2c2_address_t MCP23017_ADDR6 = 0x26;

// MCP23017 register addresses 
#define IODIRA   0x00  // I/O direction register (port A)
#define IODIRB   0x01  // I/O direction register (port B)
#define IOCON    0x0A  // IOCON shared between PORT A and B!
#define GPIOA    0x12  // GPIO-register for port A
#define GPIOB    0x13  // GPIO-register for port B
#define OLATA    0x14  // Output latch A
#define OLATB    0x15  // Output latch B

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
    IOX_RESET_SetLow();
    /* Kick all servo controllers */
    LATE = 0xFF;
    FLOWxCONTROLxOrder = STATION;

    while (1)
    {
        EXT_WDT_Toggle();
        
        //TP4_SetHigh();
        PROCESSxETHxDATA();
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
                IOX_RESET_SetHigh();
                // release all servos
                LATE = 0x00;
                MCP23017_Init();
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
                    /*
                     * Mountain track methods
                     */
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
                        
                        MCP23017_ToggleOutputs();
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
    //TP3_SetLow();
    
}

void MCP23017_Init(void) {
    // init the IO Expander ports all outputs
    for(uint8_t i = 0x21; i<0x27; i++){
        I2C2_Write1ByteRegister(i, IOCON,  0b00100000);
        I2C2_Write1ByteRegister(i, IODIRA, 0x00);
        I2C2_Write1ByteRegister(i, IODIRB, 0x00);    
        uint8_t data1[2] = {OLATA, 0xFF};
        I2C2_WriteNBytes(i, data1, 2);
        uint8_t data2[2] = {OLATB, 0xFF};
        I2C2_WriteNBytes(i, data2, 2);
    }    
    
//    I2C2_Write1ByteRegister(MCP23017_ADDR2, IOCON,  0b00100000);
//    I2C2_Write1ByteRegister(MCP23017_ADDR2, IODIRA, 0x00);
//    I2C2_Write1ByteRegister(MCP23017_ADDR2, IODIRB, 0x00);    
//    I2C2_WriteNBytes(MCP23017_ADDR2, data1, 2);
//    I2C2_WriteNBytes(MCP23017_ADDR2, data2, 2);
//    
//    I2C2_Write1ByteRegister(MCP23017_ADDR3, IOCON,  0b00100000);
//    I2C2_Write1ByteRegister(MCP23017_ADDR3, IODIRA, 0x00);
//    I2C2_Write1ByteRegister(MCP23017_ADDR3, IODIRB, 0x00);    
//    I2C2_WriteNBytes(MCP23017_ADDR3, data1, 2);
//    I2C2_WriteNBytes(MCP23017_ADDR3, data2, 2);
}

void MCP23017_ToggleOutputs(void) {
    TP4_SetHigh();
    if(toggle == true){
        //uint8_t data1[2] = {OLATA, 0xFF};
        //uint8_t data2[2] = {OLATB, 0xFF};
        uint8_t data1[3] = {OLATA, 0xAA, 0xAA};
        //I2C2_Write1ByteRegister(MCP23017_ADDR, OLATA, 0);
//        I2C2_WriteNBytes(MCP23017_ADDR1, data1, 3);
//        I2C2_WriteNBytes(MCP23017_ADDR2, data1, 3);
//        I2C2_WriteNBytes(MCP23017_ADDR3, data1, 3);
        //I2C2_WriteNBytes(MCP23017_ADDR, data2, 2);
        //I2C2_Write1ByteRegister(MCP23017_ADDR, OLATB, 0x00);
        toggle = false;
        
        for(uint8_t i = 0x21; i<0x27; i++){
            I2C2_WriteNBytes(i, data1, 3);
        }
        
    }
    else{
        //uint8_t data1[2] = {OLATA, 0x00};
        //uint8_t data2[2] = {OLATB, 0x00};
        uint8_t data1[3] = {OLATA, 0x55, 0x55};
        //I2C2_Write1ByteRegister(MCP23017_ADDR, OLATA, 0);
//        I2C2_WriteNBytes(MCP23017_ADDR1, data1, 3);
//        I2C2_WriteNBytes(MCP23017_ADDR2, data1, 3);
//        I2C2_WriteNBytes(MCP23017_ADDR3, data1, 3);
        //I2C2_WriteNBytes(MCP23017_ADDR, data2, 2);
        //I2C2_Write1ByteRegister(MCP23017_ADDR, OLATB, 0xFF);
        toggle = true;
        
        for(uint8_t i = 0x21; i<0x27; i++){
            I2C2_WriteNBytes(i, data1, 3);
        }
    }
    TP4_SetLow();
}
/**
 End of File
*/