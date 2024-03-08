#!/bin/bash

set -eou pipefail
set -x

DIR=$(realpath $(dirname "$0"))

cd "${DIR}"

PROGRAM=$(cat << EOF
#!/bin/bash
set -eou pipefail
set -x

rm -rf ~/.nuget/packages/sogepoco.*
rm -rf ~/.templateengine/packages/SogePoco.*
rm -rf /source/bin/nuget_source

# publishable ?
/source/scripts/rm_bin_obj.sh
/source/scripts/dotnet_pack_all.sh
# side effect: nuget packages for template user test

#
# builds for dev?
#
/source/scripts/switch_templates_to_projectreferences.sh

/source/scripts/rm_bin_obj.sh
cd /source/Templates/Postgres/SogePoco.Template.Postgres
dotnet build

/source/scripts/rm_bin_obj.sh
cd /source/Templates/Sqlite/SogePoco.Template.Sqlite
dotnet build

/source/scripts/rm_bin_obj.sh
cd /source/Templates/SqlServer/SogePoco.Template.SqlServer
dotnet build

#
# builds for template user?
#
/source/scripts/switch_templates_to_nuget.sh 

/source/scripts/rm_bin_obj.sh
cd /source/Templates/Postgres/SogePoco.Template.Postgres
dotnet build

/source/scripts/rm_bin_obj.sh
cd /source/Templates/Sqlite/SogePoco.Template.Sqlite
dotnet build

/source/scripts/rm_bin_obj.sh
cd /source/Templates/SqlServer/SogePoco.Template.SqlServer
dotnet build

#
#cleanup
#
/source/scripts/switch_templates_to_projectreferences.sh

EOF)

#echo "$PROGRAM"
./_exec-within-container-build_arg_is_entrypoint.sh "${PROGRAM}"

