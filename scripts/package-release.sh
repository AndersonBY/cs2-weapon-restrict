#!/usr/bin/env bash
set -euo pipefail

tag="${1:?release tag is required, for example v1.2.3-mbsifu.1}"
output_arg="${2:-artifacts}"
if [[ ! "${tag}" =~ ^v[0-9]+\.[0-9]+\.[0-9]+-mbsifu\.[0-9]+$ ]]; then
  echo "invalid release tag: ${tag}" >&2
  exit 2
fi

if [[ "${tag%%-mbsifu.*}" != "v2.1.0" ]]; then
  echo "release tag ${tag} does not match project version v2.1.0" >&2
  exit 2
fi

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
output_dir="$(mkdir -p "${repo_root}/${output_arg}" && cd "${repo_root}/${output_arg}" && pwd)"
staging="${repo_root}/.release-staging"
dotnet="${DOTNET:-dotnet}"
rm -rf "${staging}"
mkdir -p "${staging}/addons"

"${dotnet}" build "${repo_root}/WeaponRestrict.csproj" -c Release --nologo
plugin_dir="${staging}/addons/counterstrikesharp/plugins/WeaponRestrict"
mkdir -p "${plugin_dir}"
install -m 0644 "${repo_root}/bin/Release/net8.0/WeaponRestrict.dll" "${plugin_dir}/WeaponRestrict.dll"
install -m 0644 "${repo_root}/bin/Release/net8.0/WeaponRestrict.deps.json" "${plugin_dir}/WeaponRestrict.deps.json"
cp "${repo_root}/README.md" "${staging}/README.md"
cp "${repo_root}/LICENSE" "${staging}/LICENSE"
jq --arg version "${tag#v}" --arg build_revision "$(git -C "${repo_root}" rev-parse HEAD)" \
  '.version = $version | .build_revision = $build_revision' \
  "${repo_root}/packaging/plugin-manifest.json" > "${staging}/plugin-manifest.json"
cp "${staging}/plugin-manifest.json" "${output_dir}/plugin-manifest.json"

source_date_epoch="${SOURCE_DATE_EPOCH:-$(git -C "${repo_root}" show -s --format=%ct HEAD)}"
find "${staging}" -type f -exec touch -d "@${source_date_epoch}" {} +

archive="${output_dir}/cs2-weapon-restrict-${tag}-linux-x64.zip"
rm -f "${archive}" "${archive}.sha256"
(
  cd "${staging}"
  find . -type f -print | LC_ALL=C sort | zip -X -q "${archive}" -@
)
(
  cd "${output_dir}"
  sha256sum "$(basename "${archive}")" > "$(basename "${archive}").sha256"
)

"${repo_root}/scripts/validate-release.sh" "${archive}" "${output_dir}/plugin-manifest.json" "${archive}.sha256"
echo "created ${archive}"
