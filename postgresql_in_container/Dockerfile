FROM ubuntu:22.04
LABEL Description="dev srv" Version="2021.11.26"
RUN apt-get update
ENV TZ=Europe/Warsaw
ENV DEBIAN_FRONTEND=noninteractive
RUN ln -s /usr/share/zoneinfo/Europe/Warsaw /etc/localtime
RUN apt-get -y install postgresql
RUN apt-get -y install sudo

RUN echo "" >> /etc/postgresql/14/main/pg_hba.conf
RUN echo "host    all         all     0.0.0.0/0     md5" >> /etc/postgresql/14/main/pg_hba.conf
RUN echo "" >> /etc/postgresql/14/main/pg_hba.conf
#RUN cat /etc/postgresql/14/main/pg_hba.conf

ENV CFG_FILE="/etc/postgresql/14/main/postgresql.conf"
ENV OLD_CFG_BACKUP_FILE="/etc/postgresql/14/main/postgresql.conf.old"

RUN cat "${CFG_FILE}" > "${OLD_CFG_BACKUP_FILE}"

#RUN echo "" >> "$CFG_FILE"
#RUN echo "listen_addresses='*'" >> "$CFG_FILE"
#RUN echo "" >> "$CFG_FILE"

# speed up for tests, inspired by
# https://stackoverflow.com/questions/9407442/optimise-postgresql-for-fast-testing
# https://github.com/eradman/ephemeralpg/blob/master/pg_tmp.sh

RUN sed -i -E 's/^(#?port\s*=[^\n]+)/port = 54332/' "${CFG_FILE}"
RUN sed -i -E 's/^(#?listen_addresses\s*=[^\n]+)/listen_addresses='"'"'*'"'"'/' "${CFG_FILE}"
RUN sed -i -E 's/^(#?work_mem\s*=[^\n]+)/work_mem = 40MB/' "${CFG_FILE}"
RUN sed -i -E 's/^(#?shared_buffers\s*=[^\n]+)/shared_buffers = 1000MB/' "${CFG_FILE}"
RUN sed -i -E 's/^(#?fsync\s*=[^\n]+)/fsync = off/' "${CFG_FILE}"
RUN sed -i -E 's/^(#?synchronous_commit\s*=[^\n]+)/synchronous_commit = off/' "${CFG_FILE}"
RUN sed -i -E 's/^(#?full_page_writes\s*=[^\n]+)/full_page_writes = off/' "${CFG_FILE}"
RUN sed -i -E 's/^(#?log_min_duration_statement\s*=[^\n]+)/log_min_duration_statement = 0/' "${CFG_FILE}"
RUN sed -i -E 's/^(#?log_connections\s*=[^\n]+)/log_connections = on/' "${CFG_FILE}"
RUN sed -i -E 's/^(#?log_disconnections\s*=[^\n]+)/log_disconnections = on/' "${CFG_FILE}"
RUN cat "${CFG_FILE}"
RUN mv /var/lib/postgresql/14/main /main

RUN echo "#!/bin/bash" > /run.sh
RUN echo "set -eou pipefail" >> /run.sh
RUN echo "set -x" >> /run.sh
RUN echo "mv /main/* /var/lib/postgresql/14/main/" >> /run.sh
RUN echo "chown postgres:postgres /var/lib/postgresql/14/main" >> /run.sh
RUN echo "chmod u=rwx,go= /var/lib/postgresql/14/main" >> /run.sh
RUN echo "sudo -u postgres /usr/lib/postgresql/14/bin/postgres -D /var/lib/postgresql/14/main -c config_file=/etc/postgresql/14/main/postgresql.conf" >> /run.sh
RUN chmod a+x /run.sh

EXPOSE 54332
#USER postgres
ENTRYPOINT ["/run.sh"]
