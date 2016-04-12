#ifndef __Train_Detection_H
#define __Train_Detection_H

extern unsigned char Train_Detection(unsigned char ASL);                            // Start train detection program
extern void Train_Detection_Reset(unsigned char ASL);                               // Reset all local var to reset val
extern unsigned char Trains_On_Fiddle_Yard(unsigned char ASL, unsigned char Track); // Get Track status from Train_In_Track array

#endif