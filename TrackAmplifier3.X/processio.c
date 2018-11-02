#include "processio.h"
#include "modbus/General.h"
#include "mcc_generated_files/mcc.h"
#include "mcc_generated_files/adcc.h"

uint8_t sequence = 0;
uint8_t Sequence_Bmf = 0;

void ProcessBMF (){
    if (ADCON0bits.ADGO==0){        
    
        switch(Sequence_Bmf){
            case 0:
                ADCC_StartConversion(BMF);
                Sequence_Bmf++;
                break;

            case 1:
                if (ADCON0bits.ADGO==0){
                    PetitInputRegisters[0].ActValue = ADCC_GetConversionResult();
                    Sequence_Bmf = 0;
                    PORTAbits.RA6 = 0;
                }
                break;

            default: 
                break;        
        }
    }
}

void ProcessIO (){
    //PetitInputRegisters[0].ActValue |= (unsigned int)(THFLG_PORT << 11);
    if (ADCON0bits.ADGO==0){
        switch(sequence){
            case 0:
                ADCC_StartConversion(BMF);
                sequence++;
                break;

            case 1:
                if (ADCON0bits.ADGO==0){
                    PetitInputRegisters[0].ActValue = ADCC_GetConversionResult();
                    sequence++;
                }
                break;

            case 2:
                ADCC_StartConversion(LM_V);
                sequence++;
                break;

            case 3:
                if (ADCON0bits.ADGO==0){
                    PetitInputRegisters[1].ActValue = ADCC_GetConversionResult();
                    sequence++;
                }            
                break;

            case 4:
                ADCC_StartConversion(LM_TEMP);
                sequence++;
                break;

            case 5: 
                if (ADCON0bits.ADGO==0){
                    PetitInputRegisters[2].ActValue = ADCC_GetConversionResult();
                    sequence++;
                }            
                break;

            case 6:
                ADCC_StartConversion(LM_CURR);
                sequence++;
                break;

            case 7:
                if (ADCON0bits.ADGO==0){
                    PetitInputRegisters[3].ActValue = ADCC_GetConversionResult();
                    sequence = 0;
                }            
                break;

            default: sequence = 0;
                break;
        }
    }
}
