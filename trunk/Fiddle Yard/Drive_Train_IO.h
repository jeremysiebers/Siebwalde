#ifndef __Drive_Train_IO_H
#define __Drive_Train_IO_H

extern unsigned char Train_Drive_In(unsigned char ASL, unsigned char Collect_Full);
extern unsigned char Train_Drive_Out(unsigned char ASL);
extern void Drive_Train_IO_Reset(unsigned char ASL);
extern void Train_In_Track_actual(unsigned char ASL, unsigned char *Train_In_Track_Val);
extern unsigned char *Trains_On_Fiddle_Yard(unsigned char ASL);
extern unsigned char Fiddle_Yard_Full(unsigned char ASL, unsigned char Return_Val);
extern unsigned int Train_In_Track_Out_Count_Repeater_Ret(unsigned char ASL);
extern void Train_In_Track_Out_Count_Set(unsigned char ASL, unsigned int update_Train_In_Track_Out_Count_Repeater);

#endif