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
#include "mbus.h"
#include "controller.h"
#include "ethernet.h"
#include "slavestartup.h"
#include "modbus/PetitModbus.h"

#define MAILBOXRCD 100

enum SLAVE{
    
    MESSAGEBOXSIZE  = 3,
    MESSAGE1        = 1,
    MESSAGE2        = 2,
    MESSAGE3        = 3,
    
    SIPBOXSIZE      = 10,                                                       // HR4..HR22, two per read (last single)
    SIP1            = 1,
    SIP2            = 2,
    SIP3            = 3,
    SIP4            = 4,
    SIP5 = 5, SIP6 = 6, SIP7 = 7, SIP8 = 8, SIP9 = 9, SIP10 = 10,
};

static REGISTER_PROCESSING          Rcd[MAILBOXRCD];                            // message mailbox
static REGISTER_PROCESSING          *Rcd_Ptr;                                   // points to actual mailbox location that is filled
static REGISTER_PROCESSING          *Rcd_Ptr_prev;                              // points to last read out mailbox location

//static char msg[256];                                                                  // debugging messages

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

typedef enum { STOP_UNUSED, STOP_QUEUED, STOP_WRITE_WAIT, STOP_STATE_READ,
    STOP_STATE_WAIT, STOP_PWM_READ, STOP_PWM_WAIT, STOP_CONFIRMED,
    STOP_FAILED } STOP_DELIVERY;
static STOP_DELIVERY StopDelivery[NUMBER_OF_AMPLIFIERS + 1];
static uint8_t StopAttempts[NUMBER_OF_AMPLIFIERS + 1];
static uint8_t StopSlave;
static uint8_t StopReportReason;
static uint16_t StopWord, StopState;
static uint32_t StopStarted, LastLeaseRenewal;

void REPORTxGLOBALxSTOPxDELIVERY(uint8_t reason)
{
    udpTrans_t frame = {0};
    uint8_t slave, count = 0;
    if (!ISxGLOBALxSTOPxLATCHED()) return;
    StopReportReason = reason;
    frame.header = HEADER;
    frame.command = CONTROLLER;
    frame.data[0] = GLOBAL_STOP_STATE;
    frame.data[1] = DONE;
    frame.data[2] = reason;
    /* A2: ascending detected IDs captured at latch time, four entries/byte,
     * least-significant pair first. Count excludes backplanes and master. */
    for (slave = 1; slave <= NUMBER_OF_AMPLIFIERS; ++slave) {
        if (StopDelivery[slave] != STOP_UNUSED) {
            uint8_t result = StopDelivery[slave] == STOP_CONFIRMED ? GLOBAL_STOP_CONFIRMED :
                StopDelivery[slave] == STOP_FAILED ? GLOBAL_STOP_FAILED : GLOBAL_STOP_PENDING;
            frame.data[4u + count / 4u] |= (uint8_t)(result << (2u * (count % 4u)));
            ++count;
        }
    }
    frame.data[3] = count;
    PUTxDATAxINxSENDxMAILxBOX(&frame);
}

/* Sole owner of stop completion. Ordinary polling must not consume MbCommError
 * while active. Every confirmation uses two new FC03 transactions. */
static bool SERVICExGLOBALxSTOP(void)
{
    uint8_t status;
    if (!PetitModbusIsReady()) return false;
    if (StopSlave != 0) {
        status = CHECKxMODBUSxCOMMxSTATUS(StopSlave, true);
        if (status == SLAVEBUSY) return false;
        if (status != SLAVEOK || (uint32_t)(READxCORExTIMER() - StopStarted) >=
                GLOBAL_STOP_CONFIRM_MS * (uint32_t)MILISECONDS) {
            StopDelivery[StopSlave] = StopAttempts[StopSlave] < GLOBAL_STOP_MAX_ATTEMPTS ?
                STOP_QUEUED : STOP_FAILED;
            if (StopDelivery[StopSlave] == STOP_FAILED) REPORTxGLOBALxSTOPxDELIVERY(StopReportReason);
            StopSlave = 0;
        } else if (StopDelivery[StopSlave] == STOP_WRITE_WAIT) {
            StopDelivery[StopSlave] = STOP_STATE_READ;
        } else if (StopDelivery[StopSlave] == STOP_STATE_WAIT) {
            StopState = MASTER_SLAVE_DATA[StopSlave].HoldingReg[HR_STATE];
            StopDelivery[StopSlave] = STOP_PWM_READ;
        } else if (StopDelivery[StopSlave] == STOP_PWM_WAIT) {
            if ((MASTER_SLAVE_DATA[StopSlave].HoldingReg[HR_IDENTITY] & 0x1000u) != 0 &&
                (StopState & (HR_STATE_INHIBITED | HR_STATE_STOPPING_ACTIVE)) == HR_STATE_INHIBITED &&
                (MASTER_SLAVE_DATA[StopSlave].HoldingReg[HR_APPLIED_PWM] & HR_APPLIED_PWM_MASK) == 399u) {
                StopDelivery[StopSlave] = STOP_CONFIRMED;
                REPORTxGLOBALxSTOPxDELIVERY(StopReportReason);
                StopSlave = 0;
            } else StopDelivery[StopSlave] = STOP_STATE_READ;
        }
    }
    if (StopSlave == 0) {
        uint8_t slave;
        for (slave = 1; slave <= NUMBER_OF_AMPLIFIERS; ++slave)
            if (StopDelivery[slave] == STOP_QUEUED) { StopSlave = slave; break; }
    }
    if (StopSlave == 0) return false;
    Data.SlaveAddress = StopSlave;
    Data.NoOfRegisters = 1;
    if (StopDelivery[StopSlave] == STOP_QUEUED) {
        Data.Direction = WRITE;
        Data.StartRegister = HOLDINGREG0;
        Data.RegData0 = StopWord;
        ++StopAttempts[StopSlave];
        StopStarted = READxCORExTIMER();
        StopDelivery[StopSlave] = STOP_WRITE_WAIT;
    } else {
        Data.Direction = READ;
        Data.StartRegister = StopDelivery[StopSlave] == STOP_STATE_READ ? HR_STATE : HR_APPLIED_PWM;
        StopDelivery[StopSlave] = StopDelivery[StopSlave] == STOP_STATE_READ ? STOP_STATE_WAIT : STOP_PWM_WAIT;
    }
    SLAVExCOMMUNICATIONxHANDLER();
    return true;
}

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
static uint8_t      ProcessSlaveLast    = 0;
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
                /* when the current slave to be processed is outside the no of
                 * amplifiers, start over with the Master again and increase
                 * the Message and SlaveInfoProcessor. */
                if (ProcessSlave > (NUMBER_OF_AMPLIFIERS)){
                    ProcessSlave = 0;
                    Message++;
                    if (Message > MESSAGEBOXSIZE){
                        Message = MESSAGE1;

                        SlaveInfoProcessor++;
                        if (SlaveInfoProcessor > SIPBOXSIZE){
                            SlaveInfoProcessor = SIP1;
                        }
                    }
                }
            }
            iProcessNextSlave++;            
            break;
            
        case 1:            
            if(SendNextMessage()){
                ProcessSlaveLast = ProcessSlave; // keep check on last communicated slave.
                ProcessSlave++; // go already to next slave
                /* when the current slave to be processed is outside the no of
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

bool SendNextMessage(void)
{
    bool return_val = false;

    if (!PetitModbusIsReady()) return false;
    if (ISxGLOBALxSTOPxLATCHED()) {
        if (SERVICExGLOBALxSTOP()) return true;
        if (StopSlave != 0) return false;
    }
    /* Direct periodic broadcast, independent of mailbox purge/fullness. */
    if (!ISxGLOBALxSTOPxLATCHED() && (uint32_t)(READxCORExTIMER() - LastLeaseRenewal) >=
            MOTION_LEASE_RENEWAL_MS * (uint32_t)MILISECONDS) {
        Data.SlaveAddress = BROADCAST_ADDRESS;
        Data.Direction = WRITE;
        Data.NoOfRegisters = 1;
        Data.StartRegister = HOLDINGREG1;
        Data.RegData0 = MOTION_PERMISSION_HR1;
        SLAVExCOMMUNICATIONxHANDLER();
        LastLeaseRenewal = READxCORExTIMER();
        return true;
    }

    /* --------------------------------------------------------------------
     * 1) High priority: berichten uit de mailbox (PC -> Master -> Slave)
     * -------------------------------------------------------------------- */
    if (Rcd_Ptr_prev != Rcd_Ptr)
    {
        /* Keep the same FIFO entry pending until the cooperative transport can
         * accept it; no task/ISR can change Petit_Tx_State between these calls. */
        if (!PetitModbusIsReady()) {
            return false;
        }
        if (Rcd_Ptr_prev >= &Rcd[MAILBOXRCD])
        {
            Rcd_Ptr_prev = &Rcd[0];
        }

        /* Kopieer mailbox entry naar de globale Data-struct */
        Data.SlaveAddress  = Rcd_Ptr_prev->SlaveAddress;
        Data.Direction     = Rcd_Ptr_prev->Direction;
        Data.NoOfRegisters = Rcd_Ptr_prev->NoOfRegisters;
        Data.StartRegister = Rcd_Ptr_prev->StartRegister;
        Data.RegData0      = Rcd_Ptr_prev->RegData0;
        Data.RegData1      = Rcd_Ptr_prev->RegData1;

        /* Voor CHECKxMODBUSxCOMMxSTATUS() */
        ProcessSlaveLast = Data.SlaveAddress;

        SLAVExCOMMUNICATIONxHANDLER();
                
//        LOG_Printf("Mbus handler\t: Dt to #%d, dir %d, NoReg %d, Strt %d, Dat0 %d, Dat1 %d .",
//                Rcd_Ptr_prev->SlaveAddress, Rcd_Ptr_prev->Direction, Rcd_Ptr_prev->NoOfRegisters, 
//                Rcd_Ptr_prev->StartRegister, Rcd_Ptr_prev->RegData0, Rcd_Ptr_prev->RegData1);

        /* Na versturen naar volgende mailbox entry */
        Rcd_Ptr_prev++;
        if (Rcd_Ptr_prev >= &Rcd[MAILBOXRCD])
        {
            Rcd_Ptr_prev = &Rcd[0];
        }

        return true;   // deze Modbus-cyclus is gevuld met 1 PC-bericht
    }

    /* --------------------------------------------------------------------
     * 2) Geen PC-commando's ? normale cyclic slave polling
     * -------------------------------------------------------------------- */
    if (ProcessSlave > 0)
    {
        /* Fetch data from the slaves according to priority */
        switch (Message)
        {
            case MESSAGE1:
                Data.SlaveAddress  = ProcessSlave;
                Data.Direction     = READ;
                Data.NoOfRegisters = 2;
                Data.StartRegister = HOLDINGREG0;
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
                switch (SlaveInfoProcessor)
                {
                    case SIP1:
                        Data.SlaveAddress  = ProcessSlave;
                        Data.Direction     = READ;
                        Data.NoOfRegisters = 2;
                        Data.StartRegister = HOLDINGREG4;
                        SLAVExCOMMUNICATIONxHANDLER();
                        break;

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

                    case SIP5: case SIP6: case SIP7: case SIP8: case SIP9: case SIP10:
                        Data.SlaveAddress = ProcessSlave;
                        Data.Direction = READ;
                        Data.StartRegister = HR_REQUESTED_PWM + 2u * (SlaveInfoProcessor - SIP5);
                        Data.NoOfRegisters = SlaveInfoProcessor == SIP10 ? 1 : 2;
                        SLAVExCOMMUNICATIONxHANDLER();
                        break;

                    default:
                        break;
                }
                break;

            default:
                break;
        }

        ProcessSlaveLast = ProcessSlave;
        return_val = true;
        
    }
    else
    {
        /* ProcessSlave == 0 en geen mailbox-data:
         * gebruik deze cycle om direct een volgende slave/Message klaar te
         * zetten via PROCESSxNEXTxSLAVE(). */
        iProcessNextSlave = 0;
        ProcessSlave++;
        /* return_val blijft false: MBUS laat UpdateNextSlave op true staan
         * en zal PROCESSxNEXTxSLAVE() opnieuw aanroepen. */
    }
    return return_val;
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
    if (ISxGLOBALxSTOPxLATCHED() && StopSlave != 0) return false;
    
    
    /* Verify communication was OK */
    switch(CHECKxMODBUSxCOMMxSTATUS(ProcessSlaveLast, true)){
        case SLAVEOK: 
            //sprintf(msg, "Mbus handler\t: CHECKxMODBUSxCOMMxSTATUS SLAVE OK (ID=%d).", ProcessSlave);
            //LOG_Push(msg);
            return_Val = true;
            break;                    
        case SLAVENOK:            
            //sprintf(msg, "Mbus handler\t: CHECKxMODBUSxCOMMxSTATUS SLAVE NOK (ID=%d).", ProcessSlave);
            //LOG_Push(msg);
            return_Val = true;
            break;
        case SLAVE_DATA_TIMEOUT:            
            //sprintf(msg, "Mbus handler\t: CHECKxMODBUSxCOMMxSTATUS SLAVE_DATA_TIMEOUT (ID=%d).", ProcessSlave);
            //LOG_Push(msg);
            return_Val = true;
            break;
        case SLAVE_DATA_EXCEPTION:            
            //sprintf(msg, "Mbus handler\t: CHECKxMODBUSxCOMMxSTATUS SLAVE_DATA_EXCEPTION (ID=%d).", ProcessSlave);
            //LOG_Push(msg);
            return_Val = true;
            break;
        case SLAVE_DATA_IDLE:            
            //sprintf(msg, "Mbus handler\t: CHECKxMODBUSxCOMMxSTATUS SLAVE_DATA_IDLE (ID=%d).", ProcessSlave);
            //LOG_Push(msg);
            return_Val = true;
            break;
        case SLAVE_DATA_UNKNOWN:            
            //sprintf(msg, "Mbus handler\t: CHECKxMODBUSxCOMMxSTATUS SLAVE_DATA_UNKNOWN (ID=%d).", ProcessSlave);
            //LOG_Push(msg);
            return_Val = true;
            break;
        
        case SLAVEBUSY: break;
        default : break;
    }

    if(return_Val != SLAVEBUSY){
        ProcessSlaveLast = 60;
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

void ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX(uint8_t *data)
{
    /* No host broadcast/initial-address writes: they bypass identity gating. */
    if (data[1] == WRITE && (data[0] == 0 || data[0] > NUMBER_OF_SLAVES ||
        (data[0] <= NUMBER_OF_AMPLIFIERS && !ISxAMPLIFIERxCOMPATIBLE(data[0])))) return;
    /* No amplifier writes while latched, including host-originated lease
     * renewal/config/enable. Only this worker owns stop delivery. */
    if (ISxGLOBALxSTOPxLATCHED() && data[1] == WRITE &&
        data[0] <= NUMBER_OF_AMPLIFIERS) {
        return;
    }
    // Calculate the next write position
    REGISTER_PROCESSING *next = Rcd_Ptr + 1;
    if (next >= &Rcd[MAILBOXRCD]) {
        next = &Rcd[0];
    }

    // IF next is going to hit the read pointer --> buffer full
    if (next == Rcd_Ptr_prev)
    {
        CREATExTASKxSTATUSxMESSAGE(
            MBUS,
            EXEC_MBUS_SLAVE_DATA_EXCH,
            ERROR,
            MESSAGE_BUFFER_FULL);
        LOG_Push("Mbus handler\t: EXEC_MBUS_SLAVE_DATA_EXCH ERROR MESSAGE_BUFFER_FULL.");
        return;
    }

    // There is room in the buffer: write current entry on Rcd_Ptr
    Rcd_Ptr->SlaveAddress   = data[0];
    Rcd_Ptr->Direction      = data[1];
    Rcd_Ptr->NoOfRegisters  = data[2];
    Rcd_Ptr->StartRegister  = data[3];
    Rcd_Ptr->RegData0       = ((uint16_t)data[5] << 8) | data[4];
    Rcd_Ptr->RegData1       = ((uint16_t)data[7] << 8) | data[6];
    
//    LOG_Printf("Mbus handler\t: Dt rc #%d, dir %d, NoReg %d, Strt %d, Dat4 %d, Dat5 %d, Dat6 %d, Dat7 %d .",
//                data[0], data[1], data[2], data[3], data[4], data[5],data[6], data[7]);

    // Commit: schrijf-pointer verschuiven naar next
    Rcd_Ptr = next;

    CREATExTASKxSTATUSxMESSAGE(
        MBUS,
        EXEC_MBUS_SLAVE_DATA_EXCH,
        DONE,
        NONE);
}

void ENQUEUExGLOBALxSTOP(uint16_t hr0)
{
    uint8_t slave;
    /* Drop pre-latch pending commands. In-flight requests cannot be recalled.
     * Delivery retry is not a new latch edge. */
    Rcd_Ptr = Rcd_Ptr_prev = &Rcd[0];
    StopSlave = 0;
    StopWord = hr0;
    for (slave = 1; slave <= NUMBER_OF_AMPLIFIERS; ++slave) {
        StopDelivery[slave] = STOP_UNUSED;
        StopAttempts[slave] = 0;
        if (MASTER_SLAVE_DATA[slave].SlaveDetected) {
            StopDelivery[slave] = ISxAMPLIFIERxCOMPATIBLE(slave) ? STOP_QUEUED : STOP_FAILED;
        }
    }
}

