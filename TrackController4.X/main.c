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
//#include "mcc_generated_files/TCPIPLibrary/dhcp_client.h"

#define HEADER 0xAA
#define FOOTER 0x55

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
static SLAVE_INFO         EthernetTarget;

static udpStart_t udpPacket;
uint16_t SizeOfStruct = sizeof(SLAVE_INFO);
static uint8_t DataFromSlaveSend = 0;
bool Init_UDP = true;
uint16_t count = 0;
uint8_t division = 0;
uint8_t SlaveDataTx[77];
uint8_t *pSlaveDataSend;

unsigned int StateMachine = 0;
unsigned int _Delay = 0;

uint8_t UPDATE_TERMINAL;
uint8_t UPDATE_SLAVE_TOxUDP; 

typedef enum
{
    MODBUS_CMD          = 0xFF,
    BOOTLOAD_CMD        = 0xFE,
    ETHERNET_CMD        = 0xFD,
    BOOTLOAD_CONFIG     = 0x07,
    BOOTLOAD_FLASH      = 0x02,
            
    SEND_SLAVEIODATA    = 0x00,
    SEND_BOOTLOADER     = 0x01,
    INIT_PHASE_TRUE     = 0xFC,
    INIT_PHASE_FALSE    = 0xFD,
    RELEASE_ALL         = 0xFE,
    RESET_ALL           = 0xFF,
};

typedef enum 
{
    ETH_OK          = 0x82, 
    ETH_NOK         = 0x83, 
    ETH_BUSY        = 0x81,
    ETH_IDLE        = 0x80,    
};

/*----------------------------------------------------------------------------*/

void main(void)
{
    // Initialize the SLAVE_INFO struct with slave numbers(fixed))
    for (unsigned int i = 0; i <NUMBER_OF_SLAVES; i++){
        SlaveInfo[i].SlaveNumber = i;
        SlaveInfo[i].Header = HEADER;
        SlaveInfo[i].Footer = FOOTER;
        if (i > 0){
            SlaveInfo[i].HoldingReg[0] = 0x818F;                                // Set all EMO and 50%
            SlaveInfo[i].HoldingReg[2] = i;                                     // Set address of slave
        }
    }
    
    EthernetTarget.SlaveNumber = NUMBER_OF_SLAVES;
    EthernetTarget.Header = HEADER;
    EthernetTarget.Footer = FOOTER;
    
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
    /*
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
    */
        
    printf("PIC18f97j60 started up!!!\n\r");
    __delay_ms(10);
    printf("\f");                                                               // Clear terminal (printf("\033[2J");)
    __delay_ms(10);
    printf("\033[?25h");
    __delay_ms(10);   
    printf("PIC18f97j60 started up!!!\n\r");                                    // Welcome message

    //TMR0_StartTimer();
    SEND_UDP_SetDigitalInput();
/*----------------------------------------------------------------------------*/
    
    while (1)
    {
        
        switch(StateMachine){
            case 0:
                Read_Check_LAT = 1;
                Network_Manage();
                Read_Check_LAT = 0;
                if(ETH_CheckLinkUp()){
                    StateMachine ++;
                }
                break;
                
            case 1:
                Read_Check_LAT = 1;
                Network_Manage();
                Read_Check_LAT = 0;
//                if(DHCP_BOUND()){
                    ModbusReset_LAT = 0;                                                        // as last release the ModbusMaster.
                    ModbusReset_SetDigitalOutput();                                             // set the Pin direction here to output to avoid power-on reset pulses to slaves!
                    StateMachine = 4;
//                }
                break;
                
            case 2:
                if(SLAVExINITxANDxCONFIG() == true){
                    InitPhase = false;
                    StateMachine = 3;
                }
                break;
                
            case 3:
                if(ENABLExAMPLIFIER() == true){
                    StateMachine = 4;
                }
                break;
                
            default :   
                break;
        }
        
        if(UPDATE_TERMINAL == 1){
            Read_Check_LAT = 1;
            Network_Manage();
            Read_Check_LAT = 0;                        
        }
        
        if((UPDATE_SLAVE_TOxUDP == 1) && (TMR0L > 25)){
            UPDATE_SLAVE_TOxUDP = 0;
            LED1_LAT = 1;  
            ret = UDP_Start(udpPacket.destinationAddress, udpPacket.sourcePortNumber, udpPacket.destinationPortNumber);
            if(ret == SUCCESS)
            { 
                LED2_LAT = 1;                
                SlaveDataTx[0] = HEADER;
                if(DataFromSlaveSend == NUMBER_OF_SLAVES){
                    pSlaveDataSend = &(EthernetTarget.Header);
                    SlaveDataTx[1] = ETHERNET_CMD;                              // send the Data Type
                }
                else{
                    pSlaveDataSend = &(SlaveInfo[DataFromSlaveSend].Header);
                    SlaveDataTx[1] = MODBUS_CMD;                                // send the Data Type
                }                
                for(uint8_t i=2; i < (SizeOfStruct + 2); i++){
                    SlaveDataTx[i] = *pSlaveDataSend;
                    pSlaveDataSend++;
                }
                UDP_WriteBlock(SlaveDataTx,(SizeOfStruct + 2));
                UDP_Send();
                
                if (InitPhase == false){                                        // When init phase is done, communicate data to all slaves
                    DataFromSlaveSend++;   
                    if(DataFromSlaveSend > NUMBER_OF_SLAVES){                   // Defined are 51 slaves --> 51 == Ethernet target
                        DataFromSlaveSend = 0;
                    }                    
                }
                else{
                    if (DataFromSlaveSend == 0){
                        DataFromSlaveSend = NUMBER_OF_SLAVES;
                    }
                    else if(DataFromSlaveSend == NUMBER_OF_SLAVES){
                        DataFromSlaveSend = 0;
                    }            
                }
                LED2_LAT = 0;
            }
            LED1_LAT = 0;
            //UPDATE_TERMINAL     = 0;
        }
    }
}

typedef struct
{
    uint8_t header;
    uint8_t command;
    uint8_t data[77]; 
}udpDemoRecv_t;

void UDP_DATA_RECV(int length)
{
    udpDemoRecv_t udpRecv;
    
    UDP_ReadBlock(&udpRecv,sizeof(udpDemoRecv_t));
    
//    printf("header received: %02X\n\r"    , udpRecv.header);
//    printf("command received: %02X\n\r"   , udpRecv.command);
//    printf("data[0 ]: %02X\n\r"            , udpRecv.data[0 ]);
//    printf("data[1 ]: %02X\n\r"            , udpRecv.data[1 ]);
//    printf("data[2 ]: %02X\n\r"            , udpRecv.data[2 ]);
//    printf("data[3 ]: %02X\n\r"            , udpRecv.data[3 ]);
//    printf("data[4 ]: %02X\n\r"            , udpRecv.data[4 ]);
//    printf("data[5 ]: %02X\n\r"            , udpRecv.data[5 ]);
//    printf("data[6 ]: %02X\n\r"            , udpRecv.data[6 ]);
//    printf("data[7 ]: %02X\n\r"            , udpRecv.data[7 ]);
//    printf("data[8 ]: %02X\n\r"            , udpRecv.data[8 ]);
//    printf("data[9 ]: %02X\n\r"            , udpRecv.data[9 ]);
//    printf("data[10]: %02X\n\r"            , udpRecv.data[10]);
//	  printf("data[11]: %02X\n\r"            , udpRecv.data[11]);
//    printf("data[12]: %02X\n\r"            , udpRecv.data[12]);
    
    
    if(udpRecv.header == HEADER)
    {
        switch(udpRecv.command)
        {
            case MODBUS_CMD:
                if(udpRecv.data[8] == FOOTER){
                    //printf("MODBUS_CMD received.\n\r");
                    SlaveInfo[0].HoldingReg[1] = ((uint16_t)udpRecv.data[3]<<8) + udpRecv.data[2];
                    SlaveInfo[0].HoldingReg[2] = ((uint16_t)udpRecv.data[5]<<8) + udpRecv.data[4];
                    SlaveInfo[0].HoldingReg[3] = ((uint16_t)udpRecv.data[7]<<8) + udpRecv.data[6];
                    SlaveInfo[0].HoldingReg[0] = ((uint16_t)udpRecv.data[1]<<8) + udpRecv.data[0];
                }
                break;
                
            case ETHERNET_CMD:
                if(udpRecv.data[1] == FOOTER){
                    switch(udpRecv.data[0]){
                        case RESET_ALL:
                            //printf("Reset All slaves...\n\r");
                            RESET();
//                            ModbusReset_LAT = 1;
//                            __delay_ms(10);
//                            ModbusReset_LAT = 0;
//                            //__delay_ms(4000);
//                            EthernetTarget.InputReg[0] = ETH_OK;
                            break;

                        case RELEASE_ALL:
                            Read_Check_LAT ^= 1;
                            EthernetTarget.InputReg[0] = ETH_IDLE;
                            break;

                        case SEND_SLAVEIODATA:
                           //SendOperation = 0;
                           break;

                        case SEND_BOOTLOADER:
                            //SendOperation = 1;
                            break;

                        case ETH_IDLE:
                            EthernetTarget.InputReg[0] = ETH_IDLE;
                            //printf("EthernetTarget == ETH_IDLE\n\r");
                            break;
                            
                        case INIT_PHASE_TRUE:
                            InitPhase = true;
                            break;
                            
                        case INIT_PHASE_FALSE:
                            InitPhase = false;
                            break;

                        default :
                            break;
                    }
                }
                break;
                
            case BOOTLOAD_CMD:
                //Toggle_Led_LAT ^=1;
                break;      
                
            default:
                break;
        }
    }
}