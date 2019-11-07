#include <xc.h>
#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdlib.h>
#include "app.h"
#include "commhandler.h"
#include "modbus/PetitModbus.h"

bool ConfigureSlave(uint8_t TrackBackPlaneID, uint8_t AmplifierLatchSet, uint8_t TrackAmplifierId, uint8_t Mode);
bool DetectSlave(uint8_t SlaveId);

uint8_t WriteData[7] = {0, 0, 0, 1, 2, 0, 0};                                   // {start address High, start address Low, 
                                                                                // number of registers High, number of registers Low, 
                                                                                // byte count, Register Value Hi, Register Value Lo} 

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

void INITxSLAVExSTARTUP(SLAVE_INFO *location){
    MASTER_SLAVE_DATA  =  location;
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
uint8_t SlaveId = 51;

bool SLAVExDETECT(){
    bool return_val = false;    
   
    if (DetectSlave(SlaveId) == true){
        SlaveId++;
    }
    if (SlaveId > NUMBER_OF_SLAVES){ --> switch van maken, eerst backplanes checken, daarna
        return_val = true;               slaves checken, vereist bijna zelfde functie als 
    }                                    zetten van slave id in slave...
    
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
uint8_t RunSlaveDetect = 0;

bool DetectSlave(uint8_t SlaveId){
    bool return_val = false;   
    
    switch(RunSlaveDetect){
        case 0:
            SLAVExCOMMUNICATIONxHANDLER(SlaveId, 0, Read, ReadData, 4);
            RunSlaveDetect++;
            break;
            
        case 1:
            if(MASTER_SLAVE_DATA[SlaveId].MbCommError != SLAVE_DATA_BUSY &&
               MASTER_SLAVE_DATA[SlaveId].MbCommError != SLAVE_DATA_OK){
               MASTER_SLAVE_DATA[SlaveId].SlaveDetected = false;
               RunSlaveDetect = 0;
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


uint8_t RunSlaveConfig = 0;
uint8_t ProgramSlave = 1;
uint8_t ShiftSlot = 0;

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
            WriteData[1] = TrackBackPlaneID;  // Address
            WriteData[6] = AmplifierLatchSet; // Data
            SLAVExCOMMUNICATIONxHANDLER(TrackBackPlaneID, HoldingReg0, Write, WriteData, 7);
            StartupMachine = 1;
            break;

        case 1:/*
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