#include <xc.h>
#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdlib.h>
#include "app.h"
#include "commhandler.h"
#include "modbus/PetitModbus.h"

uint8_t CheckModbusCommStatus(uint8_t SlaveId, bool OverWrite);
bool    ConfigureSlave       (uint8_t TrackBackPlaneID, uint8_t AmplifierLatchSet, uint8_t TrackAmplifierId, uint8_t Mode);
bool    DetectSlave          (uint8_t SlaveId);

uint8_t WriteData2Registers[7] = {0, 0, 0, 1, 2, 0, 0};                         // {start address High, start address Low, 
                                                                                // number of registers High, number of registers Low, 
                                                                                // byte count, Register Value Hi, Register Value Lo} 

uint8_t WriteData1Register[4] = {0, 0, 0, 0};                                   // {start address High, start address Low, 
                                                                                // Register Value Hi, Register Value Lo} 

uint8_t ReadData[4] = {0, 0, 0, 2};                                             // {start address High, start address Low, 
                                                                                // number of registers High, number of registers Low, 

enum ADDR
{
    SLOT1  = 0x1,
    SLOT2  = 0x2,
    SLOT3  = 0x4,
    SLOT4  = 0x8,
    SLOT5  = 0x10,
    SLOT6  = 0x20,
    SLOT7  = 0x40,
    SLOT8  = 0x80,
    SLOT9  = 0x100,
    SLOT10 = 0x200,
    TRACKBACKPLANE1 = 51,
    TRACKBACKPLANE2 = 52,
    TRACKBACKPLANE3 = 53,
    TRACKBACKPLANE4 = 54,
    TRACKBACKPLANE5 = 55,
    SLAVE_INITIAL_ADDR = 0xAA,
    WAIT      = 99,
    SLAVEOK   = 100,
    SLAVENOK  = 101,
    SLAVEBUSY = 102,
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
static uint8_t ShiftSlot           = 0;
static uint16_t WaitCounter        = 0;
static uint8_t GoToCase            = 0;

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
            SLAVExCOMMUNICATIONxHANDLER(SlaveId, HoldingReg0, Read, ReadData, 4);
            RunSlaveDetect++;
            break;
            
        /* Verify communication was OK */
        case 2:
            
            switch(CheckModbusCommStatus(SlaveId, true)){
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
            /*
            if(MASTER_SLAVE_DATA[SlaveId].MbCommError != SLAVE_DATA_BUSY &&
               MASTER_SLAVE_DATA[SlaveId].MbCommError != SLAVE_DATA_OK){
               MASTER_SLAVE_DATA[SlaveId].SlaveDetected = false;
               MASTER_SLAVE_DATA[SlaveId].MbCommError = SLAVE_DATA_IDLE;
               RunSlaveDetect = 0;
               return_val = true;
            }
            else if 
              (MASTER_SLAVE_DATA[SlaveId].MbCommError != SLAVE_DATA_BUSY &&
               MASTER_SLAVE_DATA[SlaveId].MbCommError == SLAVE_DATA_OK){
               MASTER_SLAVE_DATA[SlaveId].SlaveDetected = true;
               MASTER_SLAVE_DATA[SlaveId].MbCommError = SLAVE_DATA_IDLE;
               RunSlaveDetect = 0;
               return_val = true;
            }
            */
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
            
            if (MASTER_SLAVE_DATA[BackplaneId].SlaveDetected == true){
                WriteData1Register[0] = 0; // start address High,                                                                                 
                WriteData1Register[1] = 0; // start address Low,
                WriteData1Register[2] = 0; // Register Value Hi,
                WriteData1Register[3] = (SLOT1 << ShiftSlot); // Register Value Lo.
                SLAVExCOMMUNICATIONxHANDLER(BackplaneId, HoldingReg0, Write, WriteData1Register, 4);
                ShiftSlot++;
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
            switch(CheckModbusCommStatus(BackplaneId, false)){                  // --> do not overwrite otherwise diagnostics are gone
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
            /*
            if( MASTER_SLAVE_DATA[BackplaneId].MbCommError != SLAVE_DATA_BUSY &&
                MASTER_SLAVE_DATA[BackplaneId].MbCommError != SLAVE_DATA_OK){
                MASTER_SLAVE_DATA[BackplaneId].SlaveDetected = false;
                //MASTER_SLAVE_DATA[BackplaneId].MbCommError = SLAVE_DATA_IDLE; --> do not overwrite otherwise diagnostics are gone
                RunSlaveDetect = 0;
                return_val = true;
                //DRV_USART0_WriteByte('4');
            }
            else if 
              ( MASTER_SLAVE_DATA[BackplaneId].MbCommError != SLAVE_DATA_BUSY &&
                MASTER_SLAVE_DATA[BackplaneId].MbCommError == SLAVE_DATA_OK){               
                MASTER_SLAVE_DATA[BackplaneId].MbCommError = SLAVE_DATA_IDLE;
                GoToCase = RunSlaveDetect + 1;
                RunSlaveDetect = WAIT;
                //DRV_USART0_WriteByte('4');
            }
            */
            break;
        
        /* Try to read the applicable amplifier slave */
        case 5:
            SLAVExCOMMUNICATIONxHANDLER(SLAVE_INITIAL_ADDR, HoldingReg0, Read, ReadData, 4);
            RunSlaveDetect++;
            //DRV_USART0_WriteByte('5');
            break;
            
        /* Verify communication was OK */
        case 6:
            switch(CheckModbusCommStatus(SLAVE_INITIAL_ADDR, true)){
                case SLAVEOK:  
                    MASTER_SLAVE_DATA[SlaveId].SlaveDetected = true;
                    RunSlaveDetect = 0;
                    return_val = true;
                    //DRV_USART0_WriteByte('6');
                    break;                    
                case SLAVENOK: 
                    MASTER_SLAVE_DATA[SlaveId].SlaveDetected = false;
                    RunSlaveDetect = 0;
                    return_val = true;
                    //DRV_USART0_WriteByte('6');
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            /*
            if( DUMP_SLAVE_DATA[0].MbCommError != SLAVE_DATA_BUSY &&
                DUMP_SLAVE_DATA[0].MbCommError == SLAVE_DATA_OK){
                MASTER_SLAVE_DATA[SlaveId].SlaveDetected = true;
                DUMP_SLAVE_DATA[0].MbCommError = SLAVE_DATA_IDLE;       // Write Idle back for next communication towards this slave!
                //RunSlaveDetect++;
                RunSlaveDetect = 0;
                return_val = true;
                //DRV_USART0_WriteByte('6');
            }
            else if( DUMP_SLAVE_DATA[0].MbCommError != SLAVE_DATA_BUSY &&
                DUMP_SLAVE_DATA[0].MbCommError != SLAVE_DATA_OK){
                MASTER_SLAVE_DATA[SlaveId].SlaveDetected = false;
                DUMP_SLAVE_DATA[0].MbCommError = SLAVE_DATA_IDLE;        // Write Idle back for next communication towards this slave!
                //RunSlaveDetect++;
                RunSlaveDetect = 0;
                return_val = true;
                //DRV_USART0_WriteByte('6');
            }
            */ 
            break;
        
        /* Command backplane slave to deselect a amplifier slave */ /*
        case 7:
            WriteData1Register[0] = 0; // start address High,                                                                                 
            WriteData1Register[1] = 0; // start address Low,
            WriteData1Register[2] = 0; // Register Value Hi,
            WriteData1Register[3] = 0; // Register Value Lo.
            SLAVExCOMMUNICATIONxHANDLER(BackplaneId, HoldingReg0, Write, WriteData1Register, 4);
            RunSlaveDetect++;
            //DRV_USART0_WriteByte('7');
            break;*/
            
        /* Verify communication was OK with backplane, if not flag backplane 
           SlaveDetected to false to indicate backplane comm error */ /*
        case 8:
            if( MASTER_SLAVE_DATA[BackplaneId].MbCommError != SLAVE_DATA_BUSY &&
                MASTER_SLAVE_DATA[BackplaneId].MbCommError != SLAVE_DATA_OK){
                MASTER_SLAVE_DATA[BackplaneId].SlaveDetected = false;
                MASTER_SLAVE_DATA[BackplaneId].MbCommError = SLAVE_DATA_IDLE;
                RunSlaveDetect = 0;
                return_val = true;
                //DRV_USART0_WriteByte('8');
            }
            else if 
              ( MASTER_SLAVE_DATA[BackplaneId].MbCommError != SLAVE_DATA_BUSY &&
                MASTER_SLAVE_DATA[BackplaneId].MbCommError == SLAVE_DATA_OK){               
                MASTER_SLAVE_DATA[BackplaneId].MbCommError = SLAVE_DATA_IDLE;
                //RunSlaveDetect = 0;
                return_val = true;
                GoToCase = 0;
                RunSlaveDetect = WAIT;
                //DRV_USART0_WriteByte('8');
            }
            break;*/
            
        case WAIT:
            WaitCounter++;
            if (WaitCounter > 50000){
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
/*  Description: uint8_t CheckModbusCommStatus(uint8_t SlaveId)
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
uint8_t CheckModbusCommStatus(uint8_t SlaveId, bool OverWrite){
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
uint8_t ProgramSlave = 1;
//uint8_t ShiftSlot = 0;

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
enum checkers
{
    OK          = 0x02, 
    
    
};

uint8_t StartupMachine = 0;

bool ConfigureSlave(uint8_t TrackBackPlaneID, uint8_t AmplifierLatchSet, uint8_t TrackAmplifierId, uint8_t Mode){
    bool return_val = false;
    
    switch(StartupMachine){
        case 0:
            if (MASTER_SLAVE_DATA[TrackBackPlaneID].SlaveDetected == true && 
                MASTER_SLAVE_DATA[TrackAmplifierId].SlaveDetected == true){
                StartupMachine = 1;
            }
            else{
                return_val = true;
                StartupMachine = 0;
            }
            break;
            
        case 1:
            WriteData1Register[0] = 0; // start address High,                                                                                 
            WriteData1Register[1] = 0; // start address Low,
            WriteData1Register[2] = 0; // Register Value Hi,
            WriteData1Register[3] = AmplifierLatchSet; // Register Value Lo.
            SLAVExCOMMUNICATIONxHANDLER(TrackBackPlaneID, HoldingReg0, Write, WriteData1Register, 4);            
            StartupMachine = 2;
            break;

        /*case 1:
            if(MASTER_SLAVE_DATA[0].InputReg[0] == OK){
                MASTER_SLAVE_DATA[0].HoldingReg[0] = MODE_MAN & WRITE & HOLDINGREG & HALT; // Remove the execute command
                StartupMachine = 2;
            }
            break;

        case 2:
            if(MASTER_SLAVE_DATA[0].InputReg[0] == IDLE){
                StartupMachine = 3;
            }
            break;

        case 3:
            MASTER_SLAVE_DATA[0].HoldingReg[1] = 0xAA;                          // Set address to first Trackamplifier in config mode, default modbus address = 0xAA
            MASTER_SLAVE_DATA[0].HoldingReg[2] = TrackAmplifierId;              // Data = Set ID 
            MASTER_SLAVE_DATA[0].HoldingReg[3] = 2;                             // Register number to write to
            MASTER_SLAVE_DATA[0].HoldingReg[0] = MODE_MAN & WRITE & HOLDINGREG & EXEC;
            StartupMachine = 4;
            break;

        case 4:
            if(MASTER_SLAVE_DATA[0].InputReg[0] == OK){
                MASTER_SLAVE_DATA[0].HoldingReg[0] = MODE_MAN & WRITE & HOLDINGREG & HALT; // Remove the execute command
                StartupMachine = 5;
            }
            break;

        case 5:
            if(MASTER_SLAVE_DATA[0].InputReg[0] == IDLE){
                StartupMachine = 6;
            }
            break;    

        case 6:
            MASTER_SLAVE_DATA[0].HoldingReg[1] = TrackBackPlaneID;              // Set address to TrackBackplane slave 55
            MASTER_SLAVE_DATA[0].HoldingReg[2] = 0xFFFF;                        // Data = Release all select lines
            MASTER_SLAVE_DATA[0].HoldingReg[3] = 0;                             // Register number to write to
            MASTER_SLAVE_DATA[0].HoldingReg[0] = MODE_MAN & WRITE & HOLDINGREG & EXEC;
            StartupMachine = 7;
            break;

        case 7:
            if(MASTER_SLAVE_DATA[0].InputReg[0] == OK){
                MASTER_SLAVE_DATA[0].HoldingReg[0] = MODE_MAN & WRITE & HOLDINGREG & HALT; // Remove the execute command
                StartupMachine = 8;
            }
            break;

        case 8:
            if(MASTER_SLAVE_DATA[0].InputReg[0] == IDLE){
                if (Mode == true){                                              // When the last slave is configured, release all amplifiers into regular communication
                    MASTER_SLAVE_DATA[0].HoldingReg[0] = MODE_AUTO & WRITE & HOLDINGREG & HALT;
                }
                StartupMachine = 9;
            }
            break;

        case 9:
            return_val = true;
            StartupMachine = 0;
            break;*/

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
unsigned int _Delay = 0;

bool ENABLExAMPLIFIER(void){
    bool return_val = false;
    
    /*
    if (PIR2bits.TMR3IF){
        _Delay++;
        PIR2bits.TMR3IF = 0;
    }
    if (_Delay > 20){
        for (unsigned int i = 1; i <NUMBER_OF_SLAVES; i++){
            MASTER_SLAVE_DATA[i].HoldingReg[1] |= 0x8000;                       // Enable each amplifier (serially)
        }
        _Delay = 0;
        return_val = true;
    }
    */
    //if (PIR2bits.TMR3IF){                                                       // Update rate 10ms
    _Delay++;

    if (_Delay > 30){
        _Delay = 0;
        switch(StartupMachine){
            case 0:
                MASTER_SLAVE_DATA[0].HoldingReg[1] = 0;                             // Address  = broadcast address
                MASTER_SLAVE_DATA[0].HoldingReg[2] = 0x8000;                        // Data     = Set AMP_ID5_SET_LAT 0x10 in TrackBackplaneSlave
                MASTER_SLAVE_DATA[0].HoldingReg[3] = 1;                             // Register = number to write to
                //MASTER_SLAVE_DATA[0].HoldingReg[0] = MODE_AUTO & WRITE & HOLDINGREG & EXEC;
                StartupMachine = 1;
                break;

            case 1:/*
                if(MASTER_SLAVE_DATA[0].InputReg[0] == OK){
                    MASTER_SLAVE_DATA[0].HoldingReg[0] = MODE_AUTO & WRITE & HOLDINGREG & HALT; // Remove the execute command
                    StartupMachine = 0;
                    return_val = true;
                }*/
                break;

            case 2:/*
                if(MASTER_SLAVE_DATA[0].InputReg[0] == IDLE){
                    StartupMachine = 0;
                    return_val = true;
                }*/
                break;

            default:
                break;
        }
    }   
    return (return_val);
}