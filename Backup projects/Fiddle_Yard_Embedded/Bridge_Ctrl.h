#ifndef __Bridge_Ctrl_H
#define __Bridge_Ctrl_H

extern unsigned char Bridge_Open(unsigned char ASL);		// Bridge Open routine
extern unsigned char Bridge_Close(unsigned char ASL);		// Brdige Close routine
extern void Bridge_Ctrl_Reset(unsigned char ASL);			// Reset all var to reset val

#endif