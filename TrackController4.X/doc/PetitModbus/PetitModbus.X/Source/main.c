#include "General.h"

int main(void)
{
    InitOSC();
    InitIO();
    
    InitPetitModbus(2);
    
    PEIE    =1;
    GIE     =1;
    
    LED0=LED1=LED2=LED3=0;
    
    for(;;)
    {
        ProcessPetitModbus();
        
        LED0 = (PetitRegisters[0].ActValue >> 0) & 0x01;
        LED1 = (PetitRegisters[0].ActValue >> 1) & 0x01;
        LED2 = (PetitRegisters[0].ActValue >> 2) & 0x01;
        LED3 = (PetitRegisters[0].ActValue >> 3) & 0x01;
        
        PetitRegisters[2].ActValue=PORTCbits.RC0;
        
        PetitRegisters[4].ActValue=PetitRegisters[3].ActValue;
        
        if(Timer1_Tick_Counter>1000)
        {
            Timer1_Tick_Counter=0;
            
            PetitRegisters[5].ActValue++;
        }
        
        PetitRegisters[10].ActValue = 12345;
    }
}


