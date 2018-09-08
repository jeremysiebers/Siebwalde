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

void InitSlaveCommunication(SLAVE_INFO *location)                                  
{   
    MASTER_SLAVE_DATA  =  location;
}

/*#--------------------------------------------------------------------------#*/
/*  Description: SpiCommDataRaw(unsigned char data)
 *
 *  Input(s)   : location of stored raw data received by SPI
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
/*
 
 
static unsigned char ReceivedDataRaw[sizeof(MASTER_SLAVE_DATA[0])];
static unsigned char AllDataCompleet = sizeof(MASTER_SLAVE_DATA[0]);
static unsigned char DataCount = 0;
static unsigned char DataReady = 0;
unsigned char *pSlaveData;

unsigned char SpiCommDataRaw(unsigned char data){
       
    ReceivedDataRaw[DataCount] = data;
    
    DataCount++;
    if (DataCount >= AllDataCompleet){
       DataCount = 0; 
       
       pSlaveData = &(MASTER_SLAVE_DATA[ReceivedDataRaw[0]].SlaveNumber);
       for(char i = 0; i < AllDataCompleet; i++){
           *pSlaveData = ReceivedDataRaw[i];
           pSlaveData += 1;
       }
    }       
    return (0x00);
}
*/
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

unsigned char *pSlaveData;
static unsigned char AllDataCompleet = sizeof(MASTER_SLAVE_DATA[0]);

void ProcessSpiData(){
        
    if(DataReady){
        pSlaveData = &(MASTER_SLAVE_DATA[ReceivedDataRaw[0]].SlaveNumber);
        for(char i = 0; i < AllDataCompleet; i++){
            *pSlaveData = ReceivedDataRaw[i];
            pSlaveData += 1;
        }
    }         
}