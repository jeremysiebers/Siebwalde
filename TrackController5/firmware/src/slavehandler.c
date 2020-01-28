/*
 * File:   slavecommhandler.c
 * Author: Jeremy Siebers
 *
 * Created on aug 28, 2018, 14:15 PM
 */
#include <xc.h>
#include "../TrackController5.X/../../mbus.h"
#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdlib.h>
#include "slavecommhandler.h"

#define MAILBOXRCD 100

enum SLAVE{
    
    MESSAGEBOXSIZE  = 3,
    MESSAGE1        = 1,
    MESSAGE2        = 2,
    MESSAGE3        = 3,
    
    SIPBOXSIZE      = 4,                                                        // How many messages are non-critical
    SIP1            = 1,
    SIP2            = 2,
    SIP3            = 3,
    SIP4            = 4,    
};

static REGISTER_PROCESSING          Rcd[MAILBOXRCD];                            // message mailbox
static REGISTER_PROCESSING          *Rcd_Ptr;                                   // points to actual mailbox location that is filled
static REGISTER_PROCESSING          *Rcd_Ptr_prev;                              // points to last read out mailbox location

bool SendNextMessage(void);

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
    
    Rcd_Ptr         = &Rcd[0];
    Rcd_Ptr_prev    = &Rcd[0];
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
 *  Pre.Cond.  : called every Modbus update cycle
 *
 *  Post.Cond. :
 *
 *  Notes      : Handles communication message to all slaves
 */
/*#--------------------------------------------------------------------------#*/
static uint8_t      ProcessSlave        = 0;
static uint8_t      iProcessNextSlave   = 0;
static uint8_t      loopcount           = 0;
static uint8_t      SlaveInfoProcessor  = 1;
static uint8_t      Message             = MESSAGE1;

bool PROCESSxNEXTxSLAVE(){    
    
    bool return_val = false;
    
    switch (iProcessNextSlave){
        case 0:
            /* Scan trough all the slaves until a detected slave is found,
             * the Master is always found and signals that all slaves
             * were checked. */
            while (MASTER_SLAVE_DATA[ProcessSlave].SlaveDetected == false){
                ProcessSlave++;
                /* when the current slave to be precessed is outside the no of
                 * amplifiers, start over with the Master again and increase
                 * the Message and SlaveInfoProcessor. */
                if (ProcessSlave > (NUMBER_OF_AMPLIFIERS)){
                    ProcessSlave = 0;
                    Message++;
                    if (Message > MESSAGEBOXSIZE){
                        Message = MESSAGE1;

                        SlaveInfoProcessor++;
                        if (SlaveInfoProcessor > SIPBOXSIZE){
                            SlaveInfoProcessor = 1;
                        }
                    }
                }
            }
            iProcessNextSlave++;            
            break;
            
        case 1:            
            if(SendNextMessage()){
                ProcessSlave++;
                /* when the current slave to be precessed is outside the no of
                 * amplifiers, start over with the Master again and increase
                 * the Message and SlaveInfoProcessor. */
                if (ProcessSlave > (NUMBER_OF_AMPLIFIERS)){
                    ProcessSlave = 0;
                    Message++;
                    if (Message > MESSAGEBOXSIZE){
                        Message = MESSAGE1;

                        SlaveInfoProcessor++;
                        if (SlaveInfoProcessor > SIPBOXSIZE){
                            SlaveInfoProcessor = 1;
                        }
                    }
                }
                iProcessNextSlave = 0;
                return_val = true;
            }            
            break;
            
        default :
            iProcessNextSlave = 0;
            break;
    }     
    return(return_val);
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

uint8_t MasterMessageCounter = 0;

bool SendNextMessage(){
    bool return_val = false;
    
    /* Messaged to be send to slaves */
    if (ProcessSlave > 0){
        switch (Message){
            case MESSAGE1:
                Data.SlaveAddress  = ProcessSlave;
                Data.Direction     = READ;
                Data.NoOfRegisters = 2;
                Data.StartRegister = HOLDINGREG0;
//                Data.RegData0      = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[0];
//                Data.RegData1      = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[1];
                SLAVExCOMMUNICATIONxHANDLER();
                break;

            case MESSAGE2:
                Data.SlaveAddress  = ProcessSlave;
                Data.Direction     = READ;
                Data.NoOfRegisters = 2;
                Data.StartRegister = HOLDINGREG2;
                SLAVExCOMMUNICATIONxHANDLER();
                break;

            case MESSAGE3:
                switch (SlaveInfoProcessor){
                    case SIP1:
                        Data.SlaveAddress  = ProcessSlave;
                        Data.Direction     = READ;
                        Data.NoOfRegisters = 2;
                        Data.StartRegister = HOLDINGREG4;
                        SLAVExCOMMUNICATIONxHANDLER();

                    case SIP2:
                        Data.SlaveAddress  = ProcessSlave;
                        Data.Direction     = READ;
                        Data.NoOfRegisters = 2;
                        Data.StartRegister = HOLDINGREG6;
                        SLAVExCOMMUNICATIONxHANDLER();
                        break;

                    case SIP3:
                        Data.SlaveAddress  = ProcessSlave;
                        Data.Direction     = READ;
                        Data.NoOfRegisters = 2;
                        Data.StartRegister = HOLDINGREG8;
                        SLAVExCOMMUNICATIONxHANDLER(); 
                        break;

                    case SIP4:
                        Data.SlaveAddress  = ProcessSlave;
                        Data.Direction     = READ;
                        Data.NoOfRegisters = 2;
                        Data.StartRegister = HOLDINGREG10;
                        SLAVExCOMMUNICATIONxHANDLER(); 
                        break;

                    default:
                        break;                
                }
                break;

            default :
                break;
        }
        return_val = true;
    }
    /* Messages directly to the slaves from the PC.*/
    else{
        /* Next counter takes care that max number of messages sent by master does not
         * exceed 50 like the normal message sequence 
         */
        MasterMessageCounter++;
        if (MasterMessageCounter    > 50){
            MasterMessageCounter    = 0;
            return_val              = true;
        }
        else{
            if (Rcd_Ptr_prev != Rcd_Ptr){
                Rcd_Ptr_prev++;
                if(Rcd_Ptr_prev >= &Rcd[MAILBOXRCD]){
                    Rcd_Ptr_prev = &Rcd[0];
                }
                Data.SlaveAddress  = Rcd_Ptr_prev->SlaveAddress;
                Data.Direction     = Rcd_Ptr_prev->Direction;
                Data.NoOfRegisters = Rcd_Ptr_prev->NoOfRegisters;
                Data.StartRegister = Rcd_Ptr_prev->StartRegister;
                Data.RegData0      = Rcd_Ptr_prev->RegData0;
                Data.RegData1      = Rcd_Ptr_prev->RegData1;
                SLAVExCOMMUNICATIONxHANDLER();
            }
            else{
                /* Stop the master processing as soon as there are no more
                 * messages to be processed */
                MasterMessageCounter    = 0;
                //return_val              = true;
                iProcessNextSlave       = 0; // reuse this cycle to actually do something else
                ProcessSlave++;              // also tell to go to the next slave
            }
        }                  
    }
    return (return_val);        
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
/*  Description: ADDxNEWxSLAVExDATAxCMDxTOxMAILBOX(uint8_t *data)
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
 *  Notes      : Stores messages into mailbox that have to go directly to slaves
 */
/*#--------------------------------------------------------------------------#*/

static bool FirstMessage = true;

void ADDxNEWxSLAVExDATAxCMDxTOxMAILBOX(uint8_t *data){    
    
    if(FirstMessage == false && Rcd_Ptr == Rcd_Ptr_prev){
    
        CREATExTASKxSTATUSxMESSAGE(
            MBUS,                                   // TASK_ID
            EXEC_MBUS_SLAVE_DATA_EXCH,              // TASK_COMMAND
            ERROR,                                  // TASK_STATE
            MESSAGE_BUFFER_FULL);                   // TASK_MESSAGE
        SYS_MESSAGE("Mbus handler\t: EXEC_MBUS_SLAVE_DATA_EXCH ERROR MESSAGE_BUFFER_FULL.\n\r");
    }
    else{
        Rcd_Ptr->SlaveAddress   = data[0];
        Rcd_Ptr->Direction      = data[1];
        Rcd_Ptr->NoOfRegisters  = data[2];
        Rcd_Ptr->StartRegister  = data[3];
        Rcd_Ptr->RegData0       = data[4];
        Rcd_Ptr->RegData1       = data[5];

        Rcd_Ptr++;

        if(Rcd_Ptr >= &Rcd[MAILBOXRCD]){
            Rcd_Ptr = &Rcd[0];
        }
        
        CREATExTASKxSTATUSxMESSAGE(
            MBUS,                                   // TASK_ID
            EXEC_MBUS_SLAVE_DATA_EXCH,              // TASK_COMMAND
            DONE,                                   // TASK_STATE
            NONE);                                  // TASK_MESSAGE
    }
    
    /* Only 1 time required to be true */
    FirstMessage = false;
}