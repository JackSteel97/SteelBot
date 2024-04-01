# Running from Docker

1. Build docker image from docker file
2. Save docker image as tar file with `docker save -o steelbot.tar steelbot:latest`
3. Copy image to host with scp `scp ./steelbot.tar user@host:SteelBotImages`
4. On host, load image into docker `docker load -i steelbot.tar`
5. Run in background on docker host `docker run -d --restart=always steelbot:latest`