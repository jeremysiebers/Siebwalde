/*
 * File:   slavecommhandler.c
 * Author: Jeremy Siebers
 *
 * Created on aug 28, 2018, 14:15 PM
 */
#include <xc.h>
#include "app.h"
#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdlib.h>
#include "slavecommhandler.h"

enum SLAVE{
    MAILBOX_SIZE = 4,                                                           // How many messages are non-critical    
    MESSAGE1 = 1,
    MESSAGE2 = 2,
    MESSAGE3 = 3,
    MESSAGE4 = 4,
};
/*
#define MAILBOX_SIZE 4                                                          // How many messages are non-critical
#define BROADCAST_ADDRESS 0
#define MESSAGE1 1
#define MESSAGE2 2
#define MESSAGE3 3
#define MESSAGE4 4
*/

void SendNextMessage(void);

/*#--------------------------------------------------------------------------#*/
/*  Description: INITxSLAVExHANDLER(SLAVE_INFO *location)
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

void INITxSLAVExHANDLER(SLAVE_INFO *location, SLAVE_INFO *Dump){
    MASTER_SLAVE_DATA  =  location;
    DUMP_SLAVE_DATA    =  Dump;
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
static uint8_t      ProcessSlave = 1;
static uint8_t      State = 0;
static uint8_t      loopcount = 0;

bool PROCESSxNEXTxSLAVE(){    
    
    bool return_Val = false;
    
    switch (State){
        case 0:
            while (MASTER_SLAVE_DATA[ProcessSlave].SlaveDetected == false){
                ProcessSlave++; 
                if (ProcessSlave > (NUMBER_OF_AMPLIFIERS)){
                    ProcessSlave = 1;
                    loopcount++;
                }
                if(loopcount > 2){
                    loopcount = 0;
                    ProcessSlave = 1;
                    break;
                }
            }
            State++;            
            break;
            
        case 1:            
            SendNextMessage();
            ProcessSlave++;
            State = 0;
            return_Val = true;            
            break;
            
        default :
            break;
    }     
    return(return_Val);
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
bool PROCESSxSLAVExCOMMUNICATION(){
    
    bool return_Val = false;
    
    /* Verify communication was OK */
    switch(CHECKxMODBUSxCOMMxSTATUS(ProcessSlave, true)){
        case SLAVEOK:  
            //MASTER_SLAVE_DATA[ProcessSlave].SlaveDetected = true;
            //ProcessSlave++;
            return_Val = true;
            //DRV_USART0_WriteByte('6');
            break;                    
        case SLAVENOK: 
            //MASTER_SLAVE_DATA[ProcessSlave].SlaveDetected = false;
            //ProcessSlave++;
            return_Val = true;
            //DRV_USART0_WriteByte('6');
            break;
        case SLAVEBUSY: break;
        default : break;
    }    
    return (return_Val);
}

/*#--------------------------------------------------------------------------#*/
/*  Description: SendNextMessage()
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
//uint8_t HoldingRegistersRead [4] = {0, 0, 0, 0};                                // {start address High, start address Low, number of registers High, number of registers Low}
//uint8_t HoldingRegistersWrite[9] = {0, 0, 0, 0, 0, 0, 0, 0, 0};                 // {start address High, start address Low, number of registers High, number of registers Low, 
                                                                                //  byte count, Register Value Hi, Register Value Lo, Register Value Hi, Register Value Lo} 

static uint8_t Mailbox = 1;
static uint8_t Message = MESSAGE1;

void SendNextMessage(){
    
    if (ProcessSlave > (NUMBER_OF_AMPLIFIERS)){
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
            Data.SlaveAddress  = ProcessSlave;
            Data.Direction     = WRITE;
            Data.NoOfRegisters = 2;
            Data.StartRegister = HOLDINGREG0;
            Data.RegData0      = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[0];
            Data.RegData1      = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[1];
            SLAVExCOMMUNICATIONxHANDLER();            
//            HoldingRegistersWrite[8]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[1];
//            HoldingRegistersWrite[7]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[1] >> 8;
//            HoldingRegistersWrite[6]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[0];
//            HoldingRegistersWrite[5]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[0] >> 8;
//            HoldingRegistersWrite[4]  = 4;
//            HoldingRegistersWrite[3]  = 2;
//            HoldingRegistersWrite[2]  = 0;
//            HoldingRegistersWrite[1]  = 0;
//            HoldingRegistersWrite[0]  = 0;
//            SLAVExCOMMUNICATIONxHANDLER(ProcessSlave, HoldingReg0, Write, HoldingRegistersWrite, 9);
            //SENDxPETITxMODBUS(ProcessSlave, PETITMODBUS_WRITE_MULTIPLE_REGISTERS, HoldingRegistersWrite, 9);
            break;

        case MESSAGE2:
            Data.SlaveAddress  = ProcessSlave;
            Data.Direction     = READ;
            Data.NoOfRegisters = 2;
            Data.StartRegister = HOLDINGREG2;
            SLAVExCOMMUNICATIONxHANDLER();
//            HoldingRegistersRead[3]  = 2;
//            HoldingRegistersRead[2]  = 0;
//            HoldingRegistersRead[1]  = 0;
//            HoldingRegistersRead[0]  = 0;
//            SLAVExCOMMUNICATIONxHANDLER(ProcessSlave, HoldingReg0, Read, HoldingRegistersRead, 4);
            //SENDxPETITxMODBUS(ProcessSlave, PETITMODBUS_READ_HOLDING_REGISTERS, HoldingRegistersRead, 4);    
            break;

        case MESSAGE3:
            switch (Mailbox){
                case 1:
                    Data.SlaveAddress  = ProcessSlave;
                    Data.Direction     = WRITE;
                    Data.NoOfRegisters = 2;
                    Data.StartRegister = HOLDINGREG9;
                    Data.RegData0      = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[9];
                    Data.RegData1      = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[10];
                    SLAVExCOMMUNICATIONxHANDLER();
//                    HoldingRegistersWrite[8]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[3];
//                    HoldingRegistersWrite[7]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[3] >> 8;
//                    HoldingRegistersWrite[6]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[2];
//                    HoldingRegistersWrite[5]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[2] >> 8;
//                    HoldingRegistersWrite[4]  = 4;
//                    HoldingRegistersWrite[3]  = 2;
//                    HoldingRegistersWrite[2]  = 0;
//                    HoldingRegistersWrite[1]  = 2;
//                    HoldingRegistersWrite[0]  = 0;
//                    SLAVExCOMMUNICATIONxHANDLER(ProcessSlave, HoldingReg0, Write, HoldingRegistersWrite, 9);
                    //SENDxPETITxMODBUS(ProcessSlave, PETITMODBUS_WRITE_MULTIPLE_REGISTERS, HoldingRegistersWrite, 9);
                    break;

                case 2:
                    Data.SlaveAddress  = ProcessSlave;
                    Data.Direction     = READ;
                    Data.NoOfRegisters = 2;
                    Data.StartRegister = HOLDINGREG4;
                    SLAVExCOMMUNICATIONxHANDLER();
//                    HoldingRegistersRead[3]  = 2;
//                    HoldingRegistersRead[2]  = 0;
//                    HoldingRegistersRead[1]  = 0;
//                    HoldingRegistersRead[0]  = 0;
//                    SLAVExCOMMUNICATIONxHANDLER(ProcessSlave, HoldingReg0, Read, HoldingRegistersRead, 4);
                    //SENDxPETITxMODBUS(ProcessSlave, PETITMODBUS_READ_HOLDING_REGISTERS, HoldingRegistersRead, 4);
                    break;

                case 3:
                    Data.SlaveAddress  = ProcessSlave;
                    Data.Direction     = READ;
                    Data.NoOfRegisters = 2;
                    Data.StartRegister = HOLDINGREG6;
                    SLAVExCOMMUNICATIONxHANDLER();
//                    HoldingRegistersRead[3]  = 2;
//                    HoldingRegistersRead[2]  = 0;
//                    HoldingRegistersRead[1]  = 2;
//                    HoldingRegistersRead[0]  = 0;
//                    SLAVExCOMMUNICATIONxHANDLER(ProcessSlave, HoldingReg0, Read, HoldingRegistersRead, 4);
                    //SENDxPETITxMODBUS(ProcessSlave, PETITMODBUS_READ_HOLDING_REGISTERS, HoldingRegistersRead, 4);  
                    break;

                case 4:
                    Data.SlaveAddress  = ProcessSlave;
                    Data.Direction     = READ;
                    Data.NoOfRegisters = 2;
                    Data.StartRegister = HOLDINGREG8;
                    SLAVExCOMMUNICATIONxHANDLER();
//                    HoldingRegistersRead[3]  = 2;
//                    HoldingRegistersRead[2]  = 0;
//                    HoldingRegistersRead[1]  = 4;
//                    HoldingRegistersRead[0]  = 0;
//                    SLAVExCOMMUNICATIONxHANDLER(ProcessSlave, HoldingReg0, Read, HoldingRegistersRead, 4);
//                    SENDxPETITxMODBUS(ProcessSlave, PETITMODBUS_READ_HOLDING_REGISTERS, HoldingRegistersRead, 4);  
                    break;

                default:
                    break;                
            }
            break;

        default :
            break;
    }
}