/*
 * File:   newmain.c
 * Author: Jeremy Siebers
 *
 * Created on April 16, 2018, 10:06 PM
 */


#include <xc.h>
#include "main.h"
#include "mcc_generated_files/mcc.h"
#include "mcc_generated_files/adcc.h"
#include "modbus/General.h"
#include "processio.h"
#include "regulator.h"

unsigned int MODBUS_ADDRESS = 0;
unsigned int LED_TX_prev, LED_RX_prev, LED_ERR_prev, LED_WAR_prev = 0;
unsigned int LED_TX_STATE, LED_RX_STATE, LED_ERR_STATE, LED_WAR_STATE = 0;
unsigned int LED_ERR, LED_WAR = 0;
unsigned int UpdateLedCnt = 0;
unsigned int Config = 1;
unsigned int Startup_Machine = 0;
unsigned int MeausureBemfCnt = 0;
unsigned int ReadBemf = 0;

static unsigned int Kp = 1;
static unsigned int Ki = 1;
static unsigned int input = 0;
static unsigned int output = 0;
static unsigned int integral = 0;
static unsigned int integral_1 = 0;
static unsigned int input_1 = 0;
static unsigned int Ts = 10;

/*----------------------------------------------------------------------------*/
void main(void) {
    
    SYSTEM_Initialize();
    TMR1_StopTimer();
    LED_RUN_LAT     = 0;
    LED_WAR_LAT     = 0;
    LED_ERR_LAT     = 1;    
    LED_TX_LAT      = 0;
    LED_RX_LAT      = 0; 
    
    LM_DIR_LAT      = 0;
    LM_PWM_LAT      = 0;
    LM_BRAKE_LAT    = 1;
    
    TRISAbits.TRISA6 = 0;
    
    __delay_ms(10);       
    
    INTCONbits.GIE = 1;
    INTCONbits.PEIE = 1;
    
    MODBUS_ADDRESS = 0xAA;                                                      // Address to listen to get configured when ID pin is pulled low 170 dec.    
    InitPetitModbus(MODBUS_ADDRESS);
/*----------------------------------------------------------------------------*/
    
    while(Config){
        
        switch(Startup_Machine){
            case 0 :
                if (ID_PORT == 0){
                    Startup_Machine = 1;
                }
                break;
                
            case 1 :
                ProcessPetitModbus();
                LED_ERR++;                
                Led_Blink();
                if (ID_PORT){
                    if((PetitHoldingRegisters[2].ActValue & 0x1F)!= 0 ){
                        LED_ERR         = 0; 
                        LED_ERR_LAT     = 0;                        
                        MODBUS_ADDRESS = (PetitHoldingRegisters[2].ActValue & 0x1F);
                        InitPetitModbus(MODBUS_ADDRESS);
                        Startup_Machine = 2;
                   }  
                    else{
                        Startup_Machine = 0;
                        Config          = 1;
                        LED_ERR         = 0;
                        LED_ERR_LAT     = 1;    
                    }
                }                
                break;
                
            case 2 :
                ProcessPetitModbus();
                LED_WAR++;
                Led_Blink();
                if ((PetitHoldingRegisters[1].ActValue & 0x8000)!= 0){
                    Startup_Machine = 0;
                    Config          = 0;
                    LED_WAR         = 0;
                    LED_WAR_LAT     = 0;
                    LED_RUN_LAT     = 1;
                }
                break;
                
            default: 
                break;
        }
    }
        
    Regulator_Init(); 
/*----------------------------------------------------------------------------*/
    
    while(1){
    
        ProcessPetitModbus();
        
        Regulator();
                      
        //LED_WAR_LAT = PORTAbits.RA5;                                            // This must be the Occupied signal LED (output of comparator 1 coupled to RA5) to be added in the final design!!!
        
        if(ReadBemf){
            PORTAbits.RA6 = 1;
            ProcessBMF();
        }
        else{
            ProcessIO();
        }
        Led_Blink();
    }
}

/*----------------------------------------------------------------------------*/
void Led_Blink (){
    if(PIR0bits.TMR0IF){
        
        MeausureBemfCnt++;
        if (MeausureBemfCnt > 499){
            MeausureBemfCnt = 0;
            TRISCbits.TRISC4 = 1;
            TRISCbits.TRISC5 = 1;
            TRISCbits.TRISC6 = 1;
            
            if (PetitHoldingRegisters[3].ActValue == 0x0001){
                input = PetitInputRegisters[0].ActValue;

                integral = integral_1 + (input/Ts);

                output = (Kp * input) + (Ki * integral);

                input_1 = input;
                integral_1 = integral;
            }
            
        }
        if (MeausureBemfCnt > 9){
            TRISCbits.TRISC4 = 0;
            TRISCbits.TRISC5 = 0;
            TRISCbits.TRISC6 = 0;
        }
        if (MeausureBemfCnt > 6 && MeausureBemfCnt < 9){
            ReadBemf = true;
        }
        else{
            ReadBemf = false;
            PORTAbits.RA6 = 0;
        }
        
        UpdateLedCnt++;
        
        if(UpdateLedCnt > 499){
            UpdateLedCnt = 0;        
        
            switch(LED_WAR_STATE){
                case 0 : 
                    if (LED_WAR > 0){
                        LED_WAR_LAT = 1;
                        LED_WAR_prev = LED_WAR;
                        LED_WAR_STATE = 1;                        
                    }
                    break;

                case 1 :
                    if (LED_WAR == LED_WAR_prev || LED_WAR != LED_WAR_prev){
                        LED_WAR_LAT = 0;
                        LED_WAR_prev = 0;
                        LED_WAR = 0;
                        LED_WAR_STATE = 0;                        
                    }
                    break;

                default :
                    LED_WAR_STATE = 0;
                    break;                       
            }

            switch(LED_ERR_STATE){
                case 0 : 
                    if (LED_ERR > 0){
                        LED_ERR_LAT = 1;
                        LED_ERR_prev = LED_ERR;
                        LED_ERR_STATE = 1;                        
                    }
                    break;

                case 1 :
                    if (LED_ERR == LED_ERR_prev || LED_ERR != LED_ERR_prev){
                        LED_ERR_LAT = 0;
                        LED_ERR_prev = 0;
                        LED_ERR = 0;
                        LED_ERR_STATE = 0;                        
                    }
                    break;

                default :
                    LED_ERR_STATE = 0;
                    break;                       
            }

            switch(LED_TX_STATE){
                case 0 : 
                    if (LED_TX > 0){
                        LED_TX_LAT = 1;
                        LED_TX_prev = LED_TX;
                        LED_TX_STATE = 1;                        
                    }
                    break;

                case 1 :
                    if (LED_TX == LED_TX_prev || LED_TX != LED_TX_prev){
                        LED_TX_LAT = 0;
                        LED_TX_prev = 0;
                        LED_TX = 0;
                        LED_TX_STATE = 0;                        
                    }
                    break;

                default :
                    LED_TX_STATE = 0;
                    break;                       
            }

            switch(LED_RX_STATE){
                case 0 : 
                    if (LED_RX > 0){
                        LED_RX_LAT = 1;
                        LED_RX_prev = LED_RX;
                        LED_RX_STATE = 1;
                    }
                    break;

                case 1 :
                    if (LED_RX == LED_RX_prev || LED_RX != LED_RX_prev){
                        LED_RX_LAT = 0;
                        LED_RX_prev = 0;
                        LED_RX = 0;
                        LED_RX_STATE = 0;
                    }
                    break;

                default :
                    LED_RX_STATE = 0;
                    break;                       
            }
        }
        PIR0bits.TMR0IF = 0;
        TMR0_Reload();
    }
}