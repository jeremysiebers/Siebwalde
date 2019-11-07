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

#define MAILBOX_SIZE 4                                                          // How many messages are non-critical
#define BROADCAST_ADDRESS 0

void SLAVExCOMMANDxHANDLER(uint16_t State);
void BOOTxLOADxHANDLER(void);

/*#--------------------------------------------------------------------------#*/
/*  Description: INITXSLAVEXCOMMUNICATION(SLAVE_INFO *location)
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
static SLAVE_INFO   *MASTER_SLAVE_DATA;                                         // Holds the address were the received slave data is stored
static SLAVE_INFO   *DUMP_SLAVE_DATA;
static SLAVE_INFO   SlaveInfoReadMask;                                          // Read mask for slave data from EhternetTarget
unsigned char       *pSlaveDataReceived, *pSlaveInfoReadMask;

const uint8_t DATAxSTRUCTxLENGTH = sizeof(SLAVE_INFO);                         
static uint8_t RECEIVEDxDATAxRAW[127];                                          // One dummy byte extra (SPI master will send extra byte to receive last byte from slave)
//const  uint8_t DATAxSTRUCTxLENGTH2 = 43;                                        // add one byte to send dummy
//static uint8_t BOOT_DATA_TO_SLAVE[DATAxSTRUCTxLENGTH2 + 1];                     // One dummy byte extra (SPI master will send extra byte to receive last byte from slave)
//static uint8_t BOOT_DATA_TO_ETHERNET[DATAxSTRUCTxLENGTH2]; 

void INITXSLAVEXCOMMUNICATION(SLAVE_INFO *location, SLAVE_INFO *Dump)                                  
{ 
    DUMP_SLAVE_DATA    =  Dump; 
    MASTER_SLAVE_DATA  =  location;
    
    SlaveInfoReadMask.Header           = 0x00;
    SlaveInfoReadMask.SlaveNumber      = 0x00;                                  // Mask for data write to local MASTER_SLAVE_DATA from EthernetTarget
    SlaveInfoReadMask.HoldingReg[0]    = 0xFFFF;                                // only new setpoints/settings are allowed to be read which need to be written to modbus slaves
    SlaveInfoReadMask.HoldingReg[1]    = 0xFFFF;
    SlaveInfoReadMask.HoldingReg[2]    = 0xFFFF;
    SlaveInfoReadMask.HoldingReg[3]    = 0xFFFF;
    SlaveInfoReadMask.HoldingReg[4]    = 0x0000;
    SlaveInfoReadMask.HoldingReg[5]    = 0x0000;
    SlaveInfoReadMask.HoldingReg[6]    = 0x0000;
    SlaveInfoReadMask.HoldingReg[7]    = 0x0000;
    SlaveInfoReadMask.HoldingReg[8]    = 0x0000;
    SlaveInfoReadMask.HoldingReg[9]    = 0x0000;
    SlaveInfoReadMask.HoldingReg[10]   = 0x0000;
    SlaveInfoReadMask.HoldingReg[11]   = 0x0000;
    SlaveInfoReadMask.MbReceiveCounter = 0x0000;
    SlaveInfoReadMask.MbSentCounter    = 0x0000;
    SlaveInfoReadMask.MbCommError      = 0x0000;
    SlaveInfoReadMask.MbExceptionCode  = 0x00;
    SlaveInfoReadMask.SpiCommErrorCounter = 0x0000;
    SlaveInfoReadMask.Footer           = 0x00;
    
}

/*#--------------------------------------------------------------------------#*/
/*  Description: PROCESSxNEXTxSLAVE()
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
 *  Notes      : Handles communication message to all slaves
 */
/*#--------------------------------------------------------------------------#*/

#define MESSAGE1 1
#define MESSAGE2 2
#define MESSAGE3 3
#define MESSAGE4 4

unsigned char HoldingRegistersRead [4] = {0, 0, 0, 0};                      // {start address High, start address Low, number of registers High, number of registers Low}
unsigned char HoldingRegistersWrite[9] = {0, 0, 0, 0, 0, 0, 0, 0, 0};       // {start address High, start address Low, number of registers High, number of registers Low, 
                                                                            //  byte count, Register Value Hi, Register Value Lo, Register Value Hi, Register Value Lo} 
static unsigned char ProcessSlave = 1;
static unsigned int Mailbox = 1;
static uint32_t Message = MESSAGE1;

void PROCESSxNEXTxSLAVE(){    
    
    if (ProcessSlave > (NUMBER_OF_SLAVES-1)){
        ProcessSlave = 1;
        
        Message++;
        if (Message > MESSAGE3){
            Message = MESSAGE1;
            
            Mailbox++;
            if (Mailbox > 4){
                Mailbox = 1;
            }
        }                
    }
    switch (Message){
        case MESSAGE1:
            HoldingRegistersWrite[8]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[1];
            HoldingRegistersWrite[7]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[1] >> 8;
            HoldingRegistersWrite[6]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[0];
            HoldingRegistersWrite[5]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[0] >> 8;
            HoldingRegistersWrite[4]  = 4;
            HoldingRegistersWrite[3]  = 2;
            HoldingRegistersWrite[2]  = 0;
            HoldingRegistersWrite[1]  = 0;
            HoldingRegistersWrite[0]  = 0;
            SENDxPETITxMODBUS(ProcessSlave, PETITMODBUS_WRITE_MULTIPLE_REGISTERS, HoldingRegistersWrite, 9);
            break;

        case MESSAGE2:
            HoldingRegistersRead[3]  = 2;
            HoldingRegistersRead[2]  = 0;
            HoldingRegistersRead[1]  = 0;
            HoldingRegistersRead[0]  = 0;
            SENDxPETITxMODBUS(ProcessSlave, PETITMODBUS_READ_HOLDING_REGISTERS, HoldingRegistersRead, 4);    
            break;

        case MESSAGE3:
            switch (Mailbox){
                case 1:
                    HoldingRegistersWrite[8]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[3];
                    HoldingRegistersWrite[7]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[3] >> 8;
                    HoldingRegistersWrite[6]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[2];
                    HoldingRegistersWrite[5]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[2] >> 8;
                    HoldingRegistersWrite[4]  = 4;
                    HoldingRegistersWrite[3]  = 2;
                    HoldingRegistersWrite[2]  = 0;
                    HoldingRegistersWrite[1]  = 2;
                    HoldingRegistersWrite[0]  = 0;
                    SENDxPETITxMODBUS(ProcessSlave, PETITMODBUS_WRITE_MULTIPLE_REGISTERS, HoldingRegistersWrite, 9);
                    break;

                case 2:
                    HoldingRegistersRead[3]  = 2;
                    HoldingRegistersRead[2]  = 0;
                    HoldingRegistersRead[1]  = 0;
                    HoldingRegistersRead[0]  = 0;
                    SENDxPETITxMODBUS(ProcessSlave, PETITMODBUS_READ_HOLDING_REGISTERS, HoldingRegistersRead, 4);
                    break;

                case 3:
                    HoldingRegistersRead[3]  = 2;
                    HoldingRegistersRead[2]  = 0;
                    HoldingRegistersRead[1]  = 2;
                    HoldingRegistersRead[0]  = 0;
                    SENDxPETITxMODBUS(ProcessSlave, PETITMODBUS_READ_HOLDING_REGISTERS, HoldingRegistersRead, 4);  
                    break;

                case 4:
                    HoldingRegistersRead[3]  = 2;
                    HoldingRegistersRead[2]  = 0;
                    HoldingRegistersRead[1]  = 4;
                    HoldingRegistersRead[0]  = 0;
                    SENDxPETITxMODBUS(ProcessSlave, PETITMODBUS_READ_HOLDING_REGISTERS, HoldingRegistersRead, 4);  
                    break;

                default:
                    break;                
            }
            break;

        default :
            break;
    } 

}

/*#--------------------------------------------------------------------------#*/
/*  Description: PROCESSxSLAVExCOMMUNICATION()
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
 *  Notes      : Keeps track of communication with a slave
 */
/*#--------------------------------------------------------------------------#*/
bool PROCESSxSLAVExCOMMUNICATION(uint8_t SlaveAddress){
    
    bool Return_Val = false;
    
    switch (MASTER_SLAVE_DATA[SlaveAddress].MbCommError){
        case SLAVE_DATA_BUSY:
            Return_Val = false;
            // count here how long the Mod-bus stack is busy, otherwise reset/action             
            break;
            
        case SLAVE_DATA_NOK:
            // count here how often the slave data is NOK, otherwise stop all slaves with broadcast
            MASTER_SLAVE_DATA[SlaveAddress].MbCommError = SLAVE_DATA_IDLE;            
            Return_Val = true;
            LED_ERR++;
            break;
            
        case SLAVE_DATA_OK:
            MASTER_SLAVE_DATA[SlaveAddress].MbCommError = SLAVE_DATA_IDLE;   
            Return_Val = true;
            break;            
            
        case SLAVE_DATA_TIMEOUT:
            // count here how often the slave data is timeout, otherwise stop all slaves with broadcast
            MASTER_SLAVE_DATA[SlaveAddress].MbCommError = SLAVE_DATA_IDLE;      
            Return_Val = true;
            LED_ERR++;
            break;
            
        case SLAVE_DATA_EXCEPTION:
            // count here how often the slave data is timeout, otherwise stop all slaves with broadcast
            MASTER_SLAVE_DATA[SlaveAddress].MbCommError = SLAVE_DATA_IDLE;     
            Return_Val = true;
            LED_ERR++;
            break;
            
        case SLAVE_DATA_IDLE:
            Return_Val = true;
            break;
            
        default : Return_Val = true;                                            
            break;
    }    
    return (Return_Val);
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
        
    }
    
    if(WriteRead == Read){
        SENDxPETITxMODBUS(SlaveAddress, PETITMODBUS_READ_HOLDING_REGISTERS, Data, length);
    }
}