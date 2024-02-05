@echo off
rem scp -r -v -i "x:\swordfish_openssh.ppk" -P 1939 x:\gpgbot\*.cs ubuntu@swordfish.ghostpeppergames.com:/download

echo Shutting down the bot (if it is running...)
curl -H "key:testtest" http://swordfish.ghostpeppergames.com:1945/shutdown

@echo on