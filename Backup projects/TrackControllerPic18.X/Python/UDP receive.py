import socket

UDP_IP = ''
UDP_PORT = 60000

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.bind((UDP_IP, UDP_PORT))

while True:
    print "start receive\n"
    data, addr = sock.recvfrom(1024)
    print "received message: ", data, "\n"