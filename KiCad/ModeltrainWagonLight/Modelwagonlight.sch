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
F 1 "100R" V 4450 3950 50  0000 C CNN
F 2 "Resistors_SMD:R_1206_HandSoldering" V 4380 3950 50  0001 C CNN
F 3 "" H 4450 3950 50  0001 C CNN
	1    4450 3950
	0    -1   -1   0   
$EndComp
$Comp
L R R2
U 1 1 5A9526BF
P 7300 4450
F 0 "R2" V 7380 4450 50  0000 C CNN
F 1 "100K" V 7300 4450 50  0000 C CNN
F 2 "Resistors_SMD:R_1206_HandSoldering" V 7230 4450 50  0001 C CNN
F 3 "" H 7300 4450 50  0001 C CNN
	1    7300 4450
	-1   0    0    1   
$EndComp
Wire Wire Line
	5250 3950 6250 3950
Wire Wire Line
	5250 4650 8750 4650
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
L NCP1117-ADJ_SOT223 U1
U 1 1 5AB3A09A
P 7300 3950
F 0 "U1" H 7150 4075 50  0000 C CNN
F 1 "LT3082-SOT223-3_Fnl2295277" H 7300 4075 50  0000 L CNN
F 2 "TO_SOT_Packages_SMD:SOT-223-3_TabPin2" H 7300 4150 50  0001 C CNN
F 3 "" H 7400 3700 50  0001 C CNN
	1    7300 3950
	1    0    0    -1  
$EndComp
Wire Wire Line
	7300 4250 7300 4300
Wire Wire Line
	7300 4600 7300 4650
Connection ~ 7300 4650
$Comp
L R R3
U 1 1 5AB3A528
P 8350 4250
F 0 "R3" V 8430 4250 50  0000 C CNN
F 1 "100R" V 8350 4250 50  0000 C CNN
F 2 "Resistors_SMD:R_1206_HandSoldering" V 8280 4250 50  0001 C CNN
F 3 "" H 8350 4250 50  0001 C CNN
	1    8350 4250
	-1   0    0    1   
$EndComp
Wire Wire Line
	7600 3950 8750 3950
Wire Wire Line
	8350 3950 8350 4100
Text Notes 6900 4800 0    60   ~ 0
100K = 1V output
Text Notes 8850 4550 0    60   ~ 0
05mA --> 1V/0.005 = 200 Ohm\n10mA --> 1V/0.010 = 100 Ohm\n15mA --> 1V/0.015 = 67 Ohm\n20mA --> 1V/0.020 = 50 Ohm\n(0.5mA is minimum requirement \nfor regulation)
Text Notes 7100 3600 0    60   ~ 0
Dropout Voltage @ Iload = 10   mA --> 1.22V typ\nDropout Voltage @ Iload = 200 mA --> 1.3V typ
Connection ~ 7500 4650
Wire Wire Line
	7300 4300 7500 4300
Wire Wire Line
	8350 4650 8350 4400
Wire Wire Line
	5900 4150 5900 3950
Connection ~ 5900 3950
Wire Wire Line
	5900 4450 5900 4650
Connection ~ 5900 4650
Wire Wire Line
	7950 4150 7950 3950
Connection ~ 7950 3950
Wire Wire Line
	7950 4450 7950 4650
Connection ~ 7950 4650
Text Notes 7950 4900 0    60   ~ 0
2.2uF is recommendation from datasheet \non output capacity to prevent oscillations\n
Text Notes 7100 3150 0    60   ~ 0
The amount of Led's depends on the supplied voltage,\nnormal operation from PWM amplifier is 17V minus 2V \ndue to rectifiers is 15V. For a red led a voltage of typ 1.2V\nis required, therfore maximum of 12 led's is achievable
Wire Wire Line
	7500 4600 7500 4650
Wire Wire Line
	6600 3950 7000 3950
Wire Wire Line
	4250 3950 4300 3950
Wire Wire Line
	4600 3950 4950 3950
$Comp
L R R4
U 1 1 5ABB5CE8
P 8550 4250
F 0 "R4" V 8630 4250 50  0000 C CNN
F 1 "100R" V 8550 4250 50  0000 C CNN
F 2 "Resistors_SMD:R_1206_HandSoldering" V 8480 4250 50  0001 C CNN
F 3 "" H 8550 4250 50  0001 C CNN
	1    8550 4250
	-1   0    0    1   
$EndComp
$Comp
L R R5
U 1 1 5ABB5D36
P 8750 4250
F 0 "R5" V 8830 4250 50  0000 C CNN
F 1 "100R" V 8750 4250 50  0000 C CNN
F 2 "Resistors_SMD:R_1206_HandSoldering" V 8680 4250 50  0001 C CNN
F 3 "" H 8750 4250 50  0001 C CNN
	1    8750 4250
	-1   0    0    1   
$EndComp
Wire Wire Line
	8550 3950 8550 4100
Connection ~ 8350 3950
Wire Wire Line
	8750 3950 8750 4100
Connection ~ 8550 3950
Wire Wire Line
	8550 4650 8550 4400
Connection ~ 8350 4650
Wire Wire Line
	8750 4650 8750 4400
Connection ~ 8550 4650
Wire Wire Line
	5600 4150 5600 3950
Connection ~ 5600 3950
Wire Wire Line
	5600 4450 5600 4650
Connection ~ 5600 4650
Text Notes 4950 5950 0    60   ~ 0
BOM:\n2(3)x C1,2,4     1206 multilayer ceramic capacitor 10uF 35V Fnl:1797011\n1x      C3          1206 multilayer ceramic capacitor 100nF 50V Fnl:1414715\n2(4)x R1,2,3,4  1206 resistor Multicomp 100Ohm 500mW    Fnl:2694436\n1x      R2           1206 resistor Multicomp 100KOhm 250mW  Fnl:2073879\n4x      D1,2,3,4  SOD-123 Shottky rectifier 40V, 500mA FV 500mV Fnl:1175677\n1x      U1           SOT-223 LT3082 linear adjustable regulator Fnl:2295277
$Comp
L C C1
U 1 1 5ABB8BA8
P 5600 4300
F 0 "C1" H 5625 4400 50  0000 L CNN
F 1 "10uF" H 5625 4200 50  0000 L CNN
F 2 "Capacitors_SMD:C_1206_HandSoldering" H 5638 4150 50  0001 C CNN
F 3 "" H 5600 4300 50  0001 C CNN
	1    5600 4300
	1    0    0    -1  
$EndComp
$Comp
L C C2
U 1 1 5ABB8CAB
P 5900 4300
F 0 "C2" H 5925 4400 50  0000 L CNN
F 1 "10uF" H 5925 4200 50  0000 L CNN
F 2 "Capacitors_SMD:C_1206_HandSoldering" H 5938 4150 50  0001 C CNN
F 3 "" H 5900 4300 50  0001 C CNN
	1    5900 4300
	1    0    0    -1  
$EndComp
$Comp
L C C3
U 1 1 5ABB8D4D
P 7500 4450
F 0 "C3" H 7525 4550 50  0000 L CNN
F 1 "100nF" H 7525 4350 50  0000 L CNN
F 2 "Capacitors_SMD:C_1206_HandSoldering" H 7538 4300 50  0001 C CNN
F 3 "" H 7500 4450 50  0001 C CNN
	1    7500 4450
	1    0    0    -1  
$EndComp
$Comp
L C C4
U 1 1 5ABB8DA0
P 7950 4300
F 0 "C4" H 7975 4400 50  0000 L CNN
F 1 "10uF" H 7975 4200 50  0000 L CNN
F 2 "Capacitors_SMD:C_1206_HandSoldering" H 7988 4150 50  0001 C CNN
F 3 "" H 7950 4300 50  0001 C CNN
	1    7950 4300
	1    0    0    -1  
$EndComp
$Comp
L Conn_01x01 J1
U 1 1 5ABBBE6F
P 4050 3950
F 0 "J1" H 4050 4050 50  0000 C CNN
F 1 "Conn_01x01" H 4050 3850 50  0000 C CNN
F 2 "Pin_Headers:Pin_Header_Straight_1x01_Pitch1.00mm" H 4050 3950 50  0001 C CNN
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
F 2 "Pin_Headers:Pin_Header_Straight_1x01_Pitch1.00mm" H 4050 4300 50  0001 C CNN
F 3 "" H 4050 4300 50  0001 C CNN
	1    4050 4300
	-1   0    0    1   
$EndComp
$Comp
L Conn_01x01 J3
U 1 1 5ABBC0DE
P 6250 3600
F 0 "J3" H 6250 3700 50  0000 C CNN
F 1 "Conn_01x01" H 6250 3500 50  0000 C CNN
F 2 "Pin_Headers:Pin_Header_Straight_1x01_Pitch1.00mm" H 6250 3600 50  0001 C CNN
F 3 "" H 6250 3600 50  0001 C CNN
	1    6250 3600
	0    -1   -1   0   
$EndComp
$Comp
L Conn_01x01 J4
U 1 1 5ABBC139
P 6600 3600
F 0 "J4" H 6600 3700 50  0000 C CNN
F 1 "Conn_01x01" H 6600 3500 50  0000 C CNN
F 2 "Pin_Headers:Pin_Header_Straight_1x01_Pitch1.00mm" H 6600 3600 50  0001 C CNN
F 3 "" H 6600 3600 50  0001 C CNN
	1    6600 3600
	0    -1   -1   0   
$EndComp
Wire Wire Line
	6250 3950 6250 3800
Wire Wire Line
	6600 3800 6600 3950
$EndSCHEMATC
