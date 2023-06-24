#!/bin/bash

# Arguments:
# $1: update.tar File path
# $2: Working Directory of Home.Service.Linux

# Step 1: Extract the zip file to the folder $2 (override existing files)
echo "Unzipping file to $2 ..."
tar xf "$1" -C "$2"	--exclude="config.json"			# "<- Important for paths with spaces, exclude config.json

# Step 2: Delete update.tar file
rm "update.tar"

# Step 3: Ensure that the replaced sh files are executable
chmod +x "hw.sh" 
chmod +x "screenshot.sh" 
chmod +x "update.sh"

# After the program exits, the systemd-service should restart an the new update is running!
echo "Done."