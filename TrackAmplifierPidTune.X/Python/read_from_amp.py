#!/bin/python

import serial
import csv
import sys
import io
import time

ser = serial.Serial('COM5', 250000, timeout=0, parity=serial.PARITY_NONE)
sio = io.TextIOWrapper(io.BufferedRWPair(ser, ser))

try:
    f = open('dump.csv', 'w')
    
    
except IOError:
    e = "Can't open output file for writing: " + '../dump.csv'
    print(__file__, ":", e, sys.stderr)
    f = sys.stdout
    exit()
    
# Create a new csv writer object to use as the output formatter
out = csv.writer(f, lineterminator='\n', delimiter=',', quotechar='\"', quoting=csv.QUOTE_MINIMAL)

# Output a set of rows for a header providing general information
out.writerow(['sample', 'BEMF', 'kp', 'plant', 'error', 'output_reg', 'pwm_setpoint'])

sample = 0
run = True

setpoint = 300
error = 0
output = 200
kp = 2
pwm = 150
plant = 15

ser.write(b'\xAA')
ser.write((pwm>> 8).to_bytes(1, byteorder='big'))
ser.write((pwm & 0x00FF).to_bytes(1, byteorder='big'))
ser.write(b'\n')
ser.write(b'\r')

time.sleep(2)

while run:

     
    s = ser.readline()
            
    if (len(s) > 2):
        
        print('sample:', str(sample))
        
        data = s[0] + (s[1] << 8)
        print(data)
                
        ######################################################
        
        error = setpoint - data;
    
        output = int((kp * error * plant) / 100);
    
        if(output < 0):
            pwm = pwm - output;
        
        else:
            pwm = pwm + output;
            
        if (pwm > 750):
            pwm = 750
        if (pwm < 50):
            pwm = 50
        
        print('PWM: ' + str(pwm))            
        
        ser.write(b'\xAA')
        ser.write((pwm>> 8).to_bytes(1, byteorder='big'))
        ser.write((pwm & 0x00FF).to_bytes(1, byteorder='big'))
        ser.write(b'\n')
        ser.write(b'\r')
        
        out.writerow([str(sample), str(data), str(kp), str(plant), str(error), str(output), str(pwm)]) 
        
        sample = sample + 1
        
            
    if (sample > 499):
        run = False
        
    
        

f.close()    
ser.close()
exit()
