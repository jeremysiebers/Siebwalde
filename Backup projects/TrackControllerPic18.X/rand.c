#include <xc.h>
#include <stdio.h>
#include "rand.h"

static int16_t RandomNumber = 0;

void INITxRANDxNUMBER(){
    srand(0x100);
    RandomNumber = rand();
    RandomNumber = rand();
    RandomNumber = rand();
    RandomNumber = rand();
}
/* use an number as is since we operate in 1ms intervals */
uint16_t GETxRANDOMxNUMBER(){
    RandomNumber = (rand() << 1);
    return ((uint16_t)RandomNumber);
}