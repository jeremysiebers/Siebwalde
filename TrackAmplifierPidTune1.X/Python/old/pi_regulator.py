#!/bin/python

import serial
import csv
import sys
import io
import time
import struct

ser = serial.Serial('COM5', 250000, parity=serial.PARITY_NONE, timeout=1)

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
out.writerow(['sample', 'BEMF', 'kp', 'plant', 'error', 'output_reg', 'pwm_setpoint', 'BEMF_setpoint', 'output_sat'])

sample = 0
run = True


sat = 0


setpoint = 512
error = 0
output = 0
kp = 3
ki = 2
dt = 
ki2 = ki * dt
pwm = 399
pwm_prev = 399
plant = 15
output_sat = 2

DRIVE_SETPOINT= 400
TOTAL_SAMPLES = 1000

OUTPUT_SAT_P = output_sat * 1
OUTPUT_SAT_M = output_sat * -1
PWM_MAX = 600
PWM_MIN = 200
direction = ''

if(DRIVE_SETPOINT < 512):
    direction = "CW"
elif(DRIVE_SETPOINT > 512):
    direction = "CCW"

ser.write(b'\xAA')
ser.write(struct.pack('>B', (pwm>> 8)))
ser.write(struct.pack('>B', (pwm & 0x00FF)))
ser.write(b'\n')
ser.write(b'\r')

#print( struct.pack('>B', (pwm>> 8))  )
#print( struct.pack('>B', (pwm & 0x00FF))  )

#print((pwm>> 8).to_bytes(1, byteorder='big'))
#print((pwm & 0x00FF).to_bytes(1, byteorder='big'))

time.sleep(2)

s = []

ser.flush()

while run:

    for c in ser.read():
        s.append(c)
            
        if (c == 170):
            if(len(s) > 2 and s[4] == 170):
                
                print('---------------------------------------------------------------------------------')
                print('sample:' + str(sample))
                
                input_data = (s[0] + (s[1] << 4) + (s[2] << 8) + (s[3] << 12)) #+ 25 # subtract/add the offset from the analog filter
                print('Data:'+ str(input_data))
                       
                ######################################################
                print('setpoint:'+ str(setpoint))
                
                error = setpoint - input_data;
                
                print('error:'+ str(error))
                
                output = int((kp * error * plant) / 100);
                
                print('output_calc:'+ str(output))
                
                if(output > OUTPUT_SAT_P):
                    output = OUTPUT_SAT_P
                elif(output < OUTPUT_SAT_M):
                    output = OUTPUT_SAT_M
                   
                print('output_sat:'+ str(output))
                
                if(output < 0):
                    pwm = pwm_prev - abs(output); #When the error is negative (measured BEMF number is higher then setpoint BEMF(300)) the PWM dutycycle needs to be increased hence adding the negative number
                elif(output > 0):
                    pwm = pwm_prev + output;
                
                   
                if (direction == "CCW" and pwm > PWM_MAX):
                    pwm = PWM_MAX
                elif(direction == "CCW" and pwm < 400):
                    pwm = 400
                elif(direction == "CW" and pwm < PWM_MIN):
                    pwm = PWM_MIN
                elif(direction == "CW" and pwm > 399):
                    pwm = 399
                       
                print('PWM: ' + str(pwm))
                
                
                if(pwm_prev != pwm):
                    ser.write(b'\xAA')
                    ser.write(struct.pack('>B', (pwm>> 8)))
                    ser.write(struct.pack('>B', (pwm & 0x00FF)))
                    ser.write(b'\n')
                    ser.write(b'\r')
                    pwm_prev = pwm
                
                out.writerow([str(sample), str(input_data), str(kp), str(plant), str(error), str(output), str(pwm), str(setpoint), str(output_sat)]) 
                
                sample = sample + 1
                
                if(sample > 5):
                    setpoint = DRIVE_SETPOINT
                
                if (sample > TOTAL_SAMPLES - 100):
                    setpoint = 512
                s = []
                print(' ')
                
            else:
                print('Data corrupt......')
                s = []
                
        if (sample > TOTAL_SAMPLES):
            run = False
        
    
        

f.close()    
ser.close()
exit()
