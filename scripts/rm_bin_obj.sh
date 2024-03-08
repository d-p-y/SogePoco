#!/bin/bash

set -eou pipefail
set -x

function do_clean() {
	find . -type d -name obj | xargs rm -rf
	find . -type d -name bin | xargs rm -rf
}


DIR=$(realpath $(dirname "$0"))
cd "${DIR}/.."

cd Templates
do_clean
cd ..

cd SogePoco
do_clean
cd ..

echo "OK"
