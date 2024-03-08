#!/bin/bash

set -eou pipefail
set -x

DIR=$(realpath $(dirname "$0"))
cd "${DIR}"

PROGRAM="/source/scripts/_impl_template_is_usable_sqlite.sh"

./_exec-within-container-program-params.sh "${PROGRAM}"
