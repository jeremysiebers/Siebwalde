/**
  Generated Main Source File

  File Name:
    main.c

  Summary:
    This is the main file generated using PIC10 / PIC12 / PIC16 / PIC18 MCUs

  Description:
    This header file provides implementations for driver APIs for all modules selected in the GUI.
    Generation Information :
        Product Revision  :  PIC10 / PIC12 / PIC16 / PIC18 MCUs - 1.65.2
        Device            :  PIC18F97J60
        Driver Version    :  2.00
*/
#include <xc.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include "main.h"
#include "mcc_generated_files/mcc.h"
#include "spicommhandler.h"
#include "slavestartup.h"
#include "mcc_generated_files/TCPIPLibrary/udpv4.h"
#include "mcc_generated_files/TCPIPLibrary/tcpip_config.h"

void ToFromTerminal(void);
void Communications(void);
void SlaveAndIoData(void);
void BootLoaderData(void);

void WriteText(unsigned char *Text, unsigned int Ln, unsigned int Col);
void WriteData(unsigned int Data, unsigned int Ln, unsigned int Col);

/*
                         Main application
 */

/*----------------------------------------------------------------------------*/
static SLAVE_INFO         SlaveInfo[NUMBER_OF_SLAVES];   
static SLAVE_INFO         EthernetTarget[1];

static udpStart_t udpPacket;
uint16_t SizeOfStruct = sizeof(SLAVE_INFO);
static uint8_t DataFromSlaveSend = 0;
bool Init_UDP = true;
uint16_t count = 0;
uint8_t division = 1;

unsigned int StateMachine = 1;
unsigned int _Delay = 0;

unsigned char UPDATExTERMINAL;
/*----------------------------------------------------------------------------*/

void main(void)
{
    // Initialize the SLAVE_INFO struct with slave numbers(fixed))
    for (unsigned int i = 0; i <NUMBER_OF_SLAVES; i++){
        SlaveInfo[i].SlaveNumber = i;
        SlaveInfo[i].Header = 0xAA;
        SlaveInfo[i].Footer = 0x55;
        if (i > 0){
            SlaveInfo[i].HoldingReg[0] = 0x818F;                                // Set all EMO and 50%
            SlaveInfo[i].HoldingReg[2] = i;                                     // Set address of slave
        }
    }
    
    EthernetTarget[0].SlaveNumber = NUMBER_OF_SLAVES;
    EthernetTarget[0].Header = 0xAA;
    EthernetTarget[0].Footer = 0x55;
    
/*----------------------------------------------------------------------------*/
    
    // Initialize the device
    SYSTEM_Initialize();
    
    INITxSPIxCOMMxHANDLER(SlaveInfo);                                           // Init SPI slave - master registers
    INITxSLAVExSTARTUP(SlaveInfo);                                              // Init Slave startup 
    
    // If using interrupts in PIC18 High/Low Priority Mode you need to enable the Global High and Low Interrupts
    // If using interrupts in PIC Mid-Range Compatibility Mode you need to enable the Global and Peripheral Interrupts
    // Use the following macros to:

    // Enable the Global Interrupts
    INTERRUPT_GlobalInterruptEnable();

    // Disable the Global Interrupts
    //INTERRUPT_GlobalInterruptDisable();

    // Disable high priority global interrupts
    //INTERRUPT_GlobalInterruptHighDisable();

    // Disable low priority global interrupts.
    //INTERRUPT_GlobalInterruptLowDisable();

    // Enable the Peripheral Interrupts
    INTERRUPT_PeripheralInterruptEnable();

    // Disable the Peripheral Interrupts
    //INTERRUPT_PeripheralInterruptDisable();
    
    /*----------------------------------------------------------------------------*/    

    /* UDP Packet Initializations*/
    udpPacket.destinationAddress = MAKE_IPV4_ADDRESS(192,168,1,19);
    udpPacket.sourcePortNumber = 65533;
    udpPacket.destinationPortNumber = 65531;

    error_msg ret = ERROR;
    
    /**************** Step 1 - Start UDP Packet ****************************
     * @Param1 - Destination Address
     * @Param2 - Source Port Number
     * @Param3 - Destination Port Number
     **********************************************************************/
    
    while(Init_UDP){
        Read_Check_LAT = 1;
        Network_Manage();
        Read_Check_LAT = 0;
        
        count++;
        
        if(count > 65530){
            LED1_LAT ^= 1;
            ret = UDP_Start(udpPacket.destinationAddress, udpPacket.sourcePortNumber, udpPacket.destinationPortNumber);
            if(ret == SUCCESS)
            {
                LED2_LAT ^= 1;
                UDP_Write16(0xDEAD);
                UDP_Send();
                Init_UDP = false;
            }
            count = 0;
        }
    }
    
    ModbusReset_LAT = 0;                                                        // as last release the ModbusMaster.
    ModbusReset_SetDigitalOutput();                                             // set the Pin direction here to output to avoid power-on reset pulses to slaves!
    
//    printf("PIC18f97j60 started up!!!\n\r");
//    __delay_ms(10);
//    printf("\f");                                                               // Clear terminal (printf("\033[2J");)
//    __delay_ms(10);
//    printf("\033[?25h");
//    __delay_ms(10);   
//    printf("PIC18f97j60 started up!!!\n\r");                                    // Welcome message

    TMR0_StartTimer();
    SEND_UDP_SetDigitalInput();
/*----------------------------------------------------------------------------*/
    
    while (1)
    {
        
        switch(StateMachine){
            case 1:
                if(SLAVExINITxANDxCONFIG() == true){
                    InitPhase = false;
                    StateMachine = 2;
                }
                break;
                
            case 2:
                if(ENABLExAMPLIFIER() == true){
                    StateMachine = 3;
                }
                break;
                
            default :   
                break;
        }
        
        if(UPDATExTERMINAL){
            Read_Check_LAT = 1;
            Network_Manage();
            Read_Check_LAT = 0;            
            UPDATExTERMINAL = 0;
            count++;
        }
        
        if(count > (uint8_t)(division - 1) && SEND_UDP_PORT == 1){
            count = 0;
            LED1_LAT ^= 1;
            ret = UDP_Start(udpPacket.destinationAddress, udpPacket.sourcePortNumber, udpPacket.destinationPortNumber);
            if(ret == SUCCESS)
            {                
                UDP_WriteBlock(&(SlaveInfo[DataFromSlaveSend].Header),(SizeOfStruct * division));
                UDP_Send();
                LED2_LAT ^= 1;
                DataFromSlaveSend+= division;   
                if(DataFromSlaveSend > (NUMBER_OF_SLAVES-division)){
                    DataFromSlaveSend = 0;
                }
            }
            INTCONbits.TMR0IF = 0;
        }
    }
}

typedef struct
{
    char command;
    char action;    
}udpDemoRecv_t;

void UDP_DEMO_Recv(void)
{
    udpDemoRecv_t udpRecv;
    
    UDP_ReadBlock(&udpRecv,sizeof(udpDemoRecv_t));
    
    if(udpRecv.command == 'L')
    {
        switch(udpRecv.action)
        {
            case '1':
                //Toggle_Led_LAT ^=1;
                break;
            default:
                break;
        }
    }
}