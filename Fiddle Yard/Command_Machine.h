#ifndef __Command_Machine_H
#define __Command_Machine_H

extern void Command(void);
extern unsigned char Exe_Cmd_(unsigned char ASL);
extern unsigned char Exe_Cmd_Ret(unsigned char ASL, char Exe_Cmd_Ret_);
extern void Exe_Cmd_Resume(unsigned char ASL, char Resume_Cmd);

extern unsigned char MACPC[6];
extern unsigned char IPPC[4];
extern unsigned char MAC_IP_READY;

#endif