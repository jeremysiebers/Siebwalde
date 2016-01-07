#ifndef __Track_Move_Ctrl_H
#define __Track_Move_Ctrl_H

extern unsigned char Track_Mover(unsigned char ASL, char New_Track);			// Routine to determine how the fiddle yard must move, makes calls to Fiddle_Move_Ctrl

extern void Track_Move_Ctrl_Reset(unsigned char ASL);							// Reset all local var to reset val

void TrackxCountxNumber(unsigned char ASL, char New_Track);                 // Move to position with direction dependent offset

#endif