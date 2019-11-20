/*
 * File:   commhandler.c
 * Author: Jeremy Siebers
 *
 * Created on aug 28, 2018, 14:15 PM
 */
#include <xc.h>
#include "app.h"
#include "modbus/PetitModbus.h"
#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdlib.h>
#include "commhandler.h"

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

void INITxCOMMxHANDLER(SLAVE_INFO *location, SLAVE_INFO *Dump){
    MASTER_SLAVE_DATA  =  location;
    DUMP_SLAVE_DATA    =  Dump;
}

/*#--------------------------------------------------------------------------#*/
/*  Description: SLAVExCOMMUNICATIONxHANDLER()
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
void SLAVExCOMMUNICATIONxHANDLER(uint8_t SlaveAddress, uint8_t Register, uint8_t WriteRead, uint8_t *Data, uint8_t length){
    if(WriteRead == Write){
        if(length > 4){ // write multiple reg
            SENDxPETITxMODBUS(SlaveAddress, PETITMODBUS_WRITE_MULTIPLE_REGISTERS, Data, length);
        }
        if(length == 4){ // write single reg
            SENDxPETITxMODBUS(SlaveAddress, PETITMODBUS_WRITE_SINGLE_REGISTER, Data, length);
        }
    }
    
    if(WriteRead == Read){
        SENDxPETITxMODBUS(SlaveAddress, PETITMODBUS_READ_HOLDING_REGISTERS, Data, length);
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