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
unsigned char *pSlaveData;
static unsigned char SS1_PORT_prev = 1;
static unsigned char DataFromSlaveSend = 1;                                     // Data to send counter

void InitSlaveCommunication(SLAVE_INFO *location)                                  
{   
    MASTER_SLAVE_DATA  =  location;
    
    /*
     * Init SPI first byte to send will be last slave number
    */
    pSlaveData = &(MASTER_SLAVE_DATA[0].SlaveNumber);                           // set the pointer to the first element of the slave number
    for(char i = 0; i < DATAxSTRUCTxLENGTH; i++){
        SENDxDATAxRAW[i] = *pSlaveData;                                         // for DATAxSTRUCTxLENGTH set every byte into SENDxDATAxRAW+ array
        pSlaveData += 1;                                                        // Increment pointer
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
    
    if (SS1_PORT && SS1_PORT_prev){
        Read_Check_LAT = 1;
        DATAxCOUNTxRECEIVED = 0;
        SSP1BUF = SENDxDATAxRAW[DATAxCOUNTxSEND];                               // load first byte[0]
        DATAxCOUNTxSEND = 1;                                                    // set that first byte is sent already as soon as the master starts clocking
        SS1_PORT_prev = 0;
        Read_Check_LAT = 0;
    }
    else if(!SS1_PORT && !SS1_PORT_prev){
        SS1_PORT_prev = 1;
        //Read_Check_LAT = 0;
    }
        
    if(DATAxREADY){
        Read_Check_LAT = 1;
        pSlaveData = &(MASTER_SLAVE_DATA[RECEIVEDxDATAxRAW[0]].SlaveNumber);    // set the pointer to the first element of the received slave number in RECEIVEDxDATAxRAW[0]
        for(char i = 0; i < DATAxSTRUCTxLENGTH; i++){
            *pSlaveData = RECEIVEDxDATAxRAW[i];                                 // for DATAxSTRUCTxLENGTH set every byte into RECEIVEDxDATAxRAW array
            pSlaveData += 1;                                                    // Increment pointer
        }
        
        pSlaveData = &(MASTER_SLAVE_DATA[DataFromSlaveSend].SlaveNumber);       // set the pointer to the first element of the received slave number in RECEIVEDxDATAxRAW[0]
        for(char i = 0; i < DATAxSTRUCTxLENGTH; i++){
            SENDxDATAxRAW[i] = *pSlaveData;                                     // for DATAxSTRUCTxLENGTH set every byte into SENDxDATAxRAW+ array
            pSlaveData += 1;                                                    // Increment pointer
        }
        DataFromSlaveSend++;
        if(DataFromSlaveSend > NUMBER_OF_SLAVES - 1){
            DataFromSlaveSend = 0;
        }
        
        DATAxREADY = 0;
        Read_Check_LAT = 0;
    }         
}