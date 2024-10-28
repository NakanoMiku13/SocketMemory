#!/bin/bash
for i in {1..2}
do
	sh -c  "./bin/Debug/net8.0/SocketMemory --interface wlan0 --mode client --port 9595 --ip 172.100.94.134" &
	./script.sh
done
