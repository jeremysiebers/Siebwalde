#ifndef __Var_Out_H
#define __Var_Out_H

extern void Var_Out_Programm(unsigned char ASL);

//////////////////Text Out//////////////////////////
extern void Fiddle_One_Left_Ok(unsigned char ASL);
extern void Fiddle_One_Right_Ok(unsigned char ASL);
extern void Fiddle_Multiple_Left_Ok(unsigned char ASL);
extern void Fiddle_Multiple_Right_Ok(unsigned char ASL);
extern void Train_Detection_Finished(unsigned char ASL);
extern void Train_On_5B(unsigned char ASL);
extern void Train_On_8A(unsigned char ASL);
extern void Fiddle_Yard_Reset(unsigned char ASL);
extern void Target_Ready(unsigned char ASL);
//////////////////ERROR CODES//////////////////////////
extern void Bezet_Uit_Blok_6_Send(unsigned char ASL);
extern void Sensor_F12_Send(unsigned char ASL);
extern void Bezet_Uit_Blok_6_AND_Sensor_F12_Send(unsigned char ASL);
extern void EndOffStroke_11_Send(unsigned char ASL);
extern void Laatste_Spoor_Send(unsigned char ASL);
extern void EndOffStroke_10_Send(unsigned char ASL);
extern void Universal_Error_Send(unsigned char ASL);
extern void F12TrainDetect_Send(unsigned char ASL);
extern void F13TrainDetect_Send(unsigned char ASL);

#endif