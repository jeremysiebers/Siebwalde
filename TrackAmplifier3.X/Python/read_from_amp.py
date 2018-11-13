#!/bin/python

import serial
import csv
import sys
import io

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

while run:

    try:    
        s = sio.readline()
        
        if (len(s) > 0):
            out.writerow([str(sample), str(s[0] + s[1] << 8)]) 
            sample = sample + 1
                    
        
        if (sample > 10):
            run = False
            
    except:
        f.close()    
        
        exit()
        

f.close()    

exit()
