#include <xc.h>
#include <string.h>
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
static SLAVE_INFO SlaveInfoReadMask = 0;                                        // Read mask for slave data from and to ModbusMaster
static SLAVE_INFO SlaveInfoWriteMask = 0;                                       // Write mask for slave data from and to ModbusMaster
static SLAVE_INFO SlaveInfoTraceMask = 0;                                       // Trace mask for slave data from and to ModbusMaster
static unsigned char   *pSlaveDataReceived, *pSlaveDataSend, 
                *pSlaveInfoReadMask, *pSlaveInfoWriteMask;
                //*pSlaveInforReadRaw;

static unsigned char DataFromSlaveSend = 0;                                     // Data to send counter
static unsigned char DataReceivedOk = 0;

const unsigned char DATAxSTRUCTxLENGTH = sizeof(SLAVE_INFO);
//const unsigned char DATAxSTRUCTxLENGTH = sizeof(MASTER_SLAVE_DATA[0]);

static unsigned char RECEIVEDxDATAxRAW[DATAxSTRUCTxLENGTH+1];                   // One dummy byte extra (SPI master will send extra byte to receive last byte from slave)
static unsigned char SENDxDATAxRAW[DATAxSTRUCTxLENGTH];
static unsigned int DATAxCOUNTxRECEIVED = 0;
static unsigned int DATAxCOUNTxSEND = 0;
static unsigned int DATAxREADY = 0;

unsigned int SpiSlaveCommErrorCounter;
unsigned bool InitPhase = true;

void INITxSPIxCOMMxHANDLER(SLAVE_INFO *location)                                  
{   
    MASTER_SLAVE_DATA  =  location;
    
    SlaveInfoReadMask.Header           = 0x00;
    SlaveInfoReadMask.SlaveNumber      = 0x00;                                  // Mask for data read from Modbus Master written to local MASTER_SLAVE_DATA
    SlaveInfoReadMask.HoldingReg[0]    = 0x0000;
    SlaveInfoReadMask.HoldingReg[1]    = 0x0000;
    SlaveInfoReadMask.HoldingReg[2]    = 0x0000;
    SlaveInfoReadMask.HoldingReg[3]    = 0x0000;
    SlaveInfoReadMask.InputReg[0]      = 0xFFFF;
    SlaveInfoReadMask.InputReg[1]      = 0xFFFF;
    SlaveInfoReadMask.InputReg[2]      = 0xFFFF;
    SlaveInfoReadMask.InputReg[3]      = 0xFFFF;
    SlaveInfoReadMask.InputReg[4]      = 0xFFFF;
    SlaveInfoReadMask.InputReg[5]      = 0xFFFF;
    SlaveInfoReadMask.DiagReg[0]       = 0xFFFF;
    SlaveInfoReadMask.DiagReg[1]       = 0xFFFF;
    SlaveInfoReadMask.MbReceiveCounter = 0xFFFF;
    SlaveInfoReadMask.MbSentCounter    = 0xFFFF;
    SlaveInfoReadMask.MbCommError      = 0xFF;
    SlaveInfoReadMask.MbExceptionCode  = 0xFF;
    SlaveInfoReadMask.SpiCommErrorCounter = 0xFFFF;
    SlaveInfoReadMask.Footer           = 0x00;
    
    SlaveInfoWriteMask.Header           = 0xFF;
    SlaveInfoWriteMask.SlaveNumber      = 0xFF;                                 // Mask for data write to Modbus Master from local MASTER_SLAVE_DATA
    SlaveInfoWriteMask.HoldingReg[0]    = 0xFFFF;
    SlaveInfoWriteMask.HoldingReg[1]    = 0xFFFF;
    SlaveInfoWriteMask.HoldingReg[2]    = 0xFFFF;
    SlaveInfoWriteMask.HoldingReg[3]    = 0xFFFF;
    SlaveInfoWriteMask.InputReg[0]      = 0x0000;
    SlaveInfoWriteMask.InputReg[1]      = 0x0000;
    SlaveInfoWriteMask.InputReg[2]      = 0x0000;
    SlaveInfoWriteMask.InputReg[3]      = 0x0000;
    SlaveInfoWriteMask.InputReg[4]      = 0x0000;
    SlaveInfoWriteMask.InputReg[5]      = 0x0000;
    SlaveInfoWriteMask.DiagReg[0]       = 0x0000;
    SlaveInfoWriteMask.DiagReg[1]       = 0x0000;
    SlaveInfoWriteMask.MbReceiveCounter = 0x0000;
    SlaveInfoWriteMask.MbSentCounter    = 0x0000;
    SlaveInfoWriteMask.MbCommError      = 0x00;
    SlaveInfoWriteMask.MbExceptionCode  = 0x00;
    SlaveInfoWriteMask.SpiCommErrorCounter = 0x0000;
    SlaveInfoWriteMask.Footer           = 0xFF;
    
//    SlaveInfoWriteMask.Header           = 0xFF;
//    SlaveInfoWriteMask.SlaveNumber      = 0xFF;                                 // Mask for data write to Modbus Master from local MASTER_SLAVE_DATA
//    SlaveInfoWriteMask.HoldingReg[0]    = 0xFFFF;
//    SlaveInfoWriteMask.HoldingReg[1]    = 0xFFFF;
//    SlaveInfoWriteMask.HoldingReg[2]    = 0xFFFF;
//    SlaveInfoWriteMask.HoldingReg[3]    = 0xFFFF;
//    SlaveInfoWriteMask.InputReg[0]      = 0xFFFF;
//    SlaveInfoWriteMask.InputReg[1]      = 0xFFFF;
//    SlaveInfoWriteMask.InputReg[2]      = 0xFFFF;
//    SlaveInfoWriteMask.InputReg[3]      = 0xFFFF;
//    SlaveInfoWriteMask.InputReg[4]      = 0xFFFF;
//    SlaveInfoWriteMask.InputReg[5]      = 0xFFFF;
//    SlaveInfoWriteMask.DiagReg[0]       = 0xFFFF;
//    SlaveInfoWriteMask.DiagReg[1]       = 0xFFFF;
//    SlaveInfoWriteMask.MbReceiveCounter = 0xFFFF;
//    SlaveInfoWriteMask.MbSentCounter    = 0xFFFF;
//    SlaveInfoWriteMask.MbCommError      = 0xFF;
//    SlaveInfoWriteMask.MbExceptionCode  = 0xFF;
//    SlaveInfoWriteMask.SpiCommErrorCounter = 0xFFFF;
//    SlaveInfoWriteMask.Footer           = 0xFF;
	    
    SpiSlaveCommErrorCounter = 0;
    /*
     * Init SPI first byte to send
    */
    pSlaveDataSend = &(MASTER_SLAVE_DATA[0].Header);                            // set the pointer to the first element of the slave number
    pSlaveInfoWriteMask = &(SlaveInfoWriteMask.Header);                         // Set the write mask pointer
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
void RESETxSPIxCOMMxHANDLER(){
    
    DATAxCOUNTxRECEIVED         =   0;
    DATAxCOUNTxSEND             =   0;
    DATAxREADY                  =   0;
    
    DataFromSlaveSend           =   0;
    SpiSlaveCommErrorCounter    =   0;
    
    MASTER_SLAVE_DATA[0].HoldingReg[0] = 0;
    MASTER_SLAVE_DATA[0].HoldingReg[1] = 0;
    MASTER_SLAVE_DATA[0].HoldingReg[2] = 0;
    MASTER_SLAVE_DATA[0].HoldingReg[3] = 0;
    
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

void PROCESSxSPIxMODBUS(){
    //SS1_Check_LAT = 1; 
    RECEIVEDxDATAxRAW[DATAxCOUNTxRECEIVED] = SSP2BUF;                       
    SSP2BUF = SENDxDATAxRAW[DATAxCOUNTxSEND];
    DATAxCOUNTxRECEIVED++;
    DATAxCOUNTxSEND++;
    //SS1_Check_LAT = 0;
    
    while(!DATAxREADY){
        
        
        if (SSP2STATbits.BF){  
            
            //SS1_Check_LAT = 1; 
            
            RECEIVEDxDATAxRAW[DATAxCOUNTxRECEIVED] = SSP2BUF; 
            SSP2BUF = SENDxDATAxRAW[DATAxCOUNTxSEND];
            
            //SS1_Check_LAT = 0;
            
            DATAxCOUNTxRECEIVED++;
            DATAxCOUNTxSEND++;
            
            if (DATAxCOUNTxRECEIVED > DATAxSTRUCTxLENGTH){
                DATAxREADY = 1;
            }      
        }
    }
    
    //SS1_Check_LAT = 1;
    
    DATAxREADY = 0;
    DATAxCOUNTxRECEIVED = 0; 
    DATAxCOUNTxSEND     = 0;
    ProcessModbusData();   
    SSP2BUF = 0;
                 
    //SS1_Check_LAT = 0;  

}

/*
void ProcessSpiData(){
    
    pSlaveDataReceived = &(MASTER_SLAVE_DATA[RECEIVEDxDATAxRAW[1]].Header);// set the pointer to the first element of the received slave number in RECEIVEDxDATAxRAW[2]([1]==header)
    pSlaveDataSend = &(MASTER_SLAVE_DATA[DataFromSlaveSend].Header);       // set the pointer to the first element of the MASTER_SLAVE_DATA according to the DataFromSlaveSend counter
        
    pSlaveInforReadRaw = (RECEIVEDxDATAxRAW);

    if(RECEIVEDxDATAxRAW[1] < NUMBER_OF_SLAVES && RECEIVEDxDATAxRAW[0]==0xAA && RECEIVEDxDATAxRAW[DATAxSTRUCTxLENGTH-1]==0x55){        
        pSlaveInforReadRaw+2;
        pSlaveDataReceived+2;
        memcpy(pSlaveDataReceived, RECEIVEDxDATAxRAW, 8);
        pSlaveInforReadRaw+16;
        pSlaveDataReceived+16;
        memcpy(pSlaveDataReceived, RECEIVEDxDATAxRAW, 39);
    }
    else{
        SpiSlaveCommErrorCounter += 1;
    }    
    memcpy(SENDxDATAxRAW, pSlaveDataSend, DATAxSTRUCTxLENGTH);

    DataFromSlaveSend++;                                                        // Count down the slaves of which the info still need to be send
    if(DataFromSlaveSend > (NUMBER_OF_SLAVES - 1)){
        DataFromSlaveSend = 0;
    }    
}
*/

void ProcessSpiData(){
    
    pSlaveDataReceived = &(MASTER_SLAVE_DATA[RECEIVEDxDATAxRAW[1]].Header);// set the pointer to the first element of the received slave number in RECEIVEDxDATAxRAW[1]
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
            *pSlaveDataReceived = RECEIVEDxDATAxRAW[i];                         // for DATAxSTRUCTxLENGTH set every byte into RECEIVEDxDATAxRAW array according to read mask                        
        }
        
        SENDxDATAxRAW[i] = (unsigned char)(*pSlaveDataSend  & *pSlaveInfoWriteMask);// for DATAxSTRUCTxLENGTH set every byte into SENDxDATAxRAW+ array according to write mask
        pSlaveDataReceived  += 1;                                               // Increment pointer
        pSlaveInfoReadMask  += 1;                                               // Increment pointer
        pSlaveDataSend      += 1;                                               // Increment pointer
        pSlaveInfoWriteMask += 1;                                               // Increment pointer
    }
    
    if (InitPhase == false){                                                     // When init phase is done, communicate data to all slaves
        DataFromSlaveSend++;                                                    // Count down the slaves of which the info still need to be send
        if(DataFromSlaveSend > (NUMBER_OF_SLAVES - 1)){
            DataFromSlaveSend = 0;
        }
    }
    else{
        DataFromSlaveSend = 0;
    }
    
}

/*#--------------------------------------------------------------------------#*/
/*  Description: PROCESSxSPIxBOOTLOAD()
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

uint8_t ReceivedBootloadData[76];
uint8_t SendBootloadData[76];
uint8_t BootloadDataCounterReceived = 0;
uint8_t BootloadDataCounterSend = 0;
uint8_t BootloadDataReady = 0;

void PROCESSxSPIxBOOTLOAD(){
    //SS1_Check_LAT = 1; 
    ReceivedBootloadData[BootloadDataCounterReceived] = SSP2BUF;                       
    SSP2BUF = SendBootloadData[BootloadDataCounterSend];
    BootloadDataCounterReceived++;
    BootloadDataCounterSend++;
    //SS1_Check_LAT = 0;
    
    while(!BootloadDataReady){
        
        
        if (SSP2STATbits.BF){  
            
            //SS1_Check_LAT = 1; 
            
            ReceivedBootloadData[BootloadDataCounterReceived] = SSP2BUF; 
            SSP2BUF = SendBootloadData[BootloadDataCounterSend];
            
            //SS1_Check_LAT = 0;
            
            BootloadDataCounterReceived++;
            BootloadDataCounterSend++;
            
            if (BootloadDataCounterReceived > 75){
                BootloadDataReady = 1;
            }      
        }
    }
    
    //SS1_Check_LAT = 1;
    
    BootloadDataReady = 0;
    BootloadDataCounterReceived = 0; 
    BootloadDataCounterSend     = 0;
    //ProcessBootloadData();   
    SSP2BUF = 0;
                 
    //SS1_Check_LAT = 0;
}

/*#--------------------------------------------------------------------------#*/
/*  Description: ProcessBootloadData()
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
void ProcessBootloadData(){
    
}