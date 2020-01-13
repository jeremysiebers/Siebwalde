#include <xc.h>
#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdlib.h>
#include "mbus.h"
#include "slavecommhandler.h"
#include "enums.h"
#include "slavefwhandler.h"
#include "ethernet.h"
#include "slavebootloaderroutines.h"

uint32_t        FwFileDownload      (void);
uint32_t        ConfigWordDownload  (void);
uint32_t        FlashAllSlavesAuto  (void);
uint32_t        FlashSequencer      (uint8_t SlaveId);

static uint8_t      FwFile[SLAVE_FLASH_SIZE];                                   //[30720];
static uint8_t      FwConfigWord[14];
static uint16_t     checksum        = 0;
static uint32_t     DelayCount1     = 0;

/*#--------------------------------------------------------------------------#*/
/*  Description: INITxSLAVExFWxHANDLER(SLAVE_INFO *location)
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

void INITxSLAVExFWxHANDLER(SLAVE_INFO *location, SLAVE_INFO *Dump){
    MASTER_SLAVE_DATA  =  location;
    DUMP_SLAVE_DATA    =  Dump;
    fwData.fwchecksum  =  0;
    fwData.state       = FW_STATE_INIT;
}



/*#--------------------------------------------------------------------------#*/
/*  Description: bool SLAVExFWxHANDLER()
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

udpTrans_t      *EthernetRecvData;

bool SLAVExFWxHANDLER(){
    bool        return_val  = false;
    uint32_t    result      = 0;
    
    switch (fwData.state){
        
        case FW_STATE_INIT:
        {
            CREATExTASKxSTATUSxMESSAGE(FWHANDLER, FW_STATE_INIT, CONNECTED, DONE);
            SYS_MESSAGE("Fw handler\t: Started.\n\r");
            fwData.state = FW_STATE_WAITING_FOR_COMMAND;
            break;
        }
        
        case FW_STATE_WAITING_FOR_COMMAND:
        {
            if(CHECKxDATAxINxRECEIVExMAILxBOX()){
                EthernetRecvData = GETxDATAxFROMxRECEIVExMAILxBOX();

                if(EthernetRecvData->header == HEADER){
                    fwData.state   = FW_STATE_COMMAND_HANDLING;
                    fwData.command = EthernetRecvData->command;
                }
                else{
                    
                    fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                    SYS_MESSAGE("Fw handler\t: received wrong HEADER, stay in wait state.\n\r");
                    CREATExTASKxSTATUSxMESSAGE(FWHANDLER, FW_STATE_WAITING_FOR_COMMAND, RECEIVED_BAD_COMMAND, ERROR);
                }
            }
            break;
        }
        
        case FW_STATE_COMMAND_HANDLING:
        {
            switch (fwData.command){
                
                case EXEC_FW_STATE_RECEIVE_FW_FILE:
                {
                    result = FwFileDownload();
                    switch (result){
                        case DONE: fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                                    CREATExTASKxSTATUSxMESSAGE(
                                        FWHANDLER,                              // TASK_ID
                                        DONE,                                   // TASK_STATE
                                        EXEC_FW_STATE_RECEIVE_FW_FILE,          // TASK_COMMAND
                                        NONE);                                  // TASK_MESSAGE
                                    SYS_MESSAGE("Fw handler\t: EXEC_FW_STATE_RECEIVE_FW_FILE DONE.\n\r");
                                    break;
                        case ERROR: fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                                    CREATExTASKxSTATUSxMESSAGE(
                                        FWHANDLER,                              // TASK_ID
                                        ERROR,                                  // TASK_STATE
                                        EXEC_FW_STATE_RECEIVE_FW_FILE,          // TASK_COMMAND
                                        NONE);                                  // TASK_MESSAGE
                                    SYS_MESSAGE("Fw handler\t: EXEC_FW_STATE_RECEIVE_FW_FILE ERROR.\n\r");
                                    break;
                        case BUSY : break;
                        default   : SYS_MESSAGE("Fw handler\t: EXEC_FW_STATE_RECEIVE_FW_FILE default case not allowed.\n\r");
                    }
                    break;
                }
                
                case EXEC_FW_STATE_RECEIVE_CONFIG_WORD:
                {
                    result = ConfigWordDownload();
                    switch (result){
                        case DONE:  fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                                    CREATExTASKxSTATUSxMESSAGE(
                                        FWHANDLER,                              // TASK_ID
                                        DONE,                                   // TASK_STATE
                                        EXEC_FW_STATE_RECEIVE_CONFIG_WORD,      // TASK_COMMAND
                                        NONE);                                  // TASK_MESSAGE
                                    SYS_MESSAGE("Fw handler\t: EXEC_FW_STATE_RECEIVE_CONFIG_WORD DONE.\n\r");
                                    break;
                        case ERROR: fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                                    CREATExTASKxSTATUSxMESSAGE(
                                        FWHANDLER,                              // TASK_ID
                                        ERROR,                                  // TASK_STATE
                                        EXEC_FW_STATE_RECEIVE_CONFIG_WORD,      // TASK_COMMAND
                                        NONE);                                  // TASK_MESSAGE
                                    SYS_MESSAGE("Fw handler\t: EXEC_FW_STATE_RECEIVE_CONFIG_WORD ERROR.\n\r");
                                    break;
                        case BUSY : break;
                        default   : SYS_MESSAGE("Fw handler\t: EXEC_FW_STATE_RECEIVE_CONFIG_WORD default case not allowed.\n\r");
                    }                  
                    break;
                }
                
                case EXEC_FW_STATE_FLASH_SLAVES:
                {
                    result = FlashAllSlavesAuto();
                    switch (result){
                        case DONE:  fwData.state = FW_STATE_INIT;
                                    CREATExTASKxSTATUSxMESSAGE(
                                        FWHANDLER,                              // TASK_ID
                                        DONE,                                   // TASK_STATE
                                        EXEC_FW_STATE_FLASH_SLAVES       ,      // TASK_COMMAND
                                        NONE);                                  // TASK_MESSAGE
                                    SYS_MESSAGE("Fw handler\t: EXEC_FW_STATE_FLASH_SLAVES DONE.\n\r");
                                    return_val = true;
                                    break;
                        case ERROR: fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                                    CREATExTASKxSTATUSxMESSAGE(
                                        FWHANDLER,                              // TASK_ID
                                        ERROR,                                  // TASK_STATE
                                        EXEC_FW_STATE_FLASH_SLAVES,             // TASK_COMMAND
                                        NONE);                                  // TASK_MESSAGE
                                    SYS_MESSAGE("Fw handler\t: EXEC_FW_STATE_FLASH_SLAVES ERROR.\n\r");
                                    break;
                        case BUSY : break;
                        default   : SYS_MESSAGE("Fw handler\t: EXEC_FW_STATE_FLASH_SLAVES default case not allowed.\n\r");
                    }
                    break;
                }
                
                case CLIENT_CONNECTION_REQUEST:
                {                    
                    fwData.state = FW_STATE_INIT;
                    return_val = true;
                    break;
                }

                default:
                {
                    SYS_MESSAGE("Fw handler\t: received unknown command, stay in wait state.\n\r");
                    CREATExTASKxSTATUSxMESSAGE(FWHANDLER, FW_STATE_COMMAND_HANDLING, RECEIVED_UNKNOWN_COMMAND, ERROR);
                    fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                    break;
                }
            }
            break;
        }
        
        default:
        {
            fwData.state = FW_STATE_INIT;
            break;
        }
    }
    return (return_val); 
}

/*#--------------------------------------------------------------------------#*/
/*  Description: uint32_t FwFileDownload(TASK_ID task_id, TASK_COMMAND task_command)
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

uint8_t     iFwFileDownload = 0;
uint8_t     *ptr_FwData     = 0;
uint16_t    FwLineCount     = 0;

uint16_t    count           = 0;
uint16_t    limit           = 0;
uint8_t     data1           = 0;
uint8_t     data2           = 0;
uint8_t     *pFw1           = 0;
uint8_t     *pFw2           = 0;

uint32_t FwFileDownload(){
    
    uint32_t return_val = BUSY;
        
    switch (iFwFileDownload){
        
        case 0:
        {
            CREATExTASKxSTATUSxMESSAGE(
                        FWFILEDOWNLOAD,                                         // TASK_ID
                        BUSY,                                                   // TASK_STATE
                        FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY,             // TASK_COMMAND
                        DONE);                                                  // TASK_MESSAGE
            ptr_FwData = &FwFile[0];
            FwLineCount = 0;
            fwData.fwchecksum = 0;
            iFwFileDownload++;
            break;
        }
        
        case 1:
        {
            if(CHECKxDATAxINxRECEIVExMAILxBOX()){
                EthernetRecvData = GETxDATAxFROMxRECEIVExMAILxBOX();
                
                if(EthernetRecvData->command == FILEDOWNLOAD_STATE_FW_DATA_RECEIVE){
                    memcpy(ptr_FwData, &(EthernetRecvData->data), 64);
                    ptr_FwData  +=  64;
                    FwLineCount +=   4;
                                
                    if(FwLineCount > 1916){
                        iFwFileDownload++;
                        FwLineCount = 0;                        
                        CREATExTASKxSTATUSxMESSAGE(                                
                                FWFILEDOWNLOAD,                                 // TASK_ID
                                BUSY,                                           // TASK_STATE
                                FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE,       // TASK_COMMAND
                                DONE);                                          // TASK_MESSAGE
                        SYS_MESSAGE("Fw handler\t: EXEC_FW_STATE_RECEIVE_FW_FILE received a FW file.\n\r");
                    }
                    else{
                        CREATExTASKxSTATUSxMESSAGE(                                
                                FWFILEDOWNLOAD,                                 // TASK_ID
                                BUSY,                                           // TASK_STATE
                                FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY,     // TASK_COMMAND
                                DONE);                                          // TASK_MESSAGE
                    }
                }
                else{
                    SYS_MESSAGE("EXEC_FW_STATE_RECEIVE_FW_FILE_STANDBY received wrong command stopping FW download.\n\r");
                    iFwFileDownload = 0;
                    return_val = ERROR;
                    CREATExTASKxSTATUSxMESSAGE(                                
                            FWFILEDOWNLOAD,                                     // TASK_ID
                            ERROR,                                              // TASK_STATE
                            FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY,         // TASK_COMMAND
                            RECEIVED_WRONG_COMMAND);                            // TASK_MESSAGE
                }
            }
            break;
        }
        
        case 2:
        {
            pFw1 = &FwFile[0];
            pFw2 = &FwFile[1];
            limit = sizeof(FwFile)- 2;
            data1 = 0;
            data2 = 0;
            count = 0;
            checksum = 0;
            
            for (count = 0; count < limit; count = count + 2)
            {                
                checksum += (uint16_t)*pFw1;
                checksum += (uint16_t)*pFw2 << 8;
                pFw1 += 2;
                pFw2 += 2;                
            }            
            
            data1 = (uint8_t) (checksum & 0x00FF);
            data2 = (uint8_t)((checksum & 0xFF00) >> 8);
            
            if(data1 == *pFw1 && data2 == *pFw2){
                fwData.fwchecksum       = checksum;
                return_val              = DONE;
                CREATExTASKxSTATUSxMESSAGE(
                        FWFILEDOWNLOAD,                                         // TASK_ID
                        DONE,                                                   // TASK_STATE
                        FILEDOWNLOAD_STATE_FW_CHECKSUM,                         // TASK_COMMAND
                        RECEIVED_CHECKSUM_OK);                                  // TASK_MESSAGE
                SYS_MESSAGE("Fw handler\t: EXEC_FW_STATE_RECEIVE_FW_FILE checksum is OK.\n\r");
            }
            else{
                fwData.fwchecksum       = 0;
                return_val              = ERROR;
                CREATExTASKxSTATUSxMESSAGE(
                        FWFILEDOWNLOAD,                                         // TASK_ID
                        ERROR,                                                  // TASK_STATE
                        FILEDOWNLOAD_STATE_FW_CHECKSUM,                         // TASK_COMMAND
                        RECEIVED_CHECKSUM_NOK);                                 // TASK_MESSAGE
                SYS_MESSAGE("Fw handler\t: EXEC_FW_STATE_RECEIVE_FW_FILE checksum is NOK.\n\r");
            }
            iFwFileDownload = 0;
            break;
        }
        
        default:
        {
            iFwFileDownload = 0;
            break;
        }
    }
    return (return_val);
}

/*#--------------------------------------------------------------------------#*/
/*  Description: bool FwFileDownload()
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

uint8_t     iConfigWordDownload = 0;

uint32_t   ConfigWordDownload  (){
    
    uint32_t return_val = BUSY;
    
    switch (iConfigWordDownload){
        
        case 0:
        {
            //CREATExTASKxSTATUSxMESSAGE((uint8_t)FWHANDLER, (uint8_t)EXEC_FW_STATE_RECEIVE_CONFIG_WORD_STANDBY, (uint8_t)DONE);
            CREATExTASKxSTATUSxMESSAGE(
                        FWCONFIGWORDDOWNLOAD,                                   // TASK_ID
                        BUSY,                                                   // TASK_STATE
                        CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY,   // TASK_COMMAND
                        DONE);                                                  // TASK_MESSAGE
            ptr_FwData = &FwConfigWord[0];
            iConfigWordDownload++;
            break;
        }
		
        case 1:
        {
            if(CHECKxDATAxINxRECEIVExMAILxBOX()){
                EthernetRecvData = GETxDATAxFROMxRECEIVExMAILxBOX();
                
                if(EthernetRecvData->command == CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE){
                    memcpy(ptr_FwData, &(EthernetRecvData->data), 14);
                    //CREATExTASKxSTATUSxMESSAGE((uint8_t)FWHANDLER, (uint8_t)EXEC_FW_STATE_CONFIG_DATA_DOWNLOAD_DONE, (uint8_t)DONE);
                    CREATExTASKxSTATUSxMESSAGE(
                        FWCONFIGWORDDOWNLOAD,                                   // TASK_ID
                        DONE,                                                   // TASK_STATE
                        CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE,  // TASK_COMMAND
                        DONE);                                                  // TASK_MESSAGE
                    SYS_MESSAGE("Fw handler\t: EXEC_FW_STATE_RECEIVE_CONFIG_WORD received config data.\n\r");
                }
                else{
                    CREATExTASKxSTATUSxMESSAGE(
                        FWCONFIGWORDDOWNLOAD,                                   // TASK_ID
                        ERROR,                                                  // TASK_STATE
                        CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE,  // TASK_COMMAND
                        RECEIVED_WRONG_COMMAND);                                // TASK_MESSAGE
                    SYS_MESSAGE("EXEC_FW_STATE_RECEIVE_CONFIG_WORD_STANDBY received wrong command stopping FW Handler.\n\r");
                }
                iConfigWordDownload = 0;
            }
            break;
        }
        
        default:
        {
            iConfigWordDownload     = 0;
            break;
        }
    }
    return (return_val);
}

/*#--------------------------------------------------------------------------#*/
/*  Description: bool FlashAllSlavesAuto()
 *
 *  Input(s)   : 
 *
 *  Output(s)  : 
 *
 *  Returns    : Init done
 *
 *  Pre.Cond.  : Checksum calculated from new FW file
 *
 *  Post.Cond. :
 *
 *  Notes      :  
 */
/*#--------------------------------------------------------------------------#*/

static uint8_t iFlashSlaves    = 0;
static uint8_t SlaveId1        = 1;

uint32_t FlashAllSlavesAuto(){
    uint32_t return_val = BUSY;
    uint32_t result = 0;
    
    switch(iFlashSlaves){
        case 0:
        {
            if(fwData.fwchecksum > 0){
                iFlashSlaves++;
            }
            else{
                CREATExTASKxSTATUSxMESSAGE(
                        FWFLASHSLAVES,                                          // TASK_ID
                        ERROR,                                                  // TASK_STATE
                        FWFLASHSLAVES_STATE_CHECK_CHECKSUM,                     // TASK_COMMAND
                        RECEIVED_CHECKSUM_NOK);                                 // TASK_MESSAGE
                SYS_MESSAGE("Fw handler\t: FlashSlaves checksum is empty, stopping FW flash.\n\r");
                return_val = ERROR;
            }
            break;
        }
        
        case 1:
        {
            if (SlaveId1 > NUMBER_OF_AMPLIFIERS){
                SlaveId1        = 1;
                iFlashSlaves    = 3;                    
            }
            else{
                if((MASTER_SLAVE_DATA[SlaveId1].SlaveDetected == true) && (MASTER_SLAVE_DATA[SlaveId1].HoldingReg[11] != fwData.fwchecksum)){
                    SYS_PRINT("Fw handler\t: EXEC_FW_STATE_FLASH_SLAVES Start flash sequence for SlaveID %d.\n\r", SlaveId1);
                    iFlashSlaves++;
                }
                else{
                    SlaveId1++;
                }
                break;
            }
        }
        
        case 2:
        {
            result = FlashSequencer(SlaveId1);
            switch (result){
                case DONE:  CREATExTASKxSTATUSxMESSAGE(
                                FWFLASHSEQUENCER,                       // TASK_ID
                                DONE,                                   // TASK_STATE
                                FWFLASHSEQUENCER_STATE_FLASHED_SLAVE,   // TASK_COMMAND
                                NONE);                                  // TASK_MESSAGE
                            SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_FLASHED_SLAVE DONE.\n\r");
                            SlaveId1++;
                            iFlashSlaves = 1;
                            break;
                case ERROR: CREATExTASKxSTATUSxMESSAGE(
                                FWFLASHSEQUENCER,                       // TASK_ID
                                ERROR,                                  // TASK_STATE
                                FWFLASHSEQUENCER_STATE_FLASHED_SLAVE,   // TASK_COMMAND
                                NONE);                                  // TASK_MESSAGE
                            SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_FLASHED_SLAVE ERROR.\n\r");
                            iFlashSlaves    = 0;
                            return_val      = ERROR;
                            break;
                case BUSY : break;
                default   : SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_FLASHED_SLAVE default case not allowed.\n\r");
            }
            break;
        }
        
        case 3:
        {
            Slaves_Disable_On();
            DelayCount1 = READxCORExTIMER();
            iFlashSlaves = 4;
            break;
        }
        
        case 4:
        {
            if((READxCORExTIMER() - DelayCount1) > (10 * MILISECONDS)){
                iFlashSlaves = 5;
            }  
            break;
        }
        
        case 5:
        {
            Slaves_Disable_Off();
            DelayCount1 = READxCORExTIMER();
            iFlashSlaves = 6;
            break;
        }
        
        case 6:
        {
            if((READxCORExTIMER() - DelayCount1) > (50 * MILISECONDS)){
                iFlashSlaves    = 0;
                return_val      = DONE;                
            }  
            break;
        }
        
        default:
        {
            break;
        }
    }
    
    return (return_val);
}

/*#--------------------------------------------------------------------------#*/
/*  Description: bool FlashSequencer(uint8_t SlaveId)
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

static uint8_t  iFlashSequencer = 0;
static uint8_t  BackplaneId     = 0;
static uint8_t  PrevBackplaneId = 0;
static uint8_t  ShiftSlot1      = 0;
static uint8_t  GoToCase        = 0;
static uint16_t WaitCounter     = 0;

uint32_t FlashSequencer(uint8_t Slave){
    uint32_t return_val = BUSY;
    uint32_t result = 0;
    
    switch(iFlashSequencer){
        case 0:
        {
            Slaves_Disable_On();
            DelayCount1 = READxCORExTIMER();
            iFlashSequencer = 1;
            break;
        }
        
        case 1:
        {
            if((READxCORExTIMER() - DelayCount1) > (10 * MILISECONDS)){
                iFlashSequencer = 2;
            }  
            break;
        }
        
        case 2:
        {
            Slaves_Disable_Off();
            DelayCount1 = READxCORExTIMER();
            iFlashSequencer = 3;
            break;
        }
        
        case 3:
        {
            if((READxCORExTIMER() - DelayCount1) > (10 * MILISECONDS)){
                iFlashSequencer = 4;
            }  
            break;
        }
        
        case 4:                                                                 // select first a slave amplifier by selecting one via a backplane slave
        {
            if(     Slave > 0  && Slave < 11){
                BackplaneId = 51;
                ShiftSlot1  = Slave - 1;}
            else if(Slave > 10 && Slave < 21){
                BackplaneId = 52;
                ShiftSlot1  = Slave - 11;}
            else if(Slave > 20 && Slave < 31){
                BackplaneId = 53;
                ShiftSlot1  = Slave - 21;}
            else if(Slave > 30 && Slave < 41){
                BackplaneId = 54;
                ShiftSlot1  = Slave - 31;}
            else if(Slave > 40 && Slave < 51){
                BackplaneId = 55;
                ShiftSlot1  = Slave - 41;}
            
            Data.SlaveAddress  = BackplaneId;
            Data.Direction     = WRITE;
            Data.NoOfRegisters = 1;
            Data.StartRegister = HOLDINGREG0;
            Data.RegData0      = (SLOT1 << ShiftSlot1);
            SLAVExCOMMUNICATIONxHANDLER();           
            DelayCount1 = READxCORExTIMER();
            iFlashSequencer++;
            break;
        }
        
        /* Verify communication was OK with backplane --> due to reset of all 
         * slaves the rx line is hampered and the feedback of modbus from the 
         * backplane slave is missed, therefore for now selecting a slave for
         * bootloader is done blind. To solve this, the routine has to be 
         * executed without resetting all slaves but a slave has to be commanded
         * to reset itself by SW reset in order to invoke the bootloader. */
        
        case 5:
        {
            if((READxCORExTIMER() - DelayCount1) > (50 * MILISECONDS)){
                iFlashSequencer = 7;
                fwData.SlaveBootloaderHandlingActive = true;
                MASTER_SLAVE_DATA[BackplaneId].MbCommError = SLAVE_DATA_OK;     // since the slave answer is lost???
            }            
            break;
        }
            
        case 6:
        {
            result = GETxBOOTxLOADERxVERSION();
            switch (result){
                case DONE:  CREATExTASKxSTATUSxMESSAGE(
                                FWFLASHSEQUENCER,                               // TASK_ID
                                DONE,                                           // TASK_STATE
                                FWFLASHSEQUENCER_STATE_GET_BOOTLOADER_VERSION,  // TASK_COMMAND
                                NONE);                                          // TASK_MESSAGE
                            SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_GET_BOOTLOADER_VERSION DONE.\n\r");
                            iFlashSequencer++;
                            break;
                case ERROR: CREATExTASKxSTATUSxMESSAGE(
                                FWFLASHSEQUENCER,                               // TASK_ID
                                ERROR,                                          // TASK_STATE
                                FWFLASHSEQUENCER_STATE_GET_BOOTLOADER_VERSION,  // TASK_COMMAND
                                NONE);                                          // TASK_MESSAGE
                            SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_GET_BOOTLOADER_VERSION ERROR.\n\r");
                            iFlashSequencer = 0;
                            return_val      = ERROR;
                            break;
                case BUSY : break;
                default   : SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_GET_BOOTLOADER_VERSION default case not allowed.\n\r");
            }
            break;
        }
        
        case 7:
        {
            result = ERASExFLASH(SLAVE_BOOT_LOADER_OFFSET, SLAVE_FLASH_END);
            switch (result){
                case DONE:  CREATExTASKxSTATUSxMESSAGE(
                                FWFLASHSEQUENCER,                               // TASK_ID
                                DONE,                                           // TASK_STATE
                                FWFLASHSEQUENCER_STATE_ERASE_FLASH,             // TASK_COMMAND
                                NONE);                                          // TASK_MESSAGE
                            SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_ERASE_FLASH DONE.\n\r");
                            iFlashSequencer++;
                            break;
                case ERROR: CREATExTASKxSTATUSxMESSAGE(
                                FWFLASHSEQUENCER,                               // TASK_ID
                                ERROR,                                          // TASK_STATE
                                FWFLASHSEQUENCER_STATE_ERASE_FLASH,             // TASK_COMMAND
                                NONE);                                          // TASK_MESSAGE
                            SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_ERASE_FLASH ERROR.\n\r");
                            iFlashSequencer = 0;
                            return_val      = ERROR;
                            break;
                case BUSY : break;
                default   : SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_ERASE_FLASH default case not allowed.\n\r");
            }            
            break;
        }
        
        case 8:
        {
            result = WRITExFLASH(SLAVE_BOOT_LOADER_OFFSET, SLAVE_FLASH_END, &FwFile[0]);
            switch (result){
                case DONE:  CREATExTASKxSTATUSxMESSAGE(
                                FWFLASHSEQUENCER,                               // TASK_ID
                                DONE,                                           // TASK_STATE
                                FWFLASHSEQUENCER_STATE_WRITE_FLASH,             // TASK_COMMAND
                                NONE);                                          // TASK_MESSAGE
                            SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_WRITE_FLASH DONE.\n\r");
                            iFlashSequencer++;
                            break;
                case ERROR: CREATExTASKxSTATUSxMESSAGE(
                                FWFLASHSEQUENCER,                               // TASK_ID
                                ERROR,                                          // TASK_STATE
                                FWFLASHSEQUENCER_STATE_WRITE_FLASH,             // TASK_COMMAND
                                NONE);                                          // TASK_MESSAGE
                            SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_WRITE_FLASH ERROR.\n\r");
                            iFlashSequencer = 0;
                            return_val      = ERROR;
                            break;
                case BUSY : break;
                default   : SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_WRITE_FLASH default case not allowed.\n\r");
            }
            break;
        }
		
		case 9:
        {
            result = WRITExCONFIG(&FwConfigWord[0]);
            switch (result){
                case DONE:  CREATExTASKxSTATUSxMESSAGE(
                                FWFLASHSEQUENCER,                               // TASK_ID
                                DONE,                                           // TASK_STATE
                                FWFLASHSEQUENCER_STATE_WRITE_CONFIG,            // TASK_COMMAND
                                NONE);                                          // TASK_MESSAGE
                            SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_WRITE_CONFIG DONE.\n\r");
                            iFlashSequencer++;
                            break;
                case ERROR: CREATExTASKxSTATUSxMESSAGE(
                                FWFLASHSEQUENCER,                               // TASK_ID
                                ERROR,                                          // TASK_STATE
                                FWFLASHSEQUENCER_STATE_WRITE_CONFIG,            // TASK_COMMAND
                                NONE);                                          // TASK_MESSAGE
                            SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_WRITE_CONFIG ERROR.\n\r");
                            iFlashSequencer = 0;
                            return_val      = ERROR;
                            break;
                case BUSY : break;
                default   : SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_WRITE_CONFIG default case not allowed.\n\r");
            }
            break;
        }
		
		case 10:
        {
            result = CHECKxCHECKSUM(SLAVE_BOOT_LOADER_OFFSET, SLAVE_FLASH_END, checksum);
            switch (result){
                case DONE:  CREATExTASKxSTATUSxMESSAGE(
                                FWFLASHSEQUENCER,                               // TASK_ID
                                DONE,                                           // TASK_STATE
                                FWFLASHSEQUENCER_STATE_CHECK_CHECKSUM,          // TASK_COMMAND
                                NONE);                                          // TASK_MESSAGE
                            SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_CHECK_CHECKSUM DONE.\n\r");
                            fwData.SlaveBootloaderHandlingActive = false;
                            iFlashSequencer++;
                            break;
                case ERROR: CREATExTASKxSTATUSxMESSAGE(
                                FWFLASHSEQUENCER,                               // TASK_ID
                                ERROR,                                          // TASK_STATE
                                FWFLASHSEQUENCER_STATE_CHECK_CHECKSUM,          // TASK_COMMAND
                                NONE);                                          // TASK_MESSAGE
                            SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_CHECK_CHECKSUM ERROR.\n\r");
                            iFlashSequencer = 0;
                            return_val      = ERROR;
                            break;
                case BUSY : break;
                default   : SYS_MESSAGE("Fw handler\t: FWFLASHSEQUENCER_STATE_CHECK_CHECKSUM default case not allowed.\n\r");
            }
            break;
        }
		
		case 11:
		{
			Data.SlaveAddress  = BackplaneId;
            Data.Direction     = WRITE;
            Data.NoOfRegisters = 1;
            Data.StartRegister = HOLDINGREG0;
            Data.RegData0      = 0;
            SLAVExCOMMUNICATIONxHANDLER();          
            DelayCount1 = READxCORExTIMER();
            iFlashSequencer++;
            break;
		}
		
		case 12:
        {
            if((READxCORExTIMER() - DelayCount1) > (10 * MILISECONDS)){
                MASTER_SLAVE_DATA[BackplaneId].MbCommError = SLAVE_DATA_OK;     // since the slave answer is lost???
                iFlashSequencer = 0;
                return_val = DONE;
            }            
            break;
        }
        
        case WAIT:
        {
            WaitCounter++;
            if (WaitCounter > WAIT_TIME){
                WaitCounter = 0;
                iFlashSequencer = GoToCase;
            } 
        }
        
        default:
        {
            break;
        }
    }
    
    return (return_val);
}