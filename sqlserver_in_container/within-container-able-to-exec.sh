#!/bin/bash

podman exec -it sogepoco-sqlserver-server /opt/mssql-tools/bin/sqlcmd -S 127.0.0.1,1433 -U SA -P "1234_some_sa_PASSWD" -Q "select 1"
