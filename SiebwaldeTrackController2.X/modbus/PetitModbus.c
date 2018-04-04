#include "PetitModbus.h"
#include "PetitModbusPort.h"
#include <xc.h>

/*******************************ModBus Functions*******************************/
#define PETITMODBUS_READ_COILS                  1
#define PETITMODBUS_READ_DISCRETE_INPUTS        2
#define PETITMODBUS_READ_HOLDING_REGISTERS      3
#define PETITMODBUS_READ_INPUT_REGISTERS        4
#define PETITMODBUS_WRITE_SINGLE_COIL           5
#define PETITMODBUS_WRITE_SINGLE_REGISTER       6
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

unsigned char PETITMODBUS_SLAVE_ADDRESS         =1;
unsigned char PETITMODBUS_BROADCAST_ADDRESS     =0;


SLAVE_DATA SLAVE_DATA_VERIFIED;

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
 * @Statistics          : @PIC16F737 @20MHz xc8 1.44 free mode (speed optm)
 *                        Calc      : 64us
 *                        Lookup    : 23us
 */
#ifdef CRC_CALC
void Petit_CRC16(const unsigned char Data, unsigned int* CRC)
{
    unsigned int i;

    *CRC = *CRC ^(unsigned int) Data;
    for (i = 8; i > 0; i--)
    {
        if (*CRC & 0x0001)
            *CRC = (*CRC >> 1) ^ 0xA001;
        else
            *CRC >>= 1;
    }
}
#else
void Petit_CRC16(const unsigned char Data, unsigned int* CRC)
{
    unsigned int uchCRCHi = *CRC >> 8;                                          /* high byte of CRC initialized */
    unsigned int uchCRCLo = *CRC & 0x00FF;                                      /* low byte of CRC initialized */
    unsigned char uIndex ;                                                      /* will index into CRC lookup table */
    
    uIndex = uchCRCLo ^ Data;
    uchCRCLo = uchCRCHi ^ auchCRCHi[uIndex] ;
    uchCRCHi = auchCRCLo[uIndex] ;
    *CRC = (unsigned int)(uchCRCHi << 8 | uchCRCLo) ;
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
    Petit_Tx_Current  =0;
    Petit_Tx_State    =PETIT_RXTX_START;

    return TRUE;
}

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
unsigned char CheckPetitModbusBufferComplete(void)
{
    int PetitExpectedReceiveCount=0;

    if(PetitReceiveCounter>4)
    {
        if(PetitReceiveBuffer[0] > 0)
        {
            if(PetitReceiveBuffer[1]==0x01 || PetitReceiveBuffer[1]==0x02 || PetitReceiveBuffer[1]==0x03 || PetitReceiveBuffer[1]==0x04 || PetitReceiveBuffer[1]==0x05 || PetitReceiveBuffer[1]==0x06)  // RHR
            {
                PetitExpectedReceiveCount    =8;
            }
            else if(PetitReceiveBuffer[1]==0x0F || PetitReceiveBuffer[1]==0x10)
            {
                PetitExpectedReceiveCount=PetitReceiveBuffer[6]+9;
            }
            else
            {
                PetitReceiveCounter=0;
                return PETIT_FALSE_FUNCTION;
            }
        }
        else
        {
            PetitReceiveCounter=0;                  // if data is not for this slave, reset the counter, however data is still coming in from the rest of the message, 
            return PETIT_FALSE_SLAVE_ADDRESS;       // this is deleted by resetting the counter again after the minimum of 3.5 char wait time: PETITMODBUS_TIMEOUTTIMER
        }
    }
    else
        return PETIT_DATA_NOT_READY;

    if(PetitReceiveCounter==PetitExpectedReceiveCount)
    {
        return PETIT_DATA_READY;
    }

    return PETIT_DATA_NOT_READY;
}

/******************************************************************************/

/*
 * Function Name        : RxRTU
 * @How to use          : Check for data ready, if it is good return answer
 * @timing              : @PIC16F737 @20MHz xc8 1.44 free mode (speed optm)
 *                        230us when data is ready (to be added transmission delay
 *                        see PetitModBus_UART_String for measurements)
 */
void Petit_RxRTU(void)
{
    unsigned char   Petit_i;
    unsigned char   Petit_ReceiveBufferControl=0;

    Petit_ReceiveBufferControl    =CheckPetitModbusBufferComplete();

    if(Petit_ReceiveBufferControl==PETIT_DATA_READY)
    {
        Petit_Rx_Data.Address               =PetitReceiveBuffer[0];
        Petit_Rx_CRC16                      = 0xffff;
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
        
        if (((unsigned int) Petit_Rx_Data.DataBuf[Petit_Rx_Data.DataLen] + ((unsigned int) Petit_Rx_Data.DataBuf[Petit_Rx_Data.DataLen + 1] << 8)) == Petit_Rx_CRC16)
        {
            // Valid message!
            Petit_Rx_Data_Available = TRUE;
        }

        Petit_Rx_State = PETIT_RXTX_IDLE;
    }
}

/******************************************************************************/

/*
 * Function Name        : TxRTU
 * @How to use          : If it is ready send answers!
 * @Timing              : PIC16F777 @20MHz compiler 1.44 Free (speed optm) 
 *                        measured with interrupt disable ~215.23us (average, 
 *                        deviation 1us)(measured up to PetitModBus_UART_String 
 *                        before the FOR loop
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
    
    Petit_Tx_Buf[Petit_Tx_Buf_Size++] = Petit_Tx_CRC16 & 0x00FF;
    Petit_Tx_Buf[Petit_Tx_Buf_Size++] =(Petit_Tx_CRC16 & 0xFF00) >> 8;

    Petit_DoSlaveTX();
    
    if (Petit_Tx_Data.Address == PETITMODBUS_BROADCAST_ADDRESS){                // When PETITMODBUS_BROADCAST_ADDRESS then no response will be sent back                                          
        Petit_Tx_State    =PETIT_RXTX_IDLE;
    }
    else{
        Petit_Tx_State    =PETIT_RXTX_WAIT_ANSWER;                              // Else Master must wait for answer (see ModBus protocol implementation)
    }
}

/******************************************************************************/

/*
 * Function Name        : ProcessModbus
 * @How to use          : ModBus main core! Call this function into main!
 */
void ProcessPetitModbus(void)
{
    if (Petit_Tx_State != PETIT_RXTX_IDLE && Petit_Tx_State != PETIT_RXTX_WAIT_ANSWER){   // If answer is ready and not waiting for response, send it!
        //PORTDbits.RD1 = 1;
        Petit_TxRTU();
        SLAVE_DATA_VERIFIED = SLAVE_DATA_BUSY;
        //PORTDbits.RD1 = 0;        
    }
    
    Petit_RxRTU();                                                              // Call this function every cycle

    if (Petit_RxDataAvailable())                                                // If data is ready after CRC check etc. then enter this!
    {
        
        
        switch (Petit_Rx_Data.Function)
        {
            
            case PETITMODBUS_READ_HOLDING_REGISTERS:    {       break;  }


            case PETITMODBUS_WRITE_SINGLE_REGISTER:     {    HandlePetitModbusWriteSingleRegisterSlaveReadback(); break;  }


            case PETITMODBUS_WRITE_MULTIPLE_REGISTERS:  {       break;  }

            default:                                    {       break;  }
        }
    }
}

/******************************************************************************/

/*
 * Function Name        : InitPetitModbus
 * @How to use          : Petite ModBus slave initialize
 */
void InitPetitModbus(void)
{
    PetitModBus_UART_Initialise();
    PetitModBus_TIMER_Initialise();
}

/******************************************************************************/

/*
 * Function Name        : SendPetitModbus
 * @How to use          : 
 */

unsigned char SendPetitModbus(unsigned char Address, unsigned char Function, unsigned char *DataBuf, unsigned short DataLen){

    
    // Initialize the output buffer. The first byte in the buffer says how many registers we have read
    Petit_Tx_Data.Function    = Function;
    Petit_Tx_Data.Address     = Address;
    Petit_Tx_Data.DataLen     = DataLen;
    
    for(Petit_Tx_Current=0; Petit_Tx_Current < Petit_Tx_Data.DataLen; Petit_Tx_Current++)
    {
        Petit_Tx_Data.DataBuf[Petit_Tx_Current]=DataBuf[Petit_Tx_Current];        
    }   
    
    return (PetitSendMessage());
}

/******************************************************************************/

/*
 * Function Name        : SlaveReadBack
 * @How to use          : 
 */
unsigned char SlaveReadBack(void){
    return (SLAVE_DATA_VERIFIED);
}

/******************************************************************************/

/*
 * Function Name        : HandleModbusReadInputRegisters
 * @How to use          : Modbus function 06 - Write single register read back from slave check
 */
void HandlePetitModbusWriteSingleRegisterSlaveReadback(void)
{
    if(Petit_Tx_Data.Function == Petit_Rx_Data.Function){
        if(Petit_Tx_Data.Address == Petit_Rx_Data.Address){
            if(Petit_Tx_Data.DataBuf[0] == Petit_Rx_Data.DataBuf[0]){
                if(Petit_Tx_Data.DataBuf[1] == Petit_Rx_Data.DataBuf[1]){
                    if(Petit_Tx_Data.DataBuf[2] == Petit_Rx_Data.DataBuf[2]){
                        if(Petit_Tx_Data.DataBuf[3] == Petit_Rx_Data.DataBuf[3]){                            
                            SLAVE_DATA_VERIFIED = SLAVE_DATA_OK;
                        }
                    }
                }
            }
        }
    }
    else{
        SLAVE_DATA_VERIFIED = SLAVE_DATA_NOK;
    }
    
    //PORTDbits.RD1 = !PORTDbits.RD1;
    Petit_Tx_State =  PETIT_RXTX_IDLE;

}

/******************************************************************************/