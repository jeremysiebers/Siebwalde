#include <xc.h>
#include "main.h"
#include "mcc_generated_files/mcc.h"

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
static SLAVE_INFO *MASTER_SLAVE_DATA = 0;                                       // Holds the address were the received slave data is stored
static SLAVE_INFO SlaveInfoReadMask, SlaveInfoWriteMask = 0;                    // Read and write mask for slave data from and to ModbusMaster
unsigned char   *pSlaveDataReceived, *pSlaveDataSend, 
                *pSlaveInfoReadMask, *pSlaveInfoWriteMask;
static unsigned char SS1_PORT_prev = 1;
static unsigned char DataFromSlaveSend = 1;                                     // Data to send counter

void InitSlaveCommunication(SLAVE_INFO *location)                                  
{   
    MASTER_SLAVE_DATA  =  location;
    
    SlaveInfoReadMask.SlaveNumber      	= 0x00;                                 // Mask for data read from Modbus Master to written to local MASTER_SLAVE_DATA
    SlaveInfoReadMask.HoldingReg[0]    	= 0xFFFF;
    SlaveInfoReadMask.HoldingReg[1]    	= 0xFFFF;
    SlaveInfoReadMask.HoldingReg[2]    	= 0xFFFF;
    SlaveInfoReadMask.HoldingReg[3]    	= 0xFFFF;
    SlaveInfoReadMask.InputReg[0]      	= 0xFFFF;
    SlaveInfoReadMask.InputReg[1]      	= 0xFFFF;
    SlaveInfoReadMask.DiagReg[0]       	= 0xFFFF;
    SlaveInfoReadMask.DiagReg[1]       	= 0xFFFF;
    SlaveInfoReadMask.DiagReg[2]       	= 0xFFFF;
    SlaveInfoReadMask.DiagReg[3]       	= 0xFFFF;
    SlaveInfoReadMask.MbReceiveCounter 	= 0xFFFF;
    SlaveInfoReadMask.MbSentCounter    	= 0xFFFF;
    SlaveInfoReadMask.MbCommError      	= 0xFFFF;
    SlaveInfoReadMask.MbExceptionCode  	= 0xFF;
    
    SlaveInfoWriteMask.SlaveNumber      = 0xFF;                                 // Mask for data write to Modbus Master from local MASTER_SLAVE_DATA
    SlaveInfoWriteMask.HoldingReg[0]    = 0xFFFF;
    SlaveInfoWriteMask.HoldingReg[1]    = 0xFFFF;
    SlaveInfoWriteMask.HoldingReg[2]    = 0xFFFF;
    SlaveInfoWriteMask.HoldingReg[3]    = 0xFFFF;
    SlaveInfoWriteMask.InputReg[0]      = 0x0000;
    SlaveInfoWriteMask.InputReg[1]      = 0x0000;
    SlaveInfoWriteMask.DiagReg[0]       = 0x0000;
    SlaveInfoWriteMask.DiagReg[1]       = 0x0000;
    SlaveInfoWriteMask.DiagReg[2]       = 0x0000;
    SlaveInfoWriteMask.DiagReg[3]       = 0x0000;
    SlaveInfoWriteMask.MbReceiveCounter = 0x0000;
    SlaveInfoWriteMask.MbSentCounter    = 0x0000;
    SlaveInfoWriteMask.MbCommError      = 0x0000;
    SlaveInfoWriteMask.MbExceptionCode  = 0x00;
    
    /*
     * Init SPI first byte to send
    */
    pSlaveDataSend = &(MASTER_SLAVE_DATA[0].SlaveNumber);                       // set the pointer to the first element of the slave number
    pSlaveInfoWriteMask = &(SlaveInfoWriteMask.SlaveNumber);                    // Set the write mask pointer
    for(char i = 0; i < DATAxSTRUCTxLENGTH; i++){
        SENDxDATAxRAW[i] = (unsigned char)(*pSlaveDataSend  & *pSlaveInfoWriteMask);// for DATAxSTRUCTxLENGTH set every byte into SENDxDATAxRAW+ array according to write mask
        pSlaveDataSend += 1;                                                    // Increment pointer
        pSlaveInfoWriteMask += 1;                                               // Increment pointer
    }    
}

/*#--------------------------------------------------------------------------#*/
/*  Description: ProcessSpiData()
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

void ProcessSpiData(){        
    
    Read_Check_LAT = 1;
    pSlaveDataReceived = &(MASTER_SLAVE_DATA[RECEIVEDxDATAxRAW[0]].SlaveNumber);// set the pointer to the first element of the received slave number in RECEIVEDxDATAxRAW[0]
    pSlaveDataSend = &(MASTER_SLAVE_DATA[DataFromSlaveSend].SlaveNumber);       // set the pointer to the first element of the MASTER_SLAVE_DATA according to the DataFromSlaveSend counter
    pSlaveInfoReadMask = &(SlaveInfoReadMask.SlaveNumber);                      // set the pointer to the first element of the SlaveInfoReadMask
    pSlaveInfoWriteMask = &(SlaveInfoWriteMask.SlaveNumber);                    // set the pointer to the first element of the SlaveInfoWriteMask

    for(char i = 0; i < DATAxSTRUCTxLENGTH; i++){
        if(*pSlaveInfoReadMask){
            *pSlaveDataReceived = RECEIVEDxDATAxRAW[i];                         // for DATAxSTRUCTxLENGTH set every byte into RECEIVEDxDATAxRAW array according to read mask
        }
        SENDxDATAxRAW[i] = (unsigned char)(*pSlaveDataSend  & *pSlaveInfoWriteMask);// for DATAxSTRUCTxLENGTH set every byte into SENDxDATAxRAW+ array according to write mask
        pSlaveDataReceived  += 1;                                               // Increment pointer
        pSlaveInfoReadMask  += 1;                                               // Increment pointer
        pSlaveDataSend      += 1;                                               // Increment pointer
        pSlaveInfoWriteMask += 1;                                               // Increment pointer
    }

    DataFromSlaveSend++;                                                        // Count down the slaves of which the info still need to be send
    if(DataFromSlaveSend > NUMBER_OF_SLAVES - 1){
        DataFromSlaveSend = 0;
    }
    Read_Check_LAT = 0;        
}

/*#--------------------------------------------------------------------------#*/
/*  Description: CheckSpiStart()
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

void CheckSpiStart(){
    if (SS1_PORT && SS1_PORT_prev){                                             // When SPI is idle, already provide the first byte of the message to send to the ModbusMaster
        Read_Check_LAT = 1;
        DATAxCOUNTxRECEIVED = 0;
        SSP1BUF = SENDxDATAxRAW[DATAxCOUNTxSEND];                               // load first byte[0] (DATAxCOUNTxSEND==0 is set in interrupt)
        DATAxCOUNTxSEND = 1;                                                    // set that first byte is sent already as soon as the master starts clocking
        SS1_PORT_prev = 0;
        Read_Check_LAT = 0;
    }
    else if(!SS1_PORT && !SS1_PORT_prev){
        SS1_PORT_prev = 1;        
    }
}