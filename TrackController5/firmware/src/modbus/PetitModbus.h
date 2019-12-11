/*  PetitModbus Version 1.0
 *  Author  :   Firat DEVECI
 *  Date    :   27.07.16
 *  
 *  Tips    :   If you want to use RS485 you have to use RX-Pull-Up Resistor!
 */

#ifndef __PETITMODBUS__H
#define __PETITMODBUS__H
                                                                                // 1 register is 16 bits. The largest register is used to determine receive and transmit buffer size!
#define NUMBER_OF_HOLDING_PETITREGISTERS                 12                     // Petit Modbus RTU Slave Holding Registers (read/write), Have to put a number of registers here It has to be bigger than 0 (zero)!!
#define NUMBER_OF_INPUT_PETITREGISTERS                   0                      // Number of (read only) input registers)
#define NUMBER_OF_DIAGNOSTIC_PETITREGISTERS              0                      // Number of diagnostic registers (send/receive counters)

#define PETITMODBUS_TIMEOUTTIMER                         2                      // Timeout Constant for Petit Modbus RTU Slave [101us tick]

/****************************Don't Touch This**********************************/
// Buffers for Petit Modbus RTU Slave
#define PETITMODBUS_RECEIVE_BUFFER_SIZE                 (NUMBER_OF_HOLDING_PETITREGISTERS*2 + 10) 
#define PETITMODBUS_TRANSMIT_BUFFER_SIZE                PETITMODBUS_RECEIVE_BUFFER_SIZE
#define PETITMODBUS_RXTX_BUFFER_SIZE                    PETITMODBUS_TRANSMIT_BUFFER_SIZE

/*******************************ModBus Functions*******************************/
#define PETITMODBUS_READ_COILS                  1
#define PETITMODBUS_READ_DISCRETE_INPUTS        2
#define PETITMODBUS_READ_HOLDING_REGISTERS      3  // used
#define PETITMODBUS_READ_INPUT_REGISTERS        4
#define PETITMODBUS_WRITE_SINGLE_COIL           5
#define PETITMODBUS_WRITE_SINGLE_REGISTER       6  // used
#define PETITMODBUS_DIAGNOSTIC_REGISTERS        8
#define PETITMODBUS_WRITE_MULTIPLE_COILS        15
#define PETITMODBUS_WRITE_MULTIPLE_REGISTERS    16 // used
/****************************End of ModBus Functions***************************/

/*******************************ModBus Functions in use ***********************/
//#define PETITMODBUS_IN_USE_READ_COILS                  1
//#define PETITMODBUS_IN_USE_READ_DISCRETE_INPUTS        2
#define PETITMODBUS_IN_USE_READ_HOLDING_REGISTERS      3  // used
//#define PETITMODBUS_IN_USE_READ_INPUT_REGISTERS        4
//#define PETITMODBUS_IN_USE_WRITE_SINGLE_COIL           5
#define PETITMODBUS_IN_USE_WRITE_SINGLE_REGISTER       6  // used
//#define PETITMODBUS_IN_USE_DIAGNOSTIC_REGISTERS        8
//#define PETITMODBUS_IN_USE_WRITE_MULTIPLE_COILS        15
#define PETITMODBUS_IN_USE_WRITE_MULTIPLE_REGISTERS    16 // used
/****************************End of ModBus Functions***************************/

extern volatile unsigned short PetitModbusTimerValue;
extern volatile unsigned int SlaveAnswerTimeoutCounter;
extern volatile unsigned int LED_TX;
extern volatile unsigned int LED_RX;

typedef enum
{
    SLAVE_DATA_IDLE = 0x00,
    SLAVE_DATA_BUSY = 0x01,
    SLAVE_DATA_OK = 0x02,
    SLAVE_DATA_NOK = 0x03,
    SLAVE_DATA_TIMEOUT = 0x04,
    SLAVE_DATA_EXCEPTION = 0x05
}SLAVE_DATA;

typedef struct
{
    unsigned char       Header;
    unsigned char       SlaveNumber;
    unsigned char       SlaveDetected;
    unsigned int        HoldingReg[NUMBER_OF_HOLDING_PETITREGISTERS];
    unsigned int        MbReceiveCounter;
    unsigned int        MbSentCounter;
    SLAVE_DATA          MbCommError;
    unsigned char       MbExceptionCode;
    unsigned int        SpiCommErrorCounter;
    unsigned char       Footer;
    
}SLAVE_INFO;

#define SLAVE_INFO_STRUCT_LENGTH sizeof(SLAVE_INFO)

// Main Functions
extern void             INITxPETITXMODBUS(SLAVE_INFO *location, SLAVE_INFO *Dump, unsigned char AmountOfSlaves);
extern void             PROCESSxPETITxMODBUS(void);
extern unsigned char    SENDxPETITxMODBUS(unsigned char Address, unsigned char Function, unsigned char *DataBuf, unsigned short DataLen);

void HandlePetitModbusWriteSingleRegisterSlaveReadback(void);
void HandlePetitModbusReadHoldingRegistersSlaveReadback(void);
void HandleMPetitodbusWriteMultipleRegistersSlaveReadback(void);
void HandleMPetitodbusMbExceptionCodesSlaveReadback(void);
void HandlePetitModbusReadInputRegistersSlaveReadback(void);
void HandleMPetitodbusDiagnosticRegistersSlaveReadback(void);

/****************************CRC stuff*****************************************/

//#define CRC_CALC                                                              // When uncommented a CRC calculation is used for the function void Petit_CRC16(const unsigned char Data, unsigned int* CRC)
#define CRC_LOOKUP                                                            // When uncommented a CRC is looked up by the processor in flash
//#define CRC_HW                                                                  // When uncommented a CRC is calculated by dedicated HW
#define CRC_HW_REVERSE														// In order to comply to Modbus standard a reverse is required, without however CRC is much faster

#endif