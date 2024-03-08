#!/bin/bash

set -eou pipefail
set -x


DIR=$(realpath $(dirname "$0"))
cd "${DIR}"

VERSION=$(./get_current_version.sh)

echo "Using version ${VERSION}"
cd ..

cd Templates

#<ProjectReference Include="..\..\..\SogePoco\SogePoco.Impl\SogePoco.Impl.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" PrivateAssets="All" />
# into
#<PackageReference Include="SogePoco.Impl" Version="0.0.1" OutputItemType="Analyzer" ReferenceOutputAssembly="false" PrivateAssets="All" />
find -type f -name '*.csproj' | xargs --max-lines=1 sed -i --regexp-extended 's/<ProjectReference Include="[\.\\]+SogePoco\\SogePoco\.Impl\\SogePoco\.Impl\.csproj"([^>]+)>/<PackageReference Include="SogePoco.Impl" Version="'"${VERSION}"'"\1>/'


# <ProjectReference Include="..\..\..\SogePoco\SogePoco.Common\SogePoco.Common.csproj" />
# into
# <PackageReference Include="SogePoco.Common" Version="0.0.1" />
find -type f -name '*.csproj' | xargs --max-lines=1 sed -i --regexp-extended 's/<ProjectReference Include="[\.\\]+SogePoco\\SogePoco\.Common\\[^>]+>/<PackageReference Include="SogePoco.Common" Version="'"${VERSION}"'" \/>/'


# comment out
#   #r "../../../../SogePoco/bin/Debug/netstandard2.0/SogePoco.Impl.dll"
find -type f -name '*.csx' | xargs --max-lines=1 sed -i --regexp-extended 's/^(#r\s+"[\.\/]+SogePoco.*)/\/\/\1/I'


# uncomment and update version no
#   //#r "nuget: SogePoco.Impl, 0.0.1"
find -type f -name '*.csx' | xargs --max-lines=1 sed -i --regexp-extended 's/^\/\/#r\s+"nuget:\s+(SogePoco[^,\s]+).*/#r "nuget: \1, '"${VERSION}"'"/I'


cd ..

echo "OK"
