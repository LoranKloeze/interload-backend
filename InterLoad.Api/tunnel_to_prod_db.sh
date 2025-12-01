#!/bin/bash
source ~/.bash_profile
LOCAL_PORT=15007
SERVER="hetzner-interstage"
CONTAINER_NAME="interload-db-1"
IP_ADDRESS=$(ssh $SERVER "docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' $CONTAINER_NAME")
echo Tunnel runs on localhost:$LOCAL_PORT
ssh -N -L $LOCAL_PORT:$IP_ADDRESS:5432 $SERVER