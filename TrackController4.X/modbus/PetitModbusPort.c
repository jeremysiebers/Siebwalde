#include "General.h"
#include "PetitModbus.h"
#include "PetitModbusPort.h"

// Modbus RTU Variables
volatile unsigned char   PetitReceiveBuffer[PETITMODBUS_RECEIVE_BUFFER_SIZE];   // Buffer to collect data from hardware
volatile unsigned char   PetitReceiveCounter=0;                                 // Collected data number

// UART Initialize for Microconrollers, yes you can use another phsycal layer!
void PetitModBus_UART_Initialise(void)
{
    InitUART();
}

// Timer Initialize for Petit Modbus, 1ms Timer will be good for us!
void PetitModBus_TIMER_Initialise(void)
{
    InitTMR1();
}


// This is used for send one character
void PetitModBus_UART_Putch(unsigned char c)
{
	while(!TXSTAbits.TRMT);
	TXREG = c;
}

/*
 * Function Name        : PetitModBus_UART_String
 * @param[out]          : TRUE
 * @How to use          : It is used for send data package over physical layer
 * @timing              : Full message (address, command, 2 byte data, 2 byte CRC)
 *                          @1250 kbps  = 74.41us
 *                          @625  kbps  = 130us
 *                          @57.6 kbps  = 1.18ms
 *                          @19.2 kbps  = 3.84ms
 *                          @9.6  kbps  = 8ms
 */
unsigned char PetitModBus_UART_String(unsigned char *s, unsigned int Length)
{
    unsigned short  DummyCounter;
    
    for(DummyCounter=0;DummyCounter<Length;DummyCounter++){
        PetitModBus_UART_Putch(s[DummyCounter]);
    } 
        
    return TRUE;
}

/*************************Interrupt Fonction Slave*****************************/
// Call this function into your UART Interrupt. Collect data from it!
// Better to use DMA
void ReceiveInterrupt(unsigned char Data)
{
    PetitReceiveBuffer[PetitReceiveCounter]   =Data;
    PetitReceiveCounter++;

    if(PetitReceiveCounter>PETITMODBUS_RECEIVE_BUFFER_SIZE){ 
        PetitReceiveCounter=0;
    }
    PetitModbusTimerValue=0;
    SlaveAnswerTimeoutCounter=0;
}

// Call this function into 1ms Interrupt or Event!
void PetitModBus_TimerValues(void)
{
    //PetitModbusTimerValue++;
    PetitModbusTimerValue = 3;
}
/******************************************************************************/
