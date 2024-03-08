#!/bin/bash

set -eou pipefail
set -x

NAME="sogepoco-sqlserver-server"
DIR=$(realpath $(dirname "$0"))
EMPTY_DIR="${DIR}/../bin/empty"

mkdir -p "${EMPTY_DIR}"

>&2 podman container rm --force "${NAME}" || true
>&2 podman image build -f "${DIR}/Dockerfile" -t="${NAME}" "${EMPTY_DIR}"

>&2 podman create \
	--cap-add SYS_PTRACE \
	-p 14332:1433 \
	-e 'ACCEPT_EULA=1' \
	-e 'MSSQL_USER=SA' \
	-e 'MSSQL_SA_PASSWORD=1234_some_sa_PASSWD' \
	--name "${NAME}" -t -i "${NAME}"

>&2 podman container start "${NAME}"

#sleep 1
#podman logs sogepoco-sqlserver-server

echo "Data Source=127.0.0.1,14332;Initial Catalog=master;Trusted_Connection=False;User id=sa;Password=1234_some_sa_PASSWD;Connection Timeout=2;TrustServerCertificate=True"
