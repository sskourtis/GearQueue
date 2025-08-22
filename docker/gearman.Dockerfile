FROM debian:trixie-slim

RUN apt-get update && apt-get install -y gearman

COPY ./status /bin/status

CMD gearmand -R --log-file=stderr -P /run/gearmand.pid --verbose WARNING