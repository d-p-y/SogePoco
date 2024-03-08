#!/bin/bash

set -eou pipefail
set -x

DIR=$(realpath $(dirname "$0"))
cd "${DIR}"

cd ..
cd Templates

#<PackageReference Include="SogePoco.Impl" Version="0.0.1" OutputItemType="Analyzer" ReferenceOutputAssembly="false" PrivateAssets="All" />
# into
#<ProjectReference Include="..\..\..\SogePoco\SogePoco.Impl\SogePoco.Impl.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" PrivateAssets="All" />
find -type f -name '*.csproj' | xargs --max-lines=1 sed -i --regexp-extended 's/<PackageReference Include="SogePoco\.Impl" Version="[^"]+"([^>]+)>/<ProjectReference Include="..\\..\\..\\..\\SogePoco\\SogePoco.Impl\\SogePoco.Impl.csproj"\1>/'


# <PackageReference Include="SogePoco.Common" Version="0.0.1" />
# into
# <ProjectReference Include="..\..\..\SogePoco\SogePoco.Common\SogePoco.Common.csproj" />
find -type f -name '*.csproj' | xargs --max-lines=1 sed -i --regexp-extended 's/<PackageReference Include="SogePoco\.Common"[^>]+>/<ProjectReference Include="..\\..\\..\\..\\SogePoco\\SogePoco.Common\\SogePoco.Common.csproj" \/>/'


# uncomment
#   #r "../../../../SogePoco/bin/Debug/netstandard2.0/SogePoco.Impl.dll"
find -type f -name '*.csx' | xargs --max-lines=1 sed -i --regexp-extended 's/^\/\/(#r\s+"[\.\/]+SogePoco.*)/\1/I'


# comment out
#   //#r "nuget: SogePoco.Impl, 0.0.1"
find -type f -name '*.csx' | xargs --max-lines=1 sed -i --regexp-extended 's/^(#r\s+"nuget:\s+SogePoco.*)/\/\/\1/I'


cd ..

echo "OK"
