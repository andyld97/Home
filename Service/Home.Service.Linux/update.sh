#!/bin/bash

# CURL REQUIRED! (Why is my favorite CURL not installed, how is that possible? :( )

# Step 1: Download update.zip file
echo "Downloading update $1 ..."
curl --silent -L -o "update.tar" "$1"						# "<-Important for pathes with spaces

# Step 2: Extract the zip file to the folder $2 (override existing files)
echo "Unzipping file to $2 ..."
tar xf "update.tar" -C "$2"	--exclude="config.json"			# "<- Important for pathes with spaces, exclude config.json

# Step 3: Delete update.tar file
rm "update.tar"

# Step 4: Ensure that the replaced sh files are executable
chmod +x "hw.sh" 
chmod +x "screenshot.sh" 
chmod +x "update.sh"

# After the program exits, the systemd-service should restart an the new update is running!
echo "Done."