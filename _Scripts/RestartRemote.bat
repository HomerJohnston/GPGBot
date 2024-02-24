start /B plink -batch -ssh ubuntu@swordfish.ghostpeppergames.com -P 1939 "nohup /opt/percival-deploy/stop.sh &"

start /B plink -batch -ssh ubuntu@swordfish.ghostpeppergames.com -P 1939 "nohup /opt/percival-deploy/start.sh &"

pause