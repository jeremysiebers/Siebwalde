#ifndef __Shift_Register_H
#define __Shift_Register_H

extern void IO(void);

extern unsigned char SPARE1(unsigned char ASL);
extern unsigned char CL_10(unsigned char ASL);
extern unsigned char CL_10_Heart(unsigned char ASL);
extern unsigned char SPARE2(unsigned char ASL);
extern unsigned char F11(unsigned char ASL);
extern unsigned char EOS_10(unsigned char ASL);
extern unsigned char EOS_11(unsigned char ASL);
extern unsigned char SPARE3(unsigned char ASL);
extern unsigned char F13(unsigned char ASL);
extern unsigned char F10(unsigned char ASL);
extern unsigned char F12(unsigned char ASL);
extern unsigned char Bezet_Uit_5B(unsigned char ASL);
extern unsigned char Bezet_Uit_6(unsigned char ASL);
extern unsigned char Bezet_Uit_7(unsigned char ASL);
extern unsigned char Bezet_Uit_8A(unsigned char ASL);
extern unsigned char Track_Nr(unsigned char ASL);

extern unsigned char M10_Status(unsigned char ASL);
extern unsigned char Enable_Track_Status(unsigned char ASL);
extern unsigned char Bezet_In_5B_Status(unsigned char ASL);
extern unsigned char Bezet_In_6_Status(unsigned char ASL);
extern unsigned char Bezet_In_7_Status(unsigned char ASL);
extern unsigned char Bezet_Weerstand_Status(unsigned char ASL);

extern void M10(unsigned char ASL, unsigned char M10_Val);
extern void Enable_Track(unsigned char ASL, unsigned char Enable_Track_Val);
extern void Bezet_In_5B(unsigned char ASL, unsigned char Bezet_In_5B_Val);
extern void Bezet_In_6(unsigned char ASL, unsigned char Bezet_In_6_Val);
extern void Bezet_In_7(unsigned char ASL, unsigned char Bezet_In_7_Val);
extern void Bezet_Weerstand(unsigned char ASL, unsigned char Bezet_Weerstand_Val);

#endif








