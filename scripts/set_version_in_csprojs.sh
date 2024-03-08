#!/bin/bash

set -eou pipefail
set -x


DIR=$(realpath $(dirname "$0"))
cd "${DIR}"
VERSION=$(./get_current_version.sh)
cd "${DIR}/.."

cd SogePoco
find -type f -name '*.csproj' | xargs --max-lines=1 sed -i 's/<Version>[^<]*<\/Version>/<Version>'${VERSION}'<\/Version>/'
find -type f -name '*.csproj' | xargs --max-lines=1 sed -i 's/<PackageVersion>[^<]*<\/PackageVersion>/<PackageVersion>'${VERSION}'<\/PackageVersion>/'

cd ..
cd Templates
find -type f -name '*.csproj' | xargs --max-lines=1 sed -i 's/<Version>[^<]*<\/Version>/<Version>'${VERSION}'<\/Version>/'
find -type f -name '*.csproj' | xargs --max-lines=1 sed -i 's/<PackageVersion>[^<]*<\/PackageVersion>/<PackageVersion>'${VERSION}'<\/PackageVersion>/'
cd ..
