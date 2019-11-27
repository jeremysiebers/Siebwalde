#include "processio.h"
#include "modbus/General.h"
#include "mcc_generated_files/mcc.h"
#include "mcc_generated_files/adcc.h"

void    ADC_BMF     (void);

/*#--------------------------------------------------------------------------#*/
/*  Description: MEASURExBMF()
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

static uint16_t MeausureBemfCnt = 0;
static uint16_t MeasueBemf = 0;
static uint16_t Sequence = 0;

uint16_t MEASURExBMF(){
    uint16_t Return_Val = false;
    
    if(MeasueBemf){
        ADC_BMF();
    }
    
    switch(Sequence){
        case 0:
            Return_Val = true;
            MeausureBemfCnt++;
            if (MeausureBemfCnt > 1){                                           // disable H bridge every 10ms
                MeausureBemfCnt = 0;
                TRISCbits.TRISC4 = 1;
                TRISCbits.TRISC5 = 1;
                TRISCbits.TRISC6 = 1;
                T1CONbits.TMR1ON = 0;
                TMR1H = 0xFF;
                TMR1L = 0x38;
                PIR4bits.TMR1IF = 0;
                T1CONbits.TMR1ON = 1;
                Return_Val = false;                                              
                NC_2_LAT = true;
                Sequence++;
            }
            break;
            
        case 1:
            if (PIR4bits.TMR1IF){
                T1CONbits.TMR1ON = 0;
                TMR1H = 0xFF;
                TMR1L = 0x38;
                PIR4bits.TMR1IF = 0;
                T1CONbits.TMR1ON = 1;
                
                MeausureBemfCnt++;
                
                if (MeausureBemfCnt > 5 && MeausureBemfCnt < 7){                // Read back EMF as late as possible
                    MeasueBemf = true;                                           // set update PID
                }
                else if(MeasueBemf == true && MeausureBemfCnt > 6){
                    MeasueBemf  = false;                        
                }
                
                if (MeausureBemfCnt > 7){                                       // after 800us enable H bridge again
                    MeausureBemfCnt = 0;
                    TRISCbits.TRISC4 = 0;
                    TRISCbits.TRISC5 = 0;
                    TRISCbits.TRISC6 = 0; 
                    NC_2_LAT = false;
                    Return_Val = true;
                    Sequence = 0;
                }
            }
            break;
            
        default :
            Sequence = 0;
            break;
    }
    
    return (Return_Val);
}

/*
    if(UpdatePID){
        //NC_2_LAT = true;
        PROCESSxBMF();
    }
    
    //if (PIR4bits.TMR1IF){
        MeausureBemfCnt++;
        if (MeausureBemfCnt > 99){                                              // disable H bridge every 10ms
            MeausureBemfCnt = 0;
            TRISCbits.TRISC4 = 1;
            TRISCbits.TRISC5 = 1;
            TRISCbits.TRISC6 = 1;
            Return_Val = false;                                                 // measuring time    
            NC_2_LAT = true;
        }
        
        if (MeausureBemfCnt > 0 && MeausureBemfCnt < 8){
            Return_Val = false;  
        }
                
        if (MeausureBemfCnt > 8){                                               // after 800us enable H bridge again
            TRISCbits.TRISC4 = 0;
            TRISCbits.TRISC5 = 0;
            TRISCbits.TRISC6 = 0; 
            NC_2_LAT = false;
        }

        if (MeausureBemfCnt > 6 && MeausureBemfCnt < 8){                        // Read back EMF as late as possible
            //PORTAbits.RA6 = 1;
            UpdatePID = true;                                                   // set update PID
        }
        else if(UpdatePID == true && MeausureBemfCnt > 7){
            UpdatePID  = false;                        
        }
        //PIR4bits.TMR1IF = 0;
        //TMR1_Reload();
    }
*/

/*#--------------------------------------------------------------------------#*/
/*  Description: ADCxBMF ()
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
static uint16_t Sequence_Bmf = 0;

void ADC_BMF (){
    if (ADCON0bits.ADGO==0){        
    
        switch(Sequence_Bmf){
            case 0:
                ADCC_StartConversion(BMF);
                Sequence_Bmf++;
                //NC_2_LAT = false;
                break;

            case 1:
                if (ADCON0bits.ADGO==0){
                    PetitHoldingRegisters[8].ActValue = ADCC_GetConversionResult();
                    Sequence_Bmf = 0;
                    //NC_2_LAT = true;
                }
                break;

            default: 
                break;        
        }
    }
}

/*#--------------------------------------------------------------------------#*/
/*  Description: ADCxIO ()
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
static uint16_t sequence = 2;

uint16_t ADCxIO (){
    
    uint16_t Return_Val = false;
    
    //PetitInputRegisters[0].ActValue |= (unsigned int)(THFLG_PORT << 11);
    if (ADCON0bits.ADGO==0){                                                    // extra check if conversion is done
    
        switch(sequence){
            /*
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
*/
            case 2:
                ADCC_StartConversion(LM_V);
                sequence++;
                break;

            case 3:
                if (ADCON0bits.ADGO==0){
                    PetitHoldingRegisters[7].ActValue = ADCC_GetConversionResult();
                    sequence++;
                    Return_Val = true;
                }            
                break;

            case 4:
                ADCC_StartConversion(LM_TEMP);
                sequence++;
                break;

            case 5: 
                if (ADCON0bits.ADGO==0){
                    PetitHoldingRegisters[6].ActValue = ADCC_GetConversionResult();
                    sequence++;
                    Return_Val = true;
                }            
                break;

            case 6:
                ADCC_StartConversion(LM_CURR);
                sequence++;
                break;

            case 7:
                if (ADCON0bits.ADGO==0){
                    PetitHoldingRegisters[5].ActValue = ADCC_GetConversionResult();
                    sequence = 2;
                    Return_Val = true;
                }            
                break;

            default: sequence = 0;
                break;
        }
    }
    
    return (Return_Val);
}
