#!/bin/bash

set -eou pipefail
set -x

NAME="exec-within-container-program-params"
DIR=$(realpath $(dirname "$0"))
BASE_DIR="${DIR}/.."
TEMP_DIR="${BASE_DIR}/bin/temp"
EMPTY_DIR="${BASE_DIR}/bin/empty"

mkdir -p "${TEMP_DIR}"
mkdir -p "${EMPTY_DIR}"

if [[ -z "${1:-}" ]];
then
	echo "one arg needed - entrypoint cmd"
	exit 1
fi

echo "#!/bin/bash
$1 "'$@' > "${EMPTY_DIR}/run.sh"

# to prepare C and V capable environment
podman image build \
	-v "${BASE_DIR}:/source" \
	--rm -f "${NAME}.Containerfile" -t="${NAME}" \
	 "${EMPTY_DIR}"

podman container rm --force "${NAME}" || echo "ok, no old container present"

args=( $@ )
unset args[0]

podman container create --net=host -t -i \
	--cap-drop all \
	-v "${BASE_DIR}:/source" \
	--rm --name "${NAME}" \
	"${NAME}" "${args[@]}"

podman container start --attach --interactive "${NAME}"
echo "OK"
