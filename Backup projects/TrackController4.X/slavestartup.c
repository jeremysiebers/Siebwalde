#include <xc.h>
#include "main.h"
#include "mcc_generated_files/mcc.h"

unsigned int ConfigureSlave(unsigned int TrackBackPlaneID, unsigned int AmplifierLatchSet, unsigned int TrackAmplifierId, unsigned int Mode);

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
/*  Description: unsigned int SLAVExINIT()
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
 *  Notes      :
 */
/*#--------------------------------------------------------------------------#*/
typedef enum
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
unsigned int _Delay = 0;
unsigned int RunSlaveConfig = 0;
unsigned int ProgramSlave = 1;
unsigned int ShiftSlot = 0;
/*
unsigned int SLAVExINITxANDxCONFIG(){
    
    unsigned return_val = false;
    
    if (PIR2bits.TMR3IF){                                                       // Update rate 50ms
        _Delay++;
            
        if (_Delay > 0){
            _Delay = 0;
            
            switch(RunSlaveConfig){
                case 0:
                    if (ConfigureSlave(TRACKBACKPLANE1, SLOT5, 2, true) == true){
                        RunSlaveConfig = 0;
                        return_val = true;
                    }
                    break;
                    
                default :
                    break;
            }
        }
    }
    return (return_val);
}
*/

unsigned int SLAVExINITxANDxCONFIG(){
    
    unsigned return_val = false;
    
//    if (1){//PIR2bits.TMR3IF){                                                       // Update rate 50ms
//        _Delay++;
//            
//        if (_Delay > 0){
//            _Delay = 0;
            
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
//        }
//        PIR2bits.TMR3IF = 0;
//    }
    return (return_val);
}
/*
unsigned int SLAVExINITxANDxCONFIG(){
    
    unsigned return_val = false;
    
    if (PIR2bits.TMR3IF){                                                       // Update rate 50ms
        _Delay++;
            
        if (_Delay > 0){
            _Delay = 0;
            
            switch(RunSlaveConfig){
                case 0:
                    if (ConfigureSlave(TRACKBACKPLANE1, SLOT1, 1, false) == true){
                        RunSlaveConfig++;
                    }
                    break;
                    
                case 1:
                    if (ConfigureSlave(TRACKBACKPLANE1, SLOT2, 2, false) == true){
                        RunSlaveConfig++;
                    }
                    break;
                    
                case 2:
                    if (ConfigureSlave(TRACKBACKPLANE1, SLOT3, 3, false) == true){
                        RunSlaveConfig++;
                    }
                    break;
                    
                case 3:
                    if (ConfigureSlave(TRACKBACKPLANE1, SLOT4, 4, false) == true){
                        RunSlaveConfig++;
                    }
                    break;
                    
                case 4:
                    if (ConfigureSlave(TRACKBACKPLANE1, SLOT5, 5, false) == true){
                        RunSlaveConfig++;
                    }
                    break;
                    
                case 5:
                    if (ConfigureSlave(TRACKBACKPLANE1, SLOT6, 6, true) == true){
                        RunSlaveConfig++;return_val = true;
                    }
                    break;
                    
                case 6:
                    if (ConfigureSlave(TRACKBACKPLANE1, SLOT7, 7, false) == true){
                        RunSlaveConfig++;
                    }
                    break;
                    
                case 7:
                    if (ConfigureSlave(TRACKBACKPLANE1, SLOT8, 8, false) == true){
                        RunSlaveConfig++;
                    }
                    break;
                    
                case 8:
                    if (ConfigureSlave(TRACKBACKPLANE1, SLOT9, 9, true) == true){
                        RunSlaveConfig++;
                        RunSlaveConfig = 0;
                        return_val = true;
                    }
                    break;
                    
                case 9:
                    if (ConfigureSlave(TRACKBACKPLANE1, SLOT10, 10, true) == true){
                    //if (ConfigureSlave(TRACKBACKPLANE1, SLOT10, 4, true) == false){
                        RunSlaveConfig = 0;
                        return_val = true;
                    }
                    break;
                    
                default :
                    break;
            }          
        }
        PIR2bits.TMR3IF = 0;
    }
    return (return_val);
}
*/

/*#--------------------------------------------------------------------------#*/
/*  Description: unsigned int ConfigureSlave(unsigned int TrackBackPlaneID, 
 *               unsigned int AmplifierLatchSet, unsigned int TrackAmplifierId)
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
 *  Notes      :
 */
/*#--------------------------------------------------------------------------#*/
typedef enum
{
    OK          = 0x02, 
    NOK         = 0x04, 
    _BUSY        = 0x01,
    IDLE        = 0x00,
    MODE_MAN    = 0b1111111111111110,
    MODE_AUTO   = 0b1111111111111111,    
    WRITE       = 0b1111111111111111,
    READ        = 0b1111111111111101,
    HOLDINGREG  = 0b1111111111100111,
    INPUTREG    = 0b1111111111101011,
    DIAGREG     = 0b1111111111110011,
    EXEC        = 0b1111111111111111,
    HALT        = 0b1111111111011111,
    
};
unsigned int StartupMachine = 0;

unsigned int ConfigureSlave(unsigned int TrackBackPlaneID, unsigned int AmplifierLatchSet, unsigned int TrackAmplifierId, unsigned int Mode){
    unsigned int return_val = false;
    
    switch(StartupMachine){
        case 0:
            MASTER_SLAVE_DATA[0].HoldingReg[1] = TrackBackPlaneID;              // Set address to TrackBackplane slave 51
            MASTER_SLAVE_DATA[0].HoldingReg[2] = AmplifierLatchSet;             // Data = Set AMP_ID5_SET_LAT 0x10 in TrackBackplaneSlave
            MASTER_SLAVE_DATA[0].HoldingReg[3] = 0;                             // Register number to write to
            MASTER_SLAVE_DATA[0].HoldingReg[0] = MODE_MAN & WRITE & HOLDINGREG & EXEC;
            StartupMachine = 1;
            break;

        case 1:
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
            break;

        default :                
            break;
    }
    return (return_val);
}

/*#--------------------------------------------------------------------------#*/
/*  Description: unsigned int SLAVExTOxRUN(void)
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
 *  Notes      :
 */
/*#--------------------------------------------------------------------------#*/
unsigned int ENABLExAMPLIFIER(void){
    unsigned int return_val = false;
    
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
                MASTER_SLAVE_DATA[0].HoldingReg[0] = MODE_AUTO & WRITE & HOLDINGREG & EXEC;
                StartupMachine = 1;
                break;

            case 1:
                if(MASTER_SLAVE_DATA[0].InputReg[0] == OK){
                    MASTER_SLAVE_DATA[0].HoldingReg[0] = MODE_AUTO & WRITE & HOLDINGREG & HALT; // Remove the execute command
                    StartupMachine = 0;
                    return_val = true;
                }
                break;

            case 2:
                if(MASTER_SLAVE_DATA[0].InputReg[0] == IDLE){
                    StartupMachine = 0;
                    return_val = true;
                }
                break;

            default:
                break;
        }
    }
        //PIR2bits.TMR3IF = 0;
    //}
    
    return (return_val);
}