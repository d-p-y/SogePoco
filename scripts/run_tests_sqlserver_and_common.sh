#!/bin/bash

set -eou pipefail
set -x


DIR=$(realpath $(dirname "$0"))

cd "${DIR}"

CONNSTR="$(../sqlserver_in_container/start.sh)"

PROGRAM=$(cat << EOF
#!/bin/bash
set -eou pipefail
set -x

rm -rf ~/.nuget/packages/sogepoco.*
rm -rf /source/bin/nuget_source

rm -rf  /source/bin/nuget_source || echo "no need to clean"
mkdir -p /source/bin/nuget_source

TEST_SKIP_POSTGRES=1
export TEST_SKIP_POSTGRES

TEST_SKIP_SQLITE=1
export TEST_SKIP_SQLITE

cd /source/SogePoco/SogePoco.Impl.Tests

TEST_SQLSERVER_CONNECTIONSTRING="${CONNSTR}"
export TEST_SQLSERVER_CONNECTIONSTRING

dotnet test --logger:"console;verbosity=detailed"

EOF)

#echo "$PROGRAM"
./_exec-within-container-build_arg_is_entrypoint.sh "${PROGRAM}"

