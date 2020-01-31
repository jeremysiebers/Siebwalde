#ifndef __Fiddle_Move_Ctrl_H
#define __Fiddle_Move_Ctrl_H

extern unsigned char Fiddle_Multiple_Right(unsigned char ASL, char New_Track_Muktiple_Right, char Brake_Track);		// 11 to 1 multiple tracks

extern unsigned char Fiddle_One_Right(unsigned char ASL);															// 11 to 1 one track

extern unsigned char Fiddle_Multiple_Left(unsigned char ASL, char New_Track_Muktiple_Left, char Brake_Track);		// 1 to 11 multiple tracks

extern unsigned char Fiddle_One_Left(unsigned char ASL);															// 1 to 11 one track

extern void Fiddle_Move_Ctrl_Reset(unsigned char ASL);																// Reset all local var to reset val

#endif