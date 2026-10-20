#!/usr/bin/env bash
# Enforces the versioning rule: the plugin version starts with the Jellyfin version it is built for.
#   - targetAbi in build.yaml must be <Jellyfin major>.<minor>.0.0 of Jellyfin.Controller in Directory.Packages.props;
#   - a release tag, if given, must be v<Jellyfin major>.<minor>.X.Y.
# Usage: build/check-version.sh [tag]
# Prints the Jellyfin line (e.g. 12.2) on success.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
tag="${1:-}"

jellyfin=$(sed -n 's/.*Include="Jellyfin.Controller" Version="\([0-9]*\.[0-9]*\)\..*".*/\1/p' "$root/Directory.Packages.props")
target_abi=$(sed -n 's/^targetAbi: *"\(.*\)"/\1/p' "$root/build.yaml")

if [ -z "$jellyfin" ]; then
  echo "Cannot read the Jellyfin.Controller version from Directory.Packages.props" >&2
  exit 1
fi

if [ "$target_abi" != "$jellyfin.0.0" ]; then
  echo "build.yaml targetAbi is '$target_abi' but Jellyfin.Controller is $jellyfin: set targetAbi to \"$jellyfin.0.0\"" >&2
  exit 1
fi

if [ -n "$tag" ]; then
  escaped="${jellyfin//./\\.}"
  if ! [[ "$tag" =~ ^v${escaped}\.[0-9]+\.[0-9]+$ ]]; then
    echo "Tag '$tag' does not match the supported Jellyfin $jellyfin: expected v$jellyfin.X.Y" >&2
    exit 1
  fi
fi

echo "$jellyfin"
