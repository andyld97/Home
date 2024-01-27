#!/bin/bash
mem=$(cat /proc/meminfo | grep MemTotal|cut -d' ' -f 2-);
free=$(cat /proc/meminfo | grep MemAvailable|cut -d' ' -f 2-);
load=$(top -bn1 | grep load | awk '{printf "%.2f\n", $(NF-2)}');

echo "$mem" | xargs;
echo "$free" | xargs;
echo "$load" | xargs;