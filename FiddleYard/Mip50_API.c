#include "Mip50_API.h"
#include "Fiddle_Yard.h"
#include "eusart1.h"
#include "eusart2.h"
#include "Shift_Register.h"
#include <stdlib.h>
#include <string.h>
#include "Diagnostic_ret.h"

////////ERROR and return CODES////////
#define ERROR 0xEE	// general switch case when error
#define Busy -1
#define Finished 0
#define ACKNOWLEDGED 1
#define MMESSAGE 2
#define EMESSAGE 3
#define MIP50NODATA 4

#define MIP50RECDATA 16
#define MIP50RECCMDARRAY 16
int MIP50HOMEOFFSETMOVEMENT[2]={5700,1900};                                     // Home offset movement BOTTOM = 5700, TOP = 1900
            
typedef struct
{
	unsigned char 			Mip50_SwitchState,                        			// Switch for Fiddle direction movement	and Homing						
                            Mip50_ReadSwitchState,                              // Switch for the Read of data received from mip switch statemachine
                            MIP50_Rec_Data_Counter,                             // Counter for reading data from MIP50 Messages and Errors
                            MIP50_Rec_Cmd_Counter_W,                            // Counter for writing receieved command into array for storrage
                            MIP50_Rec_Cmd_Counter_R;                            // Counter for reading receieved command from array Cmd storrage
    char                    Return_Val_Routine,                                 // Return value of sub routine switch
                            MIP50_Received_Data,                                // Variable used to temp store the UART output
                            MIP50_Received_Data_Count;                          // Variable used to temp store the UART RX counter value    
    char                    MIP50_Rec_Data[MIP50RECDATA],                       // Received data from MIP50 to be send to UDP
                            MIP50_Rec_Cmd[MIP50RECCMDARRAY];              // Command received from MIP50 to be used in state machines below Home and Move
}STATE_MACHINE_VAR;

static STATE_MACHINE_VAR ACT_ST_MCHN[2]= 	{{0,0,0,0,0,Busy,0,0,{0,0,0,0,0,0,0,0,0,0,0,0,0,0},0 }, // is 0 is BOTTOM
                                             {0,0,0,0,0,Busy,0,0,{0,0,0,0,0,0,0,0,0,0,0,0,0,0},0 }};// is 1 is TOP

static unsigned char Send_Var_Out[3];

void MIP50xAPIxRESET(unsigned char ASL)                                         // During ucontroller reset also reset API
{    
    //MIP50xClearxError(ASL);    
    MIP50xCLEARxRECIEVEDxDATA(ASL);                                             // Clear received data array
    ACT_ST_MCHN[ASL].Mip50_SwitchState = 0;
    ACT_ST_MCHN[ASL].Mip50_ReadSwitchState = 0;
    ACT_ST_MCHN[ASL].MIP50_Rec_Data_Counter = 0;
    ACT_ST_MCHN[ASL].Return_Val_Routine = Busy;
}

/*
 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
 Main loop call routines
 */
void UARTxCOMM(unsigned char ASL)                                               // Check if data is received from MIP or that data must be sent main loop routine
{
    ACT_ST_MCHN[ASL].MIP50_Received_Data_Count = MIP50xRECEIVEDxDATAxAVAILABLE(ASL);
    
    if(ACT_ST_MCHN[ASL].MIP50_Received_Data_Count > 0)
    {
        ACT_ST_MCHN[ASL].MIP50_Received_Data = MIP50xReadxUart(ASL);
        
        switch (ACT_ST_MCHN[ASL].Mip50_ReadSwitchState)
        {
            
/*---------------------------------------CHECK FOR CR and LF -----------------------------------------------------------------------------------------------------------------
*/            
            
            case 0 :    if(ACT_ST_MCHN[ASL].MIP50_Received_Data == 0xD)         // Check if received char is CR
                        {
                            if (ASL == TOP)
                            {
                                Send_Var_Out[0] = 'H';
                            }
                            else if (ASL == BOTTOM)
                            {
                                Send_Var_Out[0] = 'U';
                            }
                            Send_Var_Out[1] = ACT_ST_MCHN[ASL].MIP50_Received_Data;
                            Send_Var_Out[2] = 0x00;
                            Send_Diag_Comm(Send_Var_Out);
                            ACT_ST_MCHN[ASL].Mip50_ReadSwitchState = 1;         // If CR then check bext char for LF
                        }
                        else{ACT_ST_MCHN[ASL].Mip50_ReadSwitchState = 0;}       // else reset switch
                        break;
                        
            case 1 :    if(ACT_ST_MCHN[ASL].MIP50_Received_Data == 0xA)         // Check if second received char is LF
                        {
                            if (ASL == TOP)
                            {
                                Send_Var_Out[0] = 'H';
                            }
                            else if (ASL == BOTTOM)
                            {
                                Send_Var_Out[0] = 'U';
                            }
                            Send_Var_Out[1] = ACT_ST_MCHN[ASL].MIP50_Received_Data;
                            Send_Var_Out[2] = 0x00;
                            Send_Diag_Comm(Send_Var_Out);
                            ACT_ST_MCHN[ASL].Mip50_ReadSwitchState = 2;         // If LF then the next chars are data from MIP
                        }
                        else{ACT_ST_MCHN[ASL].Mip50_ReadSwitchState = 0;}       // If the second char is not LF then the whole message is carbage reset switch
                        break;
                        
            case 2 :    if (ACT_ST_MCHN[ASL].MIP50_Received_Data == 0xD)        // When the next char data is a CR then the end of the data is found and next a LF is expected
                        {
                            if (ASL == TOP)
                            {
                                Send_Var_Out[0] = 'H';
                            }
                            else if (ASL == BOTTOM)
                            {
                                Send_Var_Out[0] = 'U';
                            }
                            Send_Var_Out[1] = ACT_ST_MCHN[ASL].MIP50_Received_Data;
                            Send_Var_Out[2] = 0x00;
                            Send_Diag_Comm(Send_Var_Out);                            
                            ACT_ST_MCHN[ASL].Mip50_ReadSwitchState = 3;         // Goto the check for LF...                            
                        }
                        else
                        {
                            if (ASL == TOP)
                            {
                                Send_Var_Out[0] = 'H';
                            }
                            else if (ASL == BOTTOM)
                            {
                                Send_Var_Out[0] = 'U';
                            }
                            Send_Var_Out[1] = ACT_ST_MCHN[ASL].MIP50_Received_Data;
                            Send_Var_Out[2] = 0x00;
                            Send_Diag_Comm(Send_Var_Out);
                        }
                        break;
                        
            case 3 :    if(ACT_ST_MCHN[ASL].MIP50_Received_Data == 0xA)         // Check if second received char is LF
                        {
                            if (ASL == TOP)
                            {
                                Send_Var_Out[0] = 'H';
                            }
                            else if (ASL == BOTTOM)
                            {
                                Send_Var_Out[0] = 'U';
                            }
                            Send_Var_Out[1] = ACT_ST_MCHN[ASL].MIP50_Received_Data;
                            Send_Var_Out[2] = 0x00;
                            Send_Diag_Comm(Send_Var_Out);
                            ACT_ST_MCHN[ASL].Mip50_ReadSwitchState = 0;         // If LF then the data from the MIP is closed Reset switch                            
                        }
                        break;
                        
            default : break;
        }
    }
}

/*
 ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
 State_Machine_Update loop call routines (2.4 kHz)
 */

void MIP50xCLEARxRECIEVEDxDATA(unsigned char ASL)
{   
    int i = 0;
    for (i=0; i < MIP50RECDATA; i++)                                            // Remove all previous received data from MIP50
    {            
        ACT_ST_MCHN[ASL].MIP50_Rec_Data[i] = 0;            
    }
}

unsigned char MIP50xACK(unsigned char ASL)
{   
    char Return_Val = Busy;  
    
    if (ACT_ST_MCHN[ASL].MIP50_Rec_Cmd_Counter_R != ACT_ST_MCHN[ASL].MIP50_Rec_Cmd_Counter_W)       // When a message is added to the Mailbox (write counter incremented) 
    {
        Return_Val = ACT_ST_MCHN[ASL].MIP50_Rec_Cmd[ACT_ST_MCHN[ASL].MIP50_Rec_Cmd_Counter_R];      // Return that message
        ACT_ST_MCHN[ASL].MIP50_Rec_Cmd_Counter_R++;                                                 // Increment the Read counter
        if (ACT_ST_MCHN[ASL].MIP50_Rec_Cmd_Counter_R >= MIP50RECCMDARRAY)                           // If end of mailbox, start at 0 again
        {
            ACT_ST_MCHN[ASL].MIP50_Rec_Cmd_Counter_R = 0;                                           // Reset Read counter
        }
    }    
    return(Return_Val);
}

char MIP50xHOME(unsigned char ASL)
{
    char Return_Val = Busy, Return_Val_Sub = Busy;    
    
    switch (ACT_ST_MCHN[ASL].Mip50_SwitchState)
    {
        case 0 :
            if(MIP50xSENDxBUFFERxEMPTY(ASL))                                    // Check if TX buffer is not full, otherwise a while loop will take over adding waiting time and lockup
            {
                MIP50xCLEARxRECIEVEDxDATA(ASL);                                 // Clear UART buffer and received data buffer
                MIP50xDeactivatexPosxReg(ASL);                                  // Deactivate position regulation if ON
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 1;
            }
            break;
        
        case 1 :
            Return_Val_Sub = MIP50xACK(ASL);
            if(Return_Val_Sub == ACKNOWLEDGED)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 2;
            }
            else if(Return_Val_Sub == EMESSAGE || Return_Val_Sub == ERROR || Return_Val_Sub == MIP50NODATA)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 0;
            }
            break;
            
        case 2 :
            if(MIP50xSENDxBUFFERxEMPTY(ASL))                                    // Check if TX buffer is not full, otherwise a while loop will take over adding waiting time and lockup
            {
                MIP50xClearxError(ASL);                                         // Clear MIP50 error                
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 3;
            }
            break;
        
        case 3 :
            Return_Val_Sub = MIP50xACK(ASL);
            if(Return_Val_Sub == ACKNOWLEDGED)
            {                
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 4;
            }
            else if(Return_Val_Sub == EMESSAGE || Return_Val_Sub == ERROR || Return_Val_Sub == MIP50NODATA)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 0;
            }
            break;
        
        case 4 :  
        if(MIP50xSENDxBUFFERxEMPTY(ASL))                                        // Check if TX buffer is not full, otherwise a while loop will take over adding waiting time and lockup
        {
            MIP50xSetxAcceleration(ASL);
            ACT_ST_MCHN[ASL].Mip50_SwitchState = 5;
        }
        break;
        
        case 5 :
            Return_Val_Sub = MIP50xACK(ASL);
            if(Return_Val_Sub == ACKNOWLEDGED)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 6;
            }
            else if(Return_Val_Sub == EMESSAGE || Return_Val_Sub == ERROR || Return_Val_Sub == MIP50NODATA)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 0;
            }
            break;
        
        case 6 :  
        if(MIP50xSENDxBUFFERxEMPTY(ASL))                                        // Check if TX buffer is not full, otherwise a while loop will take over adding waiting time and lockup
        {
            MIP50xSetxPositioningxVelxDefault(ASL);
            ACT_ST_MCHN[ASL].Mip50_SwitchState = 7;
        }
        break;
        
        case 7 :
            Return_Val_Sub = MIP50xACK(ASL);
            if(Return_Val_Sub == ACKNOWLEDGED)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 8;
            }
            else if(Return_Val_Sub == EMESSAGE || Return_Val_Sub == ERROR || Return_Val_Sub == MIP50NODATA)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 0;
            }
            break;
        
        case 8 :  
        if(MIP50xSENDxBUFFERxEMPTY(ASL))                                        // Check if TX buffer is not full, otherwise a while loop will take over adding waiting time and lockup
        {
            MIP50xActivatexPosxReg(ASL);
            ACT_ST_MCHN[ASL].Mip50_SwitchState = 9;
        }
        break;
        
        case 9 :
            Return_Val_Sub = MIP50xACK(ASL);
            if(Return_Val_Sub == ACKNOWLEDGED)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 10;
            }
            else if(Return_Val_Sub == EMESSAGE || Return_Val_Sub == ERROR || Return_Val_Sub == MIP50NODATA)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 0;
            }
            break;
        
        case 10 :  
        if(MIP50xSENDxBUFFERxEMPTY(ASL))                                        // Check if TX buffer is not full, otherwise a while loop will take over adding waiting time and lockup
        {
            MIP50xHomexAxis(ASL);
            ACT_ST_MCHN[ASL].Mip50_SwitchState = 11;
        }
        break;
        
        case 11 :
            Return_Val_Sub = MIP50xACK(ASL);
            if(Return_Val_Sub == ACKNOWLEDGED)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 12;
            }
            else if(Return_Val_Sub == EMESSAGE || Return_Val_Sub == ERROR || Return_Val_Sub == MIP50NODATA)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 0;
            }
            break;
        
        case 12 :
        if(MIP50xSENDxBUFFERxEMPTY(ASL))                                        // Check if TX buffer is not full, otherwise a while loop will take over adding waiting time and lockup
        {
            MIP50xDeactivatexPosxReg(ASL);
            ACT_ST_MCHN[ASL].Mip50_SwitchState = 13;            
        }
        break;
        
        case 13 :
            Return_Val_Sub = MIP50xACK(ASL);
            if(Return_Val_Sub == ACKNOWLEDGED)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 0;
                Return_Val = Finished;
            }
            else if(Return_Val_Sub == EMESSAGE || Return_Val_Sub == ERROR || Return_Val_Sub == MIP50NODATA)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 0;
            }
            break;        
        
        default : break;
    }
    return(Return_Val);
}

char MIP50xMOVE(unsigned char ASL, long int New_Track)
{
    char Return_Val = Busy, Return_Val_Sub = Busy;
    
    switch (ACT_ST_MCHN[ASL].Mip50_SwitchState)
    {
        case 0 :
        if(MIP50xSENDxBUFFERxEMPTY(ASL))                                        // Check if TX buffer is not full, otherwise a while loop will take over adding waiting time and lockup
        {
            MIP50xActivatexPosxReg(ASL);
            ACT_ST_MCHN[ASL].Mip50_SwitchState = 1;
        }
        break;
        
        case 1 :
            Return_Val_Sub = MIP50xACK(ASL);
            if(Return_Val_Sub == ACKNOWLEDGED)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 2;
            }
            else if(Return_Val_Sub == EMESSAGE || Return_Val_Sub == ERROR || Return_Val_Sub == MIP50NODATA)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 0;
            }
            break;        
                
        case 2 :  
        if(MIP50xSENDxBUFFERxEMPTY(ASL))                                        // Check if TX buffer is not full, otherwise a while loop will take over adding waiting time and lockup
        {
            MIP50xAbs_Pos(ASL, New_Track);
            ACT_ST_MCHN[ASL].Mip50_SwitchState = 3;
        }
        break;
        
        case 3 :
            Return_Val_Sub = MIP50xACK(ASL);
            if(Return_Val_Sub == ACKNOWLEDGED)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 4;
            }
            else if(Return_Val_Sub == EMESSAGE || Return_Val_Sub == ERROR || Return_Val_Sub == MIP50NODATA)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 0;
            }
            break;
        
        case 4 :  
        if(MIP50xSENDxBUFFERxEMPTY(ASL))                                                // Check if TX buffer is not full, otherwise a while loop will take over adding waiting time and lockup
        {
            MIP50xDeactivatexPosxReg(ASL);
            ACT_ST_MCHN[ASL].Mip50_SwitchState = 5;            
        }
        break;
        
        case 5 :
            Return_Val_Sub = MIP50xACK(ASL);
            if(Return_Val_Sub == ACKNOWLEDGED)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 0;
                Return_Val = Finished;
            }
            else if(Return_Val_Sub == EMESSAGE || Return_Val_Sub == ERROR || Return_Val_Sub == MIP50NODATA)
            {
                ACT_ST_MCHN[ASL].Mip50_SwitchState = 0;
            }
            break;
        
        default : break;
    }
    return(Return_Val);
}

void MIP50xCRLFxAppend(unsigned char ASL)                                       // Used for clear terminal readout
{
    MIP50xWritexUart(ASL, 0xD);
    MIP50xWritexUart(ASL, 0xA);
}

void MIP50xSetxPermanentxParameterxHomexOffsetxMovement(unsigned char ASL)
{
    char str[6];
    char length = 0;
    int i = 0;
    itoa(MIP50HOMEOFFSETMOVEMENT[ASL], str);
    length = strlen(str);    
    
    MIP50xWritexUart(ASL, 'w');
    MIP50xWritexUart(ASL, '1');
    MIP50xWritexUart(ASL, 'x');
    MIP50xWritexUart(ASL, '3'); 
    MIP50xWritexUart(ASL, '3');
    MIP50xWritexUart(ASL, 'p');
    for (i = 0; i < length; i++)
    {
        MIP50xWritexUart(ASL, str[i]);
    }
    MIP50xWritexUart(ASL, 'x');    
    MIP50xCRLFxAppend(ASL);
}

void MIP50xReadxPermanentxParameterxHomexOffsetxMovement(unsigned char ASL)      // Response to be captured M#26 1 25 5700 or M#26 1 25 1900
{
    MIP50xWritexUart(ASL, 'q');
    MIP50xWritexUart(ASL, '1');
    MIP50xWritexUart(ASL, 'x');
    MIP50xWritexUart(ASL, '2');
    MIP50xWritexUart(ASL, '5');
    MIP50xWritexUart(ASL, 'p');
    MIP50xWritexUart(ASL, 'G');
    MIP50xCRLFxAppend(ASL);
}

void MIP50xClearxError(unsigned char ASL)
{
    MIP50xWritexUart(ASL, 'E');
    MIP50xWritexUart(ASL, '1');
    MIP50xWritexUart(ASL, 'x');
    MIP50xWritexUart(ASL, 'G'); 
    MIP50xCRLFxAppend(ASL);
}

void MIP50xSetxAcceleration(unsigned char ASL)
{
    MIP50xWritexUart(ASL, 'C');
    MIP50xWritexUart(ASL, '1');
    MIP50xWritexUart(ASL, 'x');
    MIP50xWritexUart(ASL, '2'); 
    MIP50xWritexUart(ASL, 'r');
    MIP50xWritexUart(ASL, '2');
    MIP50xWritexUart(ASL, 't');
    MIP50xWritexUart(ASL, '0');
    MIP50xWritexUart(ASL, '.');
    MIP50xWritexUart(ASL, '2');
    MIP50xWritexUart(ASL, 'G');  
    MIP50xCRLFxAppend(ASL);  
}

void MIP50xSetxPositioningxVelxDefault(unsigned char ASL)
{
    MIP50xWritexUart(ASL, 'V');
    MIP50xWritexUart(ASL, '1');
    MIP50xWritexUart(ASL, 'x');
    MIP50xWritexUart(ASL, '1');
    MIP50xWritexUart(ASL, '0');
    MIP50xWritexUart(ASL, '0');
    MIP50xWritexUart(ASL, 'G'); 
    MIP50xCRLFxAppend(ASL);
}

void MIP50xSetxPositioningxVel(unsigned char ASL, int Vel)
{
    char str[6];
    char length = 0;
    int i = 0;
    itoa(Vel, str);
    length = strlen(str);
    
    MIP50xWritexUart(ASL, 'V');
    MIP50xWritexUart(ASL, '1');
    MIP50xWritexUart(ASL, 'x');
    for (i = 0; i < length; i++)
    {
        MIP50xWritexUart(ASL, str[i]);
    }
    MIP50xWritexUart(ASL, 'G'); 
    MIP50xCRLFxAppend(ASL);
}

void MIP50xActivatexPosxReg(unsigned char ASL)
{
    MIP50xWritexUart(ASL, 'n');
    MIP50xWritexUart(ASL, '1');
    MIP50xWritexUart(ASL, 'x');
    MIP50xWritexUart(ASL, 'G');  
    MIP50xCRLFxAppend(ASL);   
}

void MIP50xDeactivatexPosxReg(unsigned char ASL)
{
    MIP50xWritexUart(ASL, 'f');
    MIP50xWritexUart(ASL, '1');
    MIP50xWritexUart(ASL, 'x');
    MIP50xWritexUart(ASL, 'G');  
    MIP50xCRLFxAppend(ASL);   
}

void MIP50xHomexAxis(unsigned char ASL)
{
    MIP50xWritexUart(ASL, 'H');
    MIP50xWritexUart(ASL, '1');
    MIP50xWritexUart(ASL, 'x');
    MIP50xWritexUart(ASL, 'G'); 
    MIP50xCRLFxAppend(ASL);
}

void MIP50xAbs_Pos(unsigned char ASL, long int Pos)
{
    char str[8];
    char length = 0;
    int i = 0;
    ltoa(Pos, str);
    length = strlen(str);
    
    MIP50xWritexUart(ASL, 'A');
    MIP50xWritexUart(ASL, '1');
    MIP50xWritexUart(ASL, 'x');
    for (i = 0; i < length; i++)
    {
        MIP50xWritexUart(ASL, str[i]);
    }
    MIP50xWritexUart(ASL, 'G'); 
    MIP50xCRLFxAppend(ASL);
}

void MIP50xWritexUart(unsigned char ASL, char Data)
{
    switch (ASL)
    {
        case TOP    :   EUSART2_Write(Data);                                      
                        break;
        case BOTTOM :   EUSART1_Write(Data);                                      
                        break;
        default : break;
    }
}

char MIP50xSENDxBUFFERxEMPTY(unsigned char ASL)                                     
{  
    char Return_Val = False;
    switch (ASL)
    {
        case TOP        :   if (eusart2TxBufferRemaining > 0){
                            Return_Val = True;
                            }
                            break;
                      
        case BOTTOM   :     if (eusart1TxBufferRemaining > 0){
                            Return_Val = True;
                            }
                            break;
    }
    return (Return_Val);
}

char MIP50xReadxUart(unsigned char ASL)
{
    switch (ASL)
    {
        case TOP    : return EUSART2_Read();
        break;
        case BOTTOM : return EUSART1_Read();                             
        break;
        default : break;
    }
}

char MIP50xRECEIVEDxDATAxAVAILABLE(unsigned char ASL)                                     
{ 
    char Return_Val = False;
    switch (ASL)
    {
        case TOP        :   if (EUSART2_DataReady > 0){
                            Return_Val = EUSART2_DataReady;
                            }
                            break;
                      
        case BOTTOM   :     if (EUSART1_DataReady > 0){
                            Return_Val = EUSART1_DataReady;
                            }
                            break;
    }
    return (Return_Val);
}





























/*
 * ////////MIP50 CODES////////
#define CLEAR_ERROR "E1xG"                                                      // Clear Error
#define SET_ACCELERATION "C1x2r2t0.2G"                                          // Set Acceleration on axis 1 for both ramps to sinusodial and to 0.20 qc/ms^2
#define SET_POSITIONING_VEL "V1x100G"                                           // Set Positioning Velocity of axis 1 to 100 qc/ms^2
#define ACTIVATE_POS_REG "n1xG"                                                 // Activate Position Regulation
#define DEACTIVATE_POS_REG "f1xG"                                               // Deactivate Position Regulation
#define HOME_AXIS "H1xG"                                                        // Home axis 1
#define ABS_POS "A1x"                                                           // Absolute positining (after homing) GO command ('G') must be appended by caller
    printf("E1xG");
    printf("C1x2r2t0.2G");
    printf("V1x100G");
    printf("n1xG");
    printf("H1xG");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("A1x0G");
    printf("f1xG");
 */