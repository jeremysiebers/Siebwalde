#include "system_config.h"
#include "system_definitions.h"
#include "PetitModbus.h"
#include "PetitModbusPort.h"

// Modbus RTU Variables
volatile unsigned char   PetitReceiveBuffer[PETITMODBUS_RECEIVE_BUFFER_SIZE];   // Buffer to collect data from hardware
volatile unsigned char   PetitReceiveCounter=0;                                 // Collected data number

static uint8_t DummyCounter;

// This is used for send one character
//void PetitModBus_UART_Putch(unsigned char c)
//{
//	// make sure the transmit buffer is not full before trying to write byte 
//    while(DRV_USART1_TransmitBufferIsFull() );
//    DRV_USART1_WriteByte(c);  // send modified byte        
//    
//}
//
///*
// * Function Name        : PetitModBus_UART_String
// * @param[out]          : TRUE
// * @How to use          : It is used for send data package over physical layer
// * @timing              : Full message (address, command, 2 byte data, 2 byte CRC)
// *                          @1250 kbps  = 74.41us
// *                          @625  kbps  = 130us
// *                          @57.6 kbps  = 1.18ms
// *                          @19.2 kbps  = 3.84ms
// *                          @9.6  kbps  = 8ms
// */
//unsigned char PetitModBus_UART_String(unsigned char *s, unsigned int Length)
//{
//    Led1On(); 
//    LED_TX++;
//    
//    for(DummyCounter=0;DummyCounter<Length;DummyCounter++){
//        PetitModBus_UART_Putch(s[DummyCounter]);        
//    }
//    
//    LED_TX++;
//    
//    Led1Off();
//    
//    return TRUE;
//}


// Low-level 9-bit write (UART2 on PIC32): write 0..0x1FF to U2TXREG
static void __attribute__((noinline)) UART2_Write9(uint16_t w)
{
    while (U2STAbits.UTXBF) { }       // wait while TX buffer full
    //U2STAbits.TX9D = (w & 0x0100u) ? 1 : 0;     // 9th bit
    U2TXREG = (uint16_t)(w & 0x01FFu);           // 8-bit/9-bit payload
}

static void __attribute__((noinline)) PetitModBus_UART_PutchData(uint8_t b)
{
    UART2_Write9((uint16_t)b);        // 9th bit = 0
}

static void __attribute__((noinline)) PetitModBus_UART_PutchAddr(uint8_t addr)
{
    UART2_Write9(0x0100u | addr);     // 9th bit = 1
}

unsigned char PetitModBus_UART_String(unsigned char *s, unsigned int Length)
{
    Led1On();
    LED_TX++;

    if (Length == 0u)
    {
        Led1Off();
        return TRUE;
    }

    // First byte is Modbus address -> send with 9th bit set
    PetitModBus_UART_PutchAddr((uint8_t)s[0]);

    // Remaining bytes -> 9th bit cleared
    for (DummyCounter = 1; DummyCounter < Length; DummyCounter++)
    {
        PetitModBus_UART_PutchData((uint8_t)s[DummyCounter]);
    }

    LED_TX++;
    Led1Off();
    return TRUE;
}




/*************************Interrupt Function Slave*****************************/
// Call this function into your UART Interrupt. Collect data from it!
// Better to use DMA
void ReceiveInterrupt(uint8_t data, bool isAddress)
{
    // On a master, the 9th-bit address mark is only a hint.
    // Never reset an in-progress frame on an address mark, because it can
    // destroy a valid response if a spurious mark appears.
    (void)isAddress; // master does not use 9th bit on RX

    PetitReceiveBuffer[PetitReceiveCounter++] = data;

    if (PetitReceiveCounter >= PETITMODBUS_RECEIVE_BUFFER_SIZE)
    {
        // Buffer overflow: drop frame
        PetitReceiveCounter = 0u;
    }

    PetitModbusTimerValue = 0u;
}


// Call this function into 1ms Interrupt or Event!
void PetitModBus_TimerValues(void)
{
    //PetitModbusTimerValue++;
    PetitModbusTimerValue = 3;
}
/******************************************************************************/
