#!/bin/bash

set -eou pipefail
set -x

DIR=$(realpath $(dirname "$0"))
cd "${DIR}"

./rm_bin_obj.sh
./switch_templates_to_nuget.sh
./set_version_in_csprojs.sh

cd "${DIR}/.."
PACKAGES_DIR=$(realpath .)/bin/nuget_source/
mkdir --parents "${PACKAGES_DIR}"

cd Templates/SqlServer
dotnet pack --configuration Release --output "${PACKAGES_DIR}"
cd ../..

cd Templates/Postgres
dotnet pack --configuration Release --output "${PACKAGES_DIR}"
cd ../..

cd Templates/Sqlite
dotnet pack --configuration Release --output "${PACKAGES_DIR}"
cd ../..

cd SogePoco/SogePoco.Common
dotnet pack --configuration Release --output "${PACKAGES_DIR}"
cd ../..

cd SogePoco/SogePoco.Impl
dotnet pack --configuration Release --output "${PACKAGES_DIR}"
cd ../..

cd scripts
./switch_templates_to_projectreferences.sh

echo "OK"



