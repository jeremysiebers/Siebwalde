/**
  Generated Main Source File

  Company:
    Microchip Technology Inc.

  File Name:
    main.c

  Summary:
    This is the main file generated using PIC10 / PIC12 / PIC16 / PIC18 MCUs

  Description:
    This header file provides implementations for driver APIs for all modules selected in the GUI.
    Generation Information :
        Product Revision  :  PIC10 / PIC12 / PIC16 / PIC18 MCUs - 1.81.8
        Device            :  PIC18F97J60
        Driver Version    :  2.00
*/

#include "mcc_generated_files/mcc.h"
#include "main.h"
#include "milisecond_counter.h"
#include "debounce.h"
#include "mainstation.h"
#include "pathway.h"
#include "mountaintrack.h"
#include "mcc_generated_files/TCPIPLibrary/udpv4.h"
#include "mcc_generated_files/TCPIPLibrary/tcpip_config.h"

void DebounceIO(void);
void UpdateTick(void);

typedef struct
{
    uint32_t destinationAddress;
    uint16_t sourcePortNumber;
    uint16_t destinationPortNumber;    
}udpStart_t;

static udpStart_t udpPacket;

uint8_t ProcessEthData = false;

uint8_t data=0;
uint32_t SrcIpAddress = 0;
uint32_t DstIpAddress = 0;
uint16_t DestPort = 0;
uint16_t SourcePort = 0;

bool autosend = false;

/*
                         Main application
 */

/* https://www.microchip.com/en-us/software-library/tcpipstack */

/* UDP Packet Initializations*/
//udpPacket.destinationAddress = MAKE_IPV4_ADDRESS(192,168,1,19);
//udpPacket.sourcePortNumber = 65533;
//udpPacket.destinationPortNumber = 65531;

bool updateTick = false;
uint32_t tBootWaitTimeCnt = 1;
bool booted = false;

void main(void)
// <editor-fold defaultstate="collapsed">
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
    INITxSTATION();
    
    /* UDP Packet Initializations*/
    udpPacket.destinationAddress = MAKE_IPV4_ADDRESS(192,168,1,19);
    udpPacket.sourcePortNumber = 65533;
    udpPacket.destinationPortNumber = 65531;

    error_msg ret = ERROR;
    
//    while(!ETH_CheckLinkUp()){
//        Network_Manage();
//    }
// </editor-fold>    
    while (1)
    {
        //TP1_SetHigh();
        /* Manage TCP/IP Stack */
//        Network_Manage();
        //TP1_SetLow();
        
// <editor-fold defaultstate="collapsed">
        /* Check if over ride key is present, if yes then disable controller. */
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
        /* When driving voltage is present and booted, execute state machines */
        else if(VOLTDET_GetValue() && booted)
        {
            TP1_SetHigh();
            
            if(true == updateTick){
                DebounceIO();                
                UPDATExTRAINxWAIT();
                UPDATExMOUNTAINxTRAINxWAIT();
                updateTick = false;
            }
            //TP1_SetHigh();
            UPDATExSTATION(&top);
            //TP2_SetLow();
            UPDATExSTATION(&bot);
            
            UPDATExMOUNTAINxSTATION(&waldsee);
            UPDATExMOUNTAINxSTATION(&siebwalde);
            UPDATExMOUNTAINxSTATION(&waldberg);
            
            TP1_SetLow();
        }
        
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
                DebounceIO();
                tBootWaitTimeCnt++;
                updateTick = false;
            }
            if(tBootWaitTimeCnt > tReadIoSignalWaitTime){
                booted = true;
            }
// </editor-fold>
        }
        
        if(true == ProcessEthData){
            ProcessEthData = false;
            SrcIpAddress    = UDP_GetSrcIP();   // IP of uController
            DstIpAddress    = UDP_GetDestIP();  // sender IP
            DestPort        = UDP_GetDestPort();// sender port used 
            SourcePort      = UDP_GetSrcPort(); // port of uController
            
            // error_msg UDP_Start(uint32_t destIP, uint16_t srcPort, uint16_t dstPort)
            
            ret = UDP_Start(DstIpAddress, 0x7000, 0x7000);
            if(ret == SUCCESS){
                UDP_Write8(data);
                UDP_Send();
            }
            /*
            switch(data){
                case 1: SETxSTATIONxPATHWAY(&bot, 1);
                break;
                case 2: SETxSTATIONxPATHWAY(&bot, 2);
                break;
                case 3: SETxSTATIONxPATHWAY(&bot, 3);
                break;
                
                case 10: SETxSTATIONxPATHWAY(&top, 10);
                break;
                case 11: SETxSTATIONxPATHWAY(&top, 11);
                break;
                case 12: SETxSTATIONxPATHWAY(&top, 12);
                break;
                
                case 16: LATB = 0x01;
                break;
                case 17: LATB = 0x02;
                break;
                case 18: LATB = 0x04;
                break;
                case 19: LATB = 0x08;
                break;
                case 20: LATB = 0x10;
                break;
                case 21: LATB = 0x20;
                break;
                case 22: LATB = 0x40;
                break;
                case 23: LATB = 0x80;
                break;
                case 24: LATB = 0;
                break;
                
                default: break;
            }
             */
            data = 0;
        }
        
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
     * almost 500us
     */
    if(VOLTDET_GetValue()){        
        TP2_SetHigh();
        uint32_t millis = GETxMILLIS();
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
        DEBOUNCExIO(&OCC_FR_T6,     &millis);
        DEBOUNCExIO(&OCC_FR_T3,     &millis);
        DEBOUNCExIO(&CTRL_OFF     , &millis);
        DEBOUNCExIO(&OCC_FR_9B    , &millis);
        DEBOUNCExIO(&OCC_FR_21B   , &millis);
        DEBOUNCExIO(&OCC_FR_22B   , &millis);
        DEBOUNCExIO(&OCC_FR_23B   , &millis);
        TP2_SetLow();
    }
}
/**
 End of File
*/

void UDP_DATA_RECV(int length)
{    
    //UDP_ReadBlock(&udpRecv,sizeof(udpDemoRecv_t));
    data = UDP_Read8();
    ProcessEthData = true;
}