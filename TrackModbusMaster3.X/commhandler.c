/*
 * File:   commhandler.c
 * Author: Jeremy Siebers
 *
 * Created on aug 28, 2018, 14:15 PM
 */
#include <xc.h>
#include "main.h"
#include "mcc_generated_files/mcc.h"
#include "modbus/Interrupts.h"
#include "modbus/PetitModbus.h"

#define MAILBOX_SIZE 4                                                          // How many messages are non-critical
#define BROADCAST_ADDRESS 0

void             SLAVExCOMMANDxHANDLER(uint16_t State);

/*#--------------------------------------------------------------------------#*/
/*  Description: InitSlaveCommunication(SLAVE_INFO *location)
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
static SLAVE_INFO   *MASTER_SLAVE_DATA = 0;                                     // Holds the address were the received slave data is stored
static SLAVE_INFO   SlaveInfoReadMask;                                          // Read mask for slave data from EhternetTarget
unsigned char       *pSlaveDataReceived, *pSlaveInfoReadMask;

void InitSlaveCommunication(SLAVE_INFO *location)                                  
{   
    MASTER_SLAVE_DATA  =  location;
    SlaveInfoReadMask.Header           = 0x00;
    SlaveInfoReadMask.SlaveNumber      = 0x00;                                  // Mask for data write to local MASTER_SLAVE_DATA from EthernetTarget
    SlaveInfoReadMask.HoldingReg[0]    = 0xFFFF;                                // only new setpoints/settings are allowed to be read which need to be written to modbus slaves
    SlaveInfoReadMask.HoldingReg[1]    = 0xFFFF;
    SlaveInfoReadMask.HoldingReg[2]    = 0xFFFF;
    SlaveInfoReadMask.HoldingReg[3]    = 0xFFFF;
    SlaveInfoReadMask.InputReg[0]      = 0x0000;
    SlaveInfoReadMask.InputReg[1]      = 0x0000;
    SlaveInfoReadMask.InputReg[2]      = 0x0000;
    SlaveInfoReadMask.InputReg[3]      = 0x0000;
    SlaveInfoReadMask.InputReg[4]      = 0x0000;
    SlaveInfoReadMask.InputReg[5]      = 0x0000;
    SlaveInfoReadMask.DiagReg[0]       = 0x0000;
    SlaveInfoReadMask.DiagReg[1]       = 0x0000;
    SlaveInfoReadMask.MbReceiveCounter = 0x0000;
    SlaveInfoReadMask.MbSentCounter    = 0x0000;
    SlaveInfoReadMask.MbCommError      = 0x0000;
    SlaveInfoReadMask.MbExceptionCode  = 0x00;
    SlaveInfoReadMask.SpiCommErrorCounter = 0x0000;
    SlaveInfoReadMask.Footer           = 0x00;
}

/*#--------------------------------------------------------------------------#*/
/*  Description: ProcessNextSlave()
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

typedef enum
{
    NrRegLo = 0,
    NrRegHi = 1,
    StAdLo  = 2,
    StAdHi  = 3     
};

typedef enum
{
    MESSAGE1 = 1,
    MESSAGE2 = 2,
    MESSAGE3 = 3,
    MESSAGE4 = 4 
};
unsigned char HoldingRegistersRead [4] = {0, 0, 0, 0};                          // {start address High, start address Low, number of registers High, number of registers Low}
unsigned char HoldingRegistersWrite[9] = {0, 0, 0, 0, 0, 0, 0, 0, 0};           // {start address High, start address Low, number of registers High, number of registers Low, byte count, Register Value Hi, Register Value Lo, Register Value Hi, Register Value Lo} 
unsigned char   InputRegistersRead [4] = {0, 0, 0, 0};
unsigned char    DiagRegistersRead [4] = {0, 0, 0, 0};

static unsigned char ProcessSlave = 1;
static unsigned int Mailbox = 1;
static unsigned int Message = MESSAGE1;

void ProcessNextSlave(){    
    
    if (ProcessSlave > (NUMBER_OF_SLAVES-1)){
        ProcessSlave = 0;
        
        Message++;
        if (Message > MESSAGE3){
            Message = MESSAGE1;
            
            Mailbox++;
            if (Mailbox > 4){
                Mailbox = 1;
            }
        }                
    }
    
    if(ProcessSlave == 0){
        SLAVExCOMMANDxHANDLER(true);        
    }
    else{
    
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
                SendPetitModbus(ProcessSlave, PETITMODBUS_WRITE_MULTIPLE_REGISTERS, HoldingRegistersWrite, 9);
                break;

            case MESSAGE2:
                InputRegistersRead[3]  = 2;
                InputRegistersRead[2]  = 0;
                InputRegistersRead[1]  = 0;
                InputRegistersRead[0]  = 0;
                SendPetitModbus(ProcessSlave, PETITMODBUS_READ_INPUT_REGISTERS, InputRegistersRead, 4);    
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
                        SendPetitModbus(ProcessSlave, PETITMODBUS_WRITE_MULTIPLE_REGISTERS, HoldingRegistersWrite, 9);
                        break;

                    case 2:
                        DiagRegistersRead[3]  = 2;
                        DiagRegistersRead[2]  = 0;
                        DiagRegistersRead[1]  = 0;
                        DiagRegistersRead[0]  = 0;
                        SendPetitModbus(ProcessSlave, PETITMODBUS_DIAGNOSTIC_REGISTERS, DiagRegistersRead, 4);
                        break;

                    case 3:
                        InputRegistersRead[3]  = 2;
                        InputRegistersRead[2]  = 0;
                        InputRegistersRead[1]  = 2;
                        InputRegistersRead[0]  = 0;
                        SendPetitModbus(ProcessSlave, PETITMODBUS_READ_INPUT_REGISTERS, InputRegistersRead, 4);  
                        break;

                    case 4:
                        InputRegistersRead[3]  = 2;
                        InputRegistersRead[2]  = 0;
                        InputRegistersRead[1]  = 4;
                        InputRegistersRead[0]  = 0;
                        SendPetitModbus(ProcessSlave, PETITMODBUS_READ_INPUT_REGISTERS, InputRegistersRead, 4);  
                        break;

                    default:
                        break;                
                }
                break;

            default :
                break;
        } 
    }
}

/*#--------------------------------------------------------------------------#*/
/*  Description: ProcessSlaveCommunication()
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
unsigned int ProcessSlaveCommunication(){
    
    unsigned int Return_Val = false;
    
    switch (MASTER_SLAVE_DATA[ProcessSlave].MbCommError){
        case SLAVE_DATA_BUSY:
            Return_Val = false;
            // count here how long the Mod-bus stack is busy, otherwise reset/action             
            break;
            
        case SLAVE_DATA_NOK:
            // count here how often the slave data is NOK, otherwise stop all slaves with broadcast
            MASTER_SLAVE_DATA[ProcessSlave].MbCommError = SLAVE_DATA_IDLE;            
            ProcessSlave++;         
            Return_Val = true;
            LED_ERR++;
            break;
            
        case SLAVE_DATA_OK:
            MASTER_SLAVE_DATA[ProcessSlave].MbCommError = SLAVE_DATA_IDLE;            
            ProcessSlave++;         
            Return_Val = true;
            break;            
            
        case SLAVE_DATA_TIMEOUT:
            // count here how often the slave data is timeout, otherwise stop all slaves with broadcast
            MASTER_SLAVE_DATA[ProcessSlave].MbCommError = SLAVE_DATA_IDLE;            
            ProcessSlave++;         
            Return_Val = true;
            LED_ERR++;
            break;
            
        case SLAVE_DATA_EXCEPTION:
            // count here how often the slave data is timeout, otherwise stop all slaves with broadcast
            MASTER_SLAVE_DATA[ProcessSlave].MbCommError = SLAVE_DATA_IDLE;            
            ProcessSlave++;         
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
/*  Description: SendDataToEthernet()
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
static unsigned int DataFromSlave = 0;
const unsigned char DATAxSTRUCTxLENGTH = sizeof(SLAVE_INFO) + 1;      // add one byte to send dummy
static unsigned char RECEIVEDxDATAxRAW[DATAxSTRUCTxLENGTH];                            // One dummy byte extra (SPI master will send extra byte to receive last byte from slave)
static uint8_t bytesWritten = 0;
static uint8_t *dataIn, *dataOut;

void SendDataToEthernet(){
    //modbus_sync_LAT = 1;
    //SPI1_Exchange8bitBuffer(&(MASTER_SLAVE_DATA[DataFromSlave].Header), 
    //        DATAxSTRUCTxLENGTH, &(RECEIVEDxDATAxRAW[0]));                       // SPI send/receive data    
    bytesWritten = 0;
    dataIn  = &(MASTER_SLAVE_DATA[DataFromSlave].Header);                       // Data to Ethernet Target
    dataOut = &(RECEIVEDxDATAxRAW[0]);                                          // Data from Ethernet Target
    SS1_LAT = 0;                                                                // Activate slave
    while(bytesWritten < DATAxSTRUCTxLENGTH){
        SSP1CON1bits.WCOL = 0;
        SSP1BUF = dataIn[bytesWritten];
        while(SSP1STATbits.BF == 0){
        }
        dataOut[bytesWritten] = SSP1BUF;
        bytesWritten++;
//        NOP();                                                                // give SPI slave time to process the data, NOP() is 3 cycles delay ~288ns
        __delay_us(1);
    }
    SS1_LAT = 1;   
    
    if(RECEIVEDxDATAxRAW[2] < NUMBER_OF_SLAVES && RECEIVEDxDATAxRAW[1]==0xAA && 
            RECEIVEDxDATAxRAW[DATAxSTRUCTxLENGTH-1]==0x55){                     // Check if received slave number is valid(during debugging sometimes wrong number received)
        pSlaveDataReceived = &(MASTER_SLAVE_DATA[RECEIVEDxDATAxRAW[2]].Header); // set the pointer to the first element of the received slave number in RECEIVEDxDATAxRAW[1](first element is dummy byte)    
        pSlaveInfoReadMask = &(SlaveInfoReadMask.Header);                       // set the pointer to the first element of the SlaveInfoReadMask
        for(char i = 1; i < DATAxSTRUCTxLENGTH-1; i++){
            if(*pSlaveInfoReadMask){
                *pSlaveDataReceived = RECEIVEDxDATAxRAW[i];                     // for DATAxSTRUCTxLENGTH set every byte into RECEIVEDxDATAxRAW array according to read mask
            }        
            pSlaveDataReceived += 1;                                            // Increment pointer
            pSlaveInfoReadMask += 1;                                            // Increment pointer        
        }   
    } 
    else{
        MASTER_SLAVE_DATA[0].SpiCommErrorCounter += 1;                          // Count error SPI messages
        LED_ERR++;
    }
    
    /*
     * send data from next slave
     */
    DataFromSlave++;                                                            
    
    /*
     * If init is not done yet, keep communicating data from master only
     */
    if (InitDone == false){
        DataFromSlave = 0;                                                      
    }
    
    /*
     * Check if all slaves are handled, if so reset counter
     */
    if (DataFromSlave > NUMBER_OF_SLAVES-1){
        DataFromSlave = 0;
    }
    //modbus_sync_LAT = 0;
}

/*#--------------------------------------------------------------------------#*/
/*  Description: SLAVExCOMMANDxHANDLER()
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
typedef enum
{
    OK          = 0x02, 
    NOK         = 0x04, 
    BUSY        = 0x01,
    IDLE        = 0x00,
    MODE        = 0x01,
    WRITE       = 0x02,
    HOLDINGREG  = 0x04,
    INPUTREG    = 0x08,
    DIAGREG     = 0x10,
    EXEC        = 0x20
};

unsigned int CommandMachine = 0;
unsigned char HoldingRegisterWrite[7] = {0, 0, 0, 1, 2, 0, 0};                  // {start address High, start address Low, 
                                                                                // number of registers High, number of registers Low, 
                                                                                // byte count, Register Value Hi, Register Value Lo} 
unsigned char HoldingRegisterRead[4] = {0, 0, 0, 1};                            // {start address High, start address Low, 
                                                                                // number of registers High, number of registers Low, 
static uint8_t AmpId = 0; 
static uint8_t RegId = 0;

void SLAVExCOMMANDxHANDLER (uint16_t State){
    
    switch(CommandMachine){
        case 0:
            if (MASTER_SLAVE_DATA[0].HoldingReg[0] & EXEC){                     // if execute is set

                if((MASTER_SLAVE_DATA[0].HoldingReg[0] & 0x1C) == HOLDINGREG){ 
                    if(MASTER_SLAVE_DATA[0].HoldingReg[0] & WRITE){             // if write is set
                        MASTER_SLAVE_DATA[0].InputReg[0] = BUSY;
                        HoldingRegisterWrite[1] = MASTER_SLAVE_DATA[0].HoldingReg[3];       // Register address to write to
                        HoldingRegisterWrite[6] = MASTER_SLAVE_DATA[0].HoldingReg[2];       // low char data
                        HoldingRegisterWrite[5] = MASTER_SLAVE_DATA[0].HoldingReg[2]>> 8;   // high char data
                        SendPetitModbus(MASTER_SLAVE_DATA[0].HoldingReg[1], PETITMODBUS_WRITE_MULTIPLE_REGISTERS, HoldingRegisterWrite, 7);
                        
                        if(MASTER_SLAVE_DATA[0].HoldingReg[1] == BROADCAST_ADDRESS){
                            ProcessSlave++;                                     // because this message is a broadcast, no timeout will be generated so increment ProcessSlave here!
                        }
                        CommandMachine = 40;
                    }
                    else{
                        MASTER_SLAVE_DATA[0].InputReg[0] = NOK;   
                        CommandMachine = 50;
                    }                                
                }
                else if((MASTER_SLAVE_DATA[0].HoldingReg[0] & 0x1C) == INPUTREG){
                    if(MASTER_SLAVE_DATA[0].HoldingReg[0] & WRITE){             // if write is set
                        MASTER_SLAVE_DATA[0].InputReg[0] = NOK;
                        CommandMachine = 50;
                    }
                    else{
                        MASTER_SLAVE_DATA[0].InputReg[0] = BUSY;
                        InputRegistersRead[3]  = MASTER_SLAVE_DATA[0].HoldingReg[2];    //2  // number of registers Low
                        InputRegistersRead[2]  = 0;//MASTER_SLAVE_DATA[0].HoldingReg[2]>>8; //0  // number of registers High
                        InputRegistersRead[1]  = MASTER_SLAVE_DATA[0].HoldingReg[3];    //4  // start address Low
                        InputRegistersRead[0]  = 0;//MASTER_SLAVE_DATA[0].HoldingReg[3]>>8; //0  // start address High
                        SendPetitModbus(MASTER_SLAVE_DATA[0].HoldingReg[1], PETITMODBUS_READ_INPUT_REGISTERS, InputRegistersRead, 4);  
                        AmpId = MASTER_SLAVE_DATA[0].HoldingReg[1];
                        RegId = MASTER_SLAVE_DATA[0].HoldingReg[3];
                        CommandMachine = 20;
                    }
                }
                else if((MASTER_SLAVE_DATA[0].HoldingReg[0] & 0x1C) == DIAGREG){
                    if(MASTER_SLAVE_DATA[0].HoldingReg[0] & WRITE){             // if write is set
                        MASTER_SLAVE_DATA[0].InputReg[0] = NOK;
                        CommandMachine = 50;
                    }
                    else{
                        MASTER_SLAVE_DATA[0].InputReg[0] = BUSY;
                        CommandMachine = 30;
                    }
                }                            
            }
            else if(State == true){                                             // Always send message during normal run operation to ensure triggering of slaves on received data     
                HoldingRegisterWrite[1] = 0;                                    // clear write registers, needed?
                HoldingRegisterWrite[6] = 0;
                HoldingRegisterWrite[5] = 0;                
                SendPetitModbus(BROADCAST_ADDRESS, PETITMODBUS_READ_HOLDING_REGISTERS, HoldingRegisterRead, 4);
                ProcessSlave++;                                                 // because this message is a broadcast on a read, no timeout will be generated so increment ProcessSlave here!
            }
            break;
    //----------------------------------------------------------------------------------------------------------------------//                              
        case 20:
            if(ProcessSlaveCommunication() == true){//              |--address of slave that responded--|---input register number that was read-----|
                MASTER_SLAVE_DATA[0].InputReg[1] = MASTER_SLAVE_DATA[AmpId].InputReg[RegId];
                AmpId = 0;
                RegId = 0;
                CommandMachine = 40;
            }
            break;
    //----------------------------------------------------------------------------------------------------------------------//

    //----------------------------------------------------------------------------------------------------------------------//

    //----------------------------------------------------------------------------------------------------------------------//                        
        case 40:
            MASTER_SLAVE_DATA[0].InputReg[0] = OK;
            CommandMachine = 50; 
            if(State == true){
                ProcessSlave++;                                                 // If the master is handling broadcast messages and processing through these states, a ProcessSlave++ is required to ensure the nextslave is handled
            }
            break;

        case 50:
            if ((MASTER_SLAVE_DATA[0].HoldingReg[0] & EXEC) == 0){              // Remove execute command before returning
                CommandMachine = 0; 
                MASTER_SLAVE_DATA[0].InputReg[0] = IDLE;                        // reset status register for readback of execution towards ethernet target
                MASTER_SLAVE_DATA[0].InputReg[1] = 0;                           // reset input register read value
            }
            if(State == true){
                ProcessSlave++;                                                 // If the master is handling broadcast messages and processing through these states, a ProcessSlave++ is required to ensure the nextslave is handled
            }
            break;

        default :
            break;
    }
}