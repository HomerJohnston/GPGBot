curl -H "key:testtest" http://swordfish.ghostpeppergames.com:9000/shutdown

start /B plink -batch -ssh ubuntu@swordfish.ghostpeppergames.com -P 1939 "nohup /opt/gpgbot-deploy/start.sh &"