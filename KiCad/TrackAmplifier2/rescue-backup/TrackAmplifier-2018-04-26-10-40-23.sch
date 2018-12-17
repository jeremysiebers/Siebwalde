EESchema Schematic File Version 2
LIBS:power
LIBS:device
LIBS:switches
LIBS:relays
LIBS:motors
LIBS:transistors
LIBS:conn
LIBS:linear
LIBS:regul
LIBS:74xx
LIBS:cmos4000
LIBS:adc-dac
LIBS:memory
LIBS:xilinx
LIBS:microcontrollers
LIBS:dsp
LIBS:microchip
LIBS:analog_switches
LIBS:motorola
LIBS:texas
LIBS:intel
LIBS:audio
LIBS:interface
LIBS:digital-audio
LIBS:philips
LIBS:display
LIBS:cypress
LIBS:siliconi
LIBS:opto
LIBS:atmel
LIBS:contrib
LIBS:valves
LIBS:MCU_MICROCHIP_PIC16
LIBS:lmd18200d
LIBS:maxim
LIBS:TrackAmplifier-cache
EELAYER 25 0
EELAYER END
$Descr A4 11693 8268
encoding utf-8
Sheet 1 1
Title ""
Date ""
Rev ""
Comp ""
Comment1 ""
Comment2 ""
Comment3 ""
Comment4 ""
$EndDescr
$Comp
L PIC16F18854-ISO U?
U 1 1 5ABE26F2
P 6250 3050
F 0 "U?" H 6350 4050 50  0000 L CNN
F 1 "PIC16F18854-ISO" H 6350 3950 50  0000 R CNN
F 2 "" H 6250 3050 50  0001 C CIN
F 3 "" H 6250 3050 50  0001 C CNN
	1    6250 3050
	1    0    0    -1  
$EndComp
$Comp
L LMD18200D U?
U 1 1 5ABE37F0
P 9200 3100
F 0 "U?" H 9200 3550 60  0000 C CNN
F 1 "LMD18200D" H 9200 3650 60  0000 C CNN
F 2 "" H 10350 3150 60  0001 C CNN
F 3 "" H 10350 3150 60  0001 C CNN
	1    9200 3100
	1    0    0    -1  
$EndComp
$Comp
L DB15_Male_MountingHoles J?
U 1 1 5ABE3833
P 1150 1600
F 0 "J?" H 1150 2550 50  0000 C CNN
F 1 "DB15_Male_MountingHoles" H 1150 2475 50  0000 C CNN
F 2 "" H 1150 1600 50  0001 C CNN
F 3 "" H 1150 1600 50  0001 C CNN
	1    1150 1600
	-1   0    0    1   
$EndComp
$Comp
L +15V #PWR?
U 1 1 5ABE39E6
P 2350 700
F 0 "#PWR?" H 2350 550 50  0001 C CNN
F 1 "+15V" H 2350 840 50  0000 C CNN
F 2 "" H 2350 700 50  0001 C CNN
F 3 "" H 2350 700 50  0001 C CNN
	1    2350 700 
	1    0    0    -1  
$EndComp
$Comp
L GND #PWR?
U 1 1 5ABE3A34
P 2300 2500
F 0 "#PWR?" H 2300 2250 50  0001 C CNN
F 1 "GND" H 2300 2350 50  0000 C CNN
F 2 "" H 2300 2500 50  0001 C CNN
F 3 "" H 2300 2500 50  0001 C CNN
	1    2300 2500
	1    0    0    -1  
$EndComp
Wire Wire Line
	1450 900  2300 900 
Wire Wire Line
	2300 900  2300 2500
Wire Wire Line
	1450 2300 2300 2300
Connection ~ 2300 2300
Wire Wire Line
	1450 1000 2350 1000
Wire Wire Line
	2350 700  2350 2200
Wire Wire Line
	2350 2200 1450 2200
Connection ~ 2350 1000
$Comp
L C C?
U 1 1 5ABE3B27
P 9200 3600
F 0 "C?" H 9225 3700 50  0000 L CNN
F 1 "100n" H 9225 3500 50  0000 L CNN
F 2 "" H 9238 3450 50  0001 C CNN
F 3 "" H 9200 3600 50  0001 C CNN
	1    9200 3600
	0    1    1    0   
$EndComp
$Comp
L C C?
U 1 1 5ABE3B6E
P 9200 3900
F 0 "C?" H 9225 4000 50  0000 L CNN
F 1 "1uF" H 9225 3800 50  0000 L CNN
F 2 "" H 9238 3750 50  0001 C CNN
F 3 "" H 9200 3900 50  0001 C CNN
	1    9200 3900
	0    1    1    0   
$EndComp
$Comp
L CP C?
U 1 1 5ABE3B92
P 9200 4250
F 0 "C?" H 9225 4350 50  0000 L CNN
F 1 "22uF" H 9225 4150 50  0000 L CNN
F 2 "" H 9238 4100 50  0001 C CNN
F 3 "" H 9200 4250 50  0001 C CNN
	1    9200 4250
	0    -1   -1   0   
$EndComp
Wire Wire Line
	8500 3400 8500 4450
Wire Wire Line
	8500 3600 9050 3600
Wire Wire Line
	9350 3600 9900 3600
Wire Wire Line
	9900 3400 9900 4450
Wire Wire Line
	8500 3900 9050 3900
Connection ~ 8500 3600
Wire Wire Line
	8500 4250 9050 4250
Connection ~ 8500 3900
Wire Wire Line
	9900 4250 9350 4250
Connection ~ 9900 3600
$Comp
L GND #PWR?
U 1 1 5ABE3DB6
P 9900 4450
F 0 "#PWR?" H 9900 4200 50  0001 C CNN
F 1 "GND" H 9900 4300 50  0000 C CNN
F 2 "" H 9900 4450 50  0001 C CNN
F 3 "" H 9900 4450 50  0001 C CNN
	1    9900 4450
	1    0    0    -1  
$EndComp
$Comp
L +15V #PWR?
U 1 1 5ABE3DDC
P 8500 4450
F 0 "#PWR?" H 8500 4300 50  0001 C CNN
F 1 "+15V" H 8500 4590 50  0000 C CNN
F 2 "" H 8500 4450 50  0001 C CNN
F 3 "" H 8500 4450 50  0001 C CNN
	1    8500 4450
	-1   0    0    1   
$EndComp
Connection ~ 8500 4250
Connection ~ 9900 4250
Wire Wire Line
	9350 3900 9900 3900
Connection ~ 9900 3900
$EndSCHEMATC
