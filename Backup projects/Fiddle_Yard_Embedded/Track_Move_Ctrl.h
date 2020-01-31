#ifndef __Track_Move_Ctrl_H
#define __Track_Move_Ctrl_H

extern unsigned char Track_Mover(unsigned char ASL, char New_Track);			// Routine to determine how the fiddle yard must move, makes calls to Fiddle_Move_Ctrl

extern void Track_Move_Ctrl_Reset(unsigned char ASL);							// Reset all local var to reset val

extern void Old_Track2_When_Error(unsigned char ASL, unsigned char Track_Nr);	// When error current track is storred

extern unsigned char Old_Track2_When_Error_Ret(unsigned char ASL);				// When solving error retry to last current track

#endif