# Siebwalde-Source
Siebwalde project

This project is dedicated for the ModelTrain Yard of Siebwalde: 
- https://www.youtube.com/c/SiebersOrg
- http://www.siebers.org/index.html
- https://www.youtube.com/channel/UCRJYaY8l7wVBrel3Bn0nD2Q/videos?view_as=subscriber

It contains sources for:

Windows Application (C#)

- Siebwalde_Application     --> C# main App to control all

uCOntrollers (C):

- FiddleYard                --> Embedded ethernet uController, controlling the FiddleYard PIC18F97j60

- ServoController.X         --> Embedded uController for RC servo driving

- TrackAmplifier4.X         --> development of track amplifiers; 1 PIC per amplifier (final setup with PIC18F25K40)(PetitModbus)*

- TrackBackplane2.X         --> development of track backplane slave; track amplifier slave select slave for initialization (final setup with PIC18F25K40)(PetitModbus)*

- TrackController5.X        --> development of track controller; PIC32MZ2048EFH144 Ethernet Target + ModBus Master(build upon PetitModbus)*

PCB Files (KiCad):

- KiCad                     --> All Siebwalde related KiCad (PCB) designs


*PetitModbus derived from https://github.com/FxDev/PetitModbus 