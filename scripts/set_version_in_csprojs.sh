#!/bin/bash

set -eou pipefail
set -x


DIR=$(realpath $(dirname "$0"))
cd "${DIR}/.."

cd SogePoco
find -type f -name '*.csproj' | xargs --max-lines=1 sed -i 's/<Version>[^<]*<\/Version>/<Version>'${VERSION}'<\/Version>/'
find -type f -name '*.csproj' | xargs --max-lines=1 sed -i 's/<PackageVersion>[^<]*<\/PackageVersion>/<PackageVersion>'${VERSION}'<\/PackageVersion>/'

cd ..