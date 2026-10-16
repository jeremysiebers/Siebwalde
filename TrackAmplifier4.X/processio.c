#include "processio.h"
#include "modbus/General.h"
#include "amp_model.h"
#include "mcc_generated_files/mcc.h"
#include "mcc_generated_files/adcc.h"

void    ADC_BMF     (void);

static uint16_t MeausureBemfCnt = 0;
static uint16_t MeasueBemf = false;
static uint16_t Sequence = 0;

static volatile  uint16_t g_occ  = 0;
static volatile  uint16_t g_tflg = 0;
static volatile  uint16_t g_bemf = 0;

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
                    PT1_SetHigh();  // debug measurement pin
                }
                else if(MeasueBemf == true && MeausureBemfCnt > 6){
                    MeasueBemf  = false;  
                    PT1_SetLow();  // debug measurement pin
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

/*#--------------------------------------------------------------------------#*/
/*  Description: ADC_BMF ()
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
 *
 *  This function performs the actual ADC conversion of the back-EMF
 *  and stores it in the internal BEMF model (not exposed in the Phase-1 map).
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
                    {
                        g_bemf = (uint16_t)(ADCC_GetConversionResult() & 0x03FFu);
                        amp_model.bemf = g_bemf;
                    }
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
 *
 *  This function multiplexes several ADC channels and places the
 *  measurements into the internal model (adapter publishes HR16..18):
 *
 *  - LM_V     -> voltage
 *  - LM_TEMP  -> temperature
 *  - LM_CURR  -> current
 *
 *  It also reports live occupancy/thermal and measurement validity.
 */
/*#--------------------------------------------------------------------------#*/
static uint16_t sequence = 2;

uint16_t ADCxIO (){
    
    PT1_SetHigh();  // debug measurement pin
    
    uint16_t Return_Val = false;
    
    /* Sample live digital status, without introducing Phase-2 protection. */
    g_occ = CMP1_GetOutputStatus();
    g_tflg = LM_THFLG_GetValue();
    
    /* Retain sampling sequence; publish live signals through the model only. */
    amp_model.fault.validity &= (uint16_t)~0x03C0u;
    if (g_occ) amp_model.fault.validity |= 0x0100u;
    if (g_tflg) amp_model.fault.validity |= 0x0200u;

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
                    /* H-bridge fuse voltage */
                    amp_model.voltage = ADCC_GetConversionResult();
                    amp_model.fault.validity &= (uint16_t)~0x0003u;
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
                    /* H-bridge temperature */
                    amp_model.temperature = ADCC_GetConversionResult();
                    amp_model.fault.validity &= (uint16_t)~0x000Cu;
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
                    /* H-bridge current */
                    amp_model.current = ADCC_GetConversionResult();
                    amp_model.fault.validity &= (uint16_t)~0x0030u;
                    sequence = 2;
                    Return_Val = true;
                }            
                break;

            default: 
                sequence = 2;
                break;
        }
    }
    if(Return_Val){PT1_SetLow();}  // debug measurement pin
    
    return (Return_Val);
}
