#!/bin/python

import serial
import csv
import sys
import io
import time
import struct

ser = serial.Serial('COM5', 250000, timeout=0, parity=serial.PARITY_NONE)

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
direction = ''

if(pwm < 400):
    direction = "CW"
elif(pwm > 399):
    direction = "CCW"

ser.write(b'\xAA')
ser.write(struct.pack('>B', (pwm>> 8)))
ser.write(struct.pack('>B', (pwm & 0x00FF)))
ser.write(b'\n')
ser.write(b'\r')

print( struct.pack('>B', (pwm>> 8))  )
print( struct.pack('>B', (pwm & 0x00FF))  )

#print((pwm>> 8).to_bytes(1, byteorder='big'))
#print((pwm & 0x00FF).to_bytes(1, byteorder='big'))

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
            pwm = pwm + output; #When the error is negative (measured BEMF number is higher then setpoint BEMF(300)) the PWM dutycycle needs to be increased hence adding the negative number
        
        elif(output > 0):
            pwm = pwm - output;
            
        if (direction == "CCW" and pwm > 780):
            pwm = 780
        elif(direction == "CCW" and pwm < 400):
            pwm = 400
        elif(direction == "CW" and pwm < 20):
            pwm = 20
        elif(direction == "CW" and pwm > 399):
            pwm = 399
                
        print('PWM: ' + str(pwm))
        
        ser.write(b'\xAA')
        ser.write(struct.pack('>B', (pwm>> 8)))
        ser.write(struct.pack('>B', (pwm & 0x00FF)))
        ser.write(b'\n')
        ser.write(b'\r')
        
        out.writerow([str(sample), str(data), str(kp), str(plant), str(error), str(output), str(pwm)]) 
        
        sample = sample + 1
        
            
    if (sample > 499):
        run = False
        
    
        

f.close()    
ser.close()
exit()
