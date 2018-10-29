/*
 * File:   newmain.c
 * Author: Jeremy Siebers
 *
 * Created on April 16, 2018, 10:06 PM
 * 
 * orange = TX
 * yellow = RX
 * black = gnd
 */


#include <xc.h>
#include "main.h"
#include "mcc_generated_files/mcc.h"
#include "modbus/General.h"

unsigned int MODBUS_ADDRESS = 0;
unsigned int result = 0;

unsigned int LED_TX_prev, LED_RX_prev = 0;
unsigned int LED_TX_STATE, LED_RX_STATE = 0;

void main(void) {
    
    SYSTEM_Initialize();
    TMR1_StopTimer();
    LED_RUN_LAT     = 0;
    LED_WAR_LAT     = 0;
    LED_ERR_LAT     = 1;    
    LED_TX_LAT      = 0;
    LED_RX_LAT      = 0; 
    
    __delay_ms(10);
    
    INTCONbits.GIE = 1;
    INTCONbits.PEIE = 1;
    
    Get_ID();
    
    InitPetitModbus(MODBUS_ADDRESS);
            
    while(1){
        ProcessPetitModbus();
        
        Set_Amplifier();
        
        if(PIR0bits.TMR0IF){
            
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
            PIR0bits.TMR0IF = 0;
            TMR0_Reload();
        }
    }
}

/******************************************************************************
 * Function:        
 *
 * PreCondition:    
 *
 * Input:           
 *
 * Output:          
 *
 * Side Effects:    
 *
 * Overview:        
 *****************************************************************************/
void Get_ID(){
    
    MODBUS_ADDRESS = (unsigned int)50 + (unsigned int)((PORTA & 0xE0)>> 5);
    
    if(MODBUS_ADDRESS > 50 && MODBUS_ADDRESS < 60){
        LED_RUN_LAT = 1;
        LED_ERR_LAT = 0;
    }
    else{
        LED_RUN_LAT = 0;
        LED_ERR_LAT = 1;
        while(1);
    }
}

/******************************************************************************
 * Function:        
 *
 * PreCondition:    
 *
 * Input:           
 *
 * Output:          
 *
 * Side Effects:    
 *
 * Overview:        
 *****************************************************************************/
void Set_Amplifier(){
    
    switch(PetitHoldingRegisters[0].ActValue){
        case 0x1   : AMP_ID1_SET_LAT  = 0; break;
        case 0x2   : AMP_ID2_SET_LAT  = 0; break;
        case 0x4   : AMP_ID3_SET_LAT  = 0; break;
        case 0x8   : AMP_ID4_SET_LAT  = 0; break;
        case 0x10  : AMP_ID5_SET_LAT  = 0; break;
        case 0x20  : AMP_ID6_SET_LAT  = 0; break;
        case 0x40  : AMP_ID7_SET_LAT  = 0; break;
        case 0x80  : AMP_ID8_SET_LAT  = 0; break;
        case 0x100 : AMP_ID9_SET_LAT  = 0; break;
        case 0x200 : AMP_ID10_SET_LAT = 0; break;
        default    : AMP_ID1_SET_LAT, AMP_ID2_SET_LAT, AMP_ID3_SET_LAT,
                     AMP_ID4_SET_LAT, AMP_ID5_SET_LAT, AMP_ID6_SET_LAT,
                     AMP_ID7_SET_LAT, AMP_ID8_SET_LAT, AMP_ID9_SET_LAT,
                     AMP_ID10_SET_LAT = 1;
        break;        
    }
}

/*
 * Modbus Backplane Slave Data Register mapping(using the same amount of registers as the amplifier slaves):
 * 
 * Data register, bit(s)        Function            	R/W     Critical
 * 
 * -----------------------------RUNNING PARAMETERS------------------------------
 * 
 * HoldingReg0, 0               Slave 1  enable       		R/W     No/Yes			// Enable Slave 1  for configuring of ID
 * HoldingReg0, 1               Slave 2  enable       		R/W     No/Yes			// Enable Slave 2  for configuring of ID 
 * HoldingReg0, 2               Slave 3  enable       		R/W     No/Yes			// Enable Slave 3  for configuring of ID 
 * HoldingReg0, 3               Slave 4  enable       		R/W     No/Yes			// Enable Slave 4  for configuring of ID 
 * HoldingReg0, 4               Slave 5  enable       		R/W     No/Yes			// Enable Slave 5  for configuring of ID 
 * HoldingReg0, 5               Slave 6  enable       		R/W     No/Yes			// Enable Slave 6  for configuring of ID 
 * HoldingReg0, 6               Slave 7  enable       		R/W     No/Yes			// Enable Slave 7  for configuring of ID 
 * HoldingReg0, 7               Slave 8  enable       		R/W     No/Yes			// Enable Slave 8  for configuring of ID 
 * HoldingReg0, 8				Slave 9  enable       		R/W     No/Yes          // Enable Slave 9  for configuring of ID 
 * HoldingReg0, 9               Slave 10 enable       		R/W     No/Yes          // Enable Slave 10 for configuring of ID 
 * HoldingReg0, 10
 * HoldingReg0, 11
 * HoldingReg0, 12
 * HoldingReg0, 13
 * HoldingReg0, 14
 * HoldingReg0, 15
 * 
 * HoldingReg1, 0 
 * HoldingReg1, 1
 * HoldingReg1, 2 
 * HoldingReg1, 3 
 * HoldingReg1, 4 
 * HoldingReg1, 5 
 * HoldingReg1, 6 
 * HoldingReg1, 7 
 * HoldingReg1, 8 
 * HoldingReg1, 9 
 * HoldingReg1, 10              
 * HoldingReg1, 11 				Clear ConfigSlave status	R/W 	No/No			// Clear amplifier status
 * HoldingReg1, 12				Clear message buffer		R/W 	No/No			// Clear message buffer registers
 * HoldingReg1, 13
 * HoldingReg1, 14
 * HoldingReg1, 15				Reset ConfigSlave			R/W		No/No			// Execute an Amplifier reset().
 *
 * HoldingReg2, 0 - 5			ConfigSlave ID			    R/W		No/No           // ConfigSlave ID modules have fixed address 51 to 55 
 * HoldingReg2, 6				
 * HoldingReg2, 7
 * HoldingReg2, 8
 * HoldingReg2, 9
 * HoldingReg2, 10
 * HoldingReg2, 11
 * HoldingReg2, 12
 * HoldingReg2, 13
 * HoldingReg2, 14
 * HoldingReg2, 15
 *
 * HoldingReg3, 0 - 15			n.a.
 *
 * InputReg0, 0 - 15			n.a.
 *                                                  	
 * InputReg1, 0 - 4          	Vbus fuse status     		R/-	    No/-            // Voltage Vbus fuse 0 - 31V
 * InputReg1, 5 				ConfigSlave ID set		    R/-		No/-			// Indicates the ConfigSlave ID is set
 *
 * InputReg2, 0 - 15			n.a.
 *
 * InputReg3, 0 - 15			n.a.
 *
 * InputReg4, 0 - 15			n.a.
 *
 * DiagnosticReg0, 0 - 15		Messages Received			R/-		No/-			// Slave register of messages Received to Master
 *	
 * DiagnosticReg1, 0 - 15		Messages Sent				R/-		No/-			// Slave register of messages sent to Master
 * 	
 */