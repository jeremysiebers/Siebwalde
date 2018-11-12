#!/bin/python

import serial
import csv
import sys

ser = serial.Serial('COM3', 520832, timeout=0, parity=serial.PARITY_NONE)

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

s = ser.read(10)
sample = 0
run = True

while run:

    try:    
        s = ser.read(1)
        
        if (len(s) > 0):
            if (s[0] == 170):
                s = ser.read(30)
                if (s[12] == 85):
                    out.writerow([str(sample), str(s[0] + s[1] << 8), str(s[2] + s[3] << 8), str(s[4] + s[5] << 8), str(s[6] + s[7] << 8), str(s[8] + s[1] << 8), str(s[10] + s[11] << 8)]) 
                    sample = sample + 1
                    
        
        if (sample > 1000):
            run = False
            
    except:
        f.close()    
        
        exit()
        

f.close()    

exit()
