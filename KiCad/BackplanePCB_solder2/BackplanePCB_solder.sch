EESchema Schematic File Version 4
LIBS:BackplanePCB_solder-cache
EELAYER 26 0
EELAYER END
$Descr A4 11693 8268
encoding utf-8
Sheet 1 2
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
L Connector:DB15_Female_MountingHoles J8
U 1 1 5B190ED2
P 4050 2750
F 0 "J8" H 4050 3700 50  0000 C CNN
F 1 "DB15_Female_MountingHoles" H 4050 3625 50  0000 C CNN
F 2 "Connector_Dsub:DSUB-15_Female_Vertical_P2.77x2.84mm_MountingHoles" H 4050 2750 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/1842458.pdf?_ga=2.174665762.1897837425.1528366125-1651149783.1519657816" H 4050 2750 50  0001 C CNN
	1    4050 2750
	1    0    0    -1  
$EndComp
$Comp
L Connector_Generic:Conn_02x05_Odd_Even J1
U 1 1 5B190FA2
P 1650 2050
F 0 "J1" H 1700 2350 50  0000 C CNN
F 1 "Conn_02x05_Odd_Even" H 1700 1750 50  0000 C CNN
F 2 "Connector_IDC:IDC-Header_2x05_P2.54mm_Vertical" H 1650 2050 50  0001 C CNN
F 3 "~" H 1650 2050 50  0001 C CNN
	1    1650 2050
	1    0    0    -1  
$EndComp
$Comp
L Connector_Generic:Conn_02x05_Odd_Even J2
U 1 1 5B19E416
P 1650 2800
F 0 "J2" H 1700 3100 50  0000 C CNN
F 1 "Conn_02x05_Odd_Even" H 1700 2500 50  0000 C CNN
F 2 "Connector_IDC:IDC-Header_2x05_P2.54mm_Vertical" H 1650 2800 50  0001 C CNN
F 3 "~" H 1650 2800 50  0001 C CNN
	1    1650 2800
	1    0    0    -1  
$EndComp
$Comp
L Connector_Generic:Conn_02x05_Counter_Clockwise J3
U 1 1 5B19E45C
P 2200 1150
F 0 "J3" H 2250 1450 50  0000 C CNN
F 1 "PowerBug+15V" H 2250 850 50  0000 C CNN
F 2 "Package_DIP:DIP-10_W7.62mm" H 2200 1150 50  0001 C CNN
F 3 "~" H 2200 1150 50  0001 C CNN
	1    2200 1150
	1    0    0    -1  
$EndComp
$Comp
L Connector_Generic:Conn_02x05_Counter_Clockwise J4
U 1 1 5B19E4E7
P 3200 1050
F 0 "J4" H 3250 1350 50  0000 C CNN
F 1 "PowerBug_GND" H 3250 750 50  0000 C CNN
F 2 "Package_DIP:DIP-10_W7.62mm" H 3200 1050 50  0001 C CNN
F 3 "~" H 3200 1050 50  0001 C CNN
	1    3200 1050
	1    0    0    -1  
$EndComp
Text Label 1100 2250 0    50   ~ 0
RESET_A
Text Label 2000 2250 0    50   ~ 0
RESET_B
Entry Wire Line
	800  1950 900  1850
Entry Wire Line
	800  2050 900  1950
Entry Wire Line
	800  2150 900  2050
Entry Wire Line
	800  2250 900  2150
Entry Wire Line
	2550 1850 2650 1750
Entry Wire Line
	2550 1950 2650 1850
Entry Wire Line
	2550 2050 2650 1950
Entry Wire Line
	2550 2150 2650 2050
Wire Wire Line
	1950 1850 2550 1850
Wire Wire Line
	2550 1950 1950 1950
Wire Wire Line
	1950 2050 2550 2050
Wire Wire Line
	1950 2150 2550 2150
Wire Wire Line
	900  1850 1450 1850
Wire Wire Line
	900  1950 1450 1950
Wire Wire Line
	1450 2050 900  2050
Wire Wire Line
	900  2150 1450 2150
Entry Wire Line
	800  2700 900  2600
Entry Wire Line
	800  2800 900  2700
Entry Wire Line
	800  2900 900  2800
Entry Wire Line
	800  3000 900  2900
Entry Wire Line
	2550 2600 2650 2500
Entry Wire Line
	2550 2700 2650 2600
Entry Wire Line
	2550 2800 2650 2700
Entry Wire Line
	2550 2900 2650 2800
Wire Wire Line
	2550 2600 1950 2600
Wire Wire Line
	1950 2700 2550 2700
Wire Wire Line
	2550 2800 1950 2800
Wire Wire Line
	1950 2900 2550 2900
Wire Wire Line
	900  2600 1450 2600
Wire Wire Line
	1450 2700 900  2700
Wire Wire Line
	900  2800 1450 2800
Wire Wire Line
	1450 2900 900  2900
Text Label 1100 3000 0    50   ~ 0
RESET_A
Text Label 2000 3000 0    50   ~ 0
RESET_B
Text Label 1100 2150 0    50   ~ 0
SYNC_A
Text Label 2000 2150 0    50   ~ 0
SYNC_B
Text Label 1100 2900 0    50   ~ 0
SYNC_A
Text Label 2000 2900 0    50   ~ 0
SYNC_B
Text Label 1100 2700 0    50   ~ 0
RX_A
Text Label 1100 1950 0    50   ~ 0
RX_A
Text Label 2000 1950 0    50   ~ 0
RX_B
Text Label 2000 2700 0    50   ~ 0
RX_B
Text Label 1100 1850 0    50   ~ 0
TX_A
Text Label 2000 1850 0    50   ~ 0
TX_B
Text Label 1100 2600 0    50   ~ 0
TX_A
Text Label 2000 2600 0    50   ~ 0
TX_B
$Comp
L power:+15V #PWR0101
U 1 1 5B1A2E93
P 1900 850
F 0 "#PWR0101" H 1900 700 50  0001 C CNN
F 1 "+15V" H 1900 990 50  0000 C CNN
F 2 "" H 1900 850 50  0001 C CNN
F 3 "" H 1900 850 50  0001 C CNN
	1    1900 850 
	1    0    0    -1  
$EndComp
$Comp
L power:GND #PWR0102
U 1 1 5B1A2F08
P 2900 1350
F 0 "#PWR0102" H 2900 1100 50  0001 C CNN
F 1 "GND" H 2900 1200 50  0000 C CNN
F 2 "" H 2900 1350 50  0001 C CNN
F 3 "" H 2900 1350 50  0001 C CNN
	1    2900 1350
	1    0    0    -1  
$EndComp
$Comp
L power:GND #PWR0103
U 1 1 5B1A2F8D
P 3600 1350
F 0 "#PWR0103" H 3600 1100 50  0001 C CNN
F 1 "GND" H 3600 1200 50  0000 C CNN
F 2 "" H 3600 1350 50  0001 C CNN
F 3 "" H 3600 1350 50  0001 C CNN
	1    3600 1350
	1    0    0    -1  
$EndComp
$Comp
L power:+15V #PWR0104
U 1 1 5B1A2FA2
P 2600 850
F 0 "#PWR0104" H 2600 700 50  0001 C CNN
F 1 "+15V" H 2600 990 50  0000 C CNN
F 2 "" H 2600 850 50  0001 C CNN
F 3 "" H 2600 850 50  0001 C CNN
	1    2600 850 
	1    0    0    -1  
$EndComp
Wire Wire Line
	2000 1350 1900 1350
Wire Wire Line
	1900 1350 1900 1250
Wire Wire Line
	2000 1250 1900 1250
Connection ~ 1900 1250
Wire Wire Line
	1900 1250 1900 1150
Wire Wire Line
	2000 1150 1900 1150
Connection ~ 1900 1150
Wire Wire Line
	1900 1150 1900 1050
Wire Wire Line
	2000 1050 1900 1050
Connection ~ 1900 1050
Wire Wire Line
	1900 1050 1900 950 
Wire Wire Line
	2000 950  1900 950 
Connection ~ 1900 950 
Wire Wire Line
	1900 950  1900 850 
Wire Wire Line
	2600 850  2600 950 
Wire Wire Line
	2600 950  2500 950 
Wire Wire Line
	2500 1050 2600 1050
Wire Wire Line
	2600 1050 2600 950 
Connection ~ 2600 950 
Wire Wire Line
	2500 1150 2600 1150
Wire Wire Line
	2600 1150 2600 1050
Connection ~ 2600 1050
Wire Wire Line
	2500 1250 2600 1250
Wire Wire Line
	2600 1250 2600 1150
Connection ~ 2600 1150
Wire Wire Line
	2500 1350 2600 1350
Wire Wire Line
	2600 1350 2600 1250
Connection ~ 2600 1250
Wire Wire Line
	2900 1350 2900 1250
Wire Wire Line
	2900 850  3000 850 
Wire Wire Line
	3000 950  2900 950 
Connection ~ 2900 950 
Wire Wire Line
	2900 950  2900 850 
Wire Wire Line
	3000 1050 2900 1050
Connection ~ 2900 1050
Wire Wire Line
	2900 1050 2900 950 
Wire Wire Line
	3000 1150 2900 1150
Connection ~ 2900 1150
Wire Wire Line
	2900 1150 2900 1050
Wire Wire Line
	3000 1250 2900 1250
Connection ~ 2900 1250
Wire Wire Line
	2900 1250 2900 1150
Wire Wire Line
	3600 1350 3600 1250
Wire Wire Line
	3600 1250 3500 1250
Wire Wire Line
	3500 1150 3600 1150
Wire Wire Line
	3600 1150 3600 1250
Connection ~ 3600 1250
Wire Wire Line
	3500 1050 3600 1050
Wire Wire Line
	3600 1050 3600 1150
Connection ~ 3600 1150
Wire Wire Line
	3500 950  3600 950 
Wire Wire Line
	3600 950  3600 1050
Connection ~ 3600 1050
Wire Wire Line
	3500 850  3600 850 
Wire Wire Line
	3600 850  3600 950 
Connection ~ 3600 950 
Entry Wire Line
	3050 2150 3150 2250
Entry Wire Line
	3050 2050 3150 2150
Entry Wire Line
	3050 1950 3150 2050
Entry Wire Line
	3050 2250 3150 2350
Entry Wire Line
	3050 2350 3150 2450
Entry Wire Line
	3050 2450 3150 2550
Entry Wire Line
	3050 2550 3150 2650
Entry Wire Line
	3050 2650 3150 2750
Entry Wire Line
	3050 2750 3150 2850
Entry Wire Line
	3050 2850 3150 2950
Entry Wire Line
	3050 2950 3150 3050
Entry Wire Line
	3050 3050 3150 3150
Entry Wire Line
	3050 3150 3150 3250
Entry Wire Line
	3050 3250 3150 3350
Entry Wire Line
	3050 3350 3150 3450
Entry Wire Line
	3050 3550 3150 3650
Entry Wire Line
	800  1350 900  1250
Entry Wire Line
	800  1600 900  1500
$Comp
L power:+15V #PWR0105
U 1 1 5B1B315B
P 1450 1250
F 0 "#PWR0105" H 1450 1100 50  0001 C CNN
F 1 "+15V" H 1450 1390 50  0000 C CNN
F 2 "" H 1450 1250 50  0001 C CNN
F 3 "" H 1450 1250 50  0001 C CNN
	1    1450 1250
	1    0    0    -1  
$EndComp
$Comp
L power:GND #PWR0106
U 1 1 5B1B3170
P 1450 1500
F 0 "#PWR0106" H 1450 1250 50  0001 C CNN
F 1 "GND" H 1450 1350 50  0000 C CNN
F 2 "" H 1450 1500 50  0001 C CNN
F 3 "" H 1450 1500 50  0001 C CNN
	1    1450 1500
	1    0    0    -1  
$EndComp
Text Label 1100 1250 0    50   ~ 0
+15V
Text Label 1100 1500 0    50   ~ 0
GND
Wire Wire Line
	900  1250 1450 1250
Wire Wire Line
	900  1500 1450 1500
Wire Wire Line
	3750 2350 3150 2350
Wire Wire Line
	3750 2450 3150 2450
Wire Wire Line
	3150 2550 3750 2550
Wire Wire Line
	3750 2650 3150 2650
Wire Wire Line
	3150 2750 3750 2750
Wire Wire Line
	3750 2850 3150 2850
Wire Wire Line
	3150 2950 3750 2950
Wire Wire Line
	3750 3050 3150 3050
Wire Wire Line
	3750 3150 3150 3150
Wire Wire Line
	3750 3250 3150 3250
Wire Wire Line
	3750 2050 3150 2050
Wire Wire Line
	3150 2150 3750 2150
Wire Wire Line
	3750 3350 3150 3350
Wire Wire Line
	3750 3450 3150 3450
Wire Wire Line
	3150 3650 4050 3650
Wire Wire Line
	3750 2250 3150 2250
Text Label 3350 3150 0    50   ~ 0
RESET_A
Text Label 3350 3050 0    50   ~ 0
RESET_B
Text Label 3350 2950 0    50   ~ 0
SYNC_A
Text Label 3350 2850 0    50   ~ 0
SYNC_B
Text Label 3350 2550 0    50   ~ 0
TX_A
Text Label 3350 2450 0    50   ~ 0
TX_B
Text Label 3350 2750 0    50   ~ 0
RX_A
Text Label 3350 2650 0    50   ~ 0
RX_B
Entry Wire Line
	800  2350 900  2250
Entry Wire Line
	800  3100 900  3000
Entry Wire Line
	2550 2250 2650 2150
Entry Wire Line
	2550 3000 2650 2900
Wire Wire Line
	900  2250 1450 2250
Wire Wire Line
	1950 2250 2550 2250
Wire Wire Line
	900  3000 1450 3000
Wire Wire Line
	1950 3000 2550 3000
Text Label 2000 2050 0    50   ~ 0
GND
Text Label 1100 2050 0    50   ~ 0
GND
Text Label 2000 2800 0    50   ~ 0
GND
Text Label 1100 2800 0    50   ~ 0
GND
Text Label 3350 3650 0    50   ~ 0
GND
Text Label 3350 3450 0    50   ~ 0
GND
Text Label 3350 2250 0    50   ~ 0
GND
Text Label 3350 2350 0    50   ~ 0
+15V
Text Label 3350 3350 0    50   ~ 0
+15V
Text Label 3350 3250 0    50   ~ 0
ID_10
Text Label 3350 2050 0    50   ~ 0
PWMO2_1
Text Label 3350 2150 0    50   ~ 0
PWMO1_1
$Comp
L Connector:DB15_Female_MountingHoles J16
U 1 1 5B235EA8
P 5700 2750
F 0 "J16" H 5700 3700 50  0000 C CNN
F 1 "DB15_Female_MountingHoles" H 5700 3625 50  0000 C CNN
F 2 "Connector_Dsub:DSUB-15_Female_Vertical_P2.77x2.84mm_MountingHoles" H 5700 2750 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/1842458.pdf?_ga=2.174665762.1897837425.1528366125-1651149783.1519657816" H 5700 2750 50  0001 C CNN
	1    5700 2750
	1    0    0    -1  
$EndComp
Entry Wire Line
	4700 2150 4800 2250
Entry Wire Line
	4700 2050 4800 2150
Entry Wire Line
	4700 1950 4800 2050
Entry Wire Line
	4700 2250 4800 2350
Entry Wire Line
	4700 2350 4800 2450
Entry Wire Line
	4700 2450 4800 2550
Entry Wire Line
	4700 2550 4800 2650
Entry Wire Line
	4700 2650 4800 2750
Entry Wire Line
	4700 2750 4800 2850
Entry Wire Line
	4700 2850 4800 2950
Entry Wire Line
	4700 2950 4800 3050
Entry Wire Line
	4700 3050 4800 3150
Entry Wire Line
	4700 3150 4800 3250
Entry Wire Line
	4700 3250 4800 3350
Entry Wire Line
	4700 3350 4800 3450
Entry Wire Line
	4700 3550 4800 3650
Wire Wire Line
	5400 2350 4800 2350
Wire Wire Line
	5400 2450 4800 2450
Wire Wire Line
	4800 2550 5400 2550
Wire Wire Line
	5400 2650 4800 2650
Wire Wire Line
	4800 2750 5400 2750
Wire Wire Line
	5400 2850 4800 2850
Wire Wire Line
	4800 2950 5400 2950
Wire Wire Line
	5400 3050 4800 3050
Wire Wire Line
	5400 3150 4800 3150
Wire Wire Line
	5400 3250 4800 3250
Wire Wire Line
	5400 2050 4800 2050
Wire Wire Line
	4800 2150 5400 2150
Wire Wire Line
	5400 3350 4800 3350
Wire Wire Line
	5400 3450 4800 3450
Wire Wire Line
	4800 3650 5700 3650
Wire Wire Line
	5400 2250 4800 2250
Text Label 5000 3150 0    50   ~ 0
RESET_A
Text Label 5000 3050 0    50   ~ 0
RESET_B
Text Label 5000 2950 0    50   ~ 0
SYNC_A
Text Label 5000 2850 0    50   ~ 0
SYNC_B
Text Label 5000 2550 0    50   ~ 0
TX_A
Text Label 5000 2450 0    50   ~ 0
TX_B
Text Label 5000 2750 0    50   ~ 0
RX_A
Text Label 5000 2650 0    50   ~ 0
RX_B
Text Label 5000 3650 0    50   ~ 0
GND
Text Label 5000 3450 0    50   ~ 0
GND
Text Label 5000 2250 0    50   ~ 0
GND
Text Label 5000 2350 0    50   ~ 0
+15V
Text Label 5000 3350 0    50   ~ 0
+15V
Text Label 5000 3250 0    50   ~ 0
ID_9
Text Label 5000 2050 0    50   ~ 0
PWMO2_2
Text Label 5000 2150 0    50   ~ 0
PWMO1_2
$Comp
L Connector:DB15_Female_MountingHoles J19
U 1 1 5B239195
P 7300 2750
F 0 "J19" H 7300 3700 50  0000 C CNN
F 1 "DB15_Female_MountingHoles" H 7300 3625 50  0000 C CNN
F 2 "Connector_Dsub:DSUB-15_Female_Vertical_P2.77x2.84mm_MountingHoles" H 7300 2750 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/1842458.pdf?_ga=2.174665762.1897837425.1528366125-1651149783.1519657816" H 7300 2750 50  0001 C CNN
	1    7300 2750
	1    0    0    -1  
$EndComp
Entry Wire Line
	6300 2150 6400 2250
Entry Wire Line
	6300 2050 6400 2150
Entry Wire Line
	6300 1950 6400 2050
Entry Wire Line
	6300 2250 6400 2350
Entry Wire Line
	6300 2350 6400 2450
Entry Wire Line
	6300 2450 6400 2550
Entry Wire Line
	6300 2550 6400 2650
Entry Wire Line
	6300 2650 6400 2750
Entry Wire Line
	6300 2750 6400 2850
Entry Wire Line
	6300 2850 6400 2950
Entry Wire Line
	6300 2950 6400 3050
Entry Wire Line
	6300 3050 6400 3150
Entry Wire Line
	6300 3150 6400 3250
Entry Wire Line
	6300 3250 6400 3350
Entry Wire Line
	6300 3350 6400 3450
Entry Wire Line
	6300 3550 6400 3650
Wire Wire Line
	7000 2350 6400 2350
Wire Wire Line
	7000 2450 6400 2450
Wire Wire Line
	6400 2550 7000 2550
Wire Wire Line
	7000 2650 6400 2650
Wire Wire Line
	6400 2750 7000 2750
Wire Wire Line
	7000 2850 6400 2850
Wire Wire Line
	6400 2950 7000 2950
Wire Wire Line
	7000 3050 6400 3050
Wire Wire Line
	7000 3150 6400 3150
Wire Wire Line
	7000 3250 6400 3250
Wire Wire Line
	7000 2050 6400 2050
Wire Wire Line
	6400 2150 7000 2150
Wire Wire Line
	7000 3350 6400 3350
Wire Wire Line
	7000 3450 6400 3450
Wire Wire Line
	6400 3650 7300 3650
Wire Wire Line
	7000 2250 6400 2250
Text Label 6600 3150 0    50   ~ 0
RESET_A
Text Label 6600 3050 0    50   ~ 0
RESET_B
Text Label 6600 2950 0    50   ~ 0
SYNC_A
Text Label 6600 2850 0    50   ~ 0
SYNC_B
Text Label 6600 2550 0    50   ~ 0
TX_A
Text Label 6600 2450 0    50   ~ 0
TX_B
Text Label 6600 2750 0    50   ~ 0
RX_A
Text Label 6600 2650 0    50   ~ 0
RX_B
Text Label 6600 3650 0    50   ~ 0
GND
Text Label 6600 3450 0    50   ~ 0
GND
Text Label 6600 2250 0    50   ~ 0
GND
Text Label 6600 2350 0    50   ~ 0
+15V
Text Label 6600 3350 0    50   ~ 0
+15V
Text Label 6600 3250 0    50   ~ 0
ID_8
Text Label 6600 2050 0    50   ~ 0
PWMO2_3
Text Label 6600 2150 0    50   ~ 0
PWMO1_3
$Comp
L Connector:DB15_Female_MountingHoles J21
U 1 1 5B24AA39
P 8850 2750
F 0 "J21" H 8850 3700 50  0000 C CNN
F 1 "DB15_Female_MountingHoles" H 8850 3625 50  0000 C CNN
F 2 "Connector_Dsub:DSUB-15_Female_Vertical_P2.77x2.84mm_MountingHoles" H 8850 2750 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/1842458.pdf?_ga=2.174665762.1897837425.1528366125-1651149783.1519657816" H 8850 2750 50  0001 C CNN
	1    8850 2750
	1    0    0    -1  
$EndComp
Entry Wire Line
	7850 2150 7950 2250
Entry Wire Line
	7850 2050 7950 2150
Entry Wire Line
	7850 1950 7950 2050
Entry Wire Line
	7850 2250 7950 2350
Entry Wire Line
	7850 2350 7950 2450
Entry Wire Line
	7850 2450 7950 2550
Entry Wire Line
	7850 2550 7950 2650
Entry Wire Line
	7850 2650 7950 2750
Entry Wire Line
	7850 2750 7950 2850
Entry Wire Line
	7850 2850 7950 2950
Entry Wire Line
	7850 2950 7950 3050
Entry Wire Line
	7850 3050 7950 3150
Entry Wire Line
	7850 3150 7950 3250
Entry Wire Line
	7850 3250 7950 3350
Entry Wire Line
	7850 3350 7950 3450
Entry Wire Line
	7850 3550 7950 3650
Wire Wire Line
	8550 2350 7950 2350
Wire Wire Line
	8550 2450 7950 2450
Wire Wire Line
	7950 2550 8550 2550
Wire Wire Line
	8550 2650 7950 2650
Wire Wire Line
	7950 2750 8550 2750
Wire Wire Line
	8550 2850 7950 2850
Wire Wire Line
	7950 2950 8550 2950
Wire Wire Line
	8550 3050 7950 3050
Wire Wire Line
	8550 3150 7950 3150
Wire Wire Line
	8550 3250 7950 3250
Wire Wire Line
	8550 2050 7950 2050
Wire Wire Line
	7950 2150 8550 2150
Wire Wire Line
	8550 3350 7950 3350
Wire Wire Line
	8550 3450 7950 3450
Wire Wire Line
	7950 3650 8850 3650
Wire Wire Line
	8550 2250 7950 2250
Text Label 8150 3150 0    50   ~ 0
RESET_A
Text Label 8150 3050 0    50   ~ 0
RESET_B
Text Label 8150 2950 0    50   ~ 0
SYNC_A
Text Label 8150 2850 0    50   ~ 0
SYNC_B
Text Label 8150 2550 0    50   ~ 0
TX_A
Text Label 8150 2450 0    50   ~ 0
TX_B
Text Label 8150 2750 0    50   ~ 0
RX_A
Text Label 8150 2650 0    50   ~ 0
RX_B
Text Label 8150 3650 0    50   ~ 0
GND
Text Label 8150 3450 0    50   ~ 0
GND
Text Label 8150 2250 0    50   ~ 0
GND
Text Label 8150 2350 0    50   ~ 0
+15V
Text Label 8150 3350 0    50   ~ 0
+15V
Text Label 8150 3250 0    50   ~ 0
ID_7
Text Label 8150 2050 0    50   ~ 0
PWMO2_4
Text Label 8150 2150 0    50   ~ 0
PWMO1_4
$Comp
L Connector:DB15_Female_MountingHoles J23
U 1 1 5B24AA70
P 10450 2750
F 0 "J23" H 10450 3700 50  0000 C CNN
F 1 "DB15_Female_MountingHoles" H 10450 3625 50  0000 C CNN
F 2 "Connector_Dsub:DSUB-15_Female_Vertical_P2.77x2.84mm_MountingHoles" H 10450 2750 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/1842458.pdf?_ga=2.174665762.1897837425.1528366125-1651149783.1519657816" H 10450 2750 50  0001 C CNN
	1    10450 2750
	1    0    0    -1  
$EndComp
Entry Wire Line
	9450 2150 9550 2250
Entry Wire Line
	9450 2050 9550 2150
Entry Wire Line
	9450 1950 9550 2050
Entry Wire Line
	9450 2250 9550 2350
Entry Wire Line
	9450 2350 9550 2450
Entry Wire Line
	9450 2450 9550 2550
Entry Wire Line
	9450 2550 9550 2650
Entry Wire Line
	9450 2650 9550 2750
Entry Wire Line
	9450 2750 9550 2850
Entry Wire Line
	9450 2850 9550 2950
Entry Wire Line
	9450 2950 9550 3050
Entry Wire Line
	9450 3050 9550 3150
Entry Wire Line
	9450 3150 9550 3250
Entry Wire Line
	9450 3250 9550 3350
Entry Wire Line
	9450 3350 9550 3450
Entry Wire Line
	9450 3550 9550 3650
Wire Wire Line
	10150 2350 9550 2350
Wire Wire Line
	10150 2450 9550 2450
Wire Wire Line
	9550 2550 10150 2550
Wire Wire Line
	10150 2650 9550 2650
Wire Wire Line
	9550 2750 10150 2750
Wire Wire Line
	10150 2850 9550 2850
Wire Wire Line
	9550 2950 10150 2950
Wire Wire Line
	10150 3050 9550 3050
Wire Wire Line
	10150 3150 9550 3150
Wire Wire Line
	10150 3250 9550 3250
Wire Wire Line
	10150 2050 9550 2050
Wire Wire Line
	9550 2150 10150 2150
Wire Wire Line
	10150 3350 9550 3350
Wire Wire Line
	10150 3450 9550 3450
Wire Wire Line
	9550 3650 10450 3650
Wire Wire Line
	10150 2250 9550 2250
Text Label 9750 3150 0    50   ~ 0
RESET_A
Text Label 9750 3050 0    50   ~ 0
RESET_B
Text Label 9750 2950 0    50   ~ 0
SYNC_A
Text Label 9750 2850 0    50   ~ 0
SYNC_B
Text Label 9750 2550 0    50   ~ 0
TX_A
Text Label 9750 2450 0    50   ~ 0
TX_B
Text Label 9750 2750 0    50   ~ 0
RX_A
Text Label 9750 2650 0    50   ~ 0
RX_B
Text Label 9750 3650 0    50   ~ 0
GND
Text Label 9750 3450 0    50   ~ 0
GND
Text Label 9750 2250 0    50   ~ 0
GND
Text Label 9750 2350 0    50   ~ 0
+15V
Text Label 9750 3350 0    50   ~ 0
+15V
Text Label 9750 3250 0    50   ~ 0
ID_6
Text Label 9750 2050 0    50   ~ 0
PWMO2_5
Text Label 9750 2150 0    50   ~ 0
PWMO1_5
$Comp
L Connector:DB15_Female_MountingHoles J9
U 1 1 5B2523E0
P 4050 5050
F 0 "J9" H 4050 6000 50  0000 C CNN
F 1 "DB15_Female_MountingHoles" H 4050 5925 50  0000 C CNN
F 2 "Connector_Dsub:DSUB-15_Female_Vertical_P2.77x2.84mm_MountingHoles" H 4050 5050 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/1842458.pdf?_ga=2.174665762.1897837425.1528366125-1651149783.1519657816" H 4050 5050 50  0001 C CNN
	1    4050 5050
	1    0    0    -1  
$EndComp
Entry Wire Line
	3050 4450 3150 4550
Entry Wire Line
	3050 4350 3150 4450
Entry Wire Line
	3050 4250 3150 4350
Entry Wire Line
	3050 4550 3150 4650
Entry Wire Line
	3050 4650 3150 4750
Entry Wire Line
	3050 4750 3150 4850
Entry Wire Line
	3050 4850 3150 4950
Entry Wire Line
	3050 4950 3150 5050
Entry Wire Line
	3050 5050 3150 5150
Entry Wire Line
	3050 5150 3150 5250
Entry Wire Line
	3050 5250 3150 5350
Entry Wire Line
	3050 5350 3150 5450
Entry Wire Line
	3050 5450 3150 5550
Entry Wire Line
	3050 5550 3150 5650
Entry Wire Line
	3050 5650 3150 5750
Entry Wire Line
	3050 5850 3150 5950
Wire Wire Line
	3750 4650 3150 4650
Wire Wire Line
	3750 4750 3150 4750
Wire Wire Line
	3150 4850 3750 4850
Wire Wire Line
	3750 4950 3150 4950
Wire Wire Line
	3150 5050 3750 5050
Wire Wire Line
	3750 5150 3150 5150
Wire Wire Line
	3150 5250 3750 5250
Wire Wire Line
	3750 5350 3150 5350
Wire Wire Line
	3750 5450 3150 5450
Wire Wire Line
	3750 5550 3150 5550
Wire Wire Line
	3750 4350 3150 4350
Wire Wire Line
	3150 4450 3750 4450
Wire Wire Line
	3750 5650 3150 5650
Wire Wire Line
	3750 5750 3150 5750
Wire Wire Line
	3150 5950 4050 5950
Wire Wire Line
	3750 4550 3150 4550
Text Label 3350 5450 0    50   ~ 0
RESET_A
Text Label 3350 5350 0    50   ~ 0
RESET_B
Text Label 3350 5250 0    50   ~ 0
SYNC_A
Text Label 3350 5150 0    50   ~ 0
SYNC_B
Text Label 3350 4850 0    50   ~ 0
TX_A
Text Label 3350 4750 0    50   ~ 0
TX_B
Text Label 3350 5050 0    50   ~ 0
RX_A
Text Label 3350 4950 0    50   ~ 0
RX_B
Text Label 3350 5950 0    50   ~ 0
GND
Text Label 3350 5750 0    50   ~ 0
GND
Text Label 3350 4550 0    50   ~ 0
GND
Text Label 3350 4650 0    50   ~ 0
+15V
Text Label 3350 5650 0    50   ~ 0
+15V
Text Label 3350 5550 0    50   ~ 0
ID_5
Text Label 3350 4350 0    50   ~ 0
PWMO2_6
Text Label 3350 4450 0    50   ~ 0
PWMO1_6
$Comp
L Connector:DB15_Female_MountingHoles J17
U 1 1 5B252417
P 5700 5050
F 0 "J17" H 5700 6000 50  0000 C CNN
F 1 "DB15_Female_MountingHoles" H 5700 5925 50  0000 C CNN
F 2 "Connector_Dsub:DSUB-15_Female_Vertical_P2.77x2.84mm_MountingHoles" H 5700 5050 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/1842458.pdf?_ga=2.174665762.1897837425.1528366125-1651149783.1519657816" H 5700 5050 50  0001 C CNN
	1    5700 5050
	1    0    0    -1  
$EndComp
Entry Wire Line
	4700 4450 4800 4550
Entry Wire Line
	4700 4350 4800 4450
Entry Wire Line
	4700 4250 4800 4350
Entry Wire Line
	4700 4550 4800 4650
Entry Wire Line
	4700 4650 4800 4750
Entry Wire Line
	4700 4750 4800 4850
Entry Wire Line
	4700 4850 4800 4950
Entry Wire Line
	4700 4950 4800 5050
Entry Wire Line
	4700 5050 4800 5150
Entry Wire Line
	4700 5150 4800 5250
Entry Wire Line
	4700 5250 4800 5350
Entry Wire Line
	4700 5350 4800 5450
Entry Wire Line
	4700 5450 4800 5550
Entry Wire Line
	4700 5550 4800 5650
Entry Wire Line
	4700 5650 4800 5750
Entry Wire Line
	4700 5850 4800 5950
Wire Wire Line
	5400 4650 4800 4650
Wire Wire Line
	5400 4750 4800 4750
Wire Wire Line
	4800 4850 5400 4850
Wire Wire Line
	5400 4950 4800 4950
Wire Wire Line
	4800 5050 5400 5050
Wire Wire Line
	5400 5150 4800 5150
Wire Wire Line
	4800 5250 5400 5250
Wire Wire Line
	5400 5350 4800 5350
Wire Wire Line
	5400 5450 4800 5450
Wire Wire Line
	5400 5550 4800 5550
Wire Wire Line
	5400 4350 4800 4350
Wire Wire Line
	4800 4450 5400 4450
Wire Wire Line
	5400 5650 4800 5650
Wire Wire Line
	5400 5750 4800 5750
Wire Wire Line
	4800 5950 5700 5950
Wire Wire Line
	5400 4550 4800 4550
Text Label 5000 5450 0    50   ~ 0
RESET_A
Text Label 5000 5350 0    50   ~ 0
RESET_B
Text Label 5000 5250 0    50   ~ 0
SYNC_A
Text Label 5000 5150 0    50   ~ 0
SYNC_B
Text Label 5000 4850 0    50   ~ 0
TX_A
Text Label 5000 4750 0    50   ~ 0
TX_B
Text Label 5000 5050 0    50   ~ 0
RX_A
Text Label 5000 4950 0    50   ~ 0
RX_B
Text Label 5000 5950 0    50   ~ 0
GND
Text Label 5000 5750 0    50   ~ 0
GND
Text Label 5000 4550 0    50   ~ 0
GND
Text Label 5000 4650 0    50   ~ 0
+15V
Text Label 5000 5650 0    50   ~ 0
+15V
Text Label 5000 5550 0    50   ~ 0
ID_4
Text Label 5000 4350 0    50   ~ 0
PWMO2_7
Text Label 5000 4450 0    50   ~ 0
PWMO1_7
$Comp
L Connector:DB15_Female_MountingHoles J20
U 1 1 5B25244E
P 7300 5050
F 0 "J20" H 7300 6000 50  0000 C CNN
F 1 "DB15_Female_MountingHoles" H 7300 5925 50  0000 C CNN
F 2 "Connector_Dsub:DSUB-15_Female_Vertical_P2.77x2.84mm_MountingHoles" H 7300 5050 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/1842458.pdf?_ga=2.174665762.1897837425.1528366125-1651149783.1519657816" H 7300 5050 50  0001 C CNN
	1    7300 5050
	1    0    0    -1  
$EndComp
Entry Wire Line
	6300 4450 6400 4550
Entry Wire Line
	6300 4350 6400 4450
Entry Wire Line
	6300 4250 6400 4350
Entry Wire Line
	6300 4550 6400 4650
Entry Wire Line
	6300 4650 6400 4750
Entry Wire Line
	6300 4750 6400 4850
Entry Wire Line
	6300 4850 6400 4950
Entry Wire Line
	6300 4950 6400 5050
Entry Wire Line
	6300 5050 6400 5150
Entry Wire Line
	6300 5150 6400 5250
Entry Wire Line
	6300 5250 6400 5350
Entry Wire Line
	6300 5350 6400 5450
Entry Wire Line
	6300 5450 6400 5550
Entry Wire Line
	6300 5550 6400 5650
Entry Wire Line
	6300 5650 6400 5750
Entry Wire Line
	6300 5850 6400 5950
Wire Wire Line
	7000 4650 6400 4650
Wire Wire Line
	7000 4750 6400 4750
Wire Wire Line
	6400 4850 7000 4850
Wire Wire Line
	7000 4950 6400 4950
Wire Wire Line
	6400 5050 7000 5050
Wire Wire Line
	7000 5150 6400 5150
Wire Wire Line
	6400 5250 7000 5250
Wire Wire Line
	7000 5350 6400 5350
Wire Wire Line
	7000 5450 6400 5450
Wire Wire Line
	7000 5550 6400 5550
Wire Wire Line
	7000 4350 6400 4350
Wire Wire Line
	6400 4450 7000 4450
Wire Wire Line
	7000 5650 6400 5650
Wire Wire Line
	7000 5750 6400 5750
Wire Wire Line
	6400 5950 7300 5950
Wire Wire Line
	7000 4550 6400 4550
Text Label 6600 5450 0    50   ~ 0
RESET_A
Text Label 6600 5350 0    50   ~ 0
RESET_B
Text Label 6600 5250 0    50   ~ 0
SYNC_A
Text Label 6600 5150 0    50   ~ 0
SYNC_B
Text Label 6600 4850 0    50   ~ 0
TX_A
Text Label 6600 4750 0    50   ~ 0
TX_B
Text Label 6600 5050 0    50   ~ 0
RX_A
Text Label 6600 4950 0    50   ~ 0
RX_B
Text Label 6600 5950 0    50   ~ 0
GND
Text Label 6600 5750 0    50   ~ 0
GND
Text Label 6600 4550 0    50   ~ 0
GND
Text Label 6600 4650 0    50   ~ 0
+15V
Text Label 6600 5650 0    50   ~ 0
+15V
Text Label 6600 5550 0    50   ~ 0
ID_3
Text Label 6600 4350 0    50   ~ 0
PWMO2_8
Text Label 6600 4450 0    50   ~ 0
PWMO1_8
$Comp
L Connector:DB15_Female_MountingHoles J22
U 1 1 5B252485
P 8850 5050
F 0 "J22" H 8850 6000 50  0000 C CNN
F 1 "DB15_Female_MountingHoles" H 8850 5925 50  0000 C CNN
F 2 "Connector_Dsub:DSUB-15_Female_Vertical_P2.77x2.84mm_MountingHoles" H 8850 5050 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/1842458.pdf?_ga=2.174665762.1897837425.1528366125-1651149783.1519657816" H 8850 5050 50  0001 C CNN
	1    8850 5050
	1    0    0    -1  
$EndComp
Entry Wire Line
	7850 4450 7950 4550
Entry Wire Line
	7850 4350 7950 4450
Entry Wire Line
	7850 4250 7950 4350
Entry Wire Line
	7850 4550 7950 4650
Entry Wire Line
	7850 4650 7950 4750
Entry Wire Line
	7850 4750 7950 4850
Entry Wire Line
	7850 4850 7950 4950
Entry Wire Line
	7850 4950 7950 5050
Entry Wire Line
	7850 5050 7950 5150
Entry Wire Line
	7850 5150 7950 5250
Entry Wire Line
	7850 5250 7950 5350
Entry Wire Line
	7850 5350 7950 5450
Entry Wire Line
	7850 5450 7950 5550
Entry Wire Line
	7850 5550 7950 5650
Entry Wire Line
	7850 5650 7950 5750
Entry Wire Line
	7850 5850 7950 5950
Wire Wire Line
	8550 4650 7950 4650
Wire Wire Line
	8550 4750 7950 4750
Wire Wire Line
	7950 4850 8550 4850
Wire Wire Line
	8550 4950 7950 4950
Wire Wire Line
	7950 5050 8550 5050
Wire Wire Line
	8550 5150 7950 5150
Wire Wire Line
	7950 5250 8550 5250
Wire Wire Line
	8550 5350 7950 5350
Wire Wire Line
	8550 5450 7950 5450
Wire Wire Line
	8550 5550 7950 5550
Wire Wire Line
	8550 4350 7950 4350
Wire Wire Line
	7950 4450 8550 4450
Wire Wire Line
	8550 5650 7950 5650
Wire Wire Line
	8550 5750 7950 5750
Wire Wire Line
	7950 5950 8850 5950
Wire Wire Line
	8550 4550 7950 4550
Text Label 8150 5450 0    50   ~ 0
RESET_A
Text Label 8150 5350 0    50   ~ 0
RESET_B
Text Label 8150 5250 0    50   ~ 0
SYNC_A
Text Label 8150 5150 0    50   ~ 0
SYNC_B
Text Label 8150 4850 0    50   ~ 0
TX_A
Text Label 8150 4750 0    50   ~ 0
TX_B
Text Label 8150 5050 0    50   ~ 0
RX_A
Text Label 8150 4950 0    50   ~ 0
RX_B
Text Label 8150 5950 0    50   ~ 0
GND
Text Label 8150 5750 0    50   ~ 0
GND
Text Label 8150 4550 0    50   ~ 0
GND
Text Label 8150 4650 0    50   ~ 0
+15V
Text Label 8150 5650 0    50   ~ 0
+15V
Text Label 8150 5550 0    50   ~ 0
ID_2
Text Label 8150 4350 0    50   ~ 0
PWMO2_9
Text Label 8150 4450 0    50   ~ 0
PWMO1_9
$Comp
L Connector:DB15_Female_MountingHoles J24
U 1 1 5B2524BC
P 10450 5050
F 0 "J24" H 10450 6000 50  0000 C CNN
F 1 "DB15_Female_MountingHoles" H 10450 5925 50  0000 C CNN
F 2 "Connector_Dsub:DSUB-15_Female_Vertical_P2.77x2.84mm_MountingHoles" H 10450 5050 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/1842458.pdf?_ga=2.174665762.1897837425.1528366125-1651149783.1519657816" H 10450 5050 50  0001 C CNN
	1    10450 5050
	1    0    0    -1  
$EndComp
Entry Wire Line
	9450 4450 9550 4550
Entry Wire Line
	9450 4350 9550 4450
Entry Wire Line
	9450 4250 9550 4350
Entry Wire Line
	9450 4550 9550 4650
Entry Wire Line
	9450 4650 9550 4750
Entry Wire Line
	9450 4750 9550 4850
Entry Wire Line
	9450 4850 9550 4950
Entry Wire Line
	9450 4950 9550 5050
Entry Wire Line
	9450 5050 9550 5150
Entry Wire Line
	9450 5150 9550 5250
Entry Wire Line
	9450 5250 9550 5350
Entry Wire Line
	9450 5350 9550 5450
Entry Wire Line
	9450 5450 9550 5550
Entry Wire Line
	9450 5550 9550 5650
Entry Wire Line
	9450 5650 9550 5750
Entry Wire Line
	9450 5850 9550 5950
Wire Wire Line
	10150 4650 9550 4650
Wire Wire Line
	10150 4750 9550 4750
Wire Wire Line
	9550 4850 10150 4850
Wire Wire Line
	10150 4950 9550 4950
Wire Wire Line
	9550 5050 10150 5050
Wire Wire Line
	10150 5150 9550 5150
Wire Wire Line
	9550 5250 10150 5250
Wire Wire Line
	10150 5350 9550 5350
Wire Wire Line
	10150 5450 9550 5450
Wire Wire Line
	10150 5550 9550 5550
Wire Wire Line
	10150 4350 9550 4350
Wire Wire Line
	9550 4450 10150 4450
Wire Wire Line
	10150 5650 9550 5650
Wire Wire Line
	10150 5750 9550 5750
Wire Wire Line
	9550 5950 10450 5950
Wire Wire Line
	10150 4550 9550 4550
Text Label 9750 5450 0    50   ~ 0
RESET_A
Text Label 9750 5350 0    50   ~ 0
RESET_B
Text Label 9750 5250 0    50   ~ 0
SYNC_A
Text Label 9750 5150 0    50   ~ 0
SYNC_B
Text Label 9750 4850 0    50   ~ 0
TX_A
Text Label 9750 4750 0    50   ~ 0
TX_B
Text Label 9750 5050 0    50   ~ 0
RX_A
Text Label 9750 4950 0    50   ~ 0
RX_B
Text Label 9750 5950 0    50   ~ 0
GND
Text Label 9750 5750 0    50   ~ 0
GND
Text Label 9750 4550 0    50   ~ 0
GND
Text Label 9750 4650 0    50   ~ 0
+15V
Text Label 9750 5650 0    50   ~ 0
+15V
Text Label 9750 5550 0    50   ~ 0
ID_1
Text Label 9750 4350 0    50   ~ 0
PWMO2_10
Text Label 9750 4450 0    50   ~ 0
PWMO1_10
Wire Bus Line
	2650 3750 3050 3750
Connection ~ 2650 3750
Wire Bus Line
	3050 3750 4700 3750
Connection ~ 3050 3750
Wire Bus Line
	4700 3750 6300 3750
Connection ~ 4700 3750
Wire Bus Line
	6300 3750 7850 3750
Connection ~ 6300 3750
Wire Bus Line
	7850 3750 9450 3750
Connection ~ 7850 3750
Wire Bus Line
	3050 6050 4000 6050
Connection ~ 3050 6050
Wire Bus Line
	4700 6050 4950 6050
Connection ~ 4700 6050
Wire Bus Line
	6300 6050 7850 6050
Connection ~ 6300 6050
Wire Bus Line
	7850 6050 9450 6050
Connection ~ 7850 6050
Entry Wire Line
	800  3600 900  3500
Entry Wire Line
	800  3750 900  3650
Entry Wire Line
	2550 3500 2650 3400
Entry Wire Line
	2550 3650 2650 3550
$Comp
L Connector_Generic:Conn_01x03 J5
U 1 1 5B90EB59
P 3750 6350
F 0 "J5" H 3750 6550 50  0000 C CNN
F 1 "Conn_01x03" H 3750 6150 50  0000 C CNN
F 2 "TerminalBlock_Phoenix:TerminalBlock_Phoenix_MKDS-3-3-5.08_1x03_P5.08mm_Horizontal" H 3750 6350 50  0001 C CNN
F 3 "~" H 3750 6350 50  0001 C CNN
	1    3750 6350
	1    0    0    -1  
$EndComp
Entry Wire Line
	3050 6350 3150 6250
Entry Wire Line
	3050 6550 3150 6450
Wire Wire Line
	3550 6250 3150 6250
Wire Wire Line
	3150 6450 3550 6450
Text Label 950  3500 0    50   ~ 0
SYNC_A
Text Label 2250 3500 0    50   ~ 0
SYNC_B
Text Label 950  3650 0    50   ~ 0
RESET_A
Text Label 2250 3650 0    50   ~ 0
RESET_B
Text Label 3150 6250 0    50   ~ 0
PWMO2_1
Text Label 3150 6450 0    50   ~ 0
PWMO1_1
Entry Wire Line
	3050 6850 3150 6750
Entry Wire Line
	3050 7050 3150 6950
Wire Wire Line
	3550 6750 3150 6750
Wire Wire Line
	3150 6950 3550 6950
Text Label 3150 6750 0    50   ~ 0
PWMO2_2
Text Label 3150 6950 0    50   ~ 0
PWMO1_2
$Comp
L Connector_Generic:Conn_01x03 J7
U 1 1 5BA128B3
P 3750 7350
F 0 "J7" H 3750 7550 50  0000 C CNN
F 1 "Conn_01x03" H 3750 7150 50  0000 C CNN
F 2 "TerminalBlock_Phoenix:TerminalBlock_Phoenix_MKDS-3-3-5.08_1x03_P5.08mm_Horizontal" H 3750 7350 50  0001 C CNN
F 3 "~" H 3750 7350 50  0001 C CNN
	1    3750 7350
	1    0    0    -1  
$EndComp
Entry Wire Line
	3050 7350 3150 7250
Entry Wire Line
	3050 7550 3150 7450
Wire Wire Line
	3550 7250 3150 7250
Wire Wire Line
	3150 7450 3550 7450
Text Label 3150 7250 0    50   ~ 0
PWMO2_3
Text Label 3150 7450 0    50   ~ 0
PWMO1_3
$Comp
L Connector_Generic:Conn_01x03 J10
U 1 1 5BA6986A
P 4700 6350
F 0 "J10" H 4700 6550 50  0000 C CNN
F 1 "Conn_01x03" H 4700 6150 50  0000 C CNN
F 2 "TerminalBlock_Phoenix:TerminalBlock_Phoenix_MKDS-3-3-5.08_1x03_P5.08mm_Horizontal" H 4700 6350 50  0001 C CNN
F 3 "~" H 4700 6350 50  0001 C CNN
	1    4700 6350
	1    0    0    -1  
$EndComp
Entry Wire Line
	4000 6350 4100 6250
Entry Wire Line
	4000 6550 4100 6450
Wire Wire Line
	4500 6250 4100 6250
Wire Wire Line
	4100 6450 4500 6450
Text Label 4100 6250 0    50   ~ 0
PWMO2_4
Text Label 4100 6450 0    50   ~ 0
PWMO1_4
$Comp
L Connector_Generic:Conn_01x03 J11
U 1 1 5BA69878
P 4700 6850
F 0 "J11" H 4700 7050 50  0000 C CNN
F 1 "Conn_01x03" H 4700 6650 50  0000 C CNN
F 2 "TerminalBlock_Phoenix:TerminalBlock_Phoenix_MKDS-3-3-5.08_1x03_P5.08mm_Horizontal" H 4700 6850 50  0001 C CNN
F 3 "~" H 4700 6850 50  0001 C CNN
	1    4700 6850
	1    0    0    -1  
$EndComp
Entry Wire Line
	4000 6850 4100 6750
Entry Wire Line
	4000 7050 4100 6950
Wire Wire Line
	4500 6750 4100 6750
Wire Wire Line
	4100 6950 4500 6950
Text Label 4100 6750 0    50   ~ 0
PWMO2_5
Text Label 4100 6950 0    50   ~ 0
PWMO1_5
$Comp
L Connector_Generic:Conn_01x03 J12
U 1 1 5BA69886
P 4700 7350
F 0 "J12" H 4700 7550 50  0000 C CNN
F 1 "Conn_01x03" H 4700 7150 50  0000 C CNN
F 2 "TerminalBlock_Phoenix:TerminalBlock_Phoenix_MKDS-3-3-5.08_1x03_P5.08mm_Horizontal" H 4700 7350 50  0001 C CNN
F 3 "~" H 4700 7350 50  0001 C CNN
	1    4700 7350
	1    0    0    -1  
$EndComp
Entry Wire Line
	4000 7350 4100 7250
Entry Wire Line
	4000 7550 4100 7450
Wire Wire Line
	4500 7250 4100 7250
Wire Wire Line
	4100 7450 4500 7450
Text Label 4100 7250 0    50   ~ 0
PWMO2_6
Text Label 4100 7450 0    50   ~ 0
PWMO1_6
$Comp
L Connector_Generic:Conn_01x03 J13
U 1 1 5BA970C6
P 5650 6350
F 0 "J13" H 5650 6550 50  0000 C CNN
F 1 "Conn_01x03" H 5650 6150 50  0000 C CNN
F 2 "TerminalBlock_Phoenix:TerminalBlock_Phoenix_MKDS-3-3-5.08_1x03_P5.08mm_Horizontal" H 5650 6350 50  0001 C CNN
F 3 "~" H 5650 6350 50  0001 C CNN
	1    5650 6350
	1    0    0    -1  
$EndComp
Entry Wire Line
	4950 6350 5050 6250
Entry Wire Line
	4950 6550 5050 6450
Wire Wire Line
	5450 6250 5050 6250
Wire Wire Line
	5050 6450 5450 6450
Text Label 5050 6250 0    50   ~ 0
PWMO2_7
Text Label 5050 6450 0    50   ~ 0
PWMO1_7
$Comp
L Connector_Generic:Conn_01x03 J14
U 1 1 5BA970D4
P 5650 6850
F 0 "J14" H 5650 7050 50  0000 C CNN
F 1 "Conn_01x03" H 5650 6650 50  0000 C CNN
F 2 "TerminalBlock_Phoenix:TerminalBlock_Phoenix_MKDS-3-3-5.08_1x03_P5.08mm_Horizontal" H 5650 6850 50  0001 C CNN
F 3 "~" H 5650 6850 50  0001 C CNN
	1    5650 6850
	1    0    0    -1  
$EndComp
Entry Wire Line
	4950 6850 5050 6750
Entry Wire Line
	4950 7050 5050 6950
Wire Wire Line
	5450 6750 5050 6750
Wire Wire Line
	5050 6950 5450 6950
Text Label 5050 6750 0    50   ~ 0
PWMO2_8
Text Label 5050 6950 0    50   ~ 0
PWMO1_8
$Comp
L Connector_Generic:Conn_01x03 J15
U 1 1 5BA970E2
P 5650 7350
F 0 "J15" H 5650 7550 50  0000 C CNN
F 1 "Conn_01x03" H 5650 7150 50  0000 C CNN
F 2 "TerminalBlock_Phoenix:TerminalBlock_Phoenix_MKDS-3-3-5.08_1x03_P5.08mm_Horizontal" H 5650 7350 50  0001 C CNN
F 3 "~" H 5650 7350 50  0001 C CNN
	1    5650 7350
	1    0    0    -1  
$EndComp
Entry Wire Line
	4950 7350 5050 7250
Entry Wire Line
	4950 7550 5050 7450
Wire Wire Line
	5450 7250 5050 7250
Wire Wire Line
	5050 7450 5450 7450
Text Label 5050 7250 0    50   ~ 0
PWMO2_9
Text Label 5050 7450 0    50   ~ 0
PWMO1_9
$Comp
L Connector_Generic:Conn_01x03 J18
U 1 1 5BAC6B5E
P 6600 6350
F 0 "J18" H 6600 6550 50  0000 C CNN
F 1 "Conn_01x03" H 6600 6150 50  0000 C CNN
F 2 "TerminalBlock_Phoenix:TerminalBlock_Phoenix_MKDS-3-3-5.08_1x03_P5.08mm_Horizontal" H 6600 6350 50  0001 C CNN
F 3 "~" H 6600 6350 50  0001 C CNN
	1    6600 6350
	1    0    0    -1  
$EndComp
Entry Wire Line
	5900 6350 6000 6250
Entry Wire Line
	5900 6550 6000 6450
Wire Wire Line
	6400 6250 6000 6250
Wire Wire Line
	6000 6450 6400 6450
Text Label 6000 6250 0    50   ~ 0
PWMO2_10
Text Label 6000 6450 0    50   ~ 0
PWMO1_10
Connection ~ 5900 6050
Wire Bus Line
	5900 6050 6300 6050
$Comp
L Device:R R1
U 1 1 5BBFCEC2
P 1400 3350
F 0 "R1" V 1480 3350 50  0000 C CNN
F 1 "60E" V 1400 3350 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1330 3350 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2158849.pdf?_ga=2.36114656.13702668.1544197496-523844734.1542029203&_gac=1.195831192.1543322461.EAIaIQobChMIof7Tlcz03gIVkuJ3Ch1UVwqwEAAYASAAEgK8PvD_BwE" H 1400 3350 50  0001 C CNN
F 4 "1100159" V 1400 3350 50  0001 C CNN "Farnell"
F 5 "WCR1206-62RFI" V 1400 3350 50  0001 C CNN "Manufacturer nr"
	1    1400 3350
	0    1    1    0   
$EndComp
Entry Wire Line
	800  3450 900  3350
Entry Wire Line
	2550 3350 2650 3250
Text Label 950  3350 0    50   ~ 0
RX_A
Text Label 2300 3350 0    50   ~ 0
RX_B
Connection ~ 4950 6050
Wire Bus Line
	4950 6050 5900 6050
Connection ~ 4000 6050
Wire Bus Line
	4000 6050 4700 6050
$Comp
L Device:CP C1
U 1 1 5BE52A1F
P 4000 1100
F 0 "C1" H 4025 1200 50  0000 L CNN
F 1 "470u" H 4025 1000 50  0000 L CNN
F 2 "Capacitor_THT:CP_Radial_D10.0mm_P7.50mm" H 4038 950 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2280829.pdf?_ga=2.252145225.1588585681.1526323724-1626853455.1515011571" H 4000 1100 50  0001 C CNN
F 4 "2750201" H 4000 1100 50  0001 C CNN "Farnell"
F 5 "B41858C7477M000" H 4000 1100 50  0001 C CNN "Manufacturer nr"
	1    4000 1100
	1    0    0    -1  
$EndComp
$Comp
L Device:CP C2
U 1 1 5BE52D58
P 4250 1100
F 0 "C2" H 4275 1200 50  0000 L CNN
F 1 "470u" H 4275 1000 50  0000 L CNN
F 2 "Capacitor_THT:CP_Radial_D10.0mm_P7.50mm" H 4288 950 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2280829.pdf?_ga=2.252145225.1588585681.1526323724-1626853455.1515011571" H 4250 1100 50  0001 C CNN
F 4 "2750201" H 4250 1100 50  0001 C CNN "Farnell"
F 5 "B41858C7477M000" H 4250 1100 50  0001 C CNN "Manufacturer nr"
	1    4250 1100
	1    0    0    -1  
$EndComp
$Comp
L power:+15V #PWR0107
U 1 1 5BE52DDE
P 4000 850
F 0 "#PWR0107" H 4000 700 50  0001 C CNN
F 1 "+15V" H 4000 990 50  0000 C CNN
F 2 "" H 4000 850 50  0001 C CNN
F 3 "" H 4000 850 50  0001 C CNN
	1    4000 850 
	1    0    0    -1  
$EndComp
$Comp
L power:GND #PWR0108
U 1 1 5BE52F43
P 4000 1350
F 0 "#PWR0108" H 4000 1100 50  0001 C CNN
F 1 "GND" H 4000 1200 50  0000 C CNN
F 2 "" H 4000 1350 50  0001 C CNN
F 3 "" H 4000 1350 50  0001 C CNN
	1    4000 1350
	1    0    0    -1  
$EndComp
Wire Wire Line
	4000 1350 4000 1300
Wire Wire Line
	4000 1300 4250 1300
Wire Wire Line
	4250 1300 4250 1250
Connection ~ 4000 1300
Wire Wire Line
	4000 1300 4000 1250
Wire Wire Line
	4000 850  4000 900 
Wire Wire Line
	4000 900  4250 900 
Wire Wire Line
	4250 900  4250 950 
Connection ~ 4000 900 
Wire Wire Line
	4000 900  4000 950 
$Sheet
S 4750 750  750  750 
U 5BD68C20
F0 "BackplanePCB_slave" 50
F1 "BackplanePCB_slave.sch" 50
$EndSheet
Entry Wire Line
	800  4100 900  4200
Text GLabel 1200 4200 2    50   Output ~ 0
RESET_A
Wire Wire Line
	1200 4200 900  4200
Entry Wire Line
	800  4200 900  4300
Entry Wire Line
	800  4350 900  4450
Entry Wire Line
	800  4450 900  4550
Entry Wire Line
	800  4600 900  4700
Entry Wire Line
	800  4700 900  4800
Entry Wire Line
	800  4950 900  5050
Text GLabel 1200 4300 2    50   Output ~ 0
RESET_B
Wire Wire Line
	1200 4300 900  4300
Text GLabel 1200 4450 2    50   Output ~ 0
SYNC_A
Wire Wire Line
	1200 4450 900  4450
Text GLabel 1200 4550 2    50   Output ~ 0
SYNC_B
Wire Wire Line
	1200 4550 900  4550
Text GLabel 1200 4700 2    50   Output ~ 0
RX_A
Wire Wire Line
	1200 4700 900  4700
Text GLabel 1200 4800 2    50   Output ~ 0
RX_B
Wire Wire Line
	1200 4800 900  4800
Entry Wire Line
	800  4850 900  4950
Text GLabel 1200 4950 2    50   Input ~ 0
TX_A
Wire Wire Line
	1200 4950 900  4950
Text GLabel 1200 5050 2    50   Input ~ 0
TX_B
Wire Wire Line
	1200 5050 900  5050
Entry Wire Line
	800  5100 900  5200
Text GLabel 1200 5200 2    50   Input ~ 0
ID_1
Wire Wire Line
	1200 5200 900  5200
Entry Wire Line
	800  5200 900  5300
Text GLabel 1200 5300 2    50   Input ~ 0
ID_2
Wire Wire Line
	1200 5300 900  5300
Entry Wire Line
	800  5300 900  5400
Text GLabel 1200 5400 2    50   Input ~ 0
ID_3
Wire Wire Line
	1200 5400 900  5400
Entry Wire Line
	800  5400 900  5500
Text GLabel 1200 5500 2    50   Input ~ 0
ID_4
Wire Wire Line
	1200 5500 900  5500
Entry Wire Line
	800  5500 900  5600
Text GLabel 1200 5600 2    50   Input ~ 0
ID_5
Wire Wire Line
	1200 5600 900  5600
Wire Bus Line
	800  7450 3050 7450
Connection ~ 3050 7450
Wire Bus Line
	3050 7450 3050 7550
Entry Wire Line
	800  5700 900  5800
Text GLabel 1200 5800 2    50   Input ~ 0
ID_6
Wire Wire Line
	1200 5800 900  5800
Entry Wire Line
	800  5800 900  5900
Text GLabel 1200 5900 2    50   Input ~ 0
ID_7
Wire Wire Line
	1200 5900 900  5900
Entry Wire Line
	800  5900 900  6000
Text GLabel 1200 6000 2    50   Input ~ 0
ID_8
Wire Wire Line
	1200 6000 900  6000
Entry Wire Line
	800  6000 900  6100
Text GLabel 1200 6100 2    50   Input ~ 0
ID_9
Wire Wire Line
	1200 6100 900  6100
Entry Wire Line
	800  6100 900  6200
Text GLabel 1200 6200 2    50   Input ~ 0
ID_10
Wire Wire Line
	1200 6200 900  6200
Text Notes 4850 1200 0    50   ~ 0
BackplaneSlave\nSchematic
$Comp
L Connector_Generic:Conn_01x03 J6
U 1 1 5B9E79FF
P 3750 6850
F 0 "J6" H 3750 7050 50  0000 C CNN
F 1 "Conn_01x03" H 3750 6650 50  0000 C CNN
F 2 "TerminalBlock_Phoenix:TerminalBlock_Phoenix_MKDS-3-3-5.08_1x03_P5.08mm_Horizontal" H 3750 6850 50  0001 C CNN
F 3 "~" H 3750 6850 50  0001 C CNN
	1    3750 6850
	1    0    0    -1  
$EndComp
Entry Wire Line
	3050 6450 3150 6350
Wire Wire Line
	3150 6350 3550 6350
Text Label 3150 6350 0    50   ~ 0
GND
Entry Wire Line
	3050 6950 3150 6850
Entry Wire Line
	3050 7450 3150 7350
Entry Wire Line
	4000 6450 4100 6350
Entry Wire Line
	4000 6950 4100 6850
Entry Wire Line
	4000 7450 4100 7350
Entry Wire Line
	4950 6450 5050 6350
Entry Wire Line
	4950 6950 5050 6850
Entry Wire Line
	4950 7450 5050 7350
Entry Wire Line
	5900 6450 6000 6350
Wire Wire Line
	3150 6850 3550 6850
Wire Wire Line
	3550 7350 3150 7350
Wire Wire Line
	4100 6350 4500 6350
Wire Wire Line
	4500 6850 4100 6850
Wire Wire Line
	4100 7350 4500 7350
Wire Wire Line
	5050 6350 5450 6350
Wire Wire Line
	5450 6850 5050 6850
Wire Wire Line
	5050 7350 5450 7350
Wire Wire Line
	6000 6350 6400 6350
Text Label 3150 6850 0    50   ~ 0
GND
Text Label 3150 7350 0    50   ~ 0
GND
Text Label 4100 6350 0    50   ~ 0
GND
Text Label 4100 6850 0    50   ~ 0
GND
Text Label 4100 7350 0    50   ~ 0
GND
Text Label 5050 6350 0    50   ~ 0
GND
Text Label 5050 6850 0    50   ~ 0
GND
Text Label 5050 7350 0    50   ~ 0
GND
Text Label 6000 6350 0    50   ~ 0
GND
$Comp
L Device:C C20
U 1 1 5BDD506D
P 2100 4250
F 0 "C20" H 2215 4296 50  0000 L CNN
F 1 "100n" H 2215 4205 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 2138 4100 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/1901289.pdf?_ga=2.262491141.804204398.1540810031-466615820.1533907205&_gac=1.154756810.1540810031.CjwKCAjw39reBRBJEiwAO1m0OUdqKKSX6YgNsFqfn4kAD7vSQS5JfvsNobbwGFQV72towt0oHUutJhoC7b8QAvD_BwE" H 2100 4250 50  0001 C CNN
F 4 "2896646" H 2100 4250 50  0001 C CNN "Farnell"
F 5 "VJ1206Y104KXXPW1BC" H 2100 4250 50  0001 C CNN "Manufacturer nr"
	1    2100 4250
	1    0    0    -1  
$EndComp
Wire Wire Line
	900  3350 1250 3350
Wire Wire Line
	900  3500 1250 3500
Wire Wire Line
	900  3650 1250 3650
Wire Wire Line
	2200 3350 2550 3350
Wire Wire Line
	2200 3500 2550 3500
Wire Wire Line
	2200 3650 2550 3650
Entry Wire Line
	2550 4400 2650 4300
Entry Wire Line
	2550 4800 2650 4700
Entry Wire Line
	2550 5200 2650 5100
Wire Wire Line
	2550 4400 2100 4400
Wire Wire Line
	2100 4800 2550 4800
Wire Wire Line
	2550 5200 2100 5200
Text Label 2300 4400 0    50   ~ 0
GND
Text Label 2300 4800 0    50   ~ 0
GND
Text Label 2300 5200 0    50   ~ 0
GND
Entry Wire Line
	800  3900 900  3800
Entry Wire Line
	2550 3800 2650 3700
Wire Wire Line
	900  3800 1250 3800
Wire Wire Line
	1550 3800 1850 3800
Wire Wire Line
	2200 3800 2550 3800
Text Label 950  3800 0    50   ~ 0
TX_A
Text Label 2250 3800 0    50   ~ 0
TX_B
Wire Wire Line
	1550 3350 1700 3350
Wire Wire Line
	1550 3500 1750 3500
Wire Bus Line
	2650 6050 3050 6050
Entry Wire Line
	2550 5500 2650 5400
Text Label 2300 5500 0    50   ~ 0
GND
Wire Wire Line
	1550 3650 1800 3650
Wire Wire Line
	2100 4100 1850 4100
Wire Wire Line
	1850 4100 1850 3800
Connection ~ 1850 3800
Wire Wire Line
	1850 3800 1900 3800
Wire Wire Line
	2100 4500 1800 4500
Wire Wire Line
	1800 4500 1800 3650
Connection ~ 1800 3650
Wire Wire Line
	1800 3650 1900 3650
Wire Wire Line
	2100 4900 1750 4900
Wire Wire Line
	1750 4900 1750 3500
Connection ~ 1750 3500
Wire Wire Line
	1750 3500 1900 3500
Wire Wire Line
	1900 5200 1700 5200
Wire Wire Line
	1700 5200 1700 3350
Connection ~ 1700 3350
Wire Wire Line
	1700 3350 1900 3350
Text Label 1050 6600 0    50   ~ 0
RESET_A
Text Label 1050 6750 0    50   ~ 0
RESET_B
$Comp
L Device:R R5
U 1 1 5C3DDAD5
P 1900 6600
F 0 "R5" V 1980 6600 50  0000 C CNN
F 1 "2k" V 1900 6600 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1830 6600 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2710383.pdf?_ga=2.193661672.418749504.1544431297-523844734.1542029203&_gac=1.14046533.1544530560.Cj0KCQiA3b3gBRDAARIsAL6D-N9ZEUEKl0enALbmpOfbmCdvFwg8FdG0SpWlwqThrqXwAE0Ba3vBvEAaAgz2EALw_wcB" H 1900 6600 50  0001 C CNN
F 4 "1100199" V 1900 6600 50  0001 C CNN "Farnell"
F 5 "WCR1206-2KFI" V 1900 6600 50  0001 C CNN "Manufacturer nr"
	1    1900 6600
	0    1    1    0   
$EndComp
Entry Wire Line
	800  6700 900  6600
Entry Wire Line
	2950 6600 3050 6500
Entry Wire Line
	2950 6750 3050 6650
Entry Wire Line
	800  6850 900  6750
Wire Wire Line
	900  6750 1750 6750
Wire Wire Line
	900  6600 1750 6600
Wire Wire Line
	2050 6600 2950 6600
Wire Wire Line
	2950 6750 2050 6750
Entry Wire Line
	800  1450 900  1350
$Comp
L power:+5V #PWR01
U 1 1 5C5387E4
P 1700 1250
F 0 "#PWR01" H 1700 1100 50  0001 C CNN
F 1 "+5V" H 1715 1423 50  0000 C CNN
F 2 "" H 1700 1250 50  0001 C CNN
F 3 "" H 1700 1250 50  0001 C CNN
	1    1700 1250
	1    0    0    -1  
$EndComp
Wire Wire Line
	900  1350 1700 1350
Wire Wire Line
	1700 1350 1700 1250
Text Label 2350 6750 0    50   ~ 0
GND
Text Label 2350 6600 0    50   ~ 0
+5V
$Comp
L Device:C C21
U 1 1 5C12C1EF
P 2100 4650
F 0 "C21" H 2215 4696 50  0000 L CNN
F 1 "100n" H 2215 4605 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 2138 4500 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/1901289.pdf?_ga=2.262491141.804204398.1540810031-466615820.1533907205&_gac=1.154756810.1540810031.CjwKCAjw39reBRBJEiwAO1m0OUdqKKSX6YgNsFqfn4kAD7vSQS5JfvsNobbwGFQV72towt0oHUutJhoC7b8QAvD_BwE" H 2100 4650 50  0001 C CNN
F 4 "2896646" H 2100 4650 50  0001 C CNN "Farnell"
F 5 "VJ1206Y104KXXPW1BC" H 2100 4650 50  0001 C CNN "Manufacturer nr"
	1    2100 4650
	1    0    0    -1  
$EndComp
$Comp
L Device:C C22
U 1 1 5C12C253
P 2100 5050
F 0 "C22" H 2215 5096 50  0000 L CNN
F 1 "100n" H 2215 5005 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 2138 4900 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/1901289.pdf?_ga=2.262491141.804204398.1540810031-466615820.1533907205&_gac=1.154756810.1540810031.CjwKCAjw39reBRBJEiwAO1m0OUdqKKSX6YgNsFqfn4kAD7vSQS5JfvsNobbwGFQV72towt0oHUutJhoC7b8QAvD_BwE" H 2100 5050 50  0001 C CNN
F 4 "2896646" H 2100 5050 50  0001 C CNN "Farnell"
F 5 "VJ1206Y104KXXPW1BC" H 2100 5050 50  0001 C CNN "Manufacturer nr"
	1    2100 5050
	1    0    0    -1  
$EndComp
$Comp
L Device:R R2
U 1 1 5C12C6DD
P 1400 3500
F 0 "R2" V 1480 3500 50  0000 C CNN
F 1 "60E" V 1400 3500 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1330 3500 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2158849.pdf?_ga=2.36114656.13702668.1544197496-523844734.1542029203&_gac=1.195831192.1543322461.EAIaIQobChMIof7Tlcz03gIVkuJ3Ch1UVwqwEAAYASAAEgK8PvD_BwE" H 1400 3500 50  0001 C CNN
F 4 "1100159" V 1400 3500 50  0001 C CNN "Farnell"
F 5 "WCR1206-62RFI" V 1400 3500 50  0001 C CNN "Manufacturer nr"
	1    1400 3500
	0    1    1    0   
$EndComp
$Comp
L Device:R R3
U 1 1 5C12C737
P 1400 3650
F 0 "R3" V 1480 3650 50  0000 C CNN
F 1 "60E" V 1400 3650 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1330 3650 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2158849.pdf?_ga=2.36114656.13702668.1544197496-523844734.1542029203&_gac=1.195831192.1543322461.EAIaIQobChMIof7Tlcz03gIVkuJ3Ch1UVwqwEAAYASAAEgK8PvD_BwE" H 1400 3650 50  0001 C CNN
F 4 "1100159" V 1400 3650 50  0001 C CNN "Farnell"
F 5 "WCR1206-62RFI" V 1400 3650 50  0001 C CNN "Manufacturer nr"
	1    1400 3650
	0    1    1    0   
$EndComp
$Comp
L Device:R R4
U 1 1 5C12C793
P 1400 3800
F 0 "R4" V 1480 3800 50  0000 C CNN
F 1 "60E" V 1400 3800 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1330 3800 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2158849.pdf?_ga=2.36114656.13702668.1544197496-523844734.1542029203&_gac=1.195831192.1543322461.EAIaIQobChMIof7Tlcz03gIVkuJ3Ch1UVwqwEAAYASAAEgK8PvD_BwE" H 1400 3800 50  0001 C CNN
F 4 "1100159" V 1400 3800 50  0001 C CNN "Farnell"
F 5 "WCR1206-62RFI" V 1400 3800 50  0001 C CNN "Manufacturer nr"
	1    1400 3800
	0    1    1    0   
$EndComp
$Comp
L Device:R R28
U 1 1 5C12C87F
P 2050 3800
F 0 "R28" V 2130 3800 50  0000 C CNN
F 1 "60E" V 2050 3800 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1980 3800 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2158849.pdf?_ga=2.36114656.13702668.1544197496-523844734.1542029203&_gac=1.195831192.1543322461.EAIaIQobChMIof7Tlcz03gIVkuJ3Ch1UVwqwEAAYASAAEgK8PvD_BwE" H 2050 3800 50  0001 C CNN
F 4 "1100159" V 2050 3800 50  0001 C CNN "Farnell"
F 5 "WCR1206-62RFI" V 2050 3800 50  0001 C CNN "Manufacturer nr"
	1    2050 3800
	0    1    1    0   
$EndComp
$Comp
L Device:R R27
U 1 1 5C12C8E5
P 2050 3650
F 0 "R27" V 2130 3650 50  0000 C CNN
F 1 "60E" V 2050 3650 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1980 3650 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2158849.pdf?_ga=2.36114656.13702668.1544197496-523844734.1542029203&_gac=1.195831192.1543322461.EAIaIQobChMIof7Tlcz03gIVkuJ3Ch1UVwqwEAAYASAAEgK8PvD_BwE" H 2050 3650 50  0001 C CNN
F 4 "1100159" V 2050 3650 50  0001 C CNN "Farnell"
F 5 "WCR1206-62RFI" V 2050 3650 50  0001 C CNN "Manufacturer nr"
	1    2050 3650
	0    1    1    0   
$EndComp
$Comp
L Device:R R21
U 1 1 5C12C947
P 2050 3500
F 0 "R21" V 2130 3500 50  0000 C CNN
F 1 "60E" V 2050 3500 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1980 3500 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2158849.pdf?_ga=2.36114656.13702668.1544197496-523844734.1542029203&_gac=1.195831192.1543322461.EAIaIQobChMIof7Tlcz03gIVkuJ3Ch1UVwqwEAAYASAAEgK8PvD_BwE" H 2050 3500 50  0001 C CNN
F 4 "1100159" V 2050 3500 50  0001 C CNN "Farnell"
F 5 "WCR1206-62RFI" V 2050 3500 50  0001 C CNN "Manufacturer nr"
	1    2050 3500
	0    1    1    0   
$EndComp
$Comp
L Device:R R11
U 1 1 5C12C9AB
P 2050 3350
F 0 "R11" V 2130 3350 50  0000 C CNN
F 1 "60E" V 2050 3350 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1980 3350 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2158849.pdf?_ga=2.36114656.13702668.1544197496-523844734.1542029203&_gac=1.195831192.1543322461.EAIaIQobChMIof7Tlcz03gIVkuJ3Ch1UVwqwEAAYASAAEgK8PvD_BwE" H 2050 3350 50  0001 C CNN
F 4 "1100159" V 2050 3350 50  0001 C CNN "Farnell"
F 5 "WCR1206-62RFI" V 2050 3350 50  0001 C CNN "Manufacturer nr"
	1    2050 3350
	0    1    1    0   
$EndComp
$Comp
L Device:R R6
U 1 1 5C12D156
P 1900 6750
F 0 "R6" V 1980 6750 50  0000 C CNN
F 1 "2k" V 1900 6750 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1830 6750 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/2710383.pdf?_ga=2.193661672.418749504.1544431297-523844734.1542029203&_gac=1.14046533.1544530560.Cj0KCQiA3b3gBRDAARIsAL6D-N9ZEUEKl0enALbmpOfbmCdvFwg8FdG0SpWlwqThrqXwAE0Ba3vBvEAaAgz2EALw_wcB" H 1900 6750 50  0001 C CNN
F 4 "1100199" V 1900 6750 50  0001 C CNN "Farnell"
F 5 "WCR1206-2KFI" V 1900 6750 50  0001 C CNN "Manufacturer nr"
	1    1900 6750
	0    1    1    0   
$EndComp
$Comp
L Connector_Generic:Conn_01x09 J26
U 1 1 5C248EAF
P 1950 6050
F 0 "J26" V 2050 6250 50  0000 C CNN
F 1 "Conn_01x09" V 2050 5850 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x09_P2.54mm_Vertical" H 1950 6050 50  0001 C CNN
F 3 "~" H 1950 6050 50  0001 C CNN
	1    1950 6050
	-1   0    0    -1  
$EndComp
Connection ~ 2650 6050
Entry Wire Line
	2550 5750 2650 5650
Entry Wire Line
	2550 5950 2650 5850
Entry Wire Line
	2550 5850 2650 5750
Entry Wire Line
	2550 6050 2650 5950
Entry Wire Line
	2550 6150 2650 6050
Entry Wire Line
	2550 6250 2650 6150
Entry Wire Line
	2550 6350 2650 6250
Entry Wire Line
	2550 6450 2650 6350
Wire Wire Line
	2550 5750 2150 5750
Wire Wire Line
	2150 5850 2550 5850
Wire Wire Line
	2550 5950 2150 5950
Wire Wire Line
	2150 6050 2550 6050
Wire Wire Line
	2550 6150 2150 6150
Wire Wire Line
	2150 6250 2550 6250
Wire Wire Line
	2550 6350 2150 6350
Text Label 2200 6250 0    50   ~ 0
TX_A
Text Label 2200 6050 0    50   ~ 0
RX_A
Text Label 2200 5850 0    50   ~ 0
SYNC_A
Text Label 2200 5650 0    50   ~ 0
RESET_A
Text Label 2200 6350 0    50   ~ 0
TX_B
Text Label 2200 6150 0    50   ~ 0
RX_B
Text Label 2200 5950 0    50   ~ 0
SYNC_B
Text Label 2200 5750 0    50   ~ 0
RESET_B
$Comp
L Device:C C23
U 1 1 5C12C2C1
P 1900 5350
F 0 "C23" H 2015 5396 50  0000 L CNN
F 1 "100n" H 2015 5305 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 1938 5200 50  0001 C CNN
F 3 "http://www.farnell.com/datasheets/1901289.pdf?_ga=2.262491141.804204398.1540810031-466615820.1533907205&_gac=1.154756810.1540810031.CjwKCAjw39reBRBJEiwAO1m0OUdqKKSX6YgNsFqfn4kAD7vSQS5JfvsNobbwGFQV72towt0oHUutJhoC7b8QAvD_BwE" H 1900 5350 50  0001 C CNN
F 4 "2896646" H 1900 5350 50  0001 C CNN "Farnell"
F 5 "VJ1206Y104KXXPW1BC" H 1900 5350 50  0001 C CNN "Manufacturer nr"
	1    1900 5350
	1    0    0    -1  
$EndComp
Wire Wire Line
	1900 5500 2550 5500
Wire Wire Line
	2550 6450 2150 6450
Entry Wire Line
	2550 5650 2650 5550
Wire Wire Line
	2550 5650 2150 5650
Text Label 2200 6450 0    50   ~ 0
GND
Wire Bus Line
	5900 6050 5900 6550
Wire Bus Line
	2650 6050 2650 6450
Wire Bus Line
	4000 6050 4000 7550
Wire Bus Line
	4950 6050 4950 7550
Wire Bus Line
	2650 3750 2650 6050
Wire Bus Line
	2650 1750 2650 3750
Wire Bus Line
	3050 6050 3050 7450
Wire Bus Line
	3050 1950 3050 3750
Wire Bus Line
	4700 1950 4700 3750
Wire Bus Line
	6300 1950 6300 3750
Wire Bus Line
	7850 1950 7850 3750
Wire Bus Line
	9450 1950 9450 3750
Wire Bus Line
	4700 4250 4700 6050
Wire Bus Line
	6300 4250 6300 6050
Wire Bus Line
	7850 4250 7850 6050
Wire Bus Line
	9450 4250 9450 6050
Wire Bus Line
	3050 4250 3050 6050
Wire Bus Line
	800  1150 800  7450
$EndSCHEMATC
