EESchema Schematic File Version 2
LIBS:power
LIBS:device
LIBS:switches
LIBS:relays
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
LIBS:LP2951DR-SOIC8
LIBS:Modelwagonlight-cache
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
L D D2
U 1 1 5A951F12
P 5100 4300
F 0 "D2" H 5100 4400 50  0000 C CNN
F 1 "STPS0540Z" H 5100 4200 50  0000 C CNN
F 2 "Diodes_SMD:D_SOD-123" H 5100 4300 50  0001 C CNN
F 3 "" H 5100 4300 50  0001 C CNN
	1    5100 4300
	-1   0    0    1   
$EndComp
$Comp
L D D1
U 1 1 5A9520CA
P 5100 3950
F 0 "D1" H 5100 4050 50  0000 C CNN
F 1 "STPS0540Z" H 5100 3850 50  0000 C CNN
F 2 "Diodes_SMD:D_SOD-123" H 5100 3950 50  0001 C CNN
F 3 "" H 5100 3950 50  0001 C CNN
	1    5100 3950
	-1   0    0    1   
$EndComp
$Comp
L D D4
U 1 1 5A95212B
P 5100 5000
F 0 "D4" H 5100 5100 50  0000 C CNN
F 1 "STPS0540Z" H 5100 4900 50  0000 C CNN
F 2 "Diodes_SMD:D_SOD-123" H 5100 5000 50  0001 C CNN
F 3 "" H 5100 5000 50  0001 C CNN
	1    5100 5000
	1    0    0    -1  
$EndComp
$Comp
L D D3
U 1 1 5A952190
P 5100 4650
F 0 "D3" H 5100 4750 50  0000 C CNN
F 1 "STPS0540Z" H 5100 4550 50  0000 C CNN
F 2 "Diodes_SMD:D_SOD-123" H 5100 4650 50  0001 C CNN
F 3 "" H 5100 4650 50  0001 C CNN
	1    5100 4650
	1    0    0    -1  
$EndComp
$Comp
L R R1
U 1 1 5A952272
P 4450 3950
F 0 "R1" V 4530 3950 50  0000 C CNN
F 1 "10R" V 4450 3950 50  0000 C CNN
F 2 "Resistors_SMD:R_1206" V 4380 3950 50  0001 C CNN
F 3 "" H 4450 3950 50  0001 C CNN
	1    4450 3950
	0    -1   -1   0   
$EndComp
Wire Wire Line
	5300 5000 5250 5000
Wire Wire Line
	5300 4650 5300 5000
Connection ~ 5300 4650
Connection ~ 5300 3950
Wire Wire Line
	4700 3950 4700 4650
Wire Wire Line
	4700 4650 4950 4650
Connection ~ 4700 3950
Wire Wire Line
	5250 4300 5300 4300
Wire Wire Line
	5300 4300 5300 3950
Wire Wire Line
	4850 4300 4850 5000
Wire Wire Line
	4850 5000 4950 5000
Connection ~ 4850 4300
Wire Wire Line
	4250 4300 4950 4300
$Comp
L R R4
U 1 1 5AB3A528
P 9050 4250
F 0 "R4" V 9130 4250 50  0000 C CNN
F 1 "100R" V 9050 4250 50  0000 C CNN
F 2 "Resistors_SMD:R_1206" V 8980 4250 50  0001 C CNN
F 3 "" H 9050 4250 50  0001 C CNN
	1    9050 4250
	-1   0    0    1   
$EndComp
Text Notes 8450 3700 0    60   ~ 0
05mA --> 1.235V/0.005 = 247 Ohm\n10mA --> 1.235V/0.010 = 123.5 Ohm\n12mA --> 1.235V/0.012 = 100 Ohm\n15mA --> 1.235V/0.015 = 82.3 Ohm\n20mA --> 1.235V/0.020 = 61.75 Ohm
Text Notes 7000 3700 0    60   ~ 0
Dropout Voltage  = 0.380V\n
Text Notes 5700 3150 0    60   ~ 0
The amount of Led's depends on the supplied voltage,\nnormal operation from PWM amplifier is 17V minus 2V \ndue to rectifiers is 15V. For a red led a voltage of typ 1.2V\nis required, therfore maximum of 12 led's is achievable
Wire Wire Line
	4250 3950 4300 3950
Wire Wire Line
	4600 3950 4950 3950
$Comp
L R R5
U 1 1 5ABB5CE8
P 9250 4250
F 0 "R5" V 9330 4250 50  0000 C CNN
F 1 "100R" V 9250 4250 50  0000 C CNN
F 2 "Resistors_SMD:R_1206" V 9180 4250 50  0001 C CNN
F 3 "" H 9250 4250 50  0001 C CNN
	1    9250 4250
	-1   0    0    1   
$EndComp
$Comp
L R R6
U 1 1 5ABB5D36
P 9450 4250
F 0 "R6" V 9530 4250 50  0000 C CNN
F 1 "100R" V 9450 4250 50  0000 C CNN
F 2 "Resistors_SMD:R_1206" V 9380 4250 50  0001 C CNN
F 3 "" H 9450 4250 50  0001 C CNN
	1    9450 4250
	-1   0    0    1   
$EndComp
Wire Wire Line
	5600 3950 5600 4150
Connection ~ 5600 3950
Wire Wire Line
	5600 4650 5600 4450
Connection ~ 5600 4650
Text Notes 4200 5900 0    60   ~ 0
BOM:\n2x C1,2   22uF Through Hole Fnl:9452397\n3(5)x R1,4,5,6  1206 resistor Multicomp 100Ohm 500mW    Fnl:2694436\n1x      R2           1206 resistor Multicomp 1KOhm 250mW  Fnl:\n1x      R3           1206 resistor Multicomp 1MOhm 250mW  Fnl:\n4x      D1,2,3,4  SOD-123 Shottky rectifier 40V, 500mA FV 500mV Fnl:1175677\n1x      U1  SOIC8 LP2951DR Fnl:2781769
$Comp
L C C1
U 1 1 5ABB8BA8
P 5600 4300
F 0 "C1" H 5625 4400 50  0000 L CNN
F 1 "22uF" H 5625 4200 50  0000 L CNN
F 2 "Capacitors_THT:CP_Radial_D6.3mm_P2.50mm" H 5638 4150 50  0001 C CNN
F 3 "" H 5600 4300 50  0001 C CNN
	1    5600 4300
	1    0    0    -1  
$EndComp
$Comp
L C C2
U 1 1 5ABB8DA0
P 8700 4250
F 0 "C2" H 8725 4350 50  0000 L CNN
F 1 "22uF" H 8725 4150 50  0000 L CNN
F 2 "Capacitors_THT:CP_Radial_D6.3mm_P2.50mm" H 8738 4100 50  0001 C CNN
F 3 "" H 8700 4250 50  0001 C CNN
	1    8700 4250
	1    0    0    -1  
$EndComp
$Comp
L Conn_01x01 J1
U 1 1 5ABBBE6F
P 4050 3950
F 0 "J1" H 4050 4050 50  0000 C CNN
F 1 "Conn_01x01" H 4050 3850 50  0000 C CNN
F 2 "Contact_fingers:Pin_Header_Straight_1x01_Pitch1.00mm" H 4050 3950 50  0001 C CNN
F 3 "" H 4050 3950 50  0001 C CNN
	1    4050 3950
	-1   0    0    1   
$EndComp
$Comp
L Conn_01x01 J2
U 1 1 5ABBBEC0
P 4050 4300
F 0 "J2" H 4050 4400 50  0000 C CNN
F 1 "Conn_01x01" H 4050 4200 50  0000 C CNN
F 2 "Contact_fingers:Pin_Header_Straight_1x01_Pitch1.00mm" H 4050 4300 50  0001 C CNN
F 3 "" H 4050 4300 50  0001 C CNN
	1    4050 4300
	-1   0    0    1   
$EndComp
$Comp
L Conn_01x01 J3
U 1 1 5ABBC0DE
P 5950 3600
F 0 "J3" H 5950 3700 50  0000 C CNN
F 1 "Conn_01x01" H 5950 3500 50  0000 C CNN
F 2 "Contact_fingers:Pin_Header_Straight_1x01_Pitch1.00mm" H 5950 3600 50  0001 C CNN
F 3 "" H 5950 3600 50  0001 C CNN
	1    5950 3600
	0    -1   -1   0   
$EndComp
$Comp
L Conn_01x01 J4
U 1 1 5ABBC139
P 6600 3600
F 0 "J4" H 6600 3700 50  0000 C CNN
F 1 "Conn_01x01" H 6600 3500 50  0000 C CNN
F 2 "Contact_fingers:Pin_Header_Straight_1x01_Pitch1.00mm" H 6600 3600 50  0001 C CNN
F 3 "" H 6600 3600 50  0001 C CNN
	1    6600 3600
	0    -1   -1   0   
$EndComp
$Comp
L LP2951DR U1
U 1 1 5AC38140
P 7450 4250
F 0 "U1" H 7450 4550 60  0000 C CNN
F 1 "LP2951DR" H 7450 4650 60  0000 C CNN
F 2 "SMD_Packages:SOIC-8-N" H 7450 4250 60  0001 C CNN
F 3 "" H 7450 4250 60  0001 C CNN
	1    7450 4250
	1    0    0    -1  
$EndComp
Wire Wire Line
	6450 4100 6800 4100
Wire Wire Line
	6600 4100 6600 3800
Wire Wire Line
	5250 4650 9450 4650
Wire Wire Line
	6800 4200 6800 4650
Connection ~ 6800 4400
Wire Wire Line
	8100 3900 8100 4200
Wire Wire Line
	8100 4100 8050 4100
Wire Wire Line
	8700 3900 8700 4100
Wire Wire Line
	9050 3900 9050 4100
Connection ~ 8700 3900
Wire Wire Line
	9250 3900 9250 4100
Connection ~ 9050 3900
Wire Wire Line
	9450 3900 9450 4100
Connection ~ 9250 3900
Wire Wire Line
	8700 4650 8700 4400
Connection ~ 6800 4650
Wire Wire Line
	9050 4650 9050 4400
Connection ~ 8700 4650
Wire Wire Line
	9250 4650 9250 4400
Connection ~ 9050 4650
Wire Wire Line
	9450 4650 9450 4400
Connection ~ 9250 4650
Wire Wire Line
	5250 3950 5950 3950
Wire Wire Line
	5950 3950 5950 3800
Wire Wire Line
	8100 3900 9450 3900
Connection ~ 6800 4300
Wire Wire Line
	8050 4300 8050 4650
Connection ~ 8050 4650
Connection ~ 8050 4400
$Comp
L C C4
U 1 1 5AC4948C
P 8400 4250
F 0 "C4" H 8425 4350 50  0000 L CNN
F 1 "100nF" H 8425 4150 50  0000 L CNN
F 2 "Capacitors_SMD:C_1206" H 8438 4100 50  0001 C CNN
F 3 "" H 8400 4250 50  0001 C CNN
	1    8400 4250
	1    0    0    -1  
$EndComp
Wire Wire Line
	8100 4200 8050 4200
Connection ~ 8100 4100
Wire Wire Line
	8400 4100 8400 3900
Connection ~ 8400 3900
Wire Wire Line
	8400 4400 8400 4650
Connection ~ 8400 4650
$Comp
L C C3
U 1 1 5AC4A12F
P 6450 4300
F 0 "C3" H 6475 4400 50  0000 L CNN
F 1 "100nF" H 6475 4200 50  0000 L CNN
F 2 "Capacitors_SMD:C_1206" H 6488 4150 50  0001 C CNN
F 3 "" H 6450 4300 50  0001 C CNN
	1    6450 4300
	1    0    0    -1  
$EndComp
Wire Wire Line
	6450 4150 6450 4100
Connection ~ 6600 4100
Wire Wire Line
	6450 4450 6450 4650
Connection ~ 6450 4650
$EndSCHEMATC
