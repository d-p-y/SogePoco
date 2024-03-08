#!/bin/bash

set -eou pipefail
set -x

DIR=$(realpath $(dirname "$0"))
cd "${DIR}"

VERSION="0.0.1"
export VERSION
./set_version_in_csprojs.sh
