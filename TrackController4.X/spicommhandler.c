#include <xc.h>
#include "main.h"
#include "mcc_generated_files/mcc.h"
#include "spicommhandler.h"

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
//#define DATAxSTRUCTxLENGTH sizeof(SLAVE_INFO)

static SLAVE_INFO *MASTER_SLAVE_DATA = 0;                                       // Holds the address were the received slave data is stored
static SLAVE_INFO SlaveInfoReadMask, SlaveInfoWriteMask = 0;                    // Read and write mask for slave data from and to ModbusMaster
static unsigned char   *pSlaveDataReceived, *pSlaveDataSend, 
                *pSlaveInfoReadMask, *pSlaveInfoWriteMask;

static unsigned char DataFromSlaveSend = 1;                                     // Data to send counter
static unsigned char DataReceivedOk = 0;

const unsigned char DATAxSTRUCTxLENGTH = sizeof(SLAVE_INFO);
//const unsigned char DATAxSTRUCTxLENGTH = sizeof(MASTER_SLAVE_DATA[0]);

static unsigned char RECEIVEDxDATAxRAW[DATAxSTRUCTxLENGTH+1];                   // One dummy byte extra (SPI master will send extra byte to receive last byte from slave)
static unsigned char SENDxDATAxRAW[DATAxSTRUCTxLENGTH];
static unsigned int DATAxCOUNTxRECEIVED = 0;
static unsigned int DATAxCOUNTxSEND = 0;
static unsigned int DATAxREADY = 0;

unsigned int SpiSlaveCommErrorCounter;

void InitSlaveCommunication(SLAVE_INFO *location)                                  
{   
    MASTER_SLAVE_DATA  =  location;
    
    SlaveInfoReadMask.Header           = 0x00;
    SlaveInfoReadMask.SlaveNumber      = 0x00;                                  // Mask for data read from Modbus Master to written to local MASTER_SLAVE_DATA
    SlaveInfoReadMask.HoldingRegRdSl[0]= 0xFFFF;
    SlaveInfoReadMask.HoldingRegRdSl[1]= 0xFFFF;
    SlaveInfoReadMask.HoldingRegRdSl[2]= 0xFFFF;
    SlaveInfoReadMask.HoldingRegRdSl[3]= 0xFFFF;
    SlaveInfoReadMask.HoldingRegWrSl[0]= 0x0000;
    SlaveInfoReadMask.HoldingRegWrSl[1]= 0x0000;
    SlaveInfoReadMask.HoldingRegWrSl[2]= 0x0000;
    SlaveInfoReadMask.HoldingRegWrSl[3]= 0x0000;
    SlaveInfoReadMask.InputReg[0]      = 0xFFFF;
    SlaveInfoReadMask.InputReg[1]      = 0xFFFF;
    SlaveInfoReadMask.DiagReg[0]       = 0xFFFF;
    SlaveInfoReadMask.DiagReg[1]       = 0xFFFF;
    SlaveInfoReadMask.DiagReg[2]       = 0xFFFF;
    SlaveInfoReadMask.DiagReg[3]       = 0xFFFF;
    SlaveInfoReadMask.MbReceiveCounter = 0xFFFF;
    SlaveInfoReadMask.MbSentCounter    = 0xFFFF;
    SlaveInfoReadMask.MbCommError      = 0xFF;
    SlaveInfoReadMask.MbExceptionCode  = 0xFF;
    SlaveInfoReadMask.SpiCommErrorCounter = 0xFFFF;
    SlaveInfoReadMask.Footer           = 0x00;
    
    SlaveInfoWriteMask.Header           = 0xFF;
    SlaveInfoWriteMask.SlaveNumber      = 0xFF;                                 // Mask for data write to Modbus Master from local MASTER_SLAVE_DATA
    SlaveInfoWriteMask.HoldingRegRdSl[0]= 0x0000;
    SlaveInfoWriteMask.HoldingRegRdSl[1]= 0x0000;
    SlaveInfoWriteMask.HoldingRegRdSl[2]= 0x0000;
    SlaveInfoWriteMask.HoldingRegRdSl[3]= 0x0000;
    SlaveInfoWriteMask.HoldingRegWrSl[0]= 0xFFFF;
    SlaveInfoWriteMask.HoldingRegWrSl[1]= 0xFFFF;
    SlaveInfoWriteMask.HoldingRegWrSl[2]= 0xFFFF;
    SlaveInfoWriteMask.HoldingRegWrSl[3]= 0xFFFF;
    SlaveInfoWriteMask.InputReg[0]      = 0x0000;
    SlaveInfoWriteMask.InputReg[1]      = 0x0000;
    SlaveInfoWriteMask.DiagReg[0]       = 0x0000;
    SlaveInfoWriteMask.DiagReg[1]       = 0x0000;
    SlaveInfoWriteMask.DiagReg[2]       = 0x0000;
    SlaveInfoWriteMask.DiagReg[3]       = 0x0000;
    SlaveInfoWriteMask.MbReceiveCounter = 0x0000;
    SlaveInfoWriteMask.MbSentCounter    = 0x0000;
    SlaveInfoWriteMask.MbCommError      = 0x00;
    SlaveInfoWriteMask.MbExceptionCode  = 0x00;
    SlaveInfoWriteMask.SpiCommErrorCounter = 0x0000;
    SlaveInfoWriteMask.Footer           = 0xFF;
    
    SpiSlaveCommErrorCounter = 0;
    /*
     * Init SPI first byte to send
    */
    pSlaveDataSend = &(MASTER_SLAVE_DATA[0].Header);                       // set the pointer to the first element of the slave number
    pSlaveInfoWriteMask = &(SlaveInfoWriteMask.Header);                    // Set the write mask pointer
    for(unsigned int i = 0; i < DATAxSTRUCTxLENGTH; i++){
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

void ProcessSpiInterrupt(){
    SS1_Check_LAT = 1; 
    RECEIVEDxDATAxRAW[DATAxCOUNTxRECEIVED] = SSP2BUF;                       
    DATAxCOUNTxRECEIVED++;            

    SSP2BUF = SENDxDATAxRAW[DATAxCOUNTxSEND];
    DATAxCOUNTxSEND++;
    SS1_Check_LAT = 0;
    
    while(!DATAxREADY){
        if (SSP2STATbits.BF){
            SS1_Check_LAT = 1;
            
            RECEIVEDxDATAxRAW[DATAxCOUNTxRECEIVED] = SSP2BUF;                       
            DATAxCOUNTxRECEIVED++;            
            
            SSP2BUF = SENDxDATAxRAW[DATAxCOUNTxSEND];
            DATAxCOUNTxSEND++;
            
            if (DATAxCOUNTxRECEIVED > DATAxSTRUCTxLENGTH){
                DATAxREADY = 1;
            }
            
            SS1_Check_LAT = 0;        
        }
    }
    
    SS1_Check_LAT = 1;
    
    DATAxREADY = 0;
    DATAxCOUNTxRECEIVED = 0; 
    DATAxCOUNTxSEND     = 0;
    ProcessSpiData();   
    SSP2BUF = 0;
                 
    SS1_Check_LAT = 0;  
}

void ProcessSpiData(){        
    
    
    pSlaveDataReceived = &(MASTER_SLAVE_DATA[RECEIVEDxDATAxRAW[1]].Header);// set the pointer to the first element of the received slave number in RECEIVEDxDATAxRAW[2]([1]==header)
    pSlaveDataSend = &(MASTER_SLAVE_DATA[DataFromSlaveSend].Header);       // set the pointer to the first element of the MASTER_SLAVE_DATA according to the DataFromSlaveSend counter
    pSlaveInfoReadMask = &(SlaveInfoReadMask.Header);                      // set the pointer to the first element of the SlaveInfoReadMask
    pSlaveInfoWriteMask = &(SlaveInfoWriteMask.Header);                    // set the pointer to the first element of the SlaveInfoWriteMask

    if(RECEIVEDxDATAxRAW[1] < NUMBER_OF_SLAVES && RECEIVEDxDATAxRAW[0]==0xAA && RECEIVEDxDATAxRAW[DATAxSTRUCTxLENGTH-1]==0x55){
        DataReceivedOk = 1;
    }
    else{
        DataReceivedOk = 0;
        SpiSlaveCommErrorCounter += 1;
    }
    
    for(unsigned int i = 0; i < DATAxSTRUCTxLENGTH; i++){                       // last received dummy byte is not used/checked
        
        if(*pSlaveInfoReadMask && DataReceivedOk){                              // If data received is OK and mask approves a write then process
            *pSlaveDataReceived = RECEIVEDxDATAxRAW[i];                       // for DATAxSTRUCTxLENGTH set every byte into RECEIVEDxDATAxRAW array according to read mask
        }
        SENDxDATAxRAW[i] = (unsigned char)(*pSlaveDataSend  & *pSlaveInfoWriteMask);// for DATAxSTRUCTxLENGTH set every byte into SENDxDATAxRAW+ array according to write mask
        pSlaveDataReceived  += 1;                                               // Increment pointer
        pSlaveInfoReadMask  += 1;                                               // Increment pointer
        pSlaveDataSend      += 1;                                               // Increment pointer
        pSlaveInfoWriteMask += 1;                                               // Increment pointer
    }

    DataFromSlaveSend++;                                                        // Count down the slaves of which the info still need to be send
    if(DataFromSlaveSend > (NUMBER_OF_SLAVES - 1)){
        DataFromSlaveSend = 0;
    }
    
}

/*#--------------------------------------------------------------------------#*/
/*  Description: 
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