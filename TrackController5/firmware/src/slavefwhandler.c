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
uint32_t        SelectSlave         (uint8_t SlaveId);

static uint8_t      FwFile[SLAVE_FLASH_SIZE];                                   //[30720];
static uint8_t      FwConfigWord[14];
static uint16_t     checksum        = 0;
static uint32_t     DelayCount1     = 0;
static uint32_t     DelayCount2     = 0;

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
            CREATExTASKxSTATUSxMESSAGE(FWHANDLER, FWHANDLERINIT, CONNECTED, NONE);
            LOG_Push("Fw handler\t: FW_STATE_INIT done.");
            fwData.state = FW_STATE_WAITING_FOR_COMMAND;
            break;
        }
        
        case FW_STATE_WAITING_FOR_COMMAND:
        {
            if(CHECKxDATAxINxRECEIVExMAILxBOX()){
                EthernetRecvData = GETxDATAxFROMxRECEIVExMAILxBOX();

                if(EthernetRecvData->header == HEADER){
                    fwData.state    = FW_STATE_COMMAND_HANDLING;
                    fwData.command  = EthernetRecvData->command;
                    fwData.data     = EthernetRecvData->data[0];
                }
                else{                    
                    fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                    LOG_Push("Fw handler\t: received wrong HEADER, stay in wait state.");
                    CREATExTASKxSTATUSxMESSAGE(
                            FWHANDLER, 
                            FW_STATE_WAITING_FOR_COMMAND, 
                            RECEIVED_BAD_COMMAND, 
                            ERROR);
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
                                        EXEC_FW_STATE_RECEIVE_FW_FILE,          // TASK_COMMAND
                                        DONE,                                   // TASK_STATE
                                        NONE);                                  // TASK_MESSAGE
                                    LOG_Push("Fw handler\t: EXEC_FW_STATE_RECEIVE_FW_FILE DONE.");
                                    break;
                        case ERROR: fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                                    CREATExTASKxSTATUSxMESSAGE(
                                        FWHANDLER,                              // TASK_ID
                                        EXEC_FW_STATE_RECEIVE_FW_FILE,          // TASK_COMMAND
                                        ERROR,                                  // TASK_STATE
                                        NONE);                                  // TASK_MESSAGE
                                    LOG_Push("Fw handler\t: EXEC_FW_STATE_RECEIVE_FW_FILE ERROR.");
                                    break;
                        case BUSY : break;
                        default   : LOG_Push("Fw handler\t: EXEC_FW_STATE_RECEIVE_FW_FILE default case not allowed.");
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
                                        EXEC_FW_STATE_RECEIVE_CONFIG_WORD,      // TASK_COMMAND
                                        DONE,                                   // TASK_STATE
                                        NONE);                                  // TASK_MESSAGE
                                    LOG_Push("Fw handler\t: EXEC_FW_STATE_RECEIVE_CONFIG_WORD DONE.");
                                    break;
                        case ERROR: fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                                    CREATExTASKxSTATUSxMESSAGE(
                                        FWHANDLER,                              // TASK_ID
                                        EXEC_FW_STATE_RECEIVE_CONFIG_WORD,      // TASK_COMMAND
                                        ERROR,                                  // TASK_STATE
                                        NONE);                                  // TASK_MESSAGE
                                    LOG_Push("Fw handler\t: EXEC_FW_STATE_RECEIVE_CONFIG_WORD ERROR.");
                                    break;
                        case BUSY : break;
                        default   : LOG_Push("Fw handler\t: EXEC_FW_STATE_RECEIVE_CONFIG_WORD default case not allowed.");
                    }                  
                    break;
                }
                
                case EXEC_FW_STATE_FLASH_ALL_SLAVES:
                {
                    result = FlashAllSlavesAuto();
                    switch (result){
                        case DONE:  fwData.state = FW_STATE_INIT;
                                    fwData.SlaveBootloaderHandlingActive = false;
                                    CREATExTASKxSTATUSxMESSAGE(
                                        FWHANDLER,                              // TASK_ID
                                        EXEC_FW_STATE_FLASH_ALL_SLAVES,        // TASK_COMMAND
                                        DONE,                                   // TASK_STATE
                                        NONE);                                  // TASK_MESSAGE
                                    LOG_Push("Fw handler\t: EXEC_FW_STATE_FLASH_ALL_SLAVES DONE.");
                                    return_val = true;
                                    break;
                        case ERROR: fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                                    fwData.SlaveBootloaderHandlingActive = false;
                                    CREATExTASKxSTATUSxMESSAGE(
                                        FWHANDLER,                              // TASK_ID
                                        EXEC_FW_STATE_FLASH_ALL_SLAVES,         // TASK_COMMAND
                                        ERROR,                                  // TASK_STATE
                                        NONE);                                  // TASK_MESSAGE
                                    LOG_Push("Fw handler\t: EXEC_FW_STATE_FLASH_ALL_SLAVES ERROR.");
                                    break;
                        case BUSY : break;
                        default   : LOG_Push("Fw handler\t: EXEC_FW_STATE_FLASH_ALL_SLAVES default case not allowed.");
                    }
                    break;
                }
                
                case EXEC_FW_STATE_SELECT_SLAVE:
                {
                    result = SelectSlave(fwData.data);
                    switch (result){
                        case DONE:
                        {
                            LOG_Push("Fw handler\t: EXEC_FW_STATE_SELECT_SLAVE DONE.");
                            fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                            break;
                        }
                        case ERROR: 
                        {
                            LOG_Push("Fw handler\t: EXEC_FW_STATE_SELECT_SLAVE ERROR.");
                            fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                            break;
                        }
                        case BUSY : {break;}
                        default   : {LOG_Push("Fw handler\t: EXEC_FW_STATE_SELECT_SLAVE default case not allowed.");}
                    }
                    break;
                }
                
                case EXEC_FW_STATE_GET_BOOTLOADER_VERSION:
                {
                    fwData.SlaveBootloaderHandlingActive = true;                // disable modbus
                    result = GETxBOOTxLOADERxVERSION();
                    switch (result){
                        case DONE:
                        {
                            LOG_Push("Fw handler\t: EXEC_FW_STATE_GET_BOOTLOADER_VERSION DONE.");
                            fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                            fwData.SlaveBootloaderHandlingActive = false;        // enable modbus
                            break;
                        }
                        case ERROR: 
                        {
                            LOG_Push("Fw handler\t: EXEC_FW_STATE_GET_BOOTLOADER_VERSION ERROR.");
                            fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                            fwData.SlaveBootloaderHandlingActive = false;        // enable modbus
                            break;
                        }
                        case BUSY : {break;}
                        default   : {LOG_Push("Fw handler\t: EXEC_FW_STATE_GET_BOOTLOADER_VERSION default case not allowed.");}
                    }
                    break;
                }
                
                case EXEC_FW_STATE_ERASE_FLASH:
                {
                    fwData.SlaveBootloaderHandlingActive = true;                // disable modbus
                    result = ERASExFLASH(SLAVE_BOOT_LOADER_OFFSET, SLAVE_FLASH_END);
                    switch (result){
                        case DONE:
                        {
                            LOG_Push("Fw handler\t: EXEC_FW_STATE_ERASE_FLASH DONE.");
                            fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                            fwData.SlaveBootloaderHandlingActive = false;        // enable modbus
                            break;
                        }
                        case ERROR: 
                        {
                            LOG_Push("Fw handler\t: EXEC_FW_STATE_ERASE_FLASH ERROR.");
                            fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                            fwData.SlaveBootloaderHandlingActive = false;        // enable modbus
                            break;
                        }
                        case BUSY : {break;}
                        default   : {LOG_Push("Fw handler\t: EXEC_FW_STATE_ERASE_FLASH default case not allowed.");}
                    }
                    break;
                }
                
                case EXEC_FW_STATE_WRITE_FLASH:
                {
                    fwData.SlaveBootloaderHandlingActive = true;                // disable modbus
                    result = WRITExFLASH(SLAVE_BOOT_LOADER_OFFSET, SLAVE_FLASH_END, &FwFile[0]);
                    switch (result){
                        case DONE:
                        {
                            LOG_Push("Fw handler\t: EXEC_FW_STATE_WRITE_FLASH DONE.");
                            fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                            fwData.SlaveBootloaderHandlingActive = false;        // enable modbus
                            break;
                        }
                        case ERROR: 
                        {
                            LOG_Push("Fw handler\t: EXEC_FW_STATE_WRITE_FLASH ERROR.");
                            fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                            fwData.SlaveBootloaderHandlingActive = false;        // enable modbus
                            break;
                        }
                        case BUSY : {break;}
                        default   : {LOG_Push("Fw handler\t: EXEC_FW_STATE_WRITE_FLASH default case not allowed.");}
                    }
                    break;
                }
                
                case EXEC_FW_STATE_WRITE_CONFIG:
                {
                    fwData.SlaveBootloaderHandlingActive = true;                // disable modbus
                    result = WRITExCONFIG(&FwConfigWord[0]);
                    switch (result){
                        case DONE:
                        {
                            LOG_Push("Fw handler\t: EXEC_FW_STATE_WRITE_CONFIG DONE.");
                            fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                            fwData.SlaveBootloaderHandlingActive = false;        // enable modbus
                            break;
                        }
                        case ERROR: 
                        {
                            LOG_Push("Fw handler\t: EXEC_FW_STATE_WRITE_CONFIG ERROR.");
                            fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                            fwData.SlaveBootloaderHandlingActive = false;        // enable modbus
                            break;
                        }
                        case BUSY : {break;}
                        default   : {LOG_Push("Fw handler\t: EXEC_FW_STATE_WRITE_CONFIG default case not allowed.");}
                    }
                    break;
                }
                
                case EXEC_FW_STATE_CHECK_CHECKSUM:
                {
                    fwData.SlaveBootloaderHandlingActive = true;                // disable modbus
                    result = CHECKxCHECKSUM(SLAVE_BOOT_LOADER_OFFSET, SLAVE_FLASH_END, checksum);
                    switch (result){
                        case DONE:
                        {
                            LOG_Push("Fw handler\t: EXEC_FW_STATE_CHECK_CHECKSUM DONE.");
                            fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                            fwData.SlaveBootloaderHandlingActive = false;        // enable modbus
                            break;
                        }
                        case ERROR: 
                        {
                            LOG_Push("Fw handler\t: EXEC_FW_STATE_CHECK_CHECKSUM ERROR.");
                            fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                            fwData.SlaveBootloaderHandlingActive = false;        // enable modbus
                            break;
                        }
                        case BUSY : {break;}
                        default   : {LOG_Push("Fw handler\t: EXEC_FW_STATE_CHECK_CHECKSUM default case not allowed.");}
                    }
                    break;
                }
                
                case EXEC_FW_STATE_SLAVE_RESET:
                {
                    fwData.SlaveBootloaderHandlingActive = true;                // disable modbus
                    result = RESETxSLAVE();
                    switch (result){
                        case DONE:
                        {
                            LOG_Push("Fw handler\t: EXEC_FW_STATE_SLAVE_RESET DONE.");
                            fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                            fwData.SlaveBootloaderHandlingActive = false;        // enable modbus
                            break;
                        }
                        case ERROR: 
                        {
                            LOG_Push("Fw handler\t: EXEC_FW_STATE_SLAVE_RESET ERROR.");
                            fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                            fwData.SlaveBootloaderHandlingActive = false;        // enable modbus
                            break;
                        }
                        case BUSY : {break;}
                        default   : {LOG_Push("Fw handler\t: EXEC_FW_STATE_SLAVE_RESET default case not allowed.");}
                    }
                    break;
                }
                
                case EXIT_SLAVExFWxHANDLER:
                {
                    fwData.SlaveBootloaderHandlingActive = false;               // enable modbus
                    fwData.state = FW_STATE_INIT;
                    return_val = true;
                    break;
                }
                                
                case CLIENT_CONNECTION_REQUEST:
                {
                    fwData.SlaveBootloaderHandlingActive = false;               // enable modbus                    
                    fwData.state = FW_STATE_INIT;
                    return_val = true;
                    break;
                }

                default:
                {
                    LOG_Push("Fw handler\t: received unknown command, stay in wait state.");
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
                        FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY,             // TASK_COMMAND
                        DONE,                                                   // TASK_STATE
                        NONE);                                                  // TASK_MESSAGE
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
                                FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE,       // TASK_COMMAND
                                DONE,                                           // TASK_STATE
                                NONE);                                          // TASK_MESSAGE
                        LOG_Push("Fw handler\t: EXEC_FW_STATE_RECEIVE_FW_FILE received a FW file.");
                    }
                    else{
                        CREATExTASKxSTATUSxMESSAGE(                                
                                FWFILEDOWNLOAD,                                 // TASK_ID
                                FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY,     // TASK_COMMAND
                                DONE,                                           // TASK_STATE
                                NONE);                                          // TASK_MESSAGE
                    }
                }
                else{
                    LOG_Push("EXEC_FW_STATE_RECEIVE_FW_FILE_STANDBY received wrong command stopping FW download.");
                    iFwFileDownload = 0;
                    return_val = ERROR;
                    CREATExTASKxSTATUSxMESSAGE(                                
                            FWFILEDOWNLOAD,                                     // TASK_ID
                            FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY,         // TASK_COMMAND
                            ERROR,                                              // TASK_STATE
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
                        FILEDOWNLOAD_STATE_FW_CHECKSUM,                         // TASK_COMMAND
                        DONE,                                                   // TASK_STATE
                        RECEIVED_CHECKSUM_OK);                                  // TASK_MESSAGE
                LOG_Push("Fw handler\t: EXEC_FW_STATE_RECEIVE_FW_FILE checksum is OK.");
            }
            else{
                fwData.fwchecksum       = 0;
                return_val              = ERROR;
                CREATExTASKxSTATUSxMESSAGE(
                        FWFILEDOWNLOAD,                                         // TASK_ID
                        FILEDOWNLOAD_STATE_FW_CHECKSUM,                         // TASK_COMMAND
                        ERROR,                                                  // TASK_STATE
                        RECEIVED_CHECKSUM_NOK);                                 // TASK_MESSAGE
                LOG_Push("Fw handler\t: EXEC_FW_STATE_RECEIVE_FW_FILE checksum is NOK.");
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
                        CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY,   // TASK_COMMAND
                        DONE,                                                   // TASK_STATE
                        NONE);                                                  // TASK_MESSAGE
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
                        CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE,  // TASK_COMMAND
                        DONE,                                                   // TASK_STATE
                        NONE);                                                  // TASK_MESSAGE
                    LOG_Push("Fw handler\t: EXEC_FW_STATE_RECEIVE_CONFIG_WORD received config data.");
                    return_val = DONE;
                }
                else{
                    CREATExTASKxSTATUSxMESSAGE(
                        FWCONFIGWORDDOWNLOAD,                                   // TASK_ID
                        CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE,  // TASK_COMMAND
                        ERROR,                                                  // TASK_STATE
                        RECEIVED_WRONG_COMMAND);                                // TASK_MESSAGE
                    LOG_Push("EXEC_FW_STATE_RECEIVE_CONFIG_WORD_STANDBY received wrong command stopping FW Handler.");
                    return_val = ERROR;
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
                        FWFLASHSLAVES_STATE_CHECK_CHECKSUM,                     // TASK_COMMAND
                        ERROR,                                                  // TASK_STATE
                        RECEIVED_CHECKSUM_NOK);                                 // TASK_MESSAGE
                LOG_Push("Fw handler\t: FlashSlaves checksum is empty, stopping FW flash.");
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
                    LOG_Printf("Fw handler\t: EXEC_FW_STATE_FLASH_ALL_SLAVES flash Slave ID %d.", SlaveId1);
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
                case DONE:  LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_FLASHED_SLAVE DONE.");
                            SlaveId1++;
                            iFlashSlaves = 1;
                            break;
                case ERROR: LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_FLASHED_SLAVE ERROR.");
                            iFlashSlaves    = 0;
                            return_val      = ERROR;
                            break;
                case BUSY : break;
                default   : LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_FLASHED_SLAVE default case not allowed.");
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

static uint8_t  iFlashSequencer = 2;
static uint8_t  BackplaneId     = 0;
static uint8_t  GoToCase        = 0;
static uint16_t WaitCounter     = 0;

uint32_t FlashSequencer(uint8_t Slave){
    uint32_t return_val = BUSY;
    uint32_t result     = 0;
    uint8_t  ShiftSlot1 = 0;
    
    if(Slave > 0  && Slave < 11){
        BackplaneId = 51;}
    else if(Slave > 10 && Slave < 21){
        BackplaneId = 52;}
    else if(Slave > 20 && Slave < 31){
        BackplaneId = 53;}
    else if(Slave > 30 && Slave < 41){
        BackplaneId = 54;}
    else if(Slave > 40 && Slave < 51){
        BackplaneId = 55;}
    
    switch(iFlashSequencer){
        case 2:
        {
            Slaves_Disable_Off();
            DelayCount1 = READxCORExTIMER();
            iFlashSequencer = 3;
            break;
        }
        
        case 3:
        {
            DelayCount2 = READxCORExTIMER();
            if((DelayCount2 - DelayCount1) > (10 * MILISECONDS)){
                iFlashSequencer = 4;
            }
            else if((DelayCount1 > DelayCount2) && ((0xFFFFFFFF - DelayCount1 + DelayCount2) > (10 * MILISECONDS) )){
                iFlashSequencer = 4;
                LOG_Push("Fw handler\t: overflow READxCORExTIMER() detected");
            }  
            break;
        }
        
        case 4:                                                                 // select first a slave amplifier by selecting one via a backplane slave
        {            
            fwData.SlaveBootloaderHandlingActive = false;
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
            iFlashSequencer = 5;            
            break;
        }
        
        /* Verify communication was OK with backplane, if not flag backplane 
           SlaveDetected to false to indicate backplane comm error */
        case 5:
            switch(CHECKxMODBUSxCOMMxSTATUS(BackplaneId, false)){               // --> do not overwrite otherwise diagnostics are gone
                case SLAVEOK:  
                    GoToCase        = 6;//iFlashSequencer + 1;
                    iFlashSequencer = WAIT;
                    DelayCount1 = READxCORExTIMER();
                    break;                    
                case SLAVENOK: 
                    LOG_Push("Fw handler\t: FWFLASHSEQUENCER SLAVENOK returned in case 5.");
                    iFlashSequencer = 2;
                    return_val = ERROR;
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            DelayCount1 = READxCORExTIMER();
            break;
        
        case 6:
        {
            /* Writing the read checksum to 0 will call a reset on the slave, 
             * since we already selected it, it will instantly go into bootloader :-) 
             */
            Data.SlaveAddress  = SLAVE_INITIAL_ADDR;
            Data.Direction     = WRITE;
            Data.NoOfRegisters = 1;
            Data.StartRegister = HOLDINGREG11;                                  
            Data.RegData0      = 0;
            SLAVExCOMMUNICATIONxHANDLER();
            iFlashSequencer = 7;
            break;
        }
        
        case 7:
            switch(CHECKxMODBUSxCOMMxSTATUS(SLAVE_INITIAL_ADDR, false)){
                case SLAVEOK:
                    GoToCase        = 8;//iFlashSequencer + 1;
                    iFlashSequencer = WAIT;
                    DelayCount1 = READxCORExTIMER();
                    break;                    
                case SLAVENOK: 
                    LOG_Push("Fw handler\t: FWFLASHSEQUENCER SLAVENOK returned in case 7.");
                    iFlashSequencer = 2;
                    return_val = ERROR;
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            DelayCount1 = READxCORExTIMER();
            break;
        
        case 8:
		{
            /* Let the slave select go already, so that after e reset is send to 
             * the slave, it will not go into bootloader again! 
             */
            Data.SlaveAddress  = BackplaneId;
            Data.Direction     = WRITE;
            Data.NoOfRegisters = 1;
            Data.StartRegister = HOLDINGREG0;
            Data.RegData0      = 0;
            SLAVExCOMMUNICATIONxHANDLER();
            iFlashSequencer = 9;
            break;
		}
		
        /* Verify communication was OK with backplane, if not flag backplane 
           SlaveDetected to false to indicate backplane comm error */
        case 9:
            switch(CHECKxMODBUSxCOMMxSTATUS(BackplaneId, false)){               // --> do not overwrite otherwise diagnostics are gone
                case SLAVEOK:  
                    fwData.SlaveBootloaderHandlingActive = true;                    // disable modbus
                    GoToCase        = 10;//iFlashSequencer + 1;
                    iFlashSequencer = WAIT;
                    DelayCount1 = READxCORExTIMER();
                    break;                    
                case SLAVENOK:
                    LOG_Push("Fw handler\t: FWFLASHSEQUENCER SLAVENOK returned in case 9.");
                    return_val = ERROR;
                    iFlashSequencer = 2;
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            break;
        
		case 10:
        {
            result = GETxBOOTxLOADERxVERSION();
            switch (result){
                case DONE:  LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_GET_BOOTLOADER_VERSION DONE.");
                            iFlashSequencer++;
                            break;
                case ERROR: LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_GET_BOOTLOADER_VERSION ERROR.");
                            iFlashSequencer = 0;
                            return_val      = ERROR;
                            break;
                case BUSY : break;
                default   : LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_GET_BOOTLOADER_VERSION default case not allowed.");
            }
            break;
        }
        
        case 11:
        {
            result = ERASExFLASH(SLAVE_BOOT_LOADER_OFFSET, SLAVE_FLASH_END);
            switch (result){
                case DONE:  LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_ERASE_FLASH DONE.");
                            iFlashSequencer++;
                            break;
                case ERROR: LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_ERASE_FLASH ERROR.");
                            iFlashSequencer = 0;
                            return_val      = ERROR;
                            break;
                case BUSY : break;
                default   : LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_ERASE_FLASH default case not allowed.");
            }            
            break;
        }
        
        case 12:
        {
            result = WRITExFLASH(SLAVE_BOOT_LOADER_OFFSET, SLAVE_FLASH_END, &FwFile[0]);
            switch (result){
                case DONE:  LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_WRITE_FLASH DONE.");
                            iFlashSequencer++;
                            break;
                case ERROR: LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_WRITE_FLASH ERROR.");
                            iFlashSequencer = 0;
                            return_val      = ERROR;
                            break;
                case BUSY : break;
                default   : LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_WRITE_FLASH default case not allowed.");
            }
            break;
        }
		
		case 13:
        {
            result = WRITExCONFIG(&FwConfigWord[0]);
            switch (result){
                case DONE:  LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_WRITE_CONFIG DONE.");
                            iFlashSequencer++;
                            break;
                case ERROR: LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_WRITE_CONFIG ERROR.");
                            iFlashSequencer = 0;
                            return_val      = ERROR;
                            break;
                case BUSY : break;
                default   : LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_WRITE_CONFIG default case not allowed.");
            }
            break;
        }
		
		case 14:
        {
            result = CHECKxCHECKSUM(SLAVE_BOOT_LOADER_OFFSET, SLAVE_FLASH_END, checksum);
            switch (result){
                case DONE:  LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_CHECK_CHECKSUM DONE.");
                            iFlashSequencer++;
                            break;
                case ERROR: LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_CHECK_CHECKSUM ERROR.");
                            iFlashSequencer = 0;
                            return_val      = ERROR;
                            break;
                case BUSY : break;
                default   : LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_CHECK_CHECKSUM default case not allowed.");
            }
            break;
        }
        
        case 15:
        {
            result = RESETxSLAVE();
            switch (result){
                case DONE:  LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_RESET_SLAVE DONE.");
                            iFlashSequencer = 2;
                            fwData.SlaveBootloaderHandlingActive = false;       // enable modbus again
                            return_val      = DONE;
                            break;
                case ERROR: LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_RESET_SLAVE ERROR.");
                            iFlashSequencer = 2;
                            fwData.SlaveBootloaderHandlingActive = false;       // enable modbus again
                            return_val      = ERROR;
                            break;
                case BUSY : break;
                default   : LOG_Push("Fw handler\t: FWFLASHSEQUENCER_STATE_RESET_SLAVE default case not allowed.");
            }
            break;
        }
        
        case WAIT:            
//            WaitCounter++;
//            if (WaitCounter > WAIT_TIME){
//                WaitCounter = 0;
//                Led1Off();
//                iFlashSequencer = GoToCase;
//            }
            DelayCount2 = READxCORExTIMER();
            if((DelayCount2 - DelayCount1) > (WAIT_TIME1 * MILISECONDS)){
                iFlashSequencer = GoToCase;
            }
            else if((DelayCount1 > DelayCount2) && ((0xFFFFFFFF - DelayCount1 + DelayCount2) > (WAIT_TIME1 * MILISECONDS) )){
                iFlashSequencer = GoToCase;               
            }
            break;
        
        default:
        {
            iFlashSequencer = 2;
            break;
        }
    }
    
    return (return_val);
}

/*#--------------------------------------------------------------------------#*/
/*  Description: uint32_t        SelectSlave         (uint8_t SlaveId)
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
static uint8_t  iSelectSlaveSequencer = 0;

uint32_t        SelectSlave         (uint8_t SlaveId){
    uint32_t    return_val  = BUSY;
    uint8_t     BackplaneId = 0;
    uint8_t     ShiftSlot1  = 0;
    
    if(     SlaveId > 0  && SlaveId < 11){
        BackplaneId = 51;
        ShiftSlot1  = SlaveId - 1;}
    else if(SlaveId > 10 && SlaveId < 21){
        BackplaneId = 52;
        ShiftSlot1  = SlaveId - 11;}
    else if(SlaveId > 20 && SlaveId < 31){
        BackplaneId = 53;
        ShiftSlot1  = SlaveId - 21;}
    else if(SlaveId > 30 && SlaveId < 41){
        BackplaneId = 54;
        ShiftSlot1  = SlaveId - 31;}
    else if(SlaveId > 40 && SlaveId < 51){
        BackplaneId = 55;
        ShiftSlot1  = SlaveId - 41;}
    else{
        CREATExTASKxSTATUSxMESSAGE(                
                FWHANDLER,                                                      // TASK_ID
                EXEC_FW_STATE_SELECT_SLAVE,                                     // TASK_COMMAND
                ERROR,                                                          // TASK_STATE
                SLAVE_ID_OUT_OF_BOUNDS);                                        // TASK_MESSAGE
        LOG_Push("Fw handler\t: EXEC_FW_STATE_SELECT_SLAVE SLAVE_ID_OUT_OF_BOUNDS ERROR.");
        return (ERROR);
    }
    
    switch(iSelectSlaveSequencer){
        case 0:
        {
            Slaves_Disable_Off();
            DelayCount1 = READxCORExTIMER();
            iSelectSlaveSequencer = 1;
            break;
        }
        
        case 1:
        {
            DelayCount2 = READxCORExTIMER();
            if((DelayCount2 - DelayCount1) > (10 * MILISECONDS)){
                iSelectSlaveSequencer = 2;
            }
            else if((DelayCount1 > DelayCount2) && ((0xFFFFFFFF - DelayCount1 + DelayCount2) > (10 * MILISECONDS) )){
                iSelectSlaveSequencer = 2;
                LOG_Push("Fw handler\t: overflow READxCORExTIMER() detected");
            }  
            break;
        }
        
        case 2:                                                                 // select first a slave amplifier by selecting one via a backplane slave
        {            
            fwData.SlaveBootloaderHandlingActive = false;
            Data.SlaveAddress  = BackplaneId;
            Data.Direction     = WRITE;
            Data.NoOfRegisters = 1;
            Data.StartRegister = HOLDINGREG0;
            Data.RegData0      = (SLOT1 << ShiftSlot1);
            SLAVExCOMMUNICATIONxHANDLER();
            iSelectSlaveSequencer = 3;            
            break;
        }
        
        /* Verify communication was OK with backplane, if not flag backplane 
           SlaveDetected to false to indicate backplane comm error */
        case 3:
            switch(CHECKxMODBUSxCOMMxSTATUS(BackplaneId, false)){               // --> do not overwrite otherwise diagnostics are gone
                case SLAVEOK:  
                    GoToCase        = 4;//iFlashSequencer + 1;
                    iSelectSlaveSequencer = WAIT;
                    DelayCount1 = READxCORExTIMER();
                    break;                    
                case SLAVENOK: 
                    LOG_Push("Fw handler\t: EXEC_FW_STATE_SELECT_SLAVE SLAVENOK returned in case 3.");
                    iSelectSlaveSequencer = 0;
                    return_val = ERROR;
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            DelayCount1 = READxCORExTIMER();
            break;
        
        case 4:
        {
            /* Writing the read checksum to 0 will call a reset on the slave, 
             * since we already selected it, it will instantly go into bootloader :-) 
             */
            Data.SlaveAddress  = SLAVE_INITIAL_ADDR;
            Data.Direction     = WRITE;
            Data.NoOfRegisters = 1;
            Data.StartRegister = HOLDINGREG11;                                  
            Data.RegData0      = 0;
            SLAVExCOMMUNICATIONxHANDLER();
            iSelectSlaveSequencer = 5;
            break;
        }
        
        case 5:
            switch(CHECKxMODBUSxCOMMxSTATUS(SLAVE_INITIAL_ADDR, false)){
                case SLAVEOK:
                    GoToCase        = 6;//iFlashSequencer + 1;
                    iSelectSlaveSequencer = WAIT;
                    DelayCount1 = READxCORExTIMER();
                    break;                    
                case SLAVENOK: 
                    LOG_Push("Fw handler\t: EXEC_FW_STATE_SELECT_SLAVE SLAVENOK returned in case 5.");
                    iSelectSlaveSequencer = 0;
                    return_val = ERROR;
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            DelayCount1 = READxCORExTIMER();
            break;
        
        case 6:
		{
            /* Let the slave select go already, so that after e reset is send to 
             * the slave, it will not go into bootloader again! 
             */
            Data.SlaveAddress  = BackplaneId;
            Data.Direction     = WRITE;
            Data.NoOfRegisters = 1;
            Data.StartRegister = HOLDINGREG0;
            Data.RegData0      = 0;
            SLAVExCOMMUNICATIONxHANDLER();
            iSelectSlaveSequencer = 7;
            break;
		}
		
        /* Verify communication was OK with backplane, if not flag backplane 
           SlaveDetected to false to indicate backplane comm error */
        case 7:
            switch(CHECKxMODBUSxCOMMxSTATUS(BackplaneId, false)){               // --> do not overwrite otherwise diagnostics are gone
                case SLAVEOK:  
                    GoToCase        = 8;//iSelectSlaveSequencer + 1;
                    iSelectSlaveSequencer = WAIT;
                    DelayCount1 = READxCORExTIMER();
                    break;                    
                case SLAVENOK:
                    LOG_Push("Fw handler\t: EXEC_FW_STATE_SELECT_SLAVE SLAVENOK returned in case 7.");
                    return_val = ERROR;
                    iSelectSlaveSequencer = 0;
                    break;
                case SLAVEBUSY: break;
                default : break;
            }
            break;
        
        case 8:
        {
            return_val = DONE;
            iSelectSlaveSequencer = 0;
            break;
        }
            
        case WAIT:
        DelayCount2 = READxCORExTIMER();
        if((DelayCount2 - DelayCount1) > (WAIT_TIME1 * MILISECONDS)){
            iSelectSlaveSequencer = GoToCase;
        }
        else if((DelayCount1 > DelayCount2) && ((0xFFFFFFFF - DelayCount1 + DelayCount2) > (WAIT_TIME1 * MILISECONDS) )){
            iSelectSlaveSequencer = GoToCase;               
        }
        break;
            
        default: 
        {
            iSelectSlaveSequencer = 0;
            break;
        }
    }
    return (return_val);
}