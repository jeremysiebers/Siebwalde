# Siebwalde-Source
Siebwalde project

This project is dedicated for the ModelTrain Yard of Siebwalde: 
- https://www.youtube.com/c/SiebersOrg
- http://www.siebers.org/index.html

It contains sources for:
- Fiddle_Yard_Embedded      --> Embedded ethernet uController with embedded control application (archive)
- FiddleYard                --> Embedded ethernet uController, control application to C# (Siebwalde_Application)
    
- KiCad                     --> All Siebwalde related KiCad (PCB) designs

- ServoController.X         --> Embedded uController for RC servo driving

- Siebwalde_Application     --> C# main App to control all

- TrackAmplifier1.X         --> development of track amplifiers; 5 dsPIC to 50 amplifiers (I2C)
- TrackAmplifier2.X         --> development of track amplifiers; 1 PIC per amplifier (test setup with PIC16F737)(PetitModbus)*
- TrackAmplifier3.X         --> development of track amplifiers; 1 PIC per amplifier (test setup with PIC16F18854)(PetitModbus)*
- TrackAmplifier4.X         --> development of track amplifiers; 1 PIC per amplifier (final setup with PIC18F25K40)(PetitModbus)*

- TrackAmplifierPidTune1.X  --> development of track amplifier P-regulator with remote python processing(test setup with PIC16F18854)(PetitModbus)*
- TrackAmplifierPidTune2.X  --> development of track amplifier P-regulator with remote python processing(final setup with PIC18F25K40)(PetitModbus)*

- TrackBackplane1.X         --> development of track backplane slave; track amplifier slave select slave for initialization (test setup with PIC16F18854)(PetitModbus)*
- TrackBackplane2.X         --> development of track backplane slave; track amplifier slave select slave for initialization (final setup with PIC18F25K40)(PetitModbus)*

- TrackController1.X        --> development of track controller; 1 Embedded ethernet uController to 5 dsPIC (I2C)
- TrackController2.X        --> development of track controller; to control 50 track amplifiers (test setup with PIC16F777)(PetitModbus)*
- TrackController3.X        --> development of track controller; to control 50 track amplifiers (test setup with PIC18F97j60)(PetitModbus)*
- TrackController4.X        --> development of track controller; slave to Modbus/SPI master (PIC18F07j60 slave of TrackModbusMaster)(PetitModbus)*

- TrackModbusMaster.X       --> development of Modbus/SPI master to control 50 TrackAmplifiers (test setup with PIC16F18854)(PetitModbus)*

*PetitModbus derived from https://github.com/FxDev/PetitModbus 