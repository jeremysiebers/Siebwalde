EESchema Schematic File Version 4
LIBS:Modelwagonlight-cache
EELAYER 26 0
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
L device:D D1
U 1 1 5A9520CA
P 5100 3950
F 0 "D1" H 5100 4050 50  0000 C CNN
F 1 "STPS0540Z" H 5100 3850 50  0000 C CNN
F 2 "Diode_SMD:D_SOD-123" H 5100 3950 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2307147.pdf?_ga=2.156369662.418749504.1544431297-523844734.1542029203&_gac=1.55988569.1544607905.Cj0KCQiAgMPgBRDDARIsAOh3uyLPfvJVUZkf7WxE18rn4hxFId_d7BLsoZhKQT-s5UnN8AQpoENdC_QaAjuvEALw_wcB" H 5100 3950 50  0001 C CNN
F 4 "1175677" H 5100 3950 50  0001 C CNN "Farnell"
F 5 "STPS0540Z" H 5100 3950 50  0001 C CNN "Manufacturer nr"
	1    5100 3950
	-1   0    0    1   
$EndComp
$Comp
L device:R R1
U 1 1 5A952272
P 4450 3950
F 0 "R1" V 4530 3950 50  0000 C CNN
F 1 "47E" V 4450 3950 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 4380 3950 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2331353.pdf?_ga=2.124196682.13702668.1544197496-523844734.1542029203&_gac=1.140706310.1543322461.EAIaIQobChMIof7Tlcz03gIVkuJ3Ch1UVwqwEAAYASAAEgK8PvD_BwE" H 4450 3950 50  0001 C CNN
F 4 "1100156" V 4450 3950 50  0001 C CNN "Farnell"
F 5 "WCR1206-47RFI" V 4450 3950 50  0001 C CNN "Manufacturer nr"
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
	4250 4300 4850 4300
$Comp
L device:R R4
U 1 1 5AB3A528
P 9050 4250
F 0 "R4" V 9130 4250 50  0000 C CNN
F 1 "470E" V 9050 4250 50  0000 C CNN
F 2 "Resistor_SMD:R_0603_1608Metric" V 8980 4250 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2331353.pdf?_ga=2.192410474.13702668.1544197496-523844734.1542029203&_gac=1.212666656.1543322461.EAIaIQobChMIof7Tlcz03gIVkuJ3Ch1UVwqwEAAYASAAEgK8PvD_BwE" H 9050 4250 50  0001 C CNN
F 4 "1100183" V 9050 4250 50  0001 C CNN "Farnell"
F 5 "WCR1206-470RFI" V 9050 4250 50  0001 C CNN "Manufacturer nr"
	1    9050 4250
	-1   0    0    1   
$EndComp
Text Notes 8450 3700 0    60   ~ 0
02mA --> 1.235V/0.002 = 617 Ohm\n05mA --> 1.235V/0.005 = 247 Ohm\n10mA --> 1.235V/0.010 = 123.5 Ohm\n12mA --> 1.235V/0.012 = 100 Ohm\n15mA --> 1.235V/0.015 = 82.3 Ohm\n20mA --> 1.235V/0.020 = 61.75 Ohm
Text Notes 7000 3700 0    60   ~ 0
Dropout Voltage  = 0.380V\n
Text Notes 5700 3150 0    60   ~ 0
The amount of Led's depends on the supplied voltage,\nnormal operation from PWM amplifier is 17V minus 2V \ndue to rectifiers is 15V. For a red led a voltage of typ 1.2V\nis required, therfore maximum of 12 led's is achievable
Wire Wire Line
	4250 3950 4300 3950
Wire Wire Line
	4600 3950 4700 3950
$Comp
L device:R R5
U 1 1 5ABB5CE8
P 9250 4250
F 0 "R5" V 9330 4250 50  0000 C CNN
F 1 "NP" V 9250 4250 50  0000 C CNN
F 2 "Resistor_SMD:R_0603_1608Metric" V 9180 4250 50  0001 C CNN
F 3 "" H 9250 4250 50  0001 C CNN
	1    9250 4250
	-1   0    0    1   
$EndComp
Wire Wire Line
	5600 3950 5600 4150
Connection ~ 5600 3950
Wire Wire Line
	5600 4650 5600 4450
Connection ~ 5600 4650
$Comp
L Modelwagonlight-rescue:Conn_01x01 J1
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
L Modelwagonlight-rescue:Conn_01x01 J2
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
L Modelwagonlight-rescue:Conn_01x01 J3
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
L Modelwagonlight-rescue:Conn_01x01 J4
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
L LP2951DR-SOIC8:LP2951DR U1
U 1 1 5AC38140
P 7450 4250
F 0 "U1" H 7450 4550 60  0000 C CNN
F 1 "LP2951DR" H 7450 4650 60  0000 C CNN
F 2 "Package_SO:SOIC-8_3.9x4.9mm_P1.27mm" H 7450 4250 60  0001 C CNN
F 3 "http://www.ti.com/general/docs/lit/getliterature.tsp?genericPartNumber=LP2951&fileType=pdf" H 7450 4250 60  0001 C CNN
F 4 "2781769" H 7450 4250 50  0001 C CNN "Farnell"
F 5 "LP2951DR" H 7450 4250 50  0001 C CNN "Manufacturer nr"
	1    7450 4250
	1    0    0    -1  
$EndComp
Wire Wire Line
	6450 4100 6600 4100
Wire Wire Line
	6600 4100 6600 3800
Wire Wire Line
	5250 4650 5300 4650
Wire Wire Line
	6800 4200 6800 4300
Connection ~ 6800 4400
Wire Wire Line
	8100 3900 8100 4100
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
	8700 4650 8700 4400
Connection ~ 6800 4650
Wire Wire Line
	9050 4650 9050 4400
Connection ~ 8700 4650
Wire Wire Line
	9250 4650 9250 4400
Connection ~ 9050 4650
Wire Wire Line
	5250 3950 5300 3950
Wire Wire Line
	5950 3950 5950 3800
Wire Wire Line
	8100 3900 8400 3900
Connection ~ 6800 4300
Wire Wire Line
	8050 4300 8050 4400
Connection ~ 8050 4650
Connection ~ 8050 4400
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
L device:C C3
U 1 1 5AC4A12F
P 6450 4300
F 0 "C3" H 6475 4400 50  0000 L CNN
F 1 "100n" H 6475 4200 50  0000 L CNN
F 2 "Capacitor_SMD:C_0603_1608Metric" H 6488 4150 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/1901289.pdf?_ga=2.262491141.804204398.1540810031-466615820.1533907205&_gac=1.154756810.1540810031.CjwKCAjw39reBRBJEiwAO1m0OUdqKKSX6YgNsFqfn4kAD7vSQS5JfvsNobbwGFQV72towt0oHUutJhoC7b8QAvD_BwE" H 6450 4300 50  0001 C CNN
F 4 "2896646" H 6450 4300 50  0001 C CNN "Farnell"
F 5 "VJ1206Y104KXXPW1BC" H 6450 4300 50  0001 C CNN "Manufacturer nr"
	1    6450 4300
	1    0    0    -1  
$EndComp
Wire Wire Line
	6450 4150 6450 4100
Connection ~ 6600 4100
Wire Wire Line
	6450 4450 6450 4650
Connection ~ 6450 4650
Wire Wire Line
	5300 4650 5600 4650
Wire Wire Line
	5300 3950 5600 3950
Wire Wire Line
	4700 3950 4950 3950
Wire Wire Line
	4850 4300 4950 4300
Wire Wire Line
	5600 3950 5950 3950
Wire Wire Line
	5600 4650 6450 4650
Wire Wire Line
	6800 4400 6800 4650
Wire Wire Line
	8700 3900 9050 3900
Wire Wire Line
	9050 3900 9250 3900
Wire Wire Line
	6800 4650 8050 4650
Wire Wire Line
	8700 4650 9050 4650
Wire Wire Line
	9050 4650 9250 4650
Wire Wire Line
	6800 4300 6800 4400
Wire Wire Line
	8050 4650 8400 4650
Wire Wire Line
	8050 4400 8050 4650
Wire Wire Line
	8100 4100 8100 4200
Wire Wire Line
	8400 3900 8700 3900
Wire Wire Line
	8400 4650 8700 4650
Wire Wire Line
	6600 4100 6800 4100
Wire Wire Line
	6450 4650 6700 4650
$Comp
L device:C C4
U 1 1 5C10E574
P 8400 4250
F 0 "C4" H 8425 4350 50  0000 L CNN
F 1 "100n" H 8425 4150 50  0000 L CNN
F 2 "Capacitor_SMD:C_0603_1608Metric" H 8438 4100 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/1901289.pdf?_ga=2.262491141.804204398.1540810031-466615820.1533907205&_gac=1.154756810.1540810031.CjwKCAjw39reBRBJEiwAO1m0OUdqKKSX6YgNsFqfn4kAD7vSQS5JfvsNobbwGFQV72towt0oHUutJhoC7b8QAvD_BwE" H 8400 4250 50  0001 C CNN
F 4 "2896646" H 8400 4250 50  0001 C CNN "Farnell"
F 5 "VJ1206Y104KXXPW1BC" H 8400 4250 50  0001 C CNN "Manufacturer nr"
	1    8400 4250
	1    0    0    -1  
$EndComp
$Comp
L device:D D2
U 1 1 5C10E997
P 5100 4300
F 0 "D2" H 5100 4400 50  0000 C CNN
F 1 "STPS0540Z" H 5100 4200 50  0000 C CNN
F 2 "Diode_SMD:D_SOD-123" H 5100 4300 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2307147.pdf?_ga=2.156369662.418749504.1544431297-523844734.1542029203&_gac=1.55988569.1544607905.Cj0KCQiAgMPgBRDDARIsAOh3uyLPfvJVUZkf7WxE18rn4hxFId_d7BLsoZhKQT-s5UnN8AQpoENdC_QaAjuvEALw_wcB" H 5100 4300 50  0001 C CNN
F 4 "1175677" H 5100 4300 50  0001 C CNN "Farnell"
F 5 "STPS0540Z" H 5100 4300 50  0001 C CNN "Manufacturer nr"
	1    5100 4300
	-1   0    0    1   
$EndComp
$Comp
L device:D D3
U 1 1 5C10E9F9
P 5100 4650
F 0 "D3" H 5100 4750 50  0000 C CNN
F 1 "STPS0540Z" H 5100 4550 50  0000 C CNN
F 2 "Diodes_SMD:D_SOD-123" H 5100 4650 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2307147.pdf?_ga=2.156369662.418749504.1544431297-523844734.1542029203&_gac=1.55988569.1544607905.Cj0KCQiAgMPgBRDDARIsAOh3uyLPfvJVUZkf7WxE18rn4hxFId_d7BLsoZhKQT-s5UnN8AQpoENdC_QaAjuvEALw_wcB" H 5100 4650 50  0001 C CNN
F 4 "1175677" H 5100 4650 50  0001 C CNN "Farnell"
F 5 "STPS0540Z" H 5100 4650 50  0001 C CNN "Manufacturer nr"
	1    5100 4650
	1    0    0    -1  
$EndComp
$Comp
L device:D D4
U 1 1 5C10EA75
P 5100 5000
F 0 "D4" H 5100 5100 50  0000 C CNN
F 1 "STPS0540Z" H 5100 4900 50  0000 C CNN
F 2 "Diodes_SMD:D_SOD-123" H 5100 5000 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2307147.pdf?_ga=2.156369662.418749504.1544431297-523844734.1542029203&_gac=1.55988569.1544607905.Cj0KCQiAgMPgBRDDARIsAOh3uyLPfvJVUZkf7WxE18rn4hxFId_d7BLsoZhKQT-s5UnN8AQpoENdC_QaAjuvEALw_wcB" H 5100 5000 50  0001 C CNN
F 4 "1175677" H 5100 5000 50  0001 C CNN "Farnell"
F 5 "STPS0540Z" H 5100 5000 50  0001 C CNN "Manufacturer nr"
	1    5100 5000
	1    0    0    -1  
$EndComp
$Comp
L device:C C1
U 1 1 5C1828A9
P 5600 4300
F 0 "C1" H 5625 4400 50  0000 L CNN
F 1 "22u" H 5625 4200 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 5638 4150 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2079197.pdf?_ga=2.237228869.453305454.1544563035-1187106011.1538513614&_gac=1.262713726.1544645790.Cj0KCQiAgMPgBRDDARIsAOh3uyJWgmg4Jmz9qsKK7EVWh3ooL0QXFRR6pElfBGS6pZkWxoBnou2jGrYaAp9cEALw_wcB" H 5600 4300 50  0001 C CNN
F 4 "2611953" H 5600 4300 50  0001 C CNN "Farnell"
F 5 "GRM31CR61E226ME15L" H 5600 4300 50  0001 C CNN "Manufacturer nr"
	1    5600 4300
	1    0    0    -1  
$EndComp
$Comp
L device:C C2
U 1 1 5C182A5F
P 8700 4250
F 0 "C2" H 8725 4350 50  0000 L CNN
F 1 "22u" H 8725 4150 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 8738 4100 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2079197.pdf?_ga=2.237228869.453305454.1544563035-1187106011.1538513614&_gac=1.262713726.1544645790.Cj0KCQiAgMPgBRDDARIsAOh3uyJWgmg4Jmz9qsKK7EVWh3ooL0QXFRR6pElfBGS6pZkWxoBnou2jGrYaAp9cEALw_wcB" H 8700 4250 50  0001 C CNN
F 4 "2611953" H 8700 4250 50  0001 C CNN "Farnell"
F 5 "GRM31CR61E226ME15L" H 8700 4250 50  0001 C CNN "Manufacturer nr"
	1    8700 4250
	1    0    0    -1  
$EndComp
$Comp
L power:GND #PWR?
U 1 1 5C1839A0
P 6700 4900
F 0 "#PWR?" H 6700 4650 50  0001 C CNN
F 1 "GND" H 6705 4727 50  0000 C CNN
F 2 "" H 6700 4900 50  0001 C CNN
F 3 "" H 6700 4900 50  0001 C CNN
	1    6700 4900
	1    0    0    -1  
$EndComp
Wire Wire Line
	6700 4900 6700 4650
Connection ~ 6700 4650
Wire Wire Line
	6700 4650 6800 4650
$EndSCHEMATC
