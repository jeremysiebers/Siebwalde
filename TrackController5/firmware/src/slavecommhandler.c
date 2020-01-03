/*
 * File:   slavecommhandler.c
 * Author: Jeremy Siebers
 *
 * Created on aug 28, 2018, 14:15 PM
 */
#include <xc.h>
#include "../TrackController5.X/../../mbus.h"
#include "modbus/PetitModbus.h"
#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdlib.h>
#include "slavecommhandler.h"

/*#--------------------------------------------------------------------------#*/
/*  Description: INITxSLAVExSTARTUP(SLAVE_INFO *location)
 *
 *  Input(s)   : location of stored data array of struct
 *
 *  Output(s)  :
 *
 *  Returns    :
 *
 *  Pre.Cond.  :
 *
 *  Post.Cond. :
 *
 *  Notes      :
 */
/*#--------------------------------------------------------------------------#*/
static SLAVE_INFO *MASTER_SLAVE_DATA = 0;                                       // Holds the address were the received slave data is stored
static SLAVE_INFO *DUMP_SLAVE_DATA   = 0;                                       // Holds the address were the received slave data is stored
REGISTER_PROCESSING *DATA = 0;                                                  // Holds the pointer to the pointed data

void INITxCOMMxHANDLER(SLAVE_INFO *location, SLAVE_INFO *Dump){
    MASTER_SLAVE_DATA  =  location;
    DUMP_SLAVE_DATA    =  Dump;
}

/*#--------------------------------------------------------------------------#*/
/*  Description: SLAVExCOMMUNICATIONxHANDLER(uint8_t SlaveAddress, uint8_t Register, REGISTER_PROCESSING *data)
 *
 *  Input(s)   : 
 *
 *  Output(s)  :
 *
 *  Returns    :
 *
 *  Pre.Cond.  :
 *
 *  Post.Cond. :
 *
 *  Notes      : Handles requests for writing and reading to slaves.
 */
/*#--------------------------------------------------------------------------#*/
uint8_t ReadData[4] = {0, 0, 0, 2};                                             // {start address High, start address Low, 
                                                                                // number of registers High, number of registers Low,

uint8_t WriteData[9] = {0, 0, 0, 0, 0, 0, 0, 0, 0};                             // {start address High, start address Low, number of registers High, number of registers Low, 
                                                                                //  byte count, Register Value Hi, Register Value Lo, Register Value Hi, Register Value Lo} 
/*
typedef struct
{
    uint8_t  Direction;
    bool     Register0Active;    
    uint8_t  Register0;
    uint16_t Data0;
    bool     Register1Active;
    uint8_t  Register1;
    uint16_t Data1;    
}REGISTER_PROCESSING;
*/
void SLAVExCOMMUNICATIONxHANDLER(){
    
    uint8_t length = 0;    
    
    if(Data.Direction == WRITE && Data.NoOfRegisters > 0){
        if(Data.NoOfRegisters > 0){
            WriteData[0] = 0;                   // start address High
            WriteData[1] = Data.StartRegister;  // start address Low
            WriteData[2] = 0;                   // number of registers High
            WriteData[3] = 1;                   // number of registers Low, 1 register to write
            WriteData[4] = 2;                   // byte count, 2 bytes
            WriteData[5] = (Data.RegData0 >> 8);// Register Value Hi
            WriteData[6] = Data.RegData0;       // Register Value Lo
            WriteData[7] = 0;
            WriteData[8] = 0;
            length = 7;
        }
        if(Data.NoOfRegisters > 1){
            WriteData[3]++;                     // number of registers Low, 1 register extra to write
            WriteData[4]+= 2;                   // byte count, 4 bytes
            WriteData[7] = (Data.RegData1 >> 8);// Register Value Hi
            WriteData[8] = Data.RegData0;       // Register Value Lo
            length += 2;
        }          
        SENDxPETITxMODBUS(Data.SlaveAddress, PETITMODBUS_WRITE_MULTIPLE_REGISTERS, WriteData, length);
    }
    else if(Data.Direction == READ && Data.NoOfRegisters > 0){
        if(Data.NoOfRegisters > 0){
            ReadData[0] = 0;                                // start address High
            ReadData[1] = Data.StartRegister;               // start address Low
            ReadData[2] = 0;                                // number of registers High
            ReadData[3] = 1;                                // number of registers Low, 1 register to read            
            length = 4;            
        }
        if(Data.NoOfRegisters > 1){
            ReadData[3]++;                                  // number of registers Low, 1 register extra to read            
        }
        SENDxPETITxMODBUS(Data.SlaveAddress, PETITMODBUS_READ_HOLDING_REGISTERS, ReadData, length);
    }    
}

/*#--------------------------------------------------------------------------#*/
/*  Description: uint8_t CHECKxMODBUSxCOMMxSTATUS(uint8_t SlaveId)
 *
 *  Input(s)   : 
 *
 *  Output(s)  : 
 *
 *  Returns    : Slave comm status from modbus
 *
 *  Pre.Cond.  :
 *
 *  Post.Cond. :
 *
 *  Notes      : 
 */
/*#--------------------------------------------------------------------------#*/
uint8_t CHECKxMODBUSxCOMMxSTATUS(uint8_t SlaveId, bool OverWrite){
    uint8_t return_val = SLAVEBUSY;
    
    if (SlaveId > NUMBER_OF_SLAVES){
        if( DUMP_SLAVE_DATA[0].MbCommError != SLAVE_DATA_BUSY &&
            DUMP_SLAVE_DATA[0].MbCommError == SLAVE_DATA_OK){
            return_val = SLAVEOK;
        }
        else if( DUMP_SLAVE_DATA[0].MbCommError != SLAVE_DATA_BUSY &&
                 DUMP_SLAVE_DATA[0].MbCommError != SLAVE_DATA_OK){
            return_val = SLAVENOK;
        }
    }
    else{
        if( MASTER_SLAVE_DATA[SlaveId].MbCommError != SLAVE_DATA_BUSY &&
            MASTER_SLAVE_DATA[SlaveId].MbCommError == SLAVE_DATA_OK){
            return_val = SLAVEOK;
        }
        else if( MASTER_SLAVE_DATA[SlaveId].MbCommError != SLAVE_DATA_BUSY &&
                 MASTER_SLAVE_DATA[SlaveId].MbCommError != SLAVE_DATA_OK){
            return_val = SLAVENOK;
        }
    }
    
    if (return_val != SLAVEBUSY && OverWrite == true){
        if(SlaveId > NUMBER_OF_SLAVES){
            DUMP_SLAVE_DATA[0].MbCommError = SLAVE_DATA_IDLE;
        }
        else{
            MASTER_SLAVE_DATA[SlaveId].MbCommError = SLAVE_DATA_IDLE;
        }
    }
    return (return_val);
}