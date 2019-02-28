#include <stdint.h>
#include "udp_demo.h"
#include "mcc_generated_files/adc.h"
#include "mcc_generated_files/TCPIPLibrary/udpv4.h"
#include "mcc_generated_files/TCPIPLibrary/tcpip_config.h"
#include "mcc_generated_files/pin_manager.h"

static adc_result_t potInitResult = 0;
static adc_result_t potCurrResult = 0;
static udpStart_t udpPacket;

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
                Toggle_Led_LAT ^=1;
                break;
            default:
                break;
        }
    }
}

void UDP_DEMO_Initialize(void)
{
    /* UDP Packet Initializations*/
    udpPacket.destinationAddress = MAKE_IPV4_ADDRESS(192,168,0,254);
    udpPacket.sourcePortNumber = 65533;
    udpPacket.destinationPortNumber = 65531;
    
    /* POT Initial Result*/
    potInitResult = (ADC_GetConversion(Pot)/10);
}

/*** Application to send Potentiometer data using UDP protocol in multiples of 10 ***/

void UDP_DEMO_Send (void)
{
    error_msg ret = ERROR;
    
    potCurrResult =  (ADC_GetConversion(Pot)/10);
    if(((potCurrResult > (potInitResult + 5)) || (potCurrResult <(potInitResult - 5))))
    {
        potInitResult = potCurrResult;
        
        /**************** Step 1 - Start UDP Packet ****************************
         * @Param1 - Destination Address
         * @Param2 - Source Port Number
         * @Param3 - Destination Port Number
         **********************************************************************/
       ret = UDP_Start(udpPacket.destinationAddress, udpPacket.sourcePortNumber, udpPacket.destinationPortNumber);
       
         /**************** Step 2 - Write UDP Packet ****************************
         * @Param1 - Data to write 
         ***********************************************************************/
       if(ret = SUCCESS)
       {
           UDP_Write16(potCurrResult);
           
         /**************** Step 3 - Send UDP Packet ****************************/
          UDP_Send();           
       }        
    }
}
  

