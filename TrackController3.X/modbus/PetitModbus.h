/*  PetitModbus Version 1.0
 *  Author  :   Firat DEVECI
 *  Date    :   27.07.16
 *  
 *  Tips    :   If you want to use RS485 you have to use RX-Pull-Up Resistor!
 */

#ifndef __PETITMODBUS__H
#define __PETITMODBUS__H

#define NUMBER_OF_OUTPUT_PETITREGISTERS                 7                       // Petit Modbus RTU Slave Output Register Number, master has to know how big the slave is
                                                                                // Have to put a number of registers here
                                                                                // It has to be bigger than 0 (zero)!!
#define PETITMODBUS_TIMEOUTTIMER                        2                       // Timeout Constant for Petit Modbus RTU Slave [101us tick]
#define PETITMODBUS_SLAVECOMMTIMEOUTTIMER               5                       // Uses tmr0 IF, 190 to 255 = 65 = 225uS, 1 timer round is 255 = 3.9 x 225us = 882 us this is the factor times the define constant here to wait for answer. 4 is about 5ms (with application jitter added) normal response is in the range of 430 us slave response.

//#define CRC_CALC                                                                // When uncommented a CRC calculation is used for the function void Petit_CRC16(const unsigned char Data, unsigned int* CRC)




/****************************Don't Touch This**********************************/
// Buffers for Petit Modbus RTU Slave
#define PETITMODBUS_RECEIVE_BUFFER_SIZE                 (NUMBER_OF_OUTPUT_PETITREGISTERS*2 + 10) 
#define PETITMODBUS_TRANSMIT_BUFFER_SIZE                PETITMODBUS_RECEIVE_BUFFER_SIZE
#define PETITMODBUS_RXTX_BUFFER_SIZE                    PETITMODBUS_TRANSMIT_BUFFER_SIZE

extern volatile unsigned short PetitModbusTimerValue;
extern volatile unsigned int SlaveAnswerTimeoutCounter;

typedef enum
{
    SLAVE_DATA_BUSY = 1,
    SLAVE_DATA_OK = 2,
    SLAVE_DATA_NOK = 3,
    SLAVE_DATA_TIMEOUT = 4
}SLAVE_DATA;

typedef struct
{
    unsigned int        Reg[NUMBER_OF_OUTPUT_PETITREGISTERS];
    unsigned char       MbCommError;
    unsigned char       MbExceptionCode;
    unsigned int        MbReceiveCounter;
    unsigned int        MbSentCounter;
}SLAVE_INFO;

// Main Functions
extern void             InitPetitModbus(SLAVE_INFO *location);
extern void             ProcessPetitModbus(void);
extern unsigned char    SendPetitModbus(unsigned char Address, unsigned char Function, unsigned char *DataBuf, unsigned short DataLen);

void HandlePetitModbusWriteSingleRegisterSlaveReadback(void);
void HandlePetitModbusReadHoldingRegistersSlaveReadback(void);
void HandleMPetitodbusWriteMultipleRegistersSlaveReadback(void);
void HandleMPetitodbusMbExceptionCodesSlaveReadback(void);

// Petit Modbus Port Header
#include "modbus/PetitModbusPort.h"

#endif
