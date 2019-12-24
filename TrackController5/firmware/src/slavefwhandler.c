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

bool FwFileDownload(){
    
    bool return_val = false;
    
    switch (sequence){
        
        case 0:
        {
            CREATExTASKxSTATUSxMESSAGE((uint8_t)FWHANDLER, (uint8_t)EXEC_FW_STATE_RECEIVE_FW_FILE_STANDBY, (uint8_t)DONE);
            ptr_FwData = &FwFile[0];
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
                                
                    if(FwLineCount > 479){
                        sequence++;
                        FwLineCount = 0;                    
                        CREATExTASKxSTATUSxMESSAGE((uint8_t)FWHANDLER, (uint8_t)EXEC_FW_STATE_RECEIVE_FW_FILE_STANDBY, (uint8_t)DONE);
                    }
                    else{
                        CREATExTASKxSTATUSxMESSAGE((uint8_t)FWHANDLER, (uint8_t)EXEC_FW_STATE_FW_DATA_DOWNLOAD_DONE, (uint8_t)DONE);
                    }
                }
                else{
                    SYS_MESSAGE("EXEC_FW_STATE_RECEIVE_FW_FILE_STANDBY received wrong command stopping FW Handler.\n\r");
                    sequence = 0;
                    fwData.state = FW_STATE_INIT;
                    return_val = true;
                }
            }
            break;
        }
        
        case 2:
        {
            _nop();
            break;
        }
        
        default:
        {
            break;
        }
    }
    return (return_val);
}