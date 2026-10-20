# КиноПоиск for Jellyfin 12

[![CI](https://github.com/dark-mechanicum/kinopoisk/actions/workflows/ci.yml/badge.svg)](https://github.com/dark-mechanicum/kinopoisk/actions/workflows/ci.yml)

A [Jellyfin](https://jellyfin.org) 12 metadata plugin for [Kinopoisk](https://www.kinopoisk.ru/): titles, descriptions,
ratings, genres, cast & crew and artwork for movies, series, episodes and people.

It only talks to Yandex's own endpoints (`graphql.kinopoisk.ru` and `www.kinopoisk.ru/api`), so it keeps working behind
the Russian white-listed internet, where third-party Kinopoisk APIs are unreachable.

Based on [MiklinMA/jellyfin-kinopoisk-white](https://github.com/MiklinMA/jellyfin-kinopoisk-white) (GPL-3.0),
ported to Jellyfin 12 / .NET 10 and reworked.

## Features

| Item    | Metadata | Search | Images                    | kinopoisk.ru link |
|---------|----------|--------|---------------------------|-------------------|
| Movie   | ✔        | ✔      | posters, backdrops, logos | ✔                 |
| Series  | ✔        | ✔      | posters, backdrops, logos | ✔                 |
| Episode | ✔        | ✔      | —                         | —                 |
| Person  | ✔        | ✔      | photos                    | ✔                 |

- Movies without a Kinopoisk id are matched by name and year, then by file name
  (`The.Matrix.1999.1080p.BluRay.mkv` → «Матрица»).
- An explicit Kinopoisk id in the file or folder name skips the search entirely — see below.
- Episodes are matched by season/episode number: through the full season list for series on the Kinopoisk streaming
  service, otherwise among the 30 most recent episodes (a Kinopoisk API limit).
- Responses are cached for an hour; requests are sent one at a time.

## Kinopoisk id in file and folder names

Put the Kinopoisk id into the name to match an item exactly (case-insensitive, with or without brackets):

```text
Movies/Матрица (1999) [kp301]/Матрица.mkv
Movies/The.Matrix.1999.kp-301.1080p.mkv
TV/Игра престолов [kp-464963]/Season 01/...
```

Accepted forms: `kp301`, `kp-301`, `kp_301`, `kp=301`, `kpid-301`, `kinopoisk-301`, `kinopoiskid=301`, optionally in
`[]`, `()` or `{}`. The tag must not touch other letters or digits (`Skp301` and `kp301x` are ignored).

- **Movies:** the file name is checked first, then the folder that contains it.
- **Series:** the series folder name.
- An id in the name **wins over the id stored on the item**, so renaming a folder fixes a wrong match on the next metadata
  refresh. If Kinopoisk does not know the id, the item gets no metadata rather than a guess by name.
- Episodes use the id of their series.

## Installation

### From the plugin repository

1. Dashboard → Plugins → Repositories → add
   `https://raw.githubusercontent.com/dark-mechanicum/kinopoisk/main/manifest.json`.
2. Catalogue → «КиноПоиск» → Install, then restart Jellyfin.
3. In the library settings enable the «КиноПоиск» metadata and image fetchers and move them to the top.

### Manually (offline)

Download `jellyfin-plugin-kinopoisk_<version>.zip` from [Releases](https://github.com/dark-mechanicum/kinopoisk/releases),
unpack it into `<jellyfin data>/plugins/Kinopoisk_<version>/` and restart Jellyfin.

## Development

Requires the .NET 10 SDK (see `global.json`).

```bash
dotnet build Kinopoisk.slnx
dotnet test Kinopoisk.slnx                         # hits the live Kinopoisk API
dotnet format Kinopoisk.slnx --verify-no-changes
./build/package.sh 1.0.0.0 artifacts               # plugin zip as Jellyfin installs it
```

### Dependencies

Every package version mirrors Jellyfin 12.2's own
[`Directory.Packages.props`](https://github.com/jellyfin/jellyfin/blob/v12.2/Directory.Packages.props): a plugin runs inside
the server process, so shared libraries must match what the server ships. All versions live in
`Directory.Packages.props`; Dependabot proposes `Jellyfin.*` updates, and the rest is bumped together with them.

### Tests run against the real API

All provider and client tests call the live Kinopoisk API — nothing is mocked — so CI sees Kinopoisk changes as soon as
they happen. They use long-lived titles (`tests/.../KnownTitles.cs`) and only check ranges for values that drift, such as
ratings.

The GraphQL documents in `src/Jellyfin.Plugin.Kinopoisk/Api/Queries` are Kinopoisk's own: the server rejects any
document it does not know (`the query is not allowed`), so they cannot be edited, only replaced with fresh copies taken
from the site.

## CI

- **`ci.yml`** runs on every push to `main`, on pull requests, **every day at 04:00 UTC** and on demand. It does a format
  check, a warnings-as-errors build, the live tests and packaging. If the daily run fails it opens (or comments on) an
  `api-drift` issue, and closes it again once a later daily run passes.
- **`release.yml`** runs on a `v1.2.3.4` tag. It builds, tests, creates the GitHub release with the plugin zip and adds the
  version with its checksum to `manifest.json` on `main`.

To release: `git tag v1.0.0.0 && git push origin v1.0.0.0`.

## License

[GPL-3.0](LICENSE), same as the original plugin and Jellyfin.
