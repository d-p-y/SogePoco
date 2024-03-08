#!/bin/bash

set -eou pipefail
set -x

NAME="sogepoco-postgresql-server"
DIR=$(realpath $(dirname "$0"))
EMPTY_DIR="${DIR}/../bin/empty"

mkdir -p "${EMPTY_DIR}"

>&2 podman container rm --force "${NAME}" || true
>&2 podman image build -f "${DIR}/Dockerfile" -t="${NAME}" "${EMPTY_DIR}"

>&2 podman create \
	--net=host --mount type=tmpfs,destination=/var/lib/postgresql/14/main \
	--name "${NAME}" -t -i "${NAME}"

>&2 podman container start "${NAME}"

# todo: add better "wait for started with timeout"
sleep 1
#podman logs sogepoco-postgresql-server

>&2 echo "adding user and creating db"

>&2 podman exec -it "${NAME}" sudo -u postgres /usr/bin/psql -U postgres --command="CREATE user sogepoco_tester_user with password 'sogepoco_tester_passwd';" || exit 1
>&2 podman exec -it "${NAME}" sudo -u postgres /usr/bin/psql -U postgres --command="ALTER USER sogepoco_tester_user CREATEDB;;" || exit 1

>&2 echo "added user and db"

echo "Host=127.0.0.1;Port=54332;Username=sogepoco_tester_user;Password=sogepoco_tester_passwd;Database=postgres"
