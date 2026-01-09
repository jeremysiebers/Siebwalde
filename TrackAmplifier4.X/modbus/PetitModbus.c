#include <xc.h>
#include "PetitModbus.h"
#include "PetitModbusPort.h"
#include "General.h"
#include "../mcc_generated_files/mcc.h"

/*******************************ModBus Functions*******************************/
#define PETITMODBUS_READ_COILS                  1
#define PETITMODBUS_READ_DISCRETE_INPUTS        2
#define PETITMODBUS_READ_HOLDING_REGISTERS      3
#define PETITMODBUS_READ_INPUT_REGISTERS        4
#define PETITMODBUS_WRITE_SINGLE_COIL           5
#define PETITMODBUS_WRITE_SINGLE_REGISTER       6
#define PETITMODBUS_DIAGNOSTIC_REGISTERS        8
#define PETITMODBUS_WRITE_MULTIPLE_COILS        15
#define PETITMODBUS_WRITE_MULTIPLE_REGISTERS    16
/****************************End of ModBus Functions***************************/

#define PETIT_FALSE_FUNCTION                    0
#define PETIT_FALSE_SLAVE_ADDRESS               1
#define PETIT_DATA_NOT_READY                    2
#define PETIT_DATA_READY                        3

#define PETIT_ERROR_CODE_01                     0x01                            // Function code is not supported
#define PETIT_ERROR_CODE_02                     0x02                            // Register address is not allowed or write-protected
#define PETIT_ERROR_CODE_03                     0x03                            // Some data values are out of range, invalid number of register

uint8_t PETITMODBUS_SLAVE_ADDRESS         = 255;
uint8_t PETITMODBUS_BROADCAST_ADDRESS     = 0;

#ifdef  CRC_HW_REVERSE
uint16_t CRC_ReverseValue(uint16_t crc);                                        // When using HW CRC the result must be reversed after the last calculation
#endif

typedef enum
{
    PETIT_RXTX_IDLE,
    PETIT_RXTX_START,
    PETIT_RXTX_DATABUF,
    PETIT_RXTX_WAIT_ANSWER,
    PETIT_RXTX_TIMEOUT
}PETIT_RXTX_STATE;

typedef struct
{
  unsigned char     Address;
  unsigned char     Function;
  unsigned char     DataBuf[PETITMODBUS_RXTX_BUFFER_SIZE];
  unsigned short    DataLen;
}PETIT_RXTX_DATA;

/**********************Slave Transmit and Receive Variables********************/
PETIT_RXTX_DATA     Petit_Tx_Data;
unsigned int        Petit_Tx_Current              = 0;
unsigned int        Petit_Tx_CRC16                = 0xFFFF;
PETIT_RXTX_STATE    Petit_Tx_State                = PETIT_RXTX_IDLE;
unsigned char       Petit_Tx_Buf[PETITMODBUS_TRANSMIT_BUFFER_SIZE];
unsigned int        Petit_Tx_Buf_Size             = 0;

PETIT_RXTX_DATA     Petit_Rx_Data;
unsigned int        Petit_Rx_CRC16                = 0xFFFF;
PETIT_RXTX_STATE    Petit_Rx_State                = PETIT_RXTX_IDLE;
unsigned char       Petit_Rx_Data_Available       = FALSE;

volatile unsigned short PetitModbusTimerValue         = 0;
volatile unsigned int LED_TX = 0;
volatile unsigned int LED_RX = 0;
/****************End of Slave Transmit and Receive Variables*******************/

/*
 * Function Name        : CRC16
 * @param[in]           : Data  - Data to Calculate CRC
 * @param[in/out]       : CRC   - Anlik CRC degeri
 * @How to use          : First initial data has to be 0xFFFF.
 * @Options             : If CRC_CALC is defined a CRC is calculated else a 
 *                        lookup is done (speed diff)
 * @Function            : The CRC field is appended to the message as the last 
 *                        field in the message. When this is done, the 
 *                        low?order byte of the field is appended first, 
 *                        followed by the high?order byte. The CRC high?order 
 *                        byte is the last byte to be sent in the message.
 * @Statistics          : @PIC16F18854 @32MHz  XC8 PRO mode optm. speed + asm
 *                        Calc      : 31.68us
 *                        Lookup    : 7.36us
 *                        HW        : 2.98us (without protection (checking on busy etc) + without jumps (direct register use instead of MCC files)
 *                                            otherwise Lookup will be faster, HW will then be around 9us!)
 *                                            secondly, HW takes a reverse CRC at the end taking 36.28us!)
 *                                            Adding a safety NOP() after the GO takes it to 3.23us.
 * 
 * When using HW without protection and after reverse for 9 bytes data it will take 66.08us
 * When using Lookup for 9 bytes data it will take 66.24 us
 */
#ifdef CRC_CALC
void Petit_CRC16(const unsigned char Data, unsigned int* CRC)
{
    LED_WAR_LAT = 1;
    unsigned int i;

    *CRC = *CRC ^(unsigned int) Data;
    for (i = 8; i > 0; i--)
    {
        if (*CRC & 0x0001)
            *CRC = (*CRC >> 1) ^ 0xA001;
        else
            *CRC >>= 1;
    }
    LED_WAR_LAT = 0;
}
#endif
#ifdef CRC_LOOKUP
void Petit_CRC16(const unsigned char Data, unsigned int* CRC)
{
    //LED_WAR_LAT = 1;
    unsigned int uchCRCHi = *CRC >> 8;                                          /* high byte of CRC initialized */
    unsigned int uchCRCLo = *CRC & 0x00FF;                                      /* low byte of CRC initialized */
    unsigned char uIndex ;                                                      /* will index into CRC lookup table */
    
    uIndex = uchCRCLo ^ Data;
    uchCRCLo = uchCRCHi ^ auchCRCHi[uIndex] ;
    uchCRCHi = auchCRCLo[uIndex] ;
    *CRC = (unsigned int)(uchCRCHi << 8 | uchCRCLo) ;
    //LED_WAR_LAT = 0;
}
#endif
/* Data = 0x02
 * uIndex = 0xFF ^ 0x02 = 0xFD
 * uchCRCLo = 0xFF ^ 0xC1 = 3E
 * uchCRCHi = 0x81
 * CRC = 0x813E
 * 
 * Data = 0x07
 * uIndex = 3E ^ 0x07 = 39
 * uchCRCLo = 0x81 ^ 0xC0 = 0x41
 * uchCRCHi = 0x12
 * CRC = 0x1241
*/
#ifdef CRC_HW
void Petit_CRC16(const unsigned char Data, unsigned int* CRC)
{
    CRCACCH = *CRC >> 8;
    CRCACCL = *CRC & 0x00FF;
    
    CRCDATL = Data;//CRC_8BitDataWrite(Data);//while (!CRC_8BitDataWrite(Data));    
    CRCCON0bits.CRCGO = 1;//CRC_Start();
    while (CRCCON0bits.BUSY);
    *CRC = ((uint16_t)CRCACCH << 8)|CRCACCL;//CRC_CalculatedResultGet(false, 0);    
}
/* 
 * https://www.microchip.com/forums/m885342.aspx
 * http://www.tahapaksu.com/crc/
 * 
 * Use reflected/reversed Modbus polynominal = 0x8005
 * 16 bit polynominal Word Width
 * start with feed: 0xFFFF
 * Seed method: Direct --> should not matter since we feed our selves
 * Seed Shift Direction: shift right --> should not matter since we feed our selves
 * Augmentation Mode: data augmented with 0's
 * Data Shift Direction: shift right
 * Data Word Width: 8 bits
 * Feed all the bytes through the HW CRC calc without reversing
 * When done, reverse the result --> done.
 * CRC example in MCC and checked on http://www.tahapaksu.com/crc/ :
 * 0x6C, 0x93 Reverse and final XOR = 0 --> 0xDD6C
*/
#endif
/******************************************************************************/

/*
 * Function Name        : DoTx
 * @param[out]          : TRUE
 * @How to use          : It is used for send data package over physical layer
 */
unsigned char Petit_DoSlaveTX(void)
{  
    PetitModBus_UART_String(Petit_Tx_Buf,Petit_Tx_Buf_Size);
    Petit_Tx_Buf_Size = 0;    
    return TRUE;
}

/******************************************************************************/

/*
 * Function Name        : SendMessage
 * @param[out]          : TRUE/FALSE
 * @How to use          : This function start to sending messages
 */
unsigned char PetitSendMessage(void)
{
    if (Petit_Tx_State != PETIT_RXTX_IDLE){
        return FALSE;
    }
	PetitHoldingRegisters[HR_MESSAGES_SENT].ActValue += 1;    // Count the amount of transmitted messages
    Petit_Tx_Current  =0;
    Petit_Tx_State    =PETIT_RXTX_START;

    return TRUE;
}

/******************************************************************************/

/*
 * Function Name        : HandleModbusError
 * @How to use          : This function generated errors to Modbus Master
 */
void HandlePetitModbusError(unsigned char ErrorCode, unsigned char Function)
{
    if (Petit_Rx_Data.Address == PETITMODBUS_BROADCAST_ADDRESS)                 // Broadcast cannot process back communication (all slaves would have to respond!)
    {
        return;
    }
    else
    {
        // Initialise the output buffer. The first byte in the buffer says how many registers we have read
        Petit_Tx_Data.Function    = (unsigned int)Function | 0x80;
        Petit_Tx_Data.DataBuf[0]  = ErrorCode;
        Petit_Tx_Data.Address     = PETITMODBUS_SLAVE_ADDRESS;
        Petit_Tx_Data.DataLen     = 1;
        PetitSendMessage();
    }
}

/******************************************************************************/

/*
 * Function Name        : HandleModbusReadHoldingRegisters
 * @How to use          : Modbus function 03 - Read holding registers
 */
#if PETITMODBUS_READ_HOLDING_REGISTERS_ENABLED > 0
void HandlePetitModbusReadHoldingRegisters(void)
{
    // Holding registers are effectively numerical outputs that can be written to by the host.
    // They can be control registers or analogue outputs.
    // We potientially have one - the pwm output value
    unsigned int    Petit_StartAddress        = 0;
    unsigned int    Petit_NumberOfRegisters   = 0;
    unsigned int    Petit_i                   = 0;

    // The message contains the requested start address and number of registers
    Petit_StartAddress        = ((unsigned int) (Petit_Rx_Data.DataBuf[0]) << 8) + (unsigned int) (Petit_Rx_Data.DataBuf[1]);
    Petit_NumberOfRegisters   = ((unsigned int) (Petit_Rx_Data.DataBuf[2]) << 8) + (unsigned int) (Petit_Rx_Data.DataBuf[3]);
    
    if (Petit_Rx_Data.Address == PETITMODBUS_BROADCAST_ADDRESS)                 // Broadcast cannot process back communication (all slaves would have to respond!)
    {
        return;
    }
    // If it is bigger than RegisterNumber return error to Modbus Master
    else if((Petit_StartAddress+Petit_NumberOfRegisters)>NUMBER_OF_HOLDING_PETITREGISTERS){
        HandlePetitModbusError(PETIT_ERROR_CODE_02, PETITMODBUS_READ_HOLDING_REGISTERS);
    }
    else
    {
        // Initialise the output buffer. The first byte in the buffer says how many registers we have read
        Petit_Tx_Data.Function    = PETITMODBUS_READ_HOLDING_REGISTERS;
        Petit_Tx_Data.Address     = PETITMODBUS_SLAVE_ADDRESS;
        Petit_Tx_Data.DataLen     = 1;
        Petit_Tx_Data.DataBuf[0]  = 0;

        for (Petit_i = 0; Petit_i < Petit_NumberOfRegisters; Petit_i++)
        {
            unsigned short Petit_CurrentData = PetitHoldingRegisters[Petit_StartAddress+Petit_i].ActValue;

            Petit_Tx_Data.DataBuf[Petit_Tx_Data.DataLen]        = (unsigned char) ((Petit_CurrentData & 0xFF00) >> 8);
            Petit_Tx_Data.DataBuf[Petit_Tx_Data.DataLen + 1]    = (unsigned char) (Petit_CurrentData & 0xFF);
            Petit_Tx_Data.DataLen                        += 2;
            Petit_Tx_Data.DataBuf[0]                      = Petit_Tx_Data.DataLen - 1;
        }

        PetitSendMessage();
    }
}
#endif

/******************************************************************************/

/*
 * Function Name        : HandleModbusReadInputRegisters
 * @How to use          : Modbus function 04 - Read input registers
 */
#if PETITMODBUS_READ_INPUT_REGISTERS_ENABLED > 0
void HandlePetitModbusReadInputRegisters(void)
{
    // Input registers are effectively numerical Inputs that can be read only by the host.
    unsigned int    Petit_StartAddress        = 0;
    unsigned int    Petit_NumberOfRegisters   = 0;
    unsigned int    Petit_i                   = 0;

    // The message contains the requested start address and number of registers
    Petit_StartAddress        = ((unsigned int) (Petit_Rx_Data.DataBuf[0]) << 8) + (unsigned int) (Petit_Rx_Data.DataBuf[1]);
    Petit_NumberOfRegisters   = ((unsigned int) (Petit_Rx_Data.DataBuf[2]) << 8) + (unsigned int) (Petit_Rx_Data.DataBuf[3]);
    
    if (Petit_Rx_Data.Address == PETITMODBUS_BROADCAST_ADDRESS)                 // Broadcast cannot process back communication (all slaves would have to respond!)
    {
        return;
    }
    // If it is bigger than RegisterNumber return error to Modbus Master
    else if((Petit_StartAddress+Petit_NumberOfRegisters)>NUMBER_OF_INPUT_PETITREGISTERS){
        HandlePetitModbusError(PETIT_ERROR_CODE_02, PETITMODBUS_READ_INPUT_REGISTERS);
    }
    else
    {
        // Initialise the output buffer. The first byte in the buffer says how many registers we have read
        Petit_Tx_Data.Function    = PETITMODBUS_READ_INPUT_REGISTERS;
        Petit_Tx_Data.Address     = PETITMODBUS_SLAVE_ADDRESS;
        Petit_Tx_Data.DataLen     = 1;
        Petit_Tx_Data.DataBuf[0]  = 0;

        for (Petit_i = 0; Petit_i < Petit_NumberOfRegisters; Petit_i++)
        {
            unsigned short Petit_CurrentData = PetitInputRegisters[Petit_StartAddress+Petit_i].ActValue;

            Petit_Tx_Data.DataBuf[Petit_Tx_Data.DataLen]        = (unsigned char) ((Petit_CurrentData & 0xFF00) >> 8);
            Petit_Tx_Data.DataBuf[Petit_Tx_Data.DataLen + 1]    = (unsigned char) (Petit_CurrentData & 0xFF);
            Petit_Tx_Data.DataLen                        += 2;
            Petit_Tx_Data.DataBuf[0]                      = Petit_Tx_Data.DataLen - 1;
        }

        PetitSendMessage();
    }
}
#endif

/******************************************************************************/

/*
 * Function Name        : HandleModbusReadInputRegisters
 * @How to use          : Modbus function 06 - Write single register
 */
#if PETITMODBUSWRITE_SINGLE_REGISTER_ENABLED > 0
void HandlePetitModbusWriteSingleRegister(void)
{
    // Write single numerical output
    unsigned int    Petit_Address   = 0;
    unsigned int    Petit_Value     = 0;
    unsigned char   Petit_i         = 0;

    // The message contains the requested start address and number of registers
    Petit_Address   = ((unsigned int) (Petit_Rx_Data.DataBuf[0]) << 8) + (unsigned int) (Petit_Rx_Data.DataBuf[1]);
    Petit_Value     = ((unsigned int) (Petit_Rx_Data.DataBuf[2]) << 8) + (unsigned int) (Petit_Rx_Data.DataBuf[3]);

    // Initialise the output buffer. The first byte in the buffer says how many registers we have read
    Petit_Tx_Data.Function    = PETITMODBUS_WRITE_SINGLE_REGISTER;
    Petit_Tx_Data.Address     = PETITMODBUS_SLAVE_ADDRESS;
    Petit_Tx_Data.DataLen     = 4;

    if (Petit_Rx_Data.Address == PETITMODBUS_BROADCAST_ADDRESS)                 // Broadcast cannot process back communication (all slaves would have to respond!)
    {
        PetitHoldingRegisters[Petit_Address].ActValue=Petit_Value;
        return;
    }
    else if(Petit_Address>=NUMBER_OF_HOLDING_PETITREGISTERS){
        HandlePetitModbusError(PETIT_ERROR_CODE_03, PETITMODBUS_WRITE_SINGLE_REGISTER);
    }
    else
    {
        PetitHoldingRegisters[Petit_Address].ActValue=Petit_Value;
        // Output data buffer is exact copy of input buffer
        for (Petit_i = 0; Petit_i < 4; ++Petit_i)
            Petit_Tx_Data.DataBuf[Petit_i] = Petit_Rx_Data.DataBuf[Petit_i];
    }
    
    PetitSendMessage();
}
#endif

/******************************************************************************/

/*
 * Function Name        : HandleModbusReadInputRegisters
 * @How to use          : Modbus function 08 - Diagnostic registers
 * At this time only read diagnostic registers
 */
#if PETITMODBUS_DIAGNOSTIC_REGISTERS_ENABLED > 0
void HandleMPetitodbusDiagnosticRegisters(void)
{
    // Input registers are effectively numerical Inputs that can be read only by the host.
    unsigned int    Petit_StartAddress        = 0;
    unsigned int    Petit_NumberOfRegisters   = 0;
    unsigned int    Petit_i                   = 0;

    // The message contains the requested start address and number of registers
    Petit_StartAddress        = ((unsigned int) (Petit_Rx_Data.DataBuf[0]) << 8) + (unsigned int) (Petit_Rx_Data.DataBuf[1]);
    Petit_NumberOfRegisters   = ((unsigned int) (Petit_Rx_Data.DataBuf[2]) << 8) + (unsigned int) (Petit_Rx_Data.DataBuf[3]);
    
    if (Petit_Rx_Data.Address == PETITMODBUS_BROADCAST_ADDRESS)                 // Broadcast cannot process back communication (all slaves would have to respond!)
    {
        return;
    }
    // If it is bigger than RegisterNumber return error to Modbus Master
    else if((Petit_StartAddress+Petit_NumberOfRegisters)>NUMBER_OF_DIAGNOSTIC_PETITREGISTERS){
        HandlePetitModbusError(PETIT_ERROR_CODE_02, PETITMODBUS_DIAGNOSTIC_REGISTERS);
    }
    else
    {
        // Initialise the output buffer. The first byte in the buffer says how many registers we have read
        Petit_Tx_Data.Function    = PETITMODBUS_DIAGNOSTIC_REGISTERS;
        Petit_Tx_Data.Address     = PETITMODBUS_SLAVE_ADDRESS;
        Petit_Tx_Data.DataLen     = 1;
        Petit_Tx_Data.DataBuf[0]  = 0;

        for (Petit_i = 0; Petit_i < Petit_NumberOfRegisters; Petit_i++)
        {
            unsigned short Petit_CurrentData = PetitDiagnosticRegisters[Petit_StartAddress+Petit_i].ActValue;

            Petit_Tx_Data.DataBuf[Petit_Tx_Data.DataLen]        = (unsigned char) ((Petit_CurrentData & 0xFF00) >> 8);
            Petit_Tx_Data.DataBuf[Petit_Tx_Data.DataLen + 1]    = (unsigned char) (Petit_CurrentData & 0xFF);
            Petit_Tx_Data.DataLen                        += 2;
            Petit_Tx_Data.DataBuf[0]                      = Petit_Tx_Data.DataLen - 1;
        }

        PetitSendMessage();
    }
}
#endif

/******************************************************************************/

/*
 * Function Name        : HandleModbusWriteMultipleRegisters
 * @How to use          : Modbus function 16 - Write multiple registers
 */
#if PETITMODBUS_WRITE_MULTIPLE_REGISTERS_ENABLED > 0
void HandleMPetitodbusWriteMultipleRegisters(void)
{
    // Write single numerical output
    unsigned int    Petit_StartAddress            =0;
    unsigned int    Petit_ByteCount               =0;
    unsigned int    Petit_NumberOfRegisters       =0;
    unsigned int    Petit_i                       =0;
    unsigned int	Petit_Value                   =0;

    // The message contains the requested start address and number of registers
    Petit_StartAddress        = ((unsigned int) (Petit_Rx_Data.DataBuf[0]) << 8) + (unsigned int) (Petit_Rx_Data.DataBuf[1]);
    Petit_NumberOfRegisters   = ((unsigned int) (Petit_Rx_Data.DataBuf[2]) << 8) + (unsigned int) (Petit_Rx_Data.DataBuf[3]);
    Petit_ByteCount           = Petit_Rx_Data.DataBuf[4];

    if (Petit_Rx_Data.Address == PETITMODBUS_BROADCAST_ADDRESS)                 // Broadcast cannot process back communication (all slaves would have to respond!)
    {
        for (Petit_i = 0; Petit_i <Petit_NumberOfRegisters; Petit_i++)
        {
            Petit_Value = ((unsigned int)Petit_Rx_Data.DataBuf[5+2*Petit_i]<<8)+((unsigned int)Petit_Rx_Data.DataBuf[6+2*Petit_i]);
            PetitHoldingRegisters[Petit_StartAddress+Petit_i].ActValue=Petit_Value;
        }
        return;
    }
    else if((Petit_StartAddress+Petit_NumberOfRegisters)>NUMBER_OF_HOLDING_PETITREGISTERS){
        HandlePetitModbusError(PETIT_ERROR_CODE_03, PETITMODBUS_WRITE_MULTIPLE_REGISTERS);
        // If it is bigger than RegisterNumber return error to Modbus Master
    }
    else
    {
        // Initialise the output buffer. The first byte in the buffer says how many outputs we have set
        Petit_Tx_Data.Function    = PETITMODBUS_WRITE_MULTIPLE_REGISTERS;
        Petit_Tx_Data.Address     = PETITMODBUS_SLAVE_ADDRESS;
        Petit_Tx_Data.DataLen     = 4;
        Petit_Tx_Data.DataBuf[0]  = Petit_Rx_Data.DataBuf[0];
        Petit_Tx_Data.DataBuf[1]  = Petit_Rx_Data.DataBuf[1];
        Petit_Tx_Data.DataBuf[2]  = Petit_Rx_Data.DataBuf[2];
        Petit_Tx_Data.DataBuf[3]  = Petit_Rx_Data.DataBuf[3];

        // Output data buffer is exact copy of input buffer
        for (Petit_i = 0; Petit_i <Petit_NumberOfRegisters; Petit_i++)
        {
            Petit_Value = ((unsigned int)Petit_Rx_Data.DataBuf[5+2*Petit_i]<<8) + ((unsigned int)Petit_Rx_Data.DataBuf[6+2*Petit_i]);
            PetitHoldingRegisters[Petit_StartAddress+Petit_i].ActValue=Petit_Value;
        }
		
        PetitSendMessage();
    }
}
#endif

/******************************************************************************/

/*
 * Function Name        : RxDataAvailable
 * @return              : If Data is Ready, Return TRUE
 *                        If Data is not Ready, Return FALSE
 */
unsigned char Petit_RxDataAvailable(void)
{
    unsigned char Result    = Petit_Rx_Data_Available;
    
    Petit_Rx_Data_Available       = FALSE;

    return Result;
}

/******************************************************************************/

/*
 * Function Name        : CheckRxTimeout
 * @return              : If Time is out return TRUE
 *                        If Time is not out return FALSE
 */
unsigned char Petit_CheckRxTimeout(void)
{
    // A return value of true indicates there is a timeout    
    if (PetitModbusTimerValue>= PETITMODBUS_TIMEOUTTIMER)
    {
        RC1STAbits.ADDEN = 1;      // ensure we return to address-detect
        PetitModbusTimerValue   =0;
        PetitReceiveCounter     =0;
        return TRUE;
    }

    return FALSE;
}

/******************************************************************************/

/*
 * Function Name        : CheckBufferComplete
 * @return              : If data is ready, return              DATA_READY
 *                        If slave address is wrong, return     FALSE_SLAVE_ADDRESS
 *                        If data is not ready, return          DATA_NOT_READY
 *                        If functions is wrong, return         FALSE_FUNCTION
 */
// Use the actual receive buffer length here.
// If PetitReceiveBuffer is sized PETITMODBUS_RXTX_BUFFER_SIZE, keep it like this.
#define PETIT_RX_BUF_LEN   PETITMODBUS_RXTX_BUFFER_SIZE

static inline uint16_t U16_FromBE(uint8_t hi, uint8_t lo)
{
    return (uint16_t)(((uint16_t)hi << 8) | (uint16_t)lo);
}

unsigned char CheckPetitModbusBufferComplete(void)
{
    uint16_t expected = 0;
    uint8_t  addr, fn;

    // Need enough bytes to identify address/function and (for variable length) reach byteCount
    if (PetitReceiveCounter <= 4u)
        return PETIT_DATA_NOT_READY;

    // Hard clamp: if counter already exceeds our buffer, drop it immediately.
    if (PetitReceiveCounter > PETIT_RX_BUF_LEN)
    {
        PetitReceiveCounter = 0;                      // overflow / desync
        return PETIT_DATA_NOT_READY;
    }

    addr = PetitReceiveBuffer[0];
    fn   = PetitReceiveBuffer[1];

    // Not for us? Drop partial frame and wait for silent gap timeout to resync.
    if ((addr != PETITMODBUS_SLAVE_ADDRESS) && (addr != PETITMODBUS_BROADCAST_ADDRESS))
    {
        PetitReceiveCounter = 0;                      // not for us; drop partial frame
        return PETIT_FALSE_SLAVE_ADDRESS;
    }

    switch (fn)
    {
        case PETITMODBUS_READ_COILS:
        case PETITMODBUS_READ_DISCRETE_INPUTS:
        case PETITMODBUS_READ_HOLDING_REGISTERS:
        case PETITMODBUS_READ_INPUT_REGISTERS:
        case PETITMODBUS_WRITE_SINGLE_COIL:
        case PETITMODBUS_WRITE_SINGLE_REGISTER:
        case PETITMODBUS_DIAGNOSTIC_REGISTERS:
            expected = 8u;                            // fixed-length request + CRC
            break;

        case PETITMODBUS_WRITE_MULTIPLE_COILS:        // FC15
        case PETITMODBUS_WRITE_MULTIPLE_REGISTERS:    // FC16
        {
            // Need at least up to byteCount at index 6
            if (PetitReceiveCounter <= 6u)
                return PETIT_DATA_NOT_READY;

            // Parse quantity field (big-endian) from request:
            // addr(0), fn(1), startHi(2), startLo(3), qtyHi(4), qtyLo(5), byteCount(6), ...
            uint16_t qty       = U16_FromBE(PetitReceiveBuffer[4], PetitReceiveBuffer[5]);
            uint16_t byteCount = (uint16_t)PetitReceiveBuffer[6];

            // Quantity must be non-zero for FC15/FC16
            if (qty == 0u)
            {
                PetitReceiveCounter = 0;              // invalid request -> drop
                return PETIT_DATA_NOT_READY;
            }

            // Validate byteCount against qty according to Modbus spec
            if (fn == PETITMODBUS_WRITE_MULTIPLE_REGISTERS)
            {
                // FC16: byteCount must be qty * 2
                uint16_t required = (uint16_t)(qty * 2u);
                if (byteCount != required)
                {
                    PetitReceiveCounter = 0;          // inconsistent length -> drop
                    return PETIT_DATA_NOT_READY;
                }
            }
            else
            {
                // FC15: byteCount must be ceil(qty/8) = (qty + 7) / 8
                uint16_t required = (uint16_t)((qty + 7u) >> 3);
                if (byteCount != required)
                {
                    PetitReceiveCounter = 0;          // inconsistent length -> drop
                    return PETIT_DATA_NOT_READY;
                }
            }

            // Total frame length (request):
            // addr(1) fn(1) start(2) qty(2) byteCount(1) data(byteCount) crc(2) = 9 + byteCount
            expected = (uint16_t)(9u + byteCount);

            // Clamp: reject impossible sizes (prevents buffer overrun).
            if (expected > PETIT_RX_BUF_LEN)
            {
                PetitReceiveCounter = 0;              // invalid length -> drop
                return PETIT_DATA_NOT_READY;
            }

            break;
        }

        default:
            PetitReceiveCounter = 0;                  // unsupported function
            return PETIT_FALSE_FUNCTION;
    }

    if (PetitReceiveCounter == expected)
    {        
        return PETIT_DATA_READY;
    }

    // If we somehow exceeded expected, we're out of sync; drop it.
    if (PetitReceiveCounter > expected)
    {
        PetitReceiveCounter = 0;                       // desync / noise
        return PETIT_DATA_NOT_READY;
    }
    return PETIT_DATA_NOT_READY;
}

/******************************************************************************/

/*
 * Function Name        : RxRTU
 * @How to use          : Check for data ready, if it is good return answer
 */
void Petit_RxRTU(void)
{
    unsigned char   Petit_i;
    unsigned char   Petit_ReceiveBufferControl=0;

    Petit_ReceiveBufferControl    =CheckPetitModbusBufferComplete();

    if(Petit_ReceiveBufferControl==PETIT_DATA_READY)
    {
        Petit_Rx_Data.Address               =PetitReceiveBuffer[0];
        Petit_Rx_CRC16                      =0xFFFF;        
        Petit_CRC16(Petit_Rx_Data.Address, &Petit_Rx_CRC16);
        Petit_Rx_Data.Function              =PetitReceiveBuffer[1];
        Petit_CRC16(Petit_Rx_Data.Function, &Petit_Rx_CRC16);

        Petit_Rx_Data.DataLen=0;

        for(Petit_i=2;Petit_i<PetitReceiveCounter;Petit_i++)
            Petit_Rx_Data.DataBuf[Petit_Rx_Data.DataLen++]=PetitReceiveBuffer[Petit_i];

        Petit_Rx_State =PETIT_RXTX_DATABUF;

        PetitReceiveCounter=0;      
    }                               

    Petit_CheckRxTimeout();         // if data is not for this slave, reset the counter, however data is still coming in from the rest of the message, 
                                    // this is deleted by resetting the counter again after the minimum of 3.5 char wait time: PETITMODBUS_TIMEOUTTIMER

    if ((Petit_Rx_State == PETIT_RXTX_DATABUF) && (Petit_Rx_Data.DataLen >= 2))
    {
        // Finish off our CRC check
        Petit_Rx_Data.DataLen -= 2;
        for (Petit_i = 0; Petit_i < Petit_Rx_Data.DataLen; ++Petit_i)
        {
            Petit_CRC16(Petit_Rx_Data.DataBuf[Petit_i], &Petit_Rx_CRC16);
        }
        
        #ifdef  CRC_HW_REVERSE
        Petit_Rx_CRC16 = CRC_ReverseValue(Petit_Rx_CRC16);
        #endif
        
        if (((unsigned int) Petit_Rx_Data.DataBuf[Petit_Rx_Data.DataLen] + ((unsigned int) Petit_Rx_Data.DataBuf[Petit_Rx_Data.DataLen + 1] << 8)) == Petit_Rx_CRC16)
        {
            // Valid message!
            LED_RX++;
            Petit_Rx_Data_Available = TRUE;
        }
        else{
            NOP();
        }

        Petit_Rx_State = PETIT_RXTX_IDLE;
    }
}

/******************************************************************************/

/*
 * Function Name        : TxRTU
 * @How to use          : If it is ready send answers!
 */
void Petit_TxRTU(void)
{
    Petit_Tx_CRC16                =0xFFFF;    
    Petit_Tx_Buf_Size             =0;
    Petit_Tx_Buf[Petit_Tx_Buf_Size++]   =Petit_Tx_Data.Address;
    Petit_CRC16(Petit_Tx_Data.Address, &Petit_Tx_CRC16);
    Petit_Tx_Buf[Petit_Tx_Buf_Size++]   =Petit_Tx_Data.Function;
    Petit_CRC16(Petit_Tx_Data.Function, &Petit_Tx_CRC16);

    for(Petit_Tx_Current=0; Petit_Tx_Current < Petit_Tx_Data.DataLen; Petit_Tx_Current++)
    {
        Petit_Tx_Buf[Petit_Tx_Buf_Size++]=Petit_Tx_Data.DataBuf[Petit_Tx_Current];
        Petit_CRC16(Petit_Tx_Data.DataBuf[Petit_Tx_Current], &Petit_Tx_CRC16);
    }
    
    #ifdef  CRC_HW_REVERSE
    Petit_Tx_CRC16 = CRC_ReverseValue(Petit_Tx_CRC16);
    #endif
    
    Petit_Tx_Buf[Petit_Tx_Buf_Size++] = Petit_Tx_CRC16 & 0x00FF;
    Petit_Tx_Buf[Petit_Tx_Buf_Size++] =(Petit_Tx_CRC16 & 0xFF00) >> 8;

    Petit_DoSlaveTX();
    
    Petit_Tx_State    =PETIT_RXTX_IDLE;
}

/******************************************************************************/

/*
 * Function Name        : ProcessModbus
 * @How to use          : ModBus main core! Call this function into main!
 */
unsigned int ProcessPetitModbus(void)
{
    unsigned int return_val = 0;
    
    if (Petit_Tx_State != PETIT_RXTX_IDLE){                                      // If answer is ready, send it!
        Petit_TxRTU();        
        return_val  = 2;
    }
    else{
        return_val  = 1;
    }
    
    Petit_RxRTU();                                                              // Call this function every cycle

    if (Petit_RxDataAvailable())                                                // If data is ready enter this!
    {
        if ((Petit_Rx_Data.Address == PETITMODBUS_SLAVE_ADDRESS) || (Petit_Rx_Data.Address == PETITMODBUS_BROADCAST_ADDRESS)) // Is Data for us?
        {
            if (Petit_Rx_Data.Address == PETITMODBUS_SLAVE_ADDRESS){
                PetitHoldingRegisters[HR_MESSAGES_RECEIVED].ActValue += 1;// Count the amount of received messages, not the broadcast messages
            }
            
            switch (Petit_Rx_Data.Function)                                     // Data is for us but which function?
            {
                #if PETITMODBUS_READ_HOLDING_REGISTERS_ENABLED > 0
                case PETITMODBUS_READ_HOLDING_REGISTERS:    {   HandlePetitModbusReadHoldingRegisters();        break;  }
                #endif
                #if PETITMODBUS_READ_INPUT_REGISTERS_ENABLED > 0
                case PETITMODBUS_READ_INPUT_REGISTERS:      {   HandlePetitModbusReadInputRegisters();          break;  }
                #endif
                #if PETITMODBUSWRITE_SINGLE_REGISTER_ENABLED > 0
                case PETITMODBUS_WRITE_SINGLE_REGISTER:     {   HandlePetitModbusWriteSingleRegister();         break;  }
                #endif
                #if PETITMODBUS_DIAGNOSTIC_REGISTERS_ENABLED > 0
                case PETITMODBUS_DIAGNOSTIC_REGISTERS:      {   HandleMPetitodbusDiagnosticRegisters();         break;  }
                #endif
                #if PETITMODBUS_WRITE_MULTIPLE_REGISTERS_ENABLED > 0
                case PETITMODBUS_WRITE_MULTIPLE_REGISTERS:  {   HandleMPetitodbusWriteMultipleRegisters();      break;  }
                #endif

                default:                                    {   HandlePetitModbusError(PETIT_ERROR_CODE_01, Petit_Rx_Data.Function);    break;  }
            }
            
            LED_RX++;
        }
    }
    return (return_val);
}

/******************************************************************************/

/*
 * Function Name        : InitPetitModbus
 * @How to use          : Petite ModBus slave initialize
 */
void InitPetitModbus(unsigned char PetitModbusSlaveAddress)
{
    PETITMODBUS_SLAVE_ADDRESS    =     PetitModbusSlaveAddress;
    
    PetitModBus_UART_Initialise();
    PetitModBus_TIMER_Initialise();
}

/******************************************************************************/

/*
 * Function Name        : CRC_ReverseValue
 * @How to use          : Feed the last HW calculated result
 */
#ifdef  CRC_HW_REVERSE

uint16_t CRC_ReverseValue(uint16_t crc)
{
    uint16_t mask;
    uint16_t reverse;

    mask = 1;
    mask <<= 15;
    reverse = 0;

    while(crc)
    {
        if(crc & 0x01)
        {
            reverse |= mask;
        }
        mask >>= 1;
        crc >>= 1;
    }
    return reverse;
}
#endif

/******************************************************************************/