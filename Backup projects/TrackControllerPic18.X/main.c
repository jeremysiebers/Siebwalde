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
#include "mcc_generated_files/TCPIPLibrary/udpv4.h"
#include "mcc_generated_files/TCPIPLibrary/tcpip_config.h"

void DebounceIO(void);
void UpdateTick(void);

void UDP_DEMO_Initialize(void);
void UDP_DEMO_Send (void);
void UDP_DEMO_Recv(void);


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

/* 
 * PORTG = INPUT  CARD 1 LOW BYTE
 * PORTH = INPUT  CARD 1 HIGH BYTE
 * PORTD = OUTPUT CARD 2 LOW BYTE
 * PORTC = OUTPUT CARD 2 HIGH BYTE
 */

bool updateTick = false;

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
    //SETxMILLISECONDxUPDATExHANDLER(DebounceIO);
    //SETxMILLISECONDxUPDATExHANDLER2(UPDATExSIGNAL);
    //SETxMILLISECONDxUPDATExHANDLER3(UPDATExTRAINxWAIT);
    SETxMILLISECONDxUPDATExHANDLER(UpdateTick);
    INITxSTATION();
    
    /* UDP Packet Initializations*/
    udpPacket.destinationAddress = MAKE_IPV4_ADDRESS(192,168,1,19);
    udpPacket.sourcePortNumber = 65533;
    udpPacket.destinationPortNumber = 65531;

    error_msg ret = ERROR;
    
    while(!ETH_CheckLinkUp()){
        Network_Manage();
    }
    
    while (1)
    {
        TP1_SetHigh();
        /* Manage TCP/IP Stack */
        Network_Manage();
        TP1_SetLow();
        
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
            OCC_TO_STN_11_SetHigh();
            OCC_TO_STN_12_SetLow();
            OCC_TO_T6_SetHigh();
            OCC_TO_T3_SetHigh();
            OCC_TO_8A_SetLow();
            OCC_TO_STN_1_SetHigh();
            OCC_TO_STN_2_SetHigh();
            OCC_TO_STN_3_SetLow();
            OCC_TO_STN_10_SetHigh();
        }
        /* When driving voltage is present execute state machines */
        else if(!VOLTDET_GetValue())
        {
            //TP1_SetHigh();
            
            if(true == updateTick){
                DebounceIO();
                UPDATExSIGNAL();
                UPDATExTRAINxWAIT();
                updateTick = false;
            }
            //TP1_SetHigh();
            UPDATExSTATION(&top);
            //TP2_SetLow();
            UPDATExSTATION(&bot);
            
            //TP1_SetLow();
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
            
            if(data == 0x55){
                autosend = true;
            }
            else{
                autosend  = false;
            }
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
    if(autosend){
           error_msg ret = UDP_Start(DstIpAddress, 0x7000, 0x7000);
            if(ret == SUCCESS){
                UDP_Write32(GETxMILLIS());
                UDP_Send();
            }
        }
    /*
     * almost 500us
     */
    if(!VOLTDET_GetValue()){
        TP2_SetHigh();
        DEBOUNCExIO(&HALL_BLK_13  );
        DEBOUNCExIO(&HALL_BLK_21A );
        DEBOUNCExIO(&HALL_BLK_T4  );
        DEBOUNCExIO(&HALL_BLK_T5  );
        DEBOUNCExIO(&HALL_BLK_T1  );
        DEBOUNCExIO(&HALL_BLK_T2  );
        DEBOUNCExIO(&HALL_BLK_9B  );
        DEBOUNCExIO(&HALL_BLK_4A  );
        DEBOUNCExIO(&HALL_BLK_T7  );
        DEBOUNCExIO(&HALL_BLK_T8  );
        DEBOUNCExIO(&OCC_FR_BLK13 );
        DEBOUNCExIO(&OCC_FR_BLK4  );
        DEBOUNCExIO(&OCC_FR_STN_1 );
        DEBOUNCExIO(&OCC_FR_STN_2 );
        DEBOUNCExIO(&OCC_FR_STN_3 );
        DEBOUNCExIO(&OCC_FR_STN_10);
        DEBOUNCExIO(&OCC_FR_STN_11);
        DEBOUNCExIO(&OCC_FR_STN_12);
        DEBOUNCExIO(&OCC_FR_STN_T6);
        DEBOUNCExIO(&OCC_FR_STN_T3);
        DEBOUNCExIO(&CTRL_OFF);
        DEBOUNCExIO(&OCC_FR_9B);
        DEBOUNCExIO(&OCC_FR_21B);
        DEBOUNCExIO(&OCC_FR_22B);
        DEBOUNCExIO(&OCC_FR_23B);
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