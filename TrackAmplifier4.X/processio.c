#include "processio.h"
#include "modbus/General.h"
#include "mcc_generated_files/mcc.h"
#include "mcc_generated_files/adcc.h"

void    ADC_BMF     (void);

static uint16_t MeausureBemfCnt = 0;
static uint16_t MeasueBemf = false;
static uint16_t Sequence = 0;

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
 *  and stores it into the status holding register (HR_STATUS).
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
                        uint16_t bemf = (uint16_t)(ADCC_GetConversionResult() & HR_STATUS_BEMF_MASK);
                        uint16_t status = PetitHoldingRegisters[HR_STATUS].ActValue;
                        status &= (uint16_t)(~HR_STATUS_BEMF_MASK);  /* clear old BEMF bits    */
                        status |= bemf;                              /* insert new BEMF value  */
                        PetitHoldingRegisters[HR_STATUS].ActValue = status;
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
 *  measurements into the corresponding Modbus holding registers:
 *
 *  - LM_V     -> HR_FUSE_VOLTAGE          (HoldingReg4)
 *  - LM_TEMP  -> HR_HBRIDGE_TEMPERATURE   (HoldingReg5)
 *  - LM_CURR  -> HR_HBRIDGE_CURRENT       (HoldingReg6)
 *
 *  It also updates the thermal flag bit in HR_STATUS from LM_THFLG.
 */
/*#--------------------------------------------------------------------------#*/
static uint16_t sequence = 2;

uint16_t ADCxIO (){
    
    uint16_t Return_Val = false;
    
    /* Update thermal flag bit in HR_STATUS from LM_THFLG input. */
    {
        uint16_t status = PetitHoldingRegisters[HR_STATUS].ActValue;
        if (LM_THFLG_GetValue())
        {
            status |= HR_STATUS_THERMAL_BIT;
        }
        else
        {
            status &= (uint16_t)(~HR_STATUS_THERMAL_BIT);
        }
        
        if (CMP1_GetOutputStatus()){
            status |= HR_STATUS_OCCUPIED_BIT;
        }
        else
        {
            status &= (uint16_t)(~HR_STATUS_OCCUPIED_BIT);
        }        
        PetitHoldingRegisters[HR_STATUS].ActValue = status;
    }
    
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
                    PetitHoldingRegisters[HR_FUSE_VOLTAGE].ActValue = ADCC_GetConversionResult();
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
                    PetitHoldingRegisters[HR_HBRIDGE_TEMPERATURE].ActValue = ADCC_GetConversionResult();
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
                    PetitHoldingRegisters[HR_HBRIDGE_CURRENT].ActValue = ADCC_GetConversionResult();
                    sequence = 2;
                    Return_Val = true;
                }            
                break;

            default: 
                sequence = 2;
                break;
        }
    }
    
    return (Return_Val);
}