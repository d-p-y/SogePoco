#!/bin/bash

set -eou pipefail
set -x

DIR=$(realpath $(dirname "$0"))
cd "${DIR}"

NO_DB_CONNSTR="$(../sqlserver_in_container/start.sh)"
PROGRAM="/source/scripts/_impl_template_is_usable_sqlserver.sh"

./_exec-within-container-program-params.sh "${PROGRAM}" "${NO_DB_CONNSTR}"
