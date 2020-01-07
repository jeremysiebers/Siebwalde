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

TASK_STATE GETxBOOTxLOADERxVERSION(){
    uint32_t return_val = BUSY;
    
    switch(btldrData.sequence){
        case 0:
        {
            btldrDataSend.bootloader_start_byte = 0x55;
            btldrDataSend.command               = (uint8_t)READ_VERSION;
            btldrDataSend.data_length_high      = 0x00;
            btldrDataSend.data_length_low       = 0x00;
            btldrDataSend.unlock_hgh            = 0x00;
            btldrDataSend.unlock_low            = 0x00;
            btldrDataSend.address_low           = 0x00;
            btldrDataSend.address_hgh           = 0x00;
            btldrDataSend.address_upp           = 0x00;
            btldrDataSend.address_ext           = 0x00;

            SendDataToBootloader((BTDR_SEND_DATA_FORMAT *)&btldrDataSend);
            btldrData.sequence++;
            break;
        }
        
        case 1:
        {
            if(btldrData.btldr_datacount > 25){
                if(btldrDataReceive.bootloader_start_byte == 0x55 &&
                        btldrDataReceive.status[0] == 0 &&
                        btldrDataReceive.status[1] == 1){
                    return_val              = DONE;
                }
                else{
                    return_val              = ERROR;
                }
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            else if(btldrData.btldr_receive_error){
                return_val                      = ERROR;
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

TASK_STATE ERASExFLASH(uint16_t flash_bootloader_offset, uint16_t flash_end_address){
    uint32_t return_val = BUSY;
    
    uint16_t EraseRows = ((flash_end_address - flash_bootloader_offset)/BLOCKWIDTH);
    
    switch(btldrData.sequence){
        case 0:
        {
            btldrDataSend.bootloader_start_byte = 0x55;
            btldrDataSend.command               = (uint8_t)ERASE_FLASH;
            btldrDataSend.data_length_high      = (EraseRows >> 8) & 0x00FF;
            btldrDataSend.data_length_low       = (EraseRows & 0x00FF);
            btldrDataSend.unlock_hgh            = 0xAA;
            btldrDataSend.unlock_low            = 0x55;
            btldrDataSend.address_low           = (flash_bootloader_offset & 0x00FF);
            btldrDataSend.address_hgh           = (flash_bootloader_offset >> 8) & 0x00FF;
            btldrDataSend.address_upp           = 0x00;
            btldrDataSend.address_ext           = 0x00;

            SendDataToBootloader((BTDR_SEND_DATA_FORMAT *)&btldrDataSend);
            btldrData.sequence++;
            break;
        }
        
        case 1:
        {
            if(btldrData.btldr_datacount > 10){
                if(btldrDataReceive.bootloader_start_byte == 0x55 &&
                        btldrDataReceive.status[0] == 1){
                    return_val              = DONE;
                }
                else{
                    return_val              = ERROR;
                }
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            else if(btldrData.btldr_receive_error){
                return_val                      = ERROR;
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

TASK_STATE WRITExFLASH(uint16_t flash_bootloader_offset, uint16_t flash_end_address, uint8_t *fwfile){
    uint32_t return_val = BUSY;
    
    FlashRows = (flash_end_address - flash_bootloader_offset);
    
    switch(btldrData.sequence){
        case 0:
        {                
            FlashAddress = flash_bootloader_offset + btldrData.flashrowcounter;
    
            btldrDataSend.bootloader_start_byte = 0x55;
            btldrDataSend.command               = (uint8_t)WRITE_FLASH;
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
            btldrData.sequence++;
            btldrData.flashrowcounter += BLOCKWIDTH; // for bytes = *2
            break;
        }
        
        case 1:
        {
            if(btldrData.btldr_datacount > 10){
                if(btldrDataReceive.bootloader_start_byte == 0x55 &&
                        btldrDataReceive.status[0] == 1){
                    if (btldrData.flashrowcounter >= FlashRows){
//                        SYS_PRINT("flashrowcounter 0x%x\n\r", btldrData.flashrowcounter);
//                        SYS_PRINT("FlashRows 0x%x\n\r"      , FlashRows);
//                        SYS_PRINT("FlashAddress 0x%x\n\r"   , FlashAddress);
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
                    return_val              = ERROR;
                    ClearOldBtldrData();
                    ClearOldBtldrReceiveData();
                }                
            }
            else if(btldrData.btldr_receive_error){
                return_val                      = ERROR;
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
TASK_STATE WRITExCONFIG(uint8_t *config_data){
    uint32_t return_val = BUSY;
            
    switch(btldrData.sequence){
        case 0:
        {
            btldrDataSend.bootloader_start_byte = 0x55;
            btldrDataSend.command               = (uint8_t)WRITE_CONFIG;
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
            btldrData.sequence++;
            break;
        }
        
        case 1:
        {
            if(btldrData.btldr_datacount > 10){
                if(btldrDataReceive.bootloader_start_byte == 0x55 &&
                        btldrDataReceive.status[0] == 1){
                    return_val              = DONE;
                }
                else{
                    return_val              = ERROR;
                }
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            else if(btldrData.btldr_receive_error){
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
TASK_STATE CHECKxCHECKSUM(uint16_t flash_bootloader_offset, uint16_t flash_end_address, 
        uint16_t file_checksum){
    
    uint32_t return_val = BUSY;
    
    uint16_t ChecksumLength = ((flash_end_address - flash_bootloader_offset)- 2);
            
    switch(btldrData.sequence){
        case 0:
        {
            btldrDataSend.bootloader_start_byte = 0x55;
            btldrDataSend.command               = (uint8_t)CALC_CHECKSUM;
            btldrDataSend.data_length_high      = (ChecksumLength >> 8) & 0x00FF;
            btldrDataSend.data_length_low       = (ChecksumLength & 0x00FF);
            btldrDataSend.unlock_hgh            = 0xAA;
            btldrDataSend.unlock_low            = 0x55;
            btldrDataSend.address_low           = (flash_bootloader_offset & 0x00FF);
            btldrDataSend.address_hgh           = (flash_bootloader_offset >> 8) & 0x00FF;
            btldrDataSend.address_upp           = 0x00;
            btldrDataSend.address_ext           = 0x00;
            
            SendDataToBootloader((BTDR_SEND_DATA_FORMAT *)&btldrDataSend);
            btldrData.sequence++;
            break;
        }
        
        case 1:
        {
            if(btldrData.btldr_datacount > 11){
                if(btldrDataReceive.bootloader_start_byte == 0x55 &&
                        btldrDataReceive.status[0] == (file_checksum & 0x00FF) &&
                        btldrDataReceive.status[1] == ((file_checksum >> 8) & 0x00FF)){
                    return_val              = DONE;
                }
                else{
                    return_val              = ERROR;
                }
                ClearOldBtldrData();
                ClearOldBtldrReceiveData();
            }
            else if(btldrData.btldr_receive_error){
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
    
    if(btldrDataSend->command == READ_VERSION  || 
       btldrDataSend->command == READ_FLASH    || 
       btldrDataSend->command == ERASE_FLASH   || 
       btldrDataSend->command == READ_EE_DATA  || 
       btldrDataSend->command == READ_CONFIG   || 
       btldrDataSend->command == CALC_CHECKSUM
       ){
        Length = 10;
    }
    else if(btldrDataSend->command == WRITE_FLASH){
        Length = 74;//sizeof(BTDR_SEND_DATA_FORMAT);
    }
    else if(btldrDataSend->command == WRITE_CONFIG){
        Length = 22;
    }
    else{
        while(1);   // ---> implement dynamic length regarding eeprom etc
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
