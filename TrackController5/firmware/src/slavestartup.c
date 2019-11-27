#include <xc.h>
#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdlib.h>
#include "app.h"
#include "commhandler.h"

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

static uint8_t GoToCase            = 0;
static uint16_t WaitCounter        = 0;

enum ADDR
{    
    WAIT_TIME  = 20000,
    WAIT_TIME2 = 65000,
};

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
uint8_t SlaveDetect = 0;

bool SLAVExDETECT(){
    bool return_val = false;
    
    switch (SlaveDetect){
        case 0:
            if (DetectSlave(SlaveId) == true){
                SlaveId++;
            }
            if (SlaveId > NUMBER_OF_SLAVES){
                SlaveDetect = 1;
                SlaveId = 1;
            }
            break;
            
        case 1:
            if (DetectSlave(SlaveId) == true){
                SlaveId++;
            }
            if (SlaveId > (NUMBER_OF_SLAVES - 5)){
                SlaveDetect = 0;
                SlaveId = 1;
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
                    RunSlaveDetect = 0;
                    return_val = true;
                    break;                    
                case SLAVENOK: 
                    MASTER_SLAVE_DATA[SlaveId].SlaveDetected = false;
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
                    //DRV_USART0_WriteByte('4');
                    break;                    
                case SLAVENOK: 
                    MASTER_SLAVE_DATA[BackplaneId].SlaveDetected = false;
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
            Data.StartRegister = HOLDINGREG11;
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
                    RunSlaveDetect++;                    
                    //DRV_USART0_WriteByte('6');
                    break;                    
                case SLAVENOK: 
                    MASTER_SLAVE_DATA[SlaveId].SlaveDetected = false;
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
                    //DRV_USART0_WriteByte('4');
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            break;
            
        case WAIT:
            WaitCounter++;
            if (WaitCounter > WAIT_TIME){
                WaitCounter = 0;
                RunSlaveDetect = GoToCase;
                //DRV_USART0_WriteByte('W');
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
                    return_val = true;
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
    
    switch(StartupMachine){
        case 0:
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
                    GoToCase = StartupMachine + 1;
                    StartupMachine = WAIT;                    
                    //DRV_USART0_WriteByte('4');
                    break;                    
                case SLAVENOK: 
                    StartupMachine = 5;
                    //DRV_USART0_WriteByte('4');
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            break;
            
        /* Program new SlaveId to slave */
        case 3:
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
            switch(CHECKxMODBUSxCOMMxSTATUS(SLAVE_INITIAL_ADDR, true)){
                case SLAVEOK:  
                    StartupMachine++;
                    //DRV_USART0_WriteByte('4');
                    break;                    
                case SLAVENOK: 
                    StartupMachine++;
                    //DRV_USART0_WriteByte('4');
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            break;
            
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
            WaitCounter++;
            if (WaitCounter > WAIT_TIME){
                WaitCounter = 0;
                StartupMachine = GoToCase;
                //DRV_USART0_WriteByte('W');
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

bool ENABLExAMPLIFIER(void){
    bool return_val = false;
        
    switch(EnableMachine){
        case 0:
            GoToCase = 1;
            EnableMachine = WAIT;
            break;

        case 1:
            Data.SlaveAddress  = BROADCAST_ADDRESS;
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
            EnableMachine++;
            break;
        
        case 2:
            return_val = true;
            EnableMachine = 0;
            break;
            
        case WAIT:
            WaitCounter++;
            if (WaitCounter > WAIT_TIME2){
                WaitCounter = 0;
                EnableMachine = GoToCase;
                //DRV_USART0_WriteByte('W');
            }            
            break;

        default:
            break;
    }
    return (return_val);
}