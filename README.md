# Siebwalde-Source
Siebwalde project

This project is dedicated for the ModelTrain Yard of Siebwalde: 
- https://www.youtube.com/c/SiebersOrg
- http://www.siebers.org/Siebwalde%202014/index.html

It contains sources for:
- FiddleYard  --> Embedded ethernet uController with embedded control application (archive)
- Fiddle Yard --> Embedded ethernet uController control application to C# (Siebwalde_Application)

- Siebwalde_Application --> C# main App to control all

- ServoController.X --> Embedded uController for RC servo driving

- SiebwaldeTrackController1.X--> development of track controller; 1 Embedded ethernet uController to 5 dsPIC (I2C)
- SiebwaldeTrackController2.X--> development of track controller; to control 50 track amplifiers (test setup with PIC16F777)(PetitModbus)*

- SiebwaldeTrackAmplifier1.X --> development of track amplifiers; 5 dsPIC to 50 amplifiers (I2C)
- SiebwaldeTrackAmplifier2.X --> development of track amplifiers; 1 PIC per amplifier (test setup with PIC16F737)(PetitModbus)*
- SiebwaldeTrackAmplifier3.X --> development of track amplifiers; 1 PIC per amplifier (final setup with PIC16F18854)(PetitModbus)*

*PetitModbus derived from https://github.com/FxDev/PetitModbus