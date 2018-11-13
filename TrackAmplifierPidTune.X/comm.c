#include "regulator.h"
#include <xc.h>
#include "mcc_generated_files/mcc.h"
#include "processio.h"
#include "main.h"

#define EUSART_TX_BUFFER_SIZE 64
#define EUSART_RX_BUFFER_SIZE 64

uint8_t Received_data = 0;
uint8_t Data[20];
uint8_t DataCnt = 0;

void Communications(){
    
    Received_data = EUSART_is_rx_ready();
    
    /*
    if(Received_data > 4){
        
        for(uint8_t i = 0; i < Received_data + 1; i++){
            Data[i] = EUSART_Read();
        }
        
        if(Data[0] == 0xAA){
            if (Data[3] == 0xA){
                if (Data[4] == 0xD){
                    PetitHoldingRegisters[0].ActValue = Data[2];
                    PetitHoldingRegisters[0].ActValue |= (Data[1] << 8);
                }
            }
        }
        
    }*/
    
    if(Received_data > 0){
        
        Data[DataCnt] = EUSART_Read();
        DataCnt++;
        
        if(DataCnt > 4){
            if(Data[0] == 0xAA){
                if (Data[3] == 0xA){
                    if (Data[4] == 0xD){
                        PetitHoldingRegisters[0].ActValue = Data[2];
                        PetitHoldingRegisters[0].ActValue |= (Data[1] << 8);
                    }
                }
            }
            DataCnt = 0;
        }
        
    }
    
}