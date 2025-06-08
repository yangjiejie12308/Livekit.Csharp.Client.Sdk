.\livekit-server.exe --dev
.\lk room join --url ws://127.0.0.1:7880 --api-key devkey --api-secret secret --publish-demo --identity bot_user test_room     
.\lk token create  --api-key devkey --api-secret secret  --join --room test_room --identity test_user1 --valid-for 24h