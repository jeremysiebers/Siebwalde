#ifndef __Command_Machine_H
#define __Command_Machine_H

extern void Command(void);
extern unsigned char Exe_Cmd_(unsigned char ASL);
extern unsigned char Exe_Cmd_Ret(unsigned char ASL, char Exe_Cmd_Ret_);
extern void Exe_Cmd_Resume(unsigned char ASL, char Resume_Cmd);

extern unsigned char MACPC[6];
extern unsigned char IPPC[4];
extern unsigned char MAC_IP_READY;
extern unsigned int PORTPC;
extern unsigned char MIP50_VEL;                                                 // To set the velocity from C# app (default 100)

#endif