#include <xc.h>
#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdlib.h>
#include "slavebootloaderroutines.h"
#include "enums.h"
#include "mbus.h"

// *****************************************************************************
// *****************************************************************************
// Section: Application Local Functions
// *****************************************************************************
// *****************************************************************************

bool SendDataToBootloader           (BTDR_SEND_DATA_FORMAT *btldrDataSend);
void ClearOldBtldrData              (void);
void ClearOldBtldrReceiveData       (void);

static uint32_t     DelayCount = 0;

/*#--------------------------------------------------------------------------#*/
/*  Description: uint8_t GETxBOOTxLOADERxVERSION()
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
 *  Notes      :
 */
/*#--------------------------------------------------------------------------#*/

uint32_t GETxBOOTxLOADERxVERSION(){
    uint32_t return_val = BUSY;
    
    switch(btldrData.sequence){
        case 0:
        {
            btldrDataSend.bootloader_start_byte = 0x55;
            btldrDataSend.command               = (uint8_t)CMD_READ_VERSION;
            btldrDataSend.data_length_high      = 0x00;
            btldrDataSend.data_length_low       = 0x00;
            btldrDataSend.unlock_hgh            = 0x00;
            btldrDataSend.unlock_low            = 0x00;
            btldrDataSend.address_low           = 0x00;
            btldrDataSend.address_hgh           = 0x00;
            btldrDataSend.address_upp           = 0x00;
            btldrDataSend.address_ext           = 0x00;

            SendDataToBootloader((BTDR_SEND_DATA_FORMAT *)&btldrDataSend);
            DelayCount                          = READxCORExTIMER();
            btldrData.sequence++;
            break;
        }
        
        case 1:
        {
            if(btldrData.btldr_datacount > 25){
                if(btldrDataReceive.bootloader_start_byte == 0x55){
                    if(btldrDataReceive.status[0] == 0 &&
                        btldrDataReceive.status[1] == 1){
                        CREATExTASKxSTATUSxMESSAGE(
                            GET_BOOTLOADER_VERSION,                             // TASK_ID
                            GET_BOOTLOADER_VERSION_OK,                          // TASK_COMMAND
                            DONE,                                               // TASK_STATE
                            NONE);                                              // TASK_MESSAGE
                        SYS_MESSAGE("Fw handler\t: GET_BOOTLOADER_VERSION GET_BOOTLOADER_VERSION_OK.\n\r");
                        return_val          = DONE;
                    }
                    else{
                        CREATExTASKxSTATUSxMESSAGE(
                            GET_BOOTLOADER_VERSION,                             // TASK_ID
                            GET_BOOTLOADER_VERSION_NOK,                         // TASK_COMMAND
                            ERROR,                                              // TASK_STATE
                            NONE);                                              // TASK_MESSAGE
                        SYS_MESSAGE("Fw handler\t: GET_BOOTLOADER_VERSION GET_BOOTLOADER_VERSION_NOK.\n\r");
                    return_val              = ERROR;
                    }
                }
                else{
                    CREATExTASKxSTATUSxMESSAGE(
                        GET_BOOTLOADER_VERSION,                                 // TASK_ID
                        BOOTLOADER_START_BYTE_ERROR,                            // TASK_COMMAND
                        ERROR,                                                  // TASK_STATE
                        NONE);                                                  // TASK_MESSAGE
                    SYS_MESSAGE("Fw handler\t: GET_BOOTLOADER_VERSION BOOTLOADER_START_BYTE_ERROR.\n\r");
                    return_val              = ERROR;
                }
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            else if(btldrData.btldr_receive_error){
                CREATExTASKxSTATUSxMESSAGE(
                    GET_BOOTLOADER_VERSION,                                     // TASK_ID
                    BOOTLOADER_DATA_RECEIVE_ERROR,                              // TASK_COMMAND
                    ERROR,                                                      // TASK_STATE
                    NONE);                                                      // TASK_MESSAGE
                SYS_MESSAGE("Fw handler\t: GET_BOOTLOADER_VERSION BOOTLOADER_DATA_RECEIVE_ERROR.\n\r");
                return_val                  = ERROR;
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            else if((READxCORExTIMER() - DelayCount) > (100 * MILISECONDS)){
                CREATExTASKxSTATUSxMESSAGE(
                    GET_BOOTLOADER_VERSION,                                     // TASK_ID
                    GET_BOOTLOADER_VERSION_RECEIVE_DATA_TIMEOUT,                // TASK_COMMAND
                    ERROR,                                                      // TASK_STATE
                    NONE);                                                      // TASK_MESSAGE
                SYS_MESSAGE("Fw handler\t: GET_BOOTLOADER_VERSION BOOTLOADER_DATA_RECEIVE_ERROR.\n\r");
                return_val                  = ERROR;
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            break;
        }
        
        default:
        {
            btldrData.sequence = 0;
            break;
        }
    }
    
    return (return_val);
}

/*#--------------------------------------------------------------------------#*/
/*  Description: uint8_t GETxBOOTxLOADERxVERSION()
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
 *  Notes      :
 */
/*#--------------------------------------------------------------------------#*/

uint32_t ERASExFLASH(uint16_t flash_bootloader_offset, uint16_t flash_end_address){
    uint32_t return_val = BUSY;
    
    uint16_t EraseRows = ((flash_end_address - flash_bootloader_offset)/BLOCKWIDTH);
    
    switch(btldrData.sequence){
        case 0:
        {
            btldrDataSend.bootloader_start_byte = 0x55;
            btldrDataSend.command               = (uint8_t)CMD_ERASE_FLASH;
            btldrDataSend.data_length_high      = (EraseRows >> 8) & 0x00FF;
            btldrDataSend.data_length_low       = (EraseRows & 0x00FF);
            btldrDataSend.unlock_hgh            = 0xAA;
            btldrDataSend.unlock_low            = 0x55;
            btldrDataSend.address_low           = (flash_bootloader_offset & 0x00FF);
            btldrDataSend.address_hgh           = (flash_bootloader_offset >> 8) & 0x00FF;
            btldrDataSend.address_upp           = 0x00;
            btldrDataSend.address_ext           = 0x00;

            SendDataToBootloader((BTDR_SEND_DATA_FORMAT *)&btldrDataSend);
            DelayCount                          = READxCORExTIMER();
            btldrData.sequence++;
            break;
        }
        
        case 1:
        {
            if(btldrData.btldr_datacount > 10){
                if(btldrDataReceive.bootloader_start_byte == 0x55){
                    if(btldrDataReceive.status[0] == 1){
                        CREATExTASKxSTATUSxMESSAGE(
                            ERASE_FLASH,                                        // TASK_ID
                            ERASE_FLASH_RETURNED_OK,                            // TASK_COMMAND
                            DONE,                                               // TASK_STATE
                            NONE);                                              // TASK_MESSAGE
                        SYS_MESSAGE("Fw handler\t: ERASE_FLASH ERASE_FLASH_RETURNED_OK.\n\r");
                        return_val          = DONE;
                    }
                    else{
                        CREATExTASKxSTATUSxMESSAGE(
                            ERASE_FLASH,                                        // TASK_ID
                            ERASE_FLASH_RETURNED_NOK,                           // TASK_COMMAND
                            ERROR,                                              // TASK_STATE
                            NONE);                                              // TASK_MESSAGE
                        SYS_MESSAGE("Fw handler\t: ERASE_FLASH ERASE_FLASH_RETURNED_NOK.\n\r");
                    return_val              = ERROR;
                    }
                }
                else{
                    CREATExTASKxSTATUSxMESSAGE(
                        ERASE_FLASH,                                            // TASK_ID
                        BOOTLOADER_START_BYTE_ERROR,                            // TASK_COMMAND
                        ERROR,                                                  // TASK_STATE
                        NONE);                                                  // TASK_MESSAGE
                    SYS_MESSAGE("Fw handler\t: ERASE_FLASH BOOTLOADER_START_BYTE_ERROR.\n\r");
                    return_val              = ERROR;
                }
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            else if(btldrData.btldr_receive_error){
                CREATExTASKxSTATUSxMESSAGE(
                    ERASE_FLASH,                                                // TASK_ID
                    BOOTLOADER_DATA_RECEIVE_ERROR,                              // TASK_COMMAND
                    ERROR,                                                      // TASK_STATE
                    NONE);                                                      // TASK_MESSAGE
                SYS_MESSAGE("Fw handler\t: ERASE_FLASH BOOTLOADER_DATA_RECEIVE_ERROR.\n\r");
                return_val                  = ERROR;
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            else if((READxCORExTIMER() - DelayCount) > (1 * SECONDS)){
                CREATExTASKxSTATUSxMESSAGE(
                    ERASE_FLASH,                                                // TASK_ID
                    ERASE_FLASH_RECEIVE_DATA_TIMEOUT,                           // TASK_COMMAND
                    ERROR,                                                      // TASK_STATE
                    NONE);                                                      // TASK_MESSAGE
                SYS_MESSAGE("Fw handler\t: ERASE_FLASH ERASE_FLASH_RECEIVE_DATA_TIMEOUT.\n\r");
                return_val                  = ERROR;
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            break;
        }
        
        default:
        {
            btldrData.sequence = 0;
            break;
        }
    }
    
    return (return_val);
}

/*#--------------------------------------------------------------------------#*/
/*  Description: uint8_t WRITExFLASH(uint16_t flash_bootloader_offset, uint16_t 
 *                                   flash_end_address, uint8_t *fwfile)
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
 *  Notes      :
 */
/*#--------------------------------------------------------------------------#*/

static uint16_t FlashRows       = 0;
static uint16_t FlashAddress    = 0;

uint32_t WRITExFLASH(uint16_t flash_bootloader_offset, uint16_t flash_end_address, uint8_t *fwfile){
    uint32_t return_val = BUSY;
    
    FlashRows = (flash_end_address - flash_bootloader_offset);
    
    switch(btldrData.sequence){
        case 0:
        {                
            FlashAddress = flash_bootloader_offset + btldrData.flashrowcounter;
    
            btldrDataSend.bootloader_start_byte = 0x55;
            btldrDataSend.command               = (uint8_t)CMD_WRITE_FLASH;
            btldrDataSend.data_length_high      = 0;
            btldrDataSend.data_length_low       = BLOCKWIDTH;
            btldrDataSend.unlock_hgh            = 0xAA;
            btldrDataSend.unlock_low            = 0x55;
            btldrDataSend.address_low           = (FlashAddress & 0x00FF);
            btldrDataSend.address_hgh           = (FlashAddress >> 8) & 0x00FF;
            btldrDataSend.address_upp           = 0x00;
            btldrDataSend.address_ext           = 0x00;
            memcpy(btldrDataSend.data, &(fwfile[btldrData.flashrowcounter]), BLOCKWIDTH);

            SendDataToBootloader((BTDR_SEND_DATA_FORMAT *)&btldrDataSend);
            DelayCount                          = READxCORExTIMER();
            btldrData.sequence++;
            btldrData.flashrowcounter += BLOCKWIDTH; // for bytes = *2
            break;
        }
        
        case 1:
        {
            if(btldrData.btldr_datacount > 10){
                if(btldrDataReceive.bootloader_start_byte == 0x55){
                    if(btldrDataReceive.status[0] == 1){                        
                        if (btldrData.flashrowcounter >= FlashRows){
                            CREATExTASKxSTATUSxMESSAGE(
                                WRITE_FLASH,                                    // TASK_ID
                                WRITE_FLASH_RETURNED_OK,                        // TASK_COMMAND
                                DONE,                                           // TASK_STATE
                                NONE);                                          // TASK_MESSAGE
                            SYS_MESSAGE("Fw handler\t: WRITE_FLASH WRITE_FLASH_RETURNED_OK.\n\r");
                            return_val          = DONE;
                            ClearOldBtldrData();
                            
                        }
                        else{
                            btldrData.sequence = 0;
                            btldrData.btldr_datacount = 0;
                        }
                        ClearOldBtldrReceiveData();
                    }
                    else{
                        CREATExTASKxSTATUSxMESSAGE(
                            WRITE_FLASH,                                        // TASK_ID
                            WRITE_FLASH_RETURNED_NOK,                           // TASK_COMMAND
                            ERROR,                                              // TASK_STATE
                            NONE);                                              // TASK_MESSAGE
                        SYS_MESSAGE("Fw handler\t: WRITE_FLASH WRITE_FLASH_RETURNED_NOK.\n\r");
                    return_val              = ERROR;
                    }
                }
                else{
                    CREATExTASKxSTATUSxMESSAGE(
                        WRITE_FLASH,                                            // TASK_ID
                        BOOTLOADER_START_BYTE_ERROR,                            // TASK_COMMAND
                        ERROR,                                                  // TASK_STATE
                        NONE);                                                  // TASK_MESSAGE
                    SYS_MESSAGE("Fw handler\t: WRITE_FLASH BOOTLOADER_START_BYTE_ERROR.\n\r");
                    return_val              = ERROR;
                    ClearOldBtldrData();
                    ClearOldBtldrReceiveData();
                }                
            }
            else if(btldrData.btldr_receive_error){
                CREATExTASKxSTATUSxMESSAGE(
                    WRITE_FLASH,                                                // TASK_ID
                    BOOTLOADER_DATA_RECEIVE_ERROR,                              // TASK_COMMAND
                    ERROR,                                                      // TASK_STATE
                    NONE);                                                      // TASK_MESSAGE
                SYS_MESSAGE("Fw handler\t: WRITE_FLASH BOOTLOADER_DATA_RECEIVE_ERROR.\n\r");
                return_val                  = ERROR;
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            else if((READxCORExTIMER() - DelayCount) > (1 * SECONDS)){
                CREATExTASKxSTATUSxMESSAGE(
                    WRITE_FLASH,                                                // TASK_ID
                    WRITE_FLASH_RECEIVE_DATA_TIMEOUT,                           // TASK_COMMAND
                    ERROR,                                                      // TASK_STATE
                    NONE);                                                      // TASK_MESSAGE
                SYS_MESSAGE("Fw handler\t: WRITE_FLASH WRITE_FLASH_RECEIVE_DATA_TIMEOUT.\n\r");
                return_val                  = ERROR;
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            break;
        }
        
        default:
        {
            btldrData.sequence = 0;
            break;
        }
    }
    
    return (return_val);
}

/*#--------------------------------------------------------------------------#*/
/*  Description: uint8_t WRITExCONFIG(uint8_t *config_data)
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
 *  Notes      :
 */
/*#--------------------------------------------------------------------------#*/
uint32_t WRITExCONFIG(uint8_t *config_data){
    uint32_t return_val = BUSY;
            
    switch(btldrData.sequence){
        case 0:
        {
            btldrDataSend.bootloader_start_byte = 0x55;
            btldrDataSend.command               = (uint8_t)CMD_WRITE_CONFIG;
            btldrDataSend.data_length_high      = 0;
            btldrDataSend.data_length_low       = 0x0C;
            btldrDataSend.unlock_hgh            = 0xAA;
            btldrDataSend.unlock_low            = 0x55;
            btldrDataSend.address_low           = 0x00;
            btldrDataSend.address_hgh           = 0x00;
            btldrDataSend.address_upp           = 0x30;
            btldrDataSend.address_ext           = 0x00;
            memcpy(btldrDataSend.data, &(config_data[0]), 0x0C);

            SendDataToBootloader((BTDR_SEND_DATA_FORMAT *)&btldrDataSend);
            DelayCount                          = READxCORExTIMER();
            btldrData.sequence++;
            break;
        }
        
        case 1:
        {
            if(btldrData.btldr_datacount > 10){
                if(btldrDataReceive.bootloader_start_byte == 0x55){
                    if(btldrDataReceive.status[0] == 1){                        
                        CREATExTASKxSTATUSxMESSAGE(
                            WRITE_CONFIG,                                       // TASK_ID
                            WRITE_CONFIG_RETURNED_OK,                           // TASK_COMMAND
                            DONE,                                               // TASK_STATE
                            NONE);                                              // TASK_MESSAGE
                        SYS_MESSAGE("Fw handler\t: WRITE_CONFIG WRITE_CONFIG_RETURNED_OK.\n\r");
                        return_val          = DONE;                        
                    }
                    else{
                        CREATExTASKxSTATUSxMESSAGE(
                            WRITE_CONFIG,                                       // TASK_ID
                            WRITE_CONFIG_RETURNED_NOK,                          // TASK_COMMAND
                            ERROR,                                              // TASK_STATE
                            NONE);                                              // TASK_MESSAGE
                        SYS_MESSAGE("Fw handler\t: WRITE_CONFIG WRITE_CONFIG_RETURNED_NOK.\n\r");
                    return_val              = ERROR;
                    }
                    ClearOldBtldrData();
                    ClearOldBtldrReceiveData();
                }
                else{
                    CREATExTASKxSTATUSxMESSAGE(
                        WRITE_CONFIG,                                           // TASK_ID
                        BOOTLOADER_START_BYTE_ERROR,                            // TASK_COMMAND
                        ERROR,                                                  // TASK_STATE
                        NONE);                                                  // TASK_MESSAGE
                    SYS_MESSAGE("Fw handler\t: WRITE_CONFIG BOOTLOADER_START_BYTE_ERROR.\n\r");
                    return_val              = ERROR;
                }
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            else if(btldrData.btldr_receive_error){
                CREATExTASKxSTATUSxMESSAGE(
                    WRITE_CONFIG,                                               // TASK_ID
                    BOOTLOADER_DATA_RECEIVE_ERROR,                              // TASK_COMMAND
                    ERROR,                                                      // TASK_STATE
                    NONE);                                                      // TASK_MESSAGE
                SYS_MESSAGE("Fw handler\t: WRITE_CONFIG BOOTLOADER_DATA_RECEIVE_ERROR.\n\r");
                return_val                  = ERROR;
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            else if((READxCORExTIMER() - DelayCount) > (1 * SECONDS)){
                CREATExTASKxSTATUSxMESSAGE(
                    WRITE_CONFIG,                                               // TASK_ID
                    WRITE_FLASH_RECEIVE_DATA_TIMEOUT,                           // TASK_COMMAND
                    ERROR,                                                      // TASK_STATE
                    NONE);                                                      // TASK_MESSAGE
                SYS_MESSAGE("Fw handler\t: WRITE_CONFIG WRITE_CONFIG_RECEIVE_DATA_TIMEOUT.\n\r");
                return_val                  = ERROR;
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            break;
        }
        
        default:
        {
            btldrData.sequence = 0;
            break;
        }
    }
    
    return (return_val);
}

/*#--------------------------------------------------------------------------#*/
/*  Description: uint8_t WRITExCONFIG(uint8_t *config_data)
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
 *  Notes      :
 */
/*#--------------------------------------------------------------------------#*/
uint32_t CHECKxCHECKSUM(uint16_t flash_bootloader_offset, uint16_t flash_end_address, 
        uint16_t file_checksum){
    
    uint32_t return_val = BUSY;
    
    uint16_t ChecksumLength = ((flash_end_address - flash_bootloader_offset)- 2);
            
    switch(btldrData.sequence){
        case 0:
        {
            btldrDataSend.bootloader_start_byte = 0x55;
            btldrDataSend.command               = (uint8_t)CMD_CALC_CHECKSUM;
            btldrDataSend.data_length_high      = (ChecksumLength >> 8) & 0x00FF;
            btldrDataSend.data_length_low       = (ChecksumLength & 0x00FF);
            btldrDataSend.unlock_hgh            = 0xAA;
            btldrDataSend.unlock_low            = 0x55;
            btldrDataSend.address_low           = (flash_bootloader_offset & 0x00FF);
            btldrDataSend.address_hgh           = (flash_bootloader_offset >> 8) & 0x00FF;
            btldrDataSend.address_upp           = 0x00;
            btldrDataSend.address_ext           = 0x00;
            
            SendDataToBootloader((BTDR_SEND_DATA_FORMAT *)&btldrDataSend);
            DelayCount                          = READxCORExTIMER();
            btldrData.sequence++;
            break;
        }
        
        case 1:
        {
            if(btldrData.btldr_datacount > 11){
                if(btldrDataReceive.bootloader_start_byte == 0x55){
                    if(btldrDataReceive.status[0] == (file_checksum & 0x00FF) &&
                       btldrDataReceive.status[1] == ((file_checksum >> 8) & 0x00FF)){
                        CREATExTASKxSTATUSxMESSAGE(
                            CHECK_CHECKSUM_CONFIG,                              // TASK_ID
                            CHECK_CHECKSUM_CONFIG_RETURNED_OK,                  // TASK_COMMAND
                            DONE,                                               // TASK_STATE
                            NONE);                                              // TASK_MESSAGE
                        SYS_MESSAGE("Fw handler\t: CHECK_CHECKSUM_CONFIG CHECK_CHECKSUM_CONFIG_RETURNED_OK.\n\r");
                        return_val          = DONE;                        
                    }
                    else{
                        CREATExTASKxSTATUSxMESSAGE(
                            CHECK_CHECKSUM_CONFIG,                              // TASK_ID
                            CHECK_CHECKSUM_CONFIG_RETURNED_NOK,                 // TASK_COMMAND
                            ERROR,                                              // TASK_STATE
                            NONE);                                              // TASK_MESSAGE
                        SYS_MESSAGE("Fw handler\t: CHECK_CHECKSUM_CONFIG CHECK_CHECKSUM_CONFIG_RETURNED_NOK.\n\r");
                    return_val              = ERROR;
                    }
                    ClearOldBtldrData();
                    ClearOldBtldrReceiveData();
                }
                else{
                    CREATExTASKxSTATUSxMESSAGE(
                        CHECK_CHECKSUM_CONFIG,                                  // TASK_ID
                        BOOTLOADER_START_BYTE_ERROR,                            // TASK_COMMAND
                        ERROR,                                                  // TASK_STATE
                        NONE);                                                  // TASK_MESSAGE
                    SYS_MESSAGE("Fw handler\t: CHECK_CHECKSUM_CONFIG BOOTLOADER_START_BYTE_ERROR.\n\r");
                    return_val              = ERROR;
                }
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            else if(btldrData.btldr_receive_error){
                CREATExTASKxSTATUSxMESSAGE(
                    CHECK_CHECKSUM_CONFIG,                                      // TASK_ID
                    BOOTLOADER_DATA_RECEIVE_ERROR,                              // TASK_COMMAND
                    ERROR,                                                      // TASK_STATE
                    NONE);                                                      // TASK_MESSAGE
                SYS_MESSAGE("Fw handler\t: CHECK_CHECKSUM_CONFIG BOOTLOADER_DATA_RECEIVE_ERROR.\n\r");
                return_val                  = ERROR;
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            else if((READxCORExTIMER() - DelayCount) > (1 * SECONDS)){
                CREATExTASKxSTATUSxMESSAGE(
                    CHECK_CHECKSUM_CONFIG,                                      // TASK_ID
                    CHECK_CHECKSUM_CONFIG_RECEIVE_DATA_TIMEOUT,                 // TASK_COMMAND
                    ERROR,                                                      // TASK_STATE
                    NONE);                                                      // TASK_MESSAGE
                SYS_MESSAGE("Fw handler\t: CHECK_CHECKSUM_CONFIG CHECK_CHECKSUM_CONFIG_RECEIVE_DATA_TIMEOUT.\n\r");
                return_val                  = ERROR;
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            break;
        }
        
        default:
        {
            btldrData.sequence = 0;
            break;
        }
    }
    
    return (return_val);
}

/*#--------------------------------------------------------------------------#*/
/*  Description: uint8_t RESETxSLAVE()
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
 *  Notes      :
 */
/*#--------------------------------------------------------------------------#*/

uint32_t RESETxSLAVE(){
    uint32_t return_val = BUSY;
    
    switch(btldrData.sequence){
        case 0:
        {
            btldrDataSend.bootloader_start_byte = 0x55;
            btldrDataSend.command               = (uint8_t)CMD_RESET_DEVICE;
            btldrDataSend.data_length_high      = 0x00;
            btldrDataSend.data_length_low       = 0x00;
            btldrDataSend.unlock_hgh            = 0x00;
            btldrDataSend.unlock_low            = 0x00;
            btldrDataSend.address_low           = 0x00;
            btldrDataSend.address_hgh           = 0x00;
            btldrDataSend.address_upp           = 0x00;
            btldrDataSend.address_ext           = 0x00;

            SendDataToBootloader((BTDR_SEND_DATA_FORMAT *)&btldrDataSend);
            DelayCount                          = READxCORExTIMER();
            btldrData.sequence++;
            break;
        }
        
        case 1:
        {
            if(btldrData.btldr_datacount > 10){
                if(btldrDataReceive.bootloader_start_byte == 0x55){
                    if(btldrDataReceive.status[0] == 1){
                        CREATExTASKxSTATUSxMESSAGE(
                            RESET_SLAVE,                                        // TASK_ID
                            RESET_SLAVE_OK,                                     // TASK_COMMAND
                            DONE,                                               // TASK_STATE
                            NONE);                                              // TASK_MESSAGE
                        SYS_MESSAGE("Fw handler\t: RESET_SLAVE RESET_SLAVE_OK.\n\r");
                        return_val          = DONE;
                    }
                    else{
                        CREATExTASKxSTATUSxMESSAGE(
                            RESET_SLAVE,                                        // TASK_ID
                            RESET_SLAVE_NOK,                                    // TASK_COMMAND
                            ERROR,                                              // TASK_STATE
                            NONE);                                              // TASK_MESSAGE
                        SYS_MESSAGE("Fw handler\t: RESET_SLAVE RESET_SLAVE_NOK.\n\r");
                    return_val              = ERROR;
                    }
                }
                else{
                    CREATExTASKxSTATUSxMESSAGE(
                        RESET_SLAVE,                                            // TASK_ID
                        BOOTLOADER_START_BYTE_ERROR,                            // TASK_COMMAND
                        ERROR,                                                  // TASK_STATE
                        NONE);                                                  // TASK_MESSAGE
                    SYS_MESSAGE("Fw handler\t: RESET_SLAVE BOOTLOADER_START_BYTE_ERROR.\n\r");
                    return_val              = ERROR;
                }
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            else if(btldrData.btldr_receive_error){
                CREATExTASKxSTATUSxMESSAGE(
                    RESET_SLAVE,                                                // TASK_ID
                    BOOTLOADER_DATA_RECEIVE_ERROR,                              // TASK_COMMAND
                    ERROR,                                                      // TASK_STATE
                    NONE);                                                      // TASK_MESSAGE
                SYS_MESSAGE("Fw handler\t: RESET_SLAVE BOOTLOADER_DATA_RECEIVE_ERROR.\n\r");
                return_val                  = ERROR;
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            else if((READxCORExTIMER() - DelayCount) > (100 * MILISECONDS)){
                CREATExTASKxSTATUSxMESSAGE(
                    RESET_SLAVE,                                                // TASK_ID
                    RESET_SLAVE_DATA_TIMEOUT,                                   // TASK_COMMAND
                    ERROR,                                                      // TASK_STATE
                    NONE);                                                      // TASK_MESSAGE
                SYS_MESSAGE("Fw handler\t: RESET_SLAVE RESET_SLAVE_DATA_TIMEOUT.\n\r");
                return_val                  = ERROR;
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            break;
        }
        
        default:
        {
            btldrData.sequence = 0;
            break;
        }
    }
    
    return (return_val);
}

/*#--------------------------------------------------------------------------#*/
/*  Description: bool SendDataToBootloader(uint8_t *data, uint8_t Length)
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
 *  Notes      :
 */
/*#--------------------------------------------------------------------------#*/
bool SendDataToBootloader(BTDR_SEND_DATA_FORMAT *btldrDataSend)
{
    uint8_t DummyCounter = 0;
    uint8_t Length = 0;
    uint8_t *btldr_p;
    
    if(btldrDataSend->command == CMD_READ_VERSION  || 
       btldrDataSend->command == CMD_READ_FLASH    || 
       btldrDataSend->command == CMD_ERASE_FLASH   || 
       btldrDataSend->command == CMD_READ_EE_DATA  || 
       btldrDataSend->command == CMD_READ_CONFIG   || 
       btldrDataSend->command == CMD_CALC_CHECKSUM ||
       btldrDataSend->command == CMD_RESET_DEVICE
       ){
        Length = 10;
    }
    else if(btldrDataSend->command == CMD_WRITE_FLASH){
        Length = 74;//sizeof(BTDR_SEND_DATA_FORMAT);
    }
    else if(btldrDataSend->command == CMD_WRITE_CONFIG){
        Length = 22;
    }
    else{
        SYS_MESSAGE("BOOTLOADER\t: Bootloader command not implemented!.\n\r");
        return false;        
    }
    
    btldr_p = &btldrDataSend->bootloader_start_byte;
    
    Led1On();
    
    for(DummyCounter=0; DummyCounter<Length; DummyCounter++){
        while(DRV_USART1_TransmitBufferIsFull() );
        DRV_USART1_WriteByte(*btldr_p);
        //SYS_PRINT("%d.\n\r", *btldr_p);
        btldr_p++;
    }   

    Led1Off();
    
    return true;
}

/*#--------------------------------------------------------------------------#*/
/*  Description: void SLAVExBOOTLOADERxDATAxRETURNED(uint8_t *buffer)
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
void SLAVExBOOTLOADERxDATAxRETURN(uint8_t data){
    
    switch (btldrData.btldr_data_return_sequence){
        case 0:
        {
            btldrDataReceive.bootloader_start_byte    = data;
            btldrData.p_btldr           = &btldrDataReceive.command;
            btldrData.btldr_data_return_sequence++;
            btldrData.btldr_datacount++;
            break;
        }
        
        case 1:
        {
            *btldrData.p_btldr = data;
            btldrData.p_btldr++;
            btldrData.btldr_datacount++;
            break;
        }
        
        default:
        {
            break;
        }
    }
}


void ClearOldBtldrData(){
    
    uint8_t i = 0;
    
    for(i = 0; i < sizeof(btldrDataReceive.status); i++){
        btldrDataReceive.status[i] = 0;
    }
    
    btldrData.sequence                      = 0;
    btldrData.btldr_sequence                = 0;
    btldrData.btldr_datacount               = 0;
    btldrData.btldr_receive_error           = 0;
    btldrData.flashrowcounter               = 0;
    btldrData.btldr_data_return_sequence    = 0;
}

void ClearOldBtldrReceiveData(){
    
    uint8_t i = 0;
    uint8_t *btldr_p;
    
    btldr_p = &btldrDataReceive.bootloader_start_byte;
    
    for(i = 0; i < 74; i++){
        *btldr_p = 0;
        btldr_p++;
    }
    
    btldrData.btldr_data_return_sequence    = 0;
}
/* *****************************************************************************
 End of File
 */
