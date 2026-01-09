#include "General.h"
#include "PetitModbus.h"
#include "PetitModbusPort.h"
#include "../mcc_generated_files/mcc.h"

// UART Initialize for Microconrollers, yes you can use another phsycal layer!
void PetitModBus_UART_Initialise(void)
{
    InitUART();
}

// Timer Initialize for Petit Modbus, 1ms Timer will be good for us!
void PetitModBus_TIMER_Initialise(void)
{
    InitTMR();
}

void EUSART1_Write9(uint8_t b, bool ninthBit)
{
    while (!TXSTAbits.TRMT) { }      // TXREG empty (device dependent: TXIF may be in PIR3)
    TXSTAbits.TX9D = 0;//(uint8_t)(ninthBit ? 1 : 0);//Ninth bit of Transmit Data, Can be address/data bit or a parity bit.
    TXSTAbits.TX9 = 0;//(uint8_t)(ninthBit ? 1 : 0); //9-bit Transmit Enable bit
    TXREG = b;
}

void EUSART1_WriteData(uint8_t b)
{
    EUSART1_Write9(b, false);        // 9th bit = 0
}

// This is used for send string, better to use DMA for it ;)
unsigned char PetitModBus_UART_String(unsigned char *s, unsigned int Length)
{
    unsigned short  DummyCounter = 0;
    LED_TX++;
    
    TX_ENA_LAT = 1;                                                             // enable the driver of the rs485
    __delay_us(2);                                                              // Wait 2 us to ensure that the driver is enabled before sending first bit
    for(DummyCounter=0;DummyCounter<Length;DummyCounter++){
        EUSART1_WriteData(s[DummyCounter]);
    }
    
    while(!TXSTAbits.TRMT);                                                     // Due to RS485 enable latch, wait until last bit is sent
    
    TX_ENA_LAT = 0;
    LED_TX++;
    
    return TRUE;
}

/*************************Interrupt Fonction Slave*****************************/
// Call this function into your UART Interrupt. Collect data from it!
// Better to use DMA
// Modbus RTU Variables
volatile unsigned char PetitReceiveBuffer[PETITMODBUS_RECEIVE_BUFFER_SIZE];
volatile unsigned char PetitReceiveCounter = 0;

// Tracks whether we are currently receiving a frame payload (ADDEN disabled)
static volatile unsigned char g_modbusInFrame = 0;

// Collect data from UART RX interrupt (9-bit address detect aware)
void ReceiveInterrupt(unsigned char data, unsigned char ninthBit)
{
    // If address detect is enabled, we only want to react to address bytes (9th=1).
    // Hardware typically filters this already, but keep logic robust.
    if (RC1STAbits.ADDEN)
    {
        if (ninthBit)
        {
            // Address byte received
            if ((data == PETITMODBUS_SLAVE_ADDRESS) || (data == PETITMODBUS_BROADCAST_ADDRESS))
            {
                // Address match: accept the rest of the message
                RC1STAbits.ADDEN = 0;              // must be cleared before next Stop bit
                g_modbusInFrame  = 1;

                PetitReceiveCounter = 0;
                PetitReceiveBuffer[PetitReceiveCounter++] = data;

                PetitModbusTimerValue = 0;         // reset inter-char timer
            }
            else
            {
                // Not for us: stay in address detect mode and ignore payload bytes
                // (they won't trigger RCIF while ADDEN=1)
            }
        }
        return;
    }

    // Normal receive mode (ADDEN=0): store all bytes
    if (PetitReceiveCounter < PETITMODBUS_RECEIVE_BUFFER_SIZE)
    {
        PetitReceiveBuffer[PetitReceiveCounter++] = data;
        PetitModbusTimerValue = 0;                 // reset inter-char timer
    }
    else
    {
        // Overflow/desync: drop frame and re-arm address detect
        PetitReceiveCounter = 0;
        g_modbusInFrame = 0;
        RC1STAbits.ADDEN = 1;
    }
}

//void ReceiveInterrupt(unsigned char Data)
//{
//    PetitReceiveBuffer[PetitReceiveCounter]   =Data;
//    PetitReceiveCounter++;
//
//    if(PetitReceiveCounter>PETITMODBUS_RECEIVE_BUFFER_SIZE){
//        PetitReceiveCounter=0;
//    }
//    PetitModbusTimerValue=0;
//}

// Call this function into 1ms Interrupt or Event!
void PetitModBus_TimerValues(void)
{
    //PetitModbusTimerValue++;
    PetitModbusTimerValue = 3;
}
/******************************************************************************/
