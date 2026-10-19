#include <xc.h>
#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdlib.h>
#include "../TrackController5.X/../../mbus.h"
#include "../TrackController5.X/../../slavecommhandler.h"
#include "../TrackController5.X/../../enums.h"
#include "modbus/PetitModbus.h"
#include "controller.h"

static bool AmplifierCompatible[NUMBER_OF_AMPLIFIERS + 1];
static SLAVE_INFO *MASTER_SLAVE_DATA;
static SLAVE_INFO *DUMP_SLAVE_DATA;

bool ISxAMPLIFIERxCOMPATIBLE(uint8_t slave)
{
    return slave > 0 && slave <= NUMBER_OF_AMPLIFIERS && AmplifierCompatible[slave];
}

/* Called exclusively during selected-slot initialization, before ANY amplifier
 * write. Each observation follows its own completed FC03, never SLAVEINFO. */
static bool VERIFYxAMPLIFIERxIDENTITY(uint8_t slave)
{
    static uint8_t phase;
    static uint16_t first;
    uint8_t status;
    uint16_t identity;
    if (phase == 0 || phase == 2) {
        if (!PetitModbusIsReady()) return false;
        Data.SlaveAddress = SLAVE_INITIAL_ADDR;
        Data.Direction = READ;
        Data.NoOfRegisters = 1;
        Data.StartRegister = HR_IDENTITY;
        SLAVExCOMMUNICATIONxHANDLER();
        ++phase;
        return false;
    }
    status = CHECKxMODBUSxCOMMxSTATUS(SLAVE_INITIAL_ADDR, true);
    if (status == SLAVEBUSY) return false;
    identity = DUMP_SLAVE_DATA[0].HoldingReg[HR_IDENTITY];
    if (status == SLAVEOK && phase == 1) {
        first = identity;
        phase = 2;
        return false;
    }
    AmplifierCompatible[slave] = status == SLAVEOK && phase == 3 &&
        identity == first && (identity & 0xffu) == 1u &&
        (identity & 0x0f00u) == 0x0100u && (identity & 0xf000u) != 0;
    MASTER_SLAVE_DATA[slave].HoldingReg[HR_IDENTITY] =
        AmplifierCompatible[slave] ? identity : 0;
    /* Init status: BUSY + 0x80|id = two-read compatible; ERROR + id = blocked.
     * Overall init completion remains the existing DONE/NONE notification. */
    CREATExTASKxSTATUSxMESSAGE(MBUS, EXEC_MBUS_STATE_SLAVE_INIT,
        AmplifierCompatible[slave] ? BUSY : ERROR,
        AmplifierCompatible[slave] ? (0x80u | slave) : slave);
    phase = 0;
    return true;
}

bool    ConfigureSlave       (uint8_t TrackBackPlaneID, uint16_t AmplifierLatchSet, uint8_t TrackAmplifierId, uint8_t Mode);
bool    DetectSlave          (uint8_t SlaveId);

//uint8_t WriteData2Registers[7] = {0, 0, 0, 1, 2, 0, 0};                         // {start address High, start address Low, 
//                                                                                // number of registers High, number of registers Low, 
//                                                                                // byte count, Register Value Hi, Register Value Lo} 
//
//uint8_t WriteData1Register[4] = {0, 0, 0, 0};                                   // {start address High, start address Low, 
//                                                                                // Register Value Hi, Register Value Lo} 
//
//uint8_t ReadData[4] = {0, 0, 0, 2};                                             // {start address High, start address Low, 
//                                                                                // number of registers High, number of registers Low, 

static uint8_t      GoToCase        = 0;
static uint16_t     WaitCounter     = 0;
static uint32_t     DelayCount1     = 0;
static uint32_t     DelayCount2     = 0;
static uint32_t     threshold       = 0;
static uint8_t      AddressWriteRetries = 0;

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

void INITxSLAVExSTARTUP(SLAVE_INFO *location, SLAVE_INFO *Dump){
    MASTER_SLAVE_DATA  =  location;
    DUMP_SLAVE_DATA    =  Dump;
}

/*#--------------------------------------------------------------------------#*/
/*  Description: bool SLAVExINITxANDxCONFIG()
 *
 *  Input(s)   : 
 *
 *  Output(s)  : 
 *
 *  Returns    : Init done
 *
 *  Pre.Cond.  :
 *
 *  Post.Cond. :
 *
 *  Notes      : Sets all the addresses to the slaves according to location
 */
/*#--------------------------------------------------------------------------#*/
uint8_t SlaveId = 51; // Start with the backplanes first
uint8_t iSlaveDetect = 0;

bool SLAVExDETECT(){
    bool return_val = false;
    
    switch (iSlaveDetect){
        case 0:
            if (DetectSlave(SlaveId) == true){
                SlaveId++;
            }
            if (SlaveId > NUMBER_OF_SLAVES){
                iSlaveDetect = 1;
                SlaveId = 1;
            }
            break;
            
        case 1:
            if (DetectSlave(SlaveId) == true){
                SlaveId++;
            }
            if (SlaveId > (NUMBER_OF_SLAVES - 5)){
                iSlaveDetect = 0;
                SlaveId = 51;
                return_val = true;
            }
            break;
            
        default : break;
    }       
    return (return_val);
}

    /*#--------------------------------------------------------------------------#*/
/*  Description: bool SLAVExINITxANDxCONFIG()
 *
 *  Input(s)   : 
 *
 *  Output(s)  : 
 *
 *  Returns    : Init done
 *
 *  Pre.Cond.  :
 *
 *  Post.Cond. :
 *
 *  Notes      : Sets all the addresses to the slaves according to location
 */
/*#--------------------------------------------------------------------------#*/
static uint8_t RunSlaveDetect      = 0;
static uint8_t BackplaneId         = 0;    // Used when slaves are checked
static uint8_t PrevBackplaneId     = 0;    // Used to point to first slot again
static uint8_t ShiftSlot2          = 0;
uint16_t       ShiftSlotNr         = 0;

bool DetectSlave(uint8_t SlaveId){
    bool return_val = false;
    
    switch(RunSlaveDetect){
        /* Select which detection have to be used based upon SlaveId */
        case 0:
            if(SlaveId > 50){
                GoToCase = 1;
                RunSlaveDetect = WAIT;
                DelayCount1 = READxCORExTIMER();
            }
            else if (SlaveId > 0 && SlaveId < 51){
                RunSlaveDetect = 3; // Slaves have to be selected before they react to address 0xAA
            }            
            break;
            
        /* Try to read the applicable backplane slave */
        case 1:
            Data.SlaveAddress  = SlaveId;
            Data.Direction     = READ;
            Data.NoOfRegisters = 1;
            Data.StartRegister = HOLDINGREG0;
            SLAVExCOMMUNICATIONxHANDLER();
            RunSlaveDetect++;
            break;
            
        /* Verify communication was OK */
        case 2:
            
            switch(CHECKxMODBUSxCOMMxSTATUS(SlaveId, true)){
                case SLAVEOK:  
                    MASTER_SLAVE_DATA[SlaveId].SlaveDetected = true;
                    LOG_Printf("Mbus handler\t: DETECTSLAVE Backplane %d detected.", SlaveId);
                    RunSlaveDetect = 0;
                    return_val = true;
                    break;                    
                case SLAVENOK: 
                    MASTER_SLAVE_DATA[SlaveId].SlaveDetected = false;
                    LOG_Printf("Mbus handler\t: DETECTSLAVE Backplane %d not detected.", SlaveId);
                    RunSlaveDetect = 0;
                    return_val = true;
                    break;
                case SLAVE_DATA_TIMEOUT: 
                    MASTER_SLAVE_DATA[SlaveId].SlaveDetected = false;
                    LOG_Printf("Mbus handler\t: DETECTSLAVE Backplane %d not detected.", SlaveId);
                    RunSlaveDetect = 0;
                    return_val = true;
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            break;
            
        /* ------------------------------------------------------------------ */
        
        /* Command backplane slave to select a amplifier slave */
        case 3:
            if(     SlaveId > 0  && SlaveId < 10){
                BackplaneId = 51;}
            else if(SlaveId > 10 && SlaveId < 21){
                BackplaneId = 52;}
            else if(SlaveId > 20 && SlaveId < 31){
                BackplaneId = 53;}
            else if(SlaveId > 30 && SlaveId < 41){
                BackplaneId = 54;}
            else if(SlaveId > 40 && SlaveId < 51){
                BackplaneId = 55;}
            
            if(PrevBackplaneId != BackplaneId){
                PrevBackplaneId = BackplaneId;
                ShiftSlot2 = 0;
            }
            
            if (MASTER_SLAVE_DATA[BackplaneId].SlaveDetected == true){
                ShiftSlotNr = (SLOT1 << ShiftSlot2);
//                WriteData1Register[0] = 0; // start address High,                                                                                 
//                WriteData1Register[1] = 0; // start address Low,
//                WriteData1Register[2] = (ShiftSlotNr & 0xFF00) >> 8; // Register Value Hi,
//                WriteData1Register[3] = (ShiftSlotNr & 0xFF); // Register Value Lo.
//                SLAVExCOMMUNICATIONxHANDLER(BackplaneId, HoldingReg0, Write, WriteData1Register, 4);                
                Data.SlaveAddress  = BackplaneId;
                Data.Direction     = WRITE;
                Data.NoOfRegisters = 1;
                Data.StartRegister = HOLDINGREG0;
                Data.RegData0      = ShiftSlotNr;
                SLAVExCOMMUNICATIONxHANDLER();
                
                ShiftSlot2++;
                RunSlaveDetect++;
                //DRV_USART0_WriteByte('3');
            }
            else{
                RunSlaveDetect = 0;
                return_val = true;
            }
            break;
            
        /* Verify communication was OK with backplane, if not flag backplane 
           SlaveDetected to false to indicate backplane comm error */
        case 4:
            switch(CHECKxMODBUSxCOMMxSTATUS(BackplaneId, false)){                  // --> do not overwrite otherwise diagnostics are gone
                case SLAVEOK:  
                    GoToCase = RunSlaveDetect + 1;
                    RunSlaveDetect = WAIT;
                    DelayCount1 = READxCORExTIMER();                    
                    break;                    
                case SLAVENOK: 
                    MASTER_SLAVE_DATA[BackplaneId].SlaveDetected = false;
                    LOG_Push("Mbus handler\t: DETECTSLAVE SLAVENOK returned in case 4.");
                    RunSlaveDetect = 0;
                    return_val = true;
                    //DRV_USART0_WriteByte('4');
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            break;
        
        /* Try to read the applicable amplifier slave */
        case 5:            
            Data.SlaveAddress  = SLAVE_INITIAL_ADDR;
            Data.Direction     = READ;
            Data.NoOfRegisters = 1;
            Data.StartRegister = HOLDINGREG11; // read the checksum of a slave
            SLAVExCOMMUNICATIONxHANDLER();
//            SLAVExCOMMUNICATIONxHANDLERxREAD(SLAVE_INITIAL_ADDR, HoldingReg11, ReadSingle);
            RunSlaveDetect++;
            //DRV_USART0_WriteByte('5');
            break;
            
        /* Verify communication was OK */
        case 6:
            switch(CHECKxMODBUSxCOMMxSTATUS(SLAVE_INITIAL_ADDR, true)){
                case SLAVEOK:  
                    MASTER_SLAVE_DATA[SlaveId].SlaveDetected = true;
                    MASTER_SLAVE_DATA[SlaveId].HoldingReg[HOLDINGREG11] = DUMP_SLAVE_DATA[0].HoldingReg[HOLDINGREG11]; // store the read FW version
                    LOG_Printf("Mbus handler\t: DETECTSLAVE Slave %d detected.", SlaveId);
                    RunSlaveDetect++;                    
                    //DRV_USART0_WriteByte('6');
                    break;                    
                case SLAVENOK: 
                    MASTER_SLAVE_DATA[SlaveId].SlaveDetected = false;
                    LOG_Printf("Mbus handler\t: DETECTSLAVE Slave %d not detected.", SlaveId);
                    RunSlaveDetect++;
                    //DRV_USART0_WriteByte('6');
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            break;
            
        /* Command backplane slave to select no amplifier slave */
        case 7:
            Data.SlaveAddress  = BackplaneId;
            Data.Direction     = WRITE;
            Data.NoOfRegisters = 1;
            Data.StartRegister = HOLDINGREG0;
            Data.RegData0      = 0;
            SLAVExCOMMUNICATIONxHANDLER();
//            WriteData1Register[0] = 0; // start address High,                                                                                 
//            WriteData1Register[1] = 0; // start address Low,
//            WriteData1Register[2] = 0; // Register Value Hi,
//            WriteData1Register[3] = 0; // Register Value Lo.
//            SLAVExCOMMUNICATIONxHANDLER(BackplaneId, HoldingReg0, Write, WriteData1Register, 4);            
            RunSlaveDetect++;
            break;
            
        /* Verify communication was OK with backplane, if not flag backplane 
           SlaveDetected to false to indicate backplane comm error */
        case 8:
            switch(CHECKxMODBUSxCOMMxSTATUS(BackplaneId, true)){
                case SLAVEOK:  
                    RunSlaveDetect = 0;
                    return_val = true;                    
                    //DRV_USART0_WriteByte('4');
                    break;                    
                case SLAVENOK: 
                    RunSlaveDetect = 0;
                    return_val = true;
                    LOG_Push("Mbus handler\t: DETECTSLAVE SLAVENOK returned in case 8.");
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            break;
            
        case WAIT:            
            DelayCount2 = READxCORExTIMER();
            threshold = (uint32_t)(WAIT_TIME1 * MILISECONDS);
            if ((uint32_t)(DelayCount2 - DelayCount1) > threshold)
            {
                RunSlaveDetect = GoToCase;
            }
            break; 
            
        default : break;
    }
    
    return (return_val);
}    

/*#--------------------------------------------------------------------------#*/
/*  Description: bool SLAVExINITxANDxCONFIG()
 *
 *  Input(s)   : 
 *
 *  Output(s)  : 
 *
 *  Returns    : Init done
 *
 *  Pre.Cond.  :
 *
 *  Post.Cond. :
 *
 *  Notes      : Sets all the addresses to the slaves according to location
 */
/*#--------------------------------------------------------------------------#*/


uint8_t RunSlaveConfig = 0;
uint8_t ProgramSlave   = 1;
uint8_t ShiftSlot      = 0;

bool SLAVExINITxANDxCONFIG(){
    
    bool return_val = false;            
    switch(RunSlaveConfig){
        case 0:
            if(ProgramSlave > 10){
                RunSlaveConfig++;
                ShiftSlot = 0;
            }
            else if (ConfigureSlave(TRACKBACKPLANE1, SLOT1 << ShiftSlot, ProgramSlave, false) == true){
                ShiftSlot++;
                ProgramSlave++;
            }
            break;

        case 1:
            if(ProgramSlave > 20){
                RunSlaveConfig++;
                ShiftSlot = 0;
            }
            else if (ConfigureSlave(TRACKBACKPLANE2, SLOT1 << ShiftSlot, ProgramSlave, false) == true){
                ShiftSlot++;
                ProgramSlave++;                        
            }
            break;

        case 2:
            if(ProgramSlave > 30){
                RunSlaveConfig++;
                ShiftSlot = 0;
            }
            else if (ConfigureSlave(TRACKBACKPLANE3, SLOT1 << ShiftSlot, ProgramSlave, false) == true){
                ShiftSlot++;
                ProgramSlave++;
            }
            break;

        case 3:
            if(ProgramSlave > 40){
                RunSlaveConfig++;
                ShiftSlot = 0;
            }
            else if (ConfigureSlave(TRACKBACKPLANE4, SLOT1 << ShiftSlot, ProgramSlave, false) == true){
                ShiftSlot++;
                ProgramSlave++;
            }
            break;

        case 4:
            if(ProgramSlave > 49){
                if (ConfigureSlave(TRACKBACKPLANE5, SLOT1 << ShiftSlot, ProgramSlave, true) == true){
                    RunSlaveConfig = 0;
                    ProgramSlave   = 1;
                    ShiftSlot      = 0;
                    return_val     = true;
                }
            }
            else if (ConfigureSlave(TRACKBACKPLANE5, SLOT1 << ShiftSlot, ProgramSlave, false) == true){
                ShiftSlot++;
                ProgramSlave++;
            }
            break;

        default :
            break;
    }          
    return (return_val);
}
/*#--------------------------------------------------------------------------#*/
/*  Description: bool ConfigureSlave(uint8_t TrackBackPlaneID, 
 *            uint8_t AmplifierLatchSet, uint8_t TrackAmplifierId, uint8_t Mode)
 *  Input(s)   : 
 *
 *  Output(s)  : 
 *
 *  Returns    : Init done
 *
 *  Pre.Cond.  :
 *
 *  Post.Cond. :
 *
 *  Notes      : calls communication handler to execute read and writes to 
 *               slaves.
 */
/*#--------------------------------------------------------------------------#*/

uint8_t StartupMachine = 0;

bool ConfigureSlave(uint8_t BackplaneId, uint16_t AmplifierLatchSet, uint8_t TrackAmplifierId, uint8_t Mode){
    bool return_val = false;
    (void)Mode; /* Retained legacy call signature; no alternate write/probe mode. */
    
    switch(StartupMachine){
        case 0:
            AmplifierCompatible[TrackAmplifierId] = false;
            AddressWriteRetries = 0;
            if (MASTER_SLAVE_DATA[BackplaneId].SlaveDetected == true && 
                MASTER_SLAVE_DATA[TrackAmplifierId].SlaveDetected == true){
                StartupMachine++;
            }
            else{
                return_val = true;
                StartupMachine = 0;
            }
            break;
        
        /* Command backplane slave to select a amplifier slave */
        case 1:
            Data.SlaveAddress  = BackplaneId;
            Data.Direction     = WRITE;
            Data.NoOfRegisters = 1;
            Data.StartRegister = HOLDINGREG0;
            Data.RegData0      = AmplifierLatchSet;
            SLAVExCOMMUNICATIONxHANDLER();
//            WriteData1Register[0] = 0; // start address High,                                                                                 
//            WriteData1Register[1] = 0; // start address Low,
//            WriteData1Register[2] = (AmplifierLatchSet & 0xFF00) >> 8; // Register Value Hi,
//            WriteData1Register[3] = (AmplifierLatchSet & 0xFF); // Register Value Lo.
//            SLAVExCOMMUNICATIONxHANDLER(TrackBackPlaneID, HoldingReg0, Write, WriteData1Register, 4);            
            StartupMachine++;
            break;
            
        /* Verify communication was OK with backplane, if not flag backplane 
           SlaveDetected to false to indicate backplane comm error */
        case 2:
            switch(CHECKxMODBUSxCOMMxSTATUS(BackplaneId, true)){
                case SLAVEOK:  
                    GoToCase = 10; /* Two fresh identity reads before address write. */
                    StartupMachine = WAIT;
                    DelayCount1 = READxCORExTIMER();
                    break;                    
                case SLAVENOK: 
                    StartupMachine = 5;
                    //DRV_USART0_WriteByte('4');
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            break;
            
        case 10:
            if (VERIFYxAMPLIFIERxIDENTITY(TrackAmplifierId)) {
                StartupMachine = ISxAMPLIFIERxCOMPATIBLE(TrackAmplifierId) ? 3 : 5;
            }
            break;

        /* Program new SlaveId only after compatibility was established. */
        case 3:
            /* Never issue the HR2 address write while the transmitter is still
             * busy with the identity read: SENDxPETITxMODBUS silently drops a
             * request when Petit_Tx_State != PETIT_RXTX_IDLE, leaving MbCommError
             * IDLE, which case 4 would misread as SLAVENOK. Stay pending until
             * the transmitter is free; the Modbus receive timeout bounds this
             * wait, so this is not an unbounded stall. */
            if (!PetitModbusIsReady()) break;
            Data.SlaveAddress  = SLAVE_INITIAL_ADDR;
            Data.Direction     = WRITE;
            Data.NoOfRegisters = 1;
            Data.StartRegister = HOLDINGREG2;
            Data.RegData0      = TrackAmplifierId;
            SLAVExCOMMUNICATIONxHANDLER();
//            WriteData1Register[0] = 0;                  // start address High,                                                                                 
//            WriteData1Register[1] = HoldingReg2;        // start address Low,
//            WriteData1Register[2] = 0;                  // Register Value Hi,
//            WriteData1Register[3] = TrackAmplifierId;   // Register Value Lo.
//            SLAVExCOMMUNICATIONxHANDLER(SLAVE_INITIAL_ADDR, 0, Write, WriteData1Register, 4);            
            StartupMachine++;
            break;
            
        /* Verify communication was OK with slave */
        case 4:
        {
            /* Capture the raw result before CHECKxMODBUSxCOMMxSTATUS's OverWrite
             * reset erases it: a transient response timeout is retryable, but a
             * genuine rejection (exception/NOK) is a hard failure. */
            uint8_t writeResult = DUMP_SLAVE_DATA[0].MbCommError;
            switch(CHECKxMODBUSxCOMMxSTATUS(SLAVE_INITIAL_ADDR, true)){
                case SLAVEOK:  
                    AddressWriteRetries = 0;
                    StartupMachine++;
                    //DRV_USART0_WriteByte('4');
                    break;                    
                case SLAVENOK:
                    if (writeResult == SLAVE_DATA_TIMEOUT &&
                        AddressWriteRetries < 3u) {
                        /* Transient non-ACK: re-issue the HR2 write (bounded). */
                        ++AddressWriteRetries;
                        StartupMachine = 3;
                    } else {
                        AddressWriteRetries = 0;
                        AmplifierCompatible[TrackAmplifierId] = false;
                        MASTER_SLAVE_DATA[TrackAmplifierId].HoldingReg[HR_IDENTITY] = 0;
                        CREATExTASKxSTATUSxMESSAGE(MBUS, EXEC_MBUS_STATE_SLAVE_INIT,
                            ERROR, TrackAmplifierId);
                        StartupMachine++;
                    }
                    //DRV_USART0_WriteByte('4');
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            break;
        }
            
        /* Command backplane slave to select no amplifier slave */
        case 5:
            Data.SlaveAddress  = BackplaneId;
            Data.Direction     = WRITE;
            Data.NoOfRegisters = 1;
            Data.StartRegister = HOLDINGREG0;
            Data.RegData0      = 0;
            SLAVExCOMMUNICATIONxHANDLER();
//            WriteData1Register[0] = 0; // start address High,                                                                                 
//            WriteData1Register[1] = 0; // start address Low,
//            WriteData1Register[2] = 0; // Register Value Hi,
//            WriteData1Register[3] = 0; // Register Value Lo.
//            SLAVExCOMMUNICATIONxHANDLER(TrackBackPlaneID, HoldingReg0, Write, WriteData1Register, 4);            
            StartupMachine++;
            break;
            
        /* Verify communication was OK with backplane, if not flag backplane 
           SlaveDetected to false to indicate backplane comm error */
        case 6:
            switch(CHECKxMODBUSxCOMMxSTATUS(BackplaneId, true)){
                case SLAVEOK:  
                    StartupMachine = 0;
                    return_val = true;                    
                    //DRV_USART0_WriteByte('4');
                    break;                    
                case SLAVENOK: 
                    StartupMachine = 0;
                    return_val = true;
                    //DRV_USART0_WriteByte('4');
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            break;
            
        case WAIT:
            DelayCount2 = READxCORExTIMER();
            threshold = (uint32_t)(WAIT_TIME1 * MILISECONDS);
            if ((uint32_t)(DelayCount2 - DelayCount1) > threshold)
            {
                StartupMachine = GoToCase;
            }            
            break;
            
        default :                
            break;
    }
    return (return_val);
}

/*#--------------------------------------------------------------------------#*/
/*  Description: bool ENABLExAMPLIFIER(void)
 *
 *  Input(s)   : 
 *
 *  Output(s)  : 
 *
 *  Returns    : Init done
 *
 *  Pre.Cond.  :
 *
 *  Post.Cond. :
 *
 *  Notes      : Enables all slaves amplifiers
 */
/*#--------------------------------------------------------------------------#*/
uint8_t EnableMachine = 0;
static uint8_t EnableSlave;

bool ENABLExAMPLIFIER(void){
    bool return_val = false;
        
    switch(EnableMachine){
        case 0:
            EnableSlave = 1;
            GoToCase = 1;
            EnableMachine = WAIT;
            DelayCount1 = READxCORExTIMER();
            break;

        case 1:
            if (ISxGLOBALxSTOPxLATCHED()) return false;
            while (EnableSlave <= NUMBER_OF_AMPLIFIERS &&
                   !ISxAMPLIFIERxCOMPATIBLE(EnableSlave)) ++EnableSlave;
            if (EnableSlave > NUMBER_OF_AMPLIFIERS) {
                EnableMachine = 2;
                break;
            }
            if (!PetitModbusIsReady()) break;
            Data.SlaveAddress  = EnableSlave++;
            Data.Direction     = WRITE;
            Data.NoOfRegisters = 1;
            Data.StartRegister = HOLDINGREG1;
            Data.RegData0      = 0x8000;
            SLAVExCOMMUNICATIONxHANDLER();
//            WriteData1Register[0] = 0;                  // start address High,                                                                                 
//            WriteData1Register[1] = HoldingReg1;        // start address Low,
//            WriteData1Register[2] = 0x80;               // Register Value Hi,
//            WriteData1Register[3] = 0x00;               // Register Value Lo.
//            SLAVExCOMMUNICATIONxHANDLER(BROADCAST_ADDRESS, 0, Write, WriteData1Register, 4);
            GoToCase = 1;
            EnableMachine = WAIT;
            DelayCount1 = READxCORExTIMER();
            break;
        
        case 2:
            return_val = true;
            EnableMachine = 0;
            break;
            
        case WAIT:
            DelayCount2 = READxCORExTIMER();
            threshold = (uint32_t)(WAIT_TIME2 * MILISECONDS);
            if ((uint32_t)(DelayCount2 - DelayCount1) > threshold)
            {
                EnableMachine = GoToCase;
            }
            break;

        default:
            break;
    }
    return (return_val);
}
