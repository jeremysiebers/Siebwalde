#include "PetitModbus.h"
#include "PetitModbusPort.h"

/***********************Input/Output Coils and Registers***********************/
#if ((PETITMODBUS_READ_HOLDING_REGISTERS_ENABLED > 0)|| (PETITMODBUSWRITE_SINGLE_REGISTER_ENABLED > 0) || (PETITMODBUS_WRITE_MULTIPLE_REGISTERS_ENABLED > 0))
    PetitRegStructure   PetitHoldingRegisters       [NUMBER_OF_HOLDING_PETITREGISTERS];
#endif

#if (PETITMODBUS_READ_INPUT_REGISTERS_ENABLED > 0)
    PetitRegStructure   PetitInputRegisters         [NUMBER_OF_INPUT_PETITREGISTERS];
#endif
    
#if (PETITMODBUS_DIAGNOSTIC_REGISTERS_ENABLED > 0)
    PetitRegStructure   PetitDiagnosticRegisters    [NUMBER_OF_DIAGNOSTIC_PETITREGISTERS];
#endif