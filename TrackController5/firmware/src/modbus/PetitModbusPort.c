#include "system_config.h"
#include "system_definitions.h"
#include "PetitModbus.h"
#include "PetitModbusPort.h"
#include "enums.h"

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


// Deadline for the UART2 TX busy-wait, sized to a few byte-times at 2 Mbaud.
// 9N1 framing is 11 bits per byte (~5.5 us/byte); 30 us covers ~5 byte-times.
// READxCORExTIMER() ticks at the 126 MHz core-timer rate (MICROSECONDS ticks).
#define PETITMODBUS_TX_WRITE_DEADLINE (30u * MICROSECONDS)

// Re-establish 9-bit Modbus framing and re-assert TX/RX enables after a stuck
// TX buffer. Mirrors UART2_Config_9bit_Modbus() in slavefwhandler.c (static there
// and not reachable from this port file).
static void __attribute__((noinline)) UART2_RecoverTx(void)
{
    U2MODEbits.ON = 0;
    U2MODEbits.PDSEL = 0x3;      /* 9-bit, no parity */
    U2MODEbits.STSEL = 0;        /* 1 stop bit */
    U2STAbits.UTXEN = 1;
    U2STAbits.URXEN = 1;
    U2STAbits.OERR = 0;
    U2MODEbits.ON = 1;
    while (U2STAbits.URXDA) { (void)U2RXREG; }
}

// Low-level 9-bit write (UART2 on PIC32): write 0..0x1FF to U2TXREG.
// Returns TRUE on success; FALSE if the TX buffer never drained within the
// deadline (stuck TX FIFO / disabled transmitter), after forcing a recovery.
static unsigned char __attribute__((noinline)) UART2_Write9(uint16_t w)
{
    uint32_t start = READxCORExTIMER();
    while (U2STAbits.UTXBF) {
        if ((uint32_t)(READxCORExTIMER() - start) >= PETITMODBUS_TX_WRITE_DEADLINE) {
            UART2_RecoverTx();
            return FALSE;
        }
    }
    //U2STAbits.TX9D = (w & 0x0100u) ? 1 : 0;     // 9th bit
    U2TXREG = (uint16_t)(w & 0x01FFu);           // 8-bit/9-bit payload
    return TRUE;
}

static unsigned char __attribute__((noinline)) PetitModBus_UART_PutchData(uint8_t b)
{
    return UART2_Write9((uint16_t)b); // 9th bit = 0
}

static unsigned char __attribute__((noinline)) PetitModBus_UART_PutchAddr(uint8_t addr)
{
    return UART2_Write9(0x0100u | addr); // 9th bit = 1
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
    if (!PetitModBus_UART_PutchAddr((uint8_t)s[0]))
    {
        Led1Off();
        return FALSE;
    }

    // Remaining bytes -> 9th bit cleared
    for (DummyCounter = 1; DummyCounter < Length; DummyCounter++)
    {
        if (!PetitModBus_UART_PutchData((uint8_t)s[DummyCounter]))
        {
            Led1Off();
            return FALSE;
        }
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
