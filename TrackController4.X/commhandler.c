/*
 * File:   commhandler.c
 * Author: Jeremy Siebers
 *
 * Created on aug 28, 2018, 14:15 PM
 */
#include <xc.h>
#include "main.h"
#include "mcc_generated_files/mcc.h"
#include "modbus/Interrupts.h"
#include "modbus/PetitModbus.h"

#define MAILBOX_SIZE 4                                                          // How many messages are non-critical

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
/*  Description: ProcessNextSlave()
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
 *  Notes      : Handles communication message to all slaves
 */
/*#--------------------------------------------------------------------------#*/

typedef enum
{
    NrRegLo = 0,
    NrRegHi = 1,
    StAdLo  = 2,
    StAdHi  = 3     
};

typedef enum
{
    MESSAGE1 = 1,
    MESSAGE2 = 2,
    MESSAGE3 = 3,
    MESSAGE4 = 4 
};
unsigned char HoldingRegistersRead [4] = {0, 0, 0, 0};                          // {start address High, start address Low, number of registers High, number of registers Low}
unsigned char HoldingRegistersWrite[9] = {0, 0, 0, 0, 0, 0, 0, 0, 0};           // {start address High, start address Low, number of registers High, number of registers Low, byte count, Register Value Hi, Register Value Lo, Register Value Hi, Register Value Lo} 
unsigned char   InputRegistersRead [4] = {0, 0, 0, 0};
unsigned char    DiagRegistersRead [4] = {0, 0, 0, 0};

static unsigned int ProcessSlave = 1;
static unsigned int Mailbox = 1;
static unsigned int Message = MESSAGE1;

void ProcessNextSlave(){    
    
    if (ProcessSlave > (NUMBER_OF_SLAVES-1)){
        ProcessSlave = 1;
        Mailbox++;
        if (Mailbox > MAILBOX_SIZE){
            Mailbox = 1;
        }
        modbus_sync_LAT ^= 1;
    }

    switch (Message){
        case MESSAGE1:
            HoldingRegistersWrite[8]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[0] << 8;
            HoldingRegistersWrite[7]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[0];
            HoldingRegistersWrite[6]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[1] << 8;
            HoldingRegistersWrite[5]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[1];
            HoldingRegistersWrite[4]  = 4;
            HoldingRegistersWrite[3]  = 2;
            HoldingRegistersWrite[2]  = 0;
            HoldingRegistersWrite[1]  = 0;
            HoldingRegistersWrite[0]  = 0;
            SendPetitModbus(ProcessSlave, PETITMODBUS_WRITE_MULTIPLE_REGISTERS, HoldingRegistersWrite, 9);                
            break;

        /*case MESSAGE2:
            InputRegistersRead[3]  = 1;
            InputRegistersRead[2]  = 0;
            InputRegistersRead[1]  = 0;
            InputRegistersRead[0]  = 0;
            SendPetitModbus(ProcessSlave, PETITMODBUS_READ_INPUT_REGISTERS, InputRegistersRead, 4);    
            break;*/

        case MESSAGE2:
            InputRegistersRead[3]  = 2;
            InputRegistersRead[2]  = 0;
            InputRegistersRead[1]  = 0;
            InputRegistersRead[0]  = 0;
            SendPetitModbus(ProcessSlave, PETITMODBUS_READ_HOLDING_REGISTERS, InputRegistersRead, 4);    
            break;

        case MESSAGE3:
            switch (Mailbox){
                case 1:
                    HoldingRegistersRead[3]  = 2;
                    HoldingRegistersRead[2]  = 0;
                    HoldingRegistersRead[1]  = 2;
                    HoldingRegistersRead[0]  = 0;
                    SendPetitModbus(ProcessSlave, PETITMODBUS_READ_HOLDING_REGISTERS, HoldingRegistersRead, 4);
                    break;
                
                case 2:
                    DiagRegistersRead[3]  = 2;
                    DiagRegistersRead[2]  = 0;
                    DiagRegistersRead[1]  = 0;
                    DiagRegistersRead[0]  = 0;
                    SendPetitModbus(ProcessSlave, PETITMODBUS_DIAGNOSTIC_REGISTERS, DiagRegistersRead, 4);
                    break;
                
                case 3:
                    HoldingRegistersWrite[8]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[2] << 8;
                    HoldingRegistersWrite[7]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[2];
                    HoldingRegistersWrite[6]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[3] << 8;
                    HoldingRegistersWrite[5]  = MASTER_SLAVE_DATA[ProcessSlave].HoldingReg[3];
                    HoldingRegistersWrite[4]  = 4;
                    HoldingRegistersWrite[3]  = 2;
                    HoldingRegistersWrite[2]  = 0;
                    HoldingRegistersWrite[1]  = 2;
                    HoldingRegistersWrite[0]  = 0;
                    SendPetitModbus(ProcessSlave, PETITMODBUS_WRITE_MULTIPLE_REGISTERS, HoldingRegistersWrite, 4);
                    break;
                
                case 4:
                    DiagRegistersRead[3]  = 2;
                    DiagRegistersRead[2]  = 0;
                    DiagRegistersRead[1]  = 2;
                    DiagRegistersRead[0]  = 0;
                    SendPetitModbus(ProcessSlave, PETITMODBUS_DIAGNOSTIC_REGISTERS, DiagRegistersRead, 4);
                    break;
                
                default:
                    break;                
            }
            break;
            
        default :
            break;
    }  
}


/*#--------------------------------------------------------------------------#*/
/*  Description: ProcessSlaveCommunication()
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
 *  Notes      : Keeps track of communication with a slave
 */
/*#--------------------------------------------------------------------------#*/
void ProcessSlaveCommunication(){
    
    switch (MASTER_SLAVE_DATA[ProcessSlave].MbCommError){
        case SLAVE_DATA_BUSY:
            // count here how long the Mod-bus stack is busy, otherwise reset/action             
            break;
            
        case SLAVE_DATA_NOK:
            // count here how often the slave data is NOK, otherwise stop all slaves with broadcast
            MASTER_SLAVE_DATA[ProcessSlave].MbCommError = SLAVE_DATA_IDLE;
            Message++;                                                          // process next message
            if (Message > MESSAGE3){
                ProcessSlave++;
                Message = MESSAGE1;
            }            
            break;
            
        case SLAVE_DATA_OK:
            MASTER_SLAVE_DATA[ProcessSlave].MbCommError = SLAVE_DATA_IDLE;
            Message++;                                                          // process next message
            if (Message > MESSAGE3){
                ProcessSlave++;
                Message = MESSAGE1;
            }
            break;
            
        case SLAVE_DATA_TIMEOUT:
            // count here how often the slave data is timeout, otherwise stop all slaves with broadcast
            MASTER_SLAVE_DATA[ProcessSlave].MbCommError = SLAVE_DATA_IDLE;
            Message++;                                                          // process next message
            if (Message > MESSAGE3){
                ProcessSlave++;
                Message = MESSAGE1;
            }
            break;
            
        case SLAVE_DATA_EXCEPTION:
            // count here how often the slave data is timeout, otherwise stop all slaves with broadcast
            MASTER_SLAVE_DATA[ProcessSlave].MbCommError = SLAVE_DATA_IDLE;
            Message++;                                                          // process next message
            if (Message > MESSAGE3){
                ProcessSlave++;
                Message = MESSAGE1;
            }
            break;
            
        default :    // Idle is here        
            break;
    }
}

/*
 *------------------------------------------------------------------------------
 * Master communication scheme during OP:
 * 
 * 
 *     |Critical      |    |Critical    |    |Critical      |    |INFO    |     
 * ----|MESSAGE1      |----|MESSAGE2    |----|MESSAGE3      |----|MESSAGE4|-------
 *     |HoldingReg1 W |    |InputReg1 R |    |HoldingReg1 R |	 |RegisterX R/W|	
 *     |HoldingReg2 W |    |InputReg2 R |    |HoldingReg2 R |    |RegisterX R/W| 
 *     		             		   							     
 *     
 * Message 4 consists out of a mailbox were info can be R/W or
 * NOP, a maximum amount of 2 registers should be addressed at once
 * (NOP operation would be a read from a register)
 *
 * 4 Holding registers
 * 2 Input registers
 * 4 Diagnostic registers
 *
 * Mailbox: 50x 
 * 1x read HoldingReg3 and HoldingReg4
 * 1x read DiagnosticReg1 and DiagnosticReg2
 * 1x write HoldingReg3 and HoldingReg4
 * 1x read DiagnosticReg3 and DiagnosticReg4
 * 
 *------------------------------------------------------------------------------ 
 * 
 * Modbus Track Slave Data Register mapping:
 * 
 * Data register, bit(s)        Function            	R/W     Critical
 * 
 * -----------------------------RUNNING PARAMETERS------------------------------
 * 
 * HoldingReg1, 0 - 7           PWM set point       	R/W     No/Yes			// new PWM setpoint (speed in 0 - 255 km/h)
 * HoldingReg1, 8				PWM direction       	R/W     No/Yes          // Forward / Backward
 * HoldingReg1, 9               PWM enable          	R/W     No/Yes          // enabling of the H-bridge
 * HoldingReg1, 10              
 * HoldingReg1, 11              
 * HoldingReg1, 12                                  	
 * HoldingReg1, 13                                  	
 * HoldingReg1, 14                                  	
 * HoldingReg1, 15              Emo Stop            	R/W     No/Yes			// Slave takes action to stop train as fast as possible
 *  
 * HoldingReg2, 0 - 9           Set BEMF speed      	R/W     No/No           // Set value of BEMF, this to allow constant speed regulation
 * HoldingReg2, 10              Set CSReg             	R/W     No/No           // Enable constant speed regulation
 * HoldingReg2, 11 				Clear Amp status		R/W 	No/No			// Clear amplifier status
 * HoldingReg2, 12				Clear message buffer	R/W 	No/No			// Clear message buffer registers
 * HoldingReg2, 13
 * HoldingReg2, 14
 * HoldingReg2, 15				Reset Amplifier			R/W		No/No			// Execute an Amplifier reset().
 
 * InputReg1, 0 - 9             Read Back EMF         	R/-     Yes/-           // Read of back EMF train motor                      
 * InputReg1, 10                Occupied				R/-     Yes/-          
 * InputReg1, 11                ThermalFlag				R/-     Yes/-			// H-bridge thermal flag output              
 * InputReg1, 12                H-bridge over current 	R/-     Yes/-			// When over current is detected
 * InputReg1, 13
 * InputReg1, 14
 * InputReg1, 15
 *                                                  	
 * InputReg2, 0 - 4          	H-bridge fuse status	R/-	    No/-            // Voltage H-bridge fuse 0 - 31V
 * InputReg2, 5 				Amplifier ID set		R/-		No/-			// Indicates that the amplifier ID is set by master
 * InputReg2, 6 
 * InputReg2, 7
 * InputReg2, 8 - 15			H-bridge temperature	R/-		No/-			// H-bridge temperature 0 - 255 degrees Celsius
 * 
 * DiagnosticReg1, 0 - 15		Messages Received		R/-		No/-			// Slave register of messages Received to Master
 *
 * DiagnosticReg2, 0 - 15		Messages Sent			R/-		No/-			// Slave register of messages sent to Master
 *
 * DiagnosticReg3, 0 - 15       Amplifier Status        R/-     No/-            // Amplifier status list
 *
 * DiagnosticReg4, 0 - 7		Slave Internall temp	R/-		No/-			// Internall processor temperature 0 - 255 degrees Celsius
 * 
 * -----------------------------CONFIG PARAMETERS------------------------------- 
 * 
 * HoldingReg3, 0 - 5			Amplifier ID			R/W		No/No           // Amplifier ID for Track amp 1 to 50. Backplane config modules have address 51 to 55 
 * HoldingReg3, 6				Single/Double PWM		R/W		No/No			// used in single or double sided PWM operation 0 is dual sided PWM, 1 is single sided PWM
 * HoldingReg3, 7
 * HoldingReg3, 8
 * HoldingReg3, 9
 * HoldingReg3, 10
 * HoldingReg3, 11
 * HoldingReg3, 12
 * HoldingReg3, 13
 * HoldingReg3, 14
 * HoldingReg3, 15
 *
 * HoldingReg4, 0 - 15          Acceleration par    	R/W     No/No			// Acceleration number 0 - 255
 * HoldingReg4, 0 - 15          Deceleration par    	R/W     No/No			// Deceleration number 0 - 255
 *
 *------------------------------------------------------------------------------
 *
 *
 *
 * Modbus Backplane Slave Data Register mapping:
 * 
 * Data register, bit(s)        Function            	R/W     Critical
 * 
 * -----------------------------RUNNING PARAMETERS------------------------------
 * 
 * HoldingReg1, 0               Slave 1  enable       		R/W     No/Yes			// Enable Slave 1  for configuring of ID
 * HoldingReg1, 1               Slave 2  enable       		R/W     No/Yes			// Enable Slave 2  for configuring of ID 
 * HoldingReg1, 2               Slave 3  enable       		R/W     No/Yes			// Enable Slave 3  for configuring of ID 
 * HoldingReg1, 3               Slave 4  enable       		R/W     No/Yes			// Enable Slave 4  for configuring of ID 
 * HoldingReg1, 4               Slave 5  enable       		R/W     No/Yes			// Enable Slave 5  for configuring of ID 
 * HoldingReg1, 5               Slave 6  enable       		R/W     No/Yes			// Enable Slave 6  for configuring of ID 
 * HoldingReg1, 6               Slave 7  enable       		R/W     No/Yes			// Enable Slave 7  for configuring of ID 
 * HoldingReg1, 7               Slave 8  enable       		R/W     No/Yes			// Enable Slave 8  for configuring of ID 
 * HoldingReg1, 8				Slave 9  enable       		R/W     No/Yes          // Enable Slave 9  for configuring of ID 
 * HoldingReg1, 9               Slave 10 enable       		R/W     No/Yes          // Enable Slave 10 for configuring of ID 
 * HoldingReg1, 10
 * HoldingReg1, 11
 * HoldingReg1, 12
 * HoldingReg1, 13
 * HoldingReg1, 14
 * HoldingReg1, 15
 * 
 * HoldingReg2, 0 
 * HoldingReg2, 1
 * HoldingReg2, 2 
 * HoldingReg2, 3 
 * HoldingReg2, 4 
 * HoldingReg2, 5 
 * HoldingReg2, 6 
 * HoldingReg2, 7 
 * HoldingReg2, 8 
 * HoldingReg2, 9 
 * HoldingReg2, 10              
 * HoldingReg2, 11 				Clear ConfigSlave status	R/W 	No/No			// Clear amplifier status
 * HoldingReg2, 12				Clear message buffer		R/W 	No/No			// Clear message buffer registers
 * HoldingReg2, 13
 * HoldingReg2, 14
 * HoldingReg2, 15				Reset ConfigSlave			R/W		No/No			// Execute an Amplifier reset().
 *
 * HoldingReg3, 0 - 5			ConfigSlave ID			    R/W		No/No           // ConfigSlave ID modules have fixed address 51 to 55 
 * HoldingReg3, 6				
 * HoldingReg3, 7
 * HoldingReg3, 8
 * HoldingReg3, 9
 * HoldingReg3, 10
 * HoldingReg3, 11
 * HoldingReg3, 12
 * HoldingReg3, 13
 * HoldingReg3, 14
 * HoldingReg3, 15
 *
 * HoldingReg4, 0 - 15			n.a.
 *
 * InputReg1, 0 - 15			n.a.
 *                                                  	
 * InputReg2, 0 - 4          	Vbus fuse status     		R/-	    No/-            // Voltage Vbus fuse 0 - 31V
 * InputReg2, 5 				ConfigSlave ID set		    R/-		No/-			// Indicates the ConfigSlave ID is set
 * InputReg2, 6 
 * InputReg2, 7
 * InputReg2, 8 
 * InputReg2, 9
 * InputReg2, 10
 * InputReg2, 11
 * InputReg2, 12
 * InputReg2, 13
 * InputReg2, 14
 * InputReg2, 15
 *
 * DiagnosticReg1, 0 - 15		Messages Received			R/-		No/-			// Slave register of messages Received to Master
 *	
 * DiagnosticReg2, 0 - 15		Messages Sent				R/-		No/-			// Slave register of messages sent to Master
 * 	
 * DiagnosticReg3, 0 - 15       ConfigSlave Status      	R/-     No/No           // ConfigSlave status list + internall temp?
 *
 * DiagnosticReg4, 0 - 7		ConfigSlave Internall temp	R/-		No/-			// Internall processor temperature 0 - 255 degrees Celsius
 */