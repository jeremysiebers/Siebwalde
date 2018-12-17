#ifndef __Var_Out_H
#define __Var_Out_H

extern void Var_Out_Programm(unsigned char ASL);

//////////////////Text Out//////////////////////////
extern void Bridge_Open_Ok(unsigned char ASL);
extern void Bridge_Close_Ok(unsigned char ASL);
extern void Fiddle_One_Left_Ok(unsigned char ASL);
extern void Fiddle_One_Right_Ok(unsigned char ASL);
extern void Fiddle_Multiple_Left_Ok(unsigned char ASL);
extern void Fiddle_Multiple_Right_Ok(unsigned char ASL);
extern void Train_Detection_Finished(unsigned char ASL);
extern void Train_Drive_Out_Finished(unsigned char ASL);
extern void Train_Drive_In_Finished(unsigned char ASL);
extern void Train_Drive_In_Failed_F12(unsigned char ASL);
extern void Init_Done(unsigned char ASL);
extern void Bridge_Opening(unsigned char ASL);
extern void Bridge_Closing(unsigned char ASL);
extern void Init_Started(unsigned char ASL);
extern void Train_On_5B(unsigned char ASL);
extern void Train_Drive_In_Start(unsigned char ASL);
extern void Train_On_8A(unsigned char ASL);
extern void Train_Drive_Out_Start(unsigned char ASL);
extern void Fiddle_Yard_Stopped(unsigned char ASL);
extern void Fiddle_Yard_Reset(unsigned char ASL);
extern void Fiddle_Yard_Soft_Start(unsigned char ASL);
extern void Collect_Finished_Fy_Full(unsigned char ASL);
extern void Collect_On(unsigned char ASL);
extern void Collect_Off(unsigned char ASL);

extern void Train_Drive_Out_Cancelled(unsigned char ASL);
extern void Target_Ready(unsigned char ASL);

//////////////////ERROR CODES//////////////////////////
extern void Bezet_Uit_Blok_6_Send(unsigned char ASL);
extern void Sensor_F12_Send(unsigned char ASL);
extern void Bezet_Uit_Blok_6_AND_Sensor_F12_Send(unsigned char ASL);
extern void CL_10_Heart_Sensor_Send(unsigned char ASL);
extern void Bridge_Open_Close_Timeout_Expired_Send(unsigned char ASL);

extern void BridgeMotorContact_10_Send(unsigned char ASL);
extern void Bridge_10L_Contact_Send(unsigned char ASL);
extern void Bridge_10R_Contact_Send(unsigned char ASL);
extern void BridgeMotorContact_11_Send(unsigned char ASL);
extern void EndOffStroke_11_Send(unsigned char ASL);
extern void Laatste_Spoor_Send(unsigned char ASL);
extern void EndOffStroke_10_Send(unsigned char ASL);
extern void Universal_Error_Send(unsigned char ASL);

//////////////////////Bridge Logging opening///////////////////////
extern void Bridge_Opening_Msg_1(unsigned char ASL);
extern void Bridge_Opening_Msg_2(unsigned char ASL);
extern void Bridge_Opening_Msg_3(unsigned char ASL);
extern void Bridge_Opening_Msg_4(unsigned char ASL);

//////////////////////Bridge Logging closing///////////////////////
extern void Bridge_Opening_Msg_11(unsigned char ASL);
extern void Bridge_Opening_Msg_12(unsigned char ASL);
extern void Bridge_Opening_Msg_13(unsigned char ASL);
extern void Bridge_Opening_Msg_14(unsigned char ASL);


#endif