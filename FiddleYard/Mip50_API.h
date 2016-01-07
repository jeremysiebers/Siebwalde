/* 
 * File:   Mip50_API.h
 * Author: siebwalde
 *
 * Created on 4 januari 2016, 14:27
 */

#ifndef MIP50_API_H
#define	MIP50_API_H

#ifdef	__cplusplus
extern "C" {
#endif

extern char MIP50xHOME(unsigned char ASL);                                      // Request Homing procedure
extern char MIP50xMOVE(unsigned char ASL, long int New_Track);                  // Move to a new track, track in counts
extern void UARTxCOMM(unsigned char ASL);                                       // Check if data is received from MIP or that data must be sent (MAIN LOOP CALLER)
extern void MIP50xAPIxRESET(unsigned char ASL);                                 // Reset API

void MIP50xCLEARxRECIEVEDxDATA(unsigned char ASL);
unsigned char MIP50xACK(unsigned char ASL);                                     // Check if acknowledge is received from MIP after execution of command that was sent
void MIP50xCRLFxAppend(unsigned char ASL);                                      // Append CR and LF to get clear status logging in a terminal
void MIP50xSetxPermanentxParameterxHomexOffsetxMovement(unsigned char ASL);     // Set Homing home offset movement number
void MIP50xReadxPermanentxParameterxHomexOffsetxMovement(unsigned char ASL);     // Read the Homing home offset movement number 
void MIP50xClearxError(unsigned char ASL);
void MIP50xSetxAcceleration(unsigned char ASL);
void MIP50xSetxPositioningxVelxDefault(unsigned char ASL);
void MIP50xSetxPositioningxVel(unsigned char ASL, int Vel);
void MIP50xActivatexPosxReg(unsigned char ASL);
void MIP50xDeactivatexPosxReg(unsigned char ASL);
void MIP50xHomexAxis(unsigned char ASL);
void MIP50xAbs_Pos(unsigned char ASL, long int Pos);
void MIP50xWritexUart(unsigned char ASL, char Data);                            // Switch regarding ASL to write to UART1 or UART2
char MIP50xSENDxBUFFERxEMPTY(unsigned char ASL);                                // Check if buffer is not full
char MIP50xReadxUart(unsigned char ASL);                                        // Read received data
char MIP50xRECEIVEDxDATAxAVAILABLE(unsigned char ASL);                          // Check if data is received

#ifdef	__cplusplus
}
#endif

#endif	/* MIP50_API_H */

