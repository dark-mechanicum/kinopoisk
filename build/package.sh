#!/usr/bin/env bash
# Builds the plugin and packs it the way Jellyfin installs it: a zip with the plugin dll and meta.json.
# Usage: build/package.sh <version a.b.c.d> <output dir>
set -euo pipefail

version="${1:?version (a.b.c.d) required}"
output="${2:?output directory required}"
root="$(cd "$(dirname "$0")/.." && pwd)"
project="$root/src/Jellyfin.Plugin.Kinopoisk/Jellyfin.Plugin.Kinopoisk.csproj"
publish="$(mktemp -d)"
trap 'rm -rf "$publish"' EXIT

dotnet publish "$project" --configuration Release --output "$publish" -p:Version="$version"

guid=$(sed -n 's/^guid: *"\(.*\)"/\1/p' "$root/build.yaml")
target_abi=$(sed -n 's/^targetAbi: *"\(.*\)"/\1/p' "$root/build.yaml")
timestamp=$(date -u +%Y-%m-%dT%H:%M:%SZ)

cat > "$publish/meta.json" <<JSON
{
  "category": "MoviesAndShows",
  "guid": "$guid",
  "name": "КиноПоиск",
  "description": "Метаданные и изображения фильмов, сериалов, эпизодов и персон с КиноПоиска.",
  "overview": "Информация о фильмах, сериалах и персонах с КиноПоиска",
  "owner": "dark-mechanicum",
  "targetAbi": "$target_abi",
  "timestamp": "$timestamp",
  "version": "$version",
  "status": "Active",
  "autoUpdate": true,
  "assemblies": ["Jellyfin.Plugin.Kinopoisk.dll"]
}
JSON

mkdir -p "$output"
zip_path="$(cd "$output" && pwd)/jellyfin-plugin-kinopoisk_${version}.zip"
rm -f "$zip_path"
(cd "$publish" && zip -q "$zip_path" Jellyfin.Plugin.Kinopoisk.dll meta.json)
echo "$zip_path"
