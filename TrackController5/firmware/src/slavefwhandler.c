#include <xc.h>
#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdlib.h>
#include "app.h"
#include "slavecommhandler.h"
#include "enums.h"
#include "slavefwhandler.h"
#include "ethernet.h"

bool FwFileDownload(void);
bool FlashSlaves   (void);

static uint8_t FwFile[30720];

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
    
    fwData.state = FW_STATE_INIT;
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
    bool return_val = false;
    
    switch (fwData.state){
        
        case FW_STATE_INIT:
        {
            CREATExTASKxSTATUSxMESSAGE((uint8_t)FWHANDLER, (uint8_t)CONNECTED, (uint8_t)DONE);
            SYS_MESSAGE("FW handler started.\n\r");
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
                    //SYS_MESSAGE("FW Handler command received.\n\r");
                }
                else{
                    
                    fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                    SYS_MESSAGE("FW handler received wrong HEADER, stay in wait state.\n\r");
                    CREATExTASKxSTATUSxMESSAGE((uint8_t)FWHANDLER, (uint8_t)COMMAND, (uint8_t)ERROR);
                }
            }
            break;
        }
        
        case FW_STATE_COMMAND_HANDLING:
        {
            switch (fwData.command){
                
                case EXEC_FW_STATE_RECEIVE_FW_FILE:
                {
                    if(FwFileDownload()){
                        CREATExTASKxSTATUSxMESSAGE((uint8_t)FWHANDLER, (uint8_t)EXEC_FW_STATE_RECEIVE_FW_FILE, (uint8_t)DONE);
                        SYS_MESSAGE("FW handler EXEC_FW_STATE_RECEIVE_FW_FILE done.\n\r");
                        fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                    }
                    break;
                }
                
                case EXEC_FW_STATE_FLASH_SLAVES:
                {
                    if(FlashSlaves()){
                        CREATExTASKxSTATUSxMESSAGE((uint8_t)FWHANDLER, (uint8_t)EXEC_FW_STATE_FLASH_SLAVES, (uint8_t)DONE);
                        SYS_MESSAGE("FW handler EXEC_FW_STATE_FLASH_SLAVES done.\n\r");
                        fwData.state = FW_STATE_INIT;
                        return_val = true;
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
                    SYS_MESSAGE("FW handler received wrong command, stay in wait state.\n\r");
                    CREATExTASKxSTATUSxMESSAGE((uint8_t)FWHANDLER, (uint8_t)COMMAND, (uint8_t)ERROR);
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

uint8_t     sequence        = 0;
uint8_t     *ptr_FwData     = 0;
uint16_t    FwLineCount     = 0;

uint16_t    count           = 0;
uint16_t    limit           = 0;
uint16_t    checksum        = 0;
uint8_t     data1           = 0;
uint8_t     data2           = 0;
uint8_t     *pFw1           = 0;
uint8_t     *pFw2           = 0;

bool FwFileDownload(){
    
    bool return_val = false;
    
    switch (sequence){
        
        case 0:
        {
            CREATExTASKxSTATUSxMESSAGE((uint8_t)FWHANDLER, (uint8_t)EXEC_FW_STATE_RECEIVE_FW_FILE_STANDBY, (uint8_t)DONE);
            ptr_FwData = &FwFile[0];
            FwLineCount = 0;
            sequence++;
            break;
        }
        
        case 1:
        {
            if(CHECKxDATAxINxRECEIVExMAILxBOX()){
                EthernetRecvData = GETxDATAxFROMxRECEIVExMAILxBOX();
                
                if(EthernetRecvData->command == EXEC_FW_STATE_FW_DATA){
                    //*ptr_FwData = EthernetRecvData->data;
                    memcpy(ptr_FwData, &(EthernetRecvData->data), 64);
                    ptr_FwData  +=  64;
                    FwLineCount +=   4;
                                
                    if(FwLineCount > 1916){
                        sequence++;
                        FwLineCount = 0;                    
                        CREATExTASKxSTATUSxMESSAGE((uint8_t)FWHANDLER, (uint8_t)EXEC_FW_STATE_FW_DATA_DOWNLOAD_DONE, (uint8_t)DONE);
                        SYS_MESSAGE("FW handler EXEC_FW_STATE_RECEIVE_FW_FILE received a FW file.\n\r");
                    }
                    else{
                        CREATExTASKxSTATUSxMESSAGE((uint8_t)FWHANDLER, (uint8_t)EXEC_FW_STATE_RECEIVE_FW_FILE_STANDBY, (uint8_t)DONE);
                    }
                }
                else{
                    SYS_MESSAGE("EXEC_FW_STATE_RECEIVE_FW_FILE_STANDBY received wrong command stopping FW Handler.\n\r");
                    sequence = 0;
                    fwData.state = FW_STATE_WAITING_FOR_COMMAND;
                    return_val = true;
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
                CREATExTASKxSTATUSxMESSAGE((uint8_t)FWHANDLER, (uint8_t)EXEC_FW_STATE_FW_CHECKSUM, (uint8_t)DONE);
                SYS_MESSAGE("FW handler EXEC_FW_STATE_RECEIVE_FW_FILE checksum is OK.\n\r");
            }
            else{
                CREATExTASKxSTATUSxMESSAGE((uint8_t)FWHANDLER, (uint8_t)EXEC_FW_STATE_FW_CHECKSUM, (uint8_t)ERROR);
                SYS_MESSAGE("FW handler EXEC_FW_STATE_RECEIVE_FW_FILE checksum is NOK.\n\r");
            }
            sequence = 0;
            return_val = true;
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
/*  Description: bool FlashSlaves()
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
bool FlashSlaves(){
    bool return_val = false;
    
    
    
    return (return_val);
}