#!/bin/bash
SERVICE_NAME="home.client"

# Step 1: Stop the service
echo "Stopping service $SERVICE_NAME ..."
service $SERVICE_NAME stop

# Step 2: Download update.zip file
echo "Downloading update $1 ..."
curl --silent -L -o "update.tar" "$1"						# "<-Important for pathes with spaces

# Step 3: Extract the zip file to the folder $2 (override existing files)
echo "Unzipping file to $2 ..."
tar xf "update.tar" -C "$2"	--exclude="config.json"			# "<- Important for pathes with spaces, exclude config.json

# Step 4: Start the service
echo "Starting service $SERVICE_NAME ..."
service $SERVICE_NAME start

# Step 5: Delete update.tar file
rm "update.tar"

# Step 6: Ensure that the replaced sh files are executable
chmod +x "hw.sh" 
chmod +x "screenshot.sh" 
chmod +x "update.sh"

echo "Done."