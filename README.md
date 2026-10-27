# КиноПоиск for Jellyfin 12.2

[![CI](https://github.com/dark-mechanicum/kinopoisk/actions/workflows/ci.yml/badge.svg)](https://github.com/dark-mechanicum/kinopoisk/actions/workflows/ci.yml)

A [Jellyfin](https://jellyfin.org) 12.2 metadata plugin for [Kinopoisk](https://www.kinopoisk.ru/): titles, descriptions,
ratings, genres, cast & crew and artwork for movies, series, episodes and people. Plugin versions follow the Jellyfin
version they support — `12.2.X.Y` for Jellyfin 12.2 (see [Versioning](#versioning)).

It only talks to Yandex's own endpoints (`graphql.kinopoisk.ru` and `www.kinopoisk.ru/api`), so it keeps working behind
the Russian white-listed internet, where third-party Kinopoisk APIs are unreachable.

Based on [MiklinMA/jellyfin-kinopoisk-white](https://github.com/MiklinMA/jellyfin-kinopoisk-white) (GPL-3.0),
ported to Jellyfin 12.2 / .NET 10 and reworked.

## Installation

### From the plugin repository

1. Dashboard → Plugins → Repositories → add
   `https://raw.githubusercontent.com/dark-mechanicum/kinopoisk/main/manifest.json`.
2. Catalogue → «КиноПоиск» → Install, then restart Jellyfin.
3. In the library settings enable the «КиноПоиск» metadata and image fetchers and move them to the top.

### Manually (offline)

Download `jellyfin-plugin-kinopoisk_<version>.zip` matching your Jellyfin version (`12.2.X.Y` for Jellyfin 12.2) from
[Releases](https://github.com/dark-mechanicum/kinopoisk/releases), unpack it into
`<jellyfin data>/plugins/Kinopoisk_<version>/` and restart Jellyfin.

## Features

| Item    | Metadata | Search | Images                    | kinopoisk.ru link |
| ------- | -------- | ------ | ------------------------- | ----------------- |
| Movie   | ✔        | ✔      | posters, backdrops, logos | ✔                 |
| Series  | ✔        | ✔      | posters, backdrops, logos | ✔                 |
| Episode | ✔        | ✔      | —                         | —                 |
| Person  | ✔        | ✔      | photos                    | ✔                 |

The Kinopoisk id field («КиноПоиск») is shown in «Edit metadata» for movies, series, episodes and people. Episodes get no
kinopoisk.ru link: the site has no page for a single episode.

- Movies without a Kinopoisk id are matched by name and year, then by file name
  (`The.Matrix.1999.1080p.BluRay.mkv` → «Матрица»). The year only ranks the matches (titles released that year come
  first), so a slightly wrong year never hides the right title.
- A movie can also be matched to a title Kinopoisk lists as a series (e.g. multi-part cartoons like «Незнайка на Луне»).
- An explicit Kinopoisk id in the file or folder name skips the search entirely — see below.
- Episodes are matched in this order:
  1. by the Kinopoisk id typed into the episode's «КиноПоиск» field in «Edit metadata». The library's season and
     episode numbers are kept, so this also fixes series numbered differently from Kinopoisk;
  2. by season/episode number in the full season list, for series on the Kinopoisk streaming service;
  3. among the 30 latest released and the 30 next announced episodes (a Kinopoisk API limit), which carry the episode's
     Kinopoisk id;
  4. on the series' episode list page (`kinopoisk.ru/film/{id}/episodes/`), which lists every episode with its title,
     original title and release date, but has no episode ids or descriptions. Episodes matched here get no Kinopoisk
     id.
- Cast & crew include cinematographers (role «Оператор», shown under Crew — Jellyfin has no cinematographer kind) and
  designers («Художник»); dubbing actors («Озвучка») on request.
- Studios are the networks and distributors Kinopoisk lists (e.g. «HBO», «Каро-Премьер»); Kinopoisk's API exposes no
  production companies to these endpoints.
- Genres get a capital first letter («Фантастика»), so they merge with genres from other providers. Items fetched before
  12.2.2.0 keep the lowercase genres until a «Replace all metadata» refresh.
- Kinopoisk's "no photo" placeholders are never saved as images.
- **Series** get the episode length as their runtime, «Завершён» / «Продолжается» (Ended / Continuing) and the end year
  from Kinopoisk's release years. Kinopoisk only knows the year, so the end date is 1 July of that year. Movies and
  episodes take their runtime from the media file: Jellyfin ignores a provider runtime for videos.
- **People** get their death date, so Jellyfin shows the date and age at death. With «Биография персон» enabled the
  overview is built from the first 10 Kinopoisk facts, as plain text without Kinopoisk's links.
- With «Тег Топ-250» enabled, films and series in a Kinopoisk Top-250 get the tag **«Топ-250 КиноПоиска»**. Jellyfin adds
  tags to the existing ones, so a title that later drops out of the list keeps the tag until «Replace all metadata».
- Responses are cached for an hour; requests are sent one at a time and retried with backoff on 429, 5xx, timeouts and
  network errors. An outdated Kinopoisk id is logged as one warning line, not as an error.

### Not supported

- **Lookup by IMDb / TMDb id.** No Yandex endpoint reachable behind the white list resolves an external id (search for
  `tt0133093` finds nothing). Use a `kp…` tag in the name instead.
- **Trailers.** Kinopoisk gives either a trailer page (Jellyfin cannot play an HTML page) or a signed stream URL that
  expires within days and exists only for some titles.

## Settings

Dashboard → Plugins → «КиноПоиск». Defaults keep the behaviour of earlier releases. Changes apply to new requests;
cached responses expire within an hour.

| Setting                             | Default   | Effect                                                                   |
| ----------------------------------- | --------- | ------------------------------------------------------------------------ |
| Actors to fetch (1–200)             | 30        | Size of the cast list (and of the dubbing cast).                         |
| Community rating                    | Kinopoisk | Kinopoisk or IMDb rating first; the other is the fallback.               |
| Capitalize genres                   | on        | «Фантастика» instead of «фантастика».                                    |
| Dubbing actors                      | off       | Adds the Russian voice cast with role «Озвучка».                         |
| Top-250 tag                         | off       | Tags titles in a Kinopoisk Top-250 with «Топ-250 КиноПоиска».            |
| Person biography                    | off       | Builds a person's overview from Kinopoisk facts, replacing the old one.  |
| Pause between requests, ms (0–5000) | 0         | Raise it if Kinopoisk answers `429 Too Many Requests` during big scans.  |
| Prefer IPv4                         | off       | Connects over IPv4 first; helps when IPv6 is broken and images time out. |

New fields (series runtime, status and end date, death dates) fill in on the next «Search for missing metadata»
refresh. When «КиноПоиск» is the first metadata fetcher, any refresh also replaces values other providers set before,
e.g. the exact end date of a series with Kinopoisk's end year.

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

The episode list page is read with the cookie `disable_server_sso_redirect=1`. Without it kinopoisk.ru redirects every
page to the Yandex login. If Kinopoisk stops honouring it, the page is skipped with a warning and the other sources still
work.

## Development

Requires the .NET 10 SDK (see `global.json`).

```bash
dotnet build Kinopoisk.slnx
dotnet test Kinopoisk.slnx                         # hits the live Kinopoisk API
dotnet format Kinopoisk.slnx --verify-no-changes
./build/check-version.sh v12.2.0.0                 # versioning rules (see below)
./build/package.sh 12.2.0.0 artifacts              # plugin zip as Jellyfin installs it
```

### Dependencies

Every package version mirrors Jellyfin 12.2's own
[`Directory.Packages.props`](https://github.com/jellyfin/jellyfin/blob/v12.2/Directory.Packages.props): a plugin runs inside
the server process, so shared libraries must match what the server ships. All versions live in
`Directory.Packages.props`; Dependabot proposes `Jellyfin.*` updates, and the rest is bumped together with them.

### Tests run against the real API

All provider and client tests call the live Kinopoisk API — nothing is mocked — so CI sees Kinopoisk changes as soon as
they happen. They use long-lived titles (`tests/.../KnownTitles.cs`) and only check ranges for values that drift, such as
ratings. The only fakes are an HTTP handler in the retry tests, which checks the plugin's own retry logic, and a few rows
copied from the real episode list page, which pin parsing cases live pages do not reliably show (announced episodes
without dates, Kinopoisk's placeholder titles).

The GraphQL documents in `src/Jellyfin.Plugin.Kinopoisk/Api/Queries` are Kinopoisk's own: the server rejects any
document it does not know (`the query is not allowed`), so they cannot be edited, only replaced with fresh copies taken
from the site.

## CI

- **`ci.yml`** runs on every push to `main`, on pull requests, **every day at 04:00 UTC** and on demand. It checks that the
  version is aligned with Jellyfin (see [Versioning](#versioning)), formatting, does a warnings-as-errors build, the live
  tests and packaging. If the daily run fails it opens (or comments on) an `api-drift` issue, and closes it again once a
  later daily run passes.
- The daily (and manual) run also has a **`streaming-catalog`** job. Kinopoisk hides its streaming catalog (season
  lists) from datacenter networks, so on GitHub's runners those tests skip. This job runs only them
  (`Category=StreamingCatalog`) on a **self-hosted runner** with the label `kinopoisk-home` on a residential connection,
  and fails instead of skipping. If that runner is offline, the job waits in the queue and the daily drift report
  fails after the 24-hour queue timeout. It never runs for pull requests.
- **`release.yml`** runs on a `v12.2.X.Y` tag. It rejects tags that do not match the supported Jellyfin version, then
  builds, tests, creates the GitHub release «v12.2.X.Y for Jellyfin 12.2» with the plugin zip and adds the version with
  its checksum to `manifest.json` on `main`.

## Versioning

Releases are aligned with the Jellyfin version they support:

| Plugin version | Jellyfin                              | .NET |
| -------------- | ------------------------------------- | ---- |
| `12.2.X.Y`     | 12.2 — the latest Jellyfin 12 release | 10   |

1. **The first two numbers are the Jellyfin version** the release is built and tested against: `12.2.X.Y` supports
   Jellyfin 12.2. `X` is bumped for new features, `Y` for fixes. The first release is `12.2.0.0`.
2. **Only the latest Jellyfin minor version is supported.** The plugin always targets the newest Jellyfin 12.x release;
   older Jellyfin versions are not supported and their plugin line gets no more releases.
3. **`targetAbi` equals the Jellyfin version** (`12.2.0.0`), so Jellyfin only offers a release to servers it was built for.
   Jellyfin installs a release on servers of that version or newer and always picks the highest compatible version.
4. The rules are enforced by `build/check-version.sh`: CI fails when `targetAbi` in `build.yaml` does not match the
   `Jellyfin.Controller` version in `Directory.Packages.props`, and the release workflow refuses tags that do not start
   with that Jellyfin version.

### Releasing

```bash
git tag v12.2.0.0 && git push origin v12.2.0.0     # next feature: v12.2.1.0, next fix: v12.2.1.1
```

### When Jellyfin publishes a new version

- **Patch release** (e.g. 12.2.1): Dependabot bumps `Jellyfin.*`; nothing else changes and the plugin stays on `12.2.X.Y`.
- **Minor release** (e.g. 12.3): Dependabot's pull request fails the version check on purpose. In that pull request:
    1. update every version in `Directory.Packages.props` to match Jellyfin 12.3's own
       [`Directory.Packages.props`](https://github.com/jellyfin/jellyfin/blob/master/Directory.Packages.props);
    2. set `targetAbi: "12.3.0.0"` in `build.yaml` and `<Version>12.3.0.0</Version>` in `Directory.Build.props`;
    3. update the version table above and the Jellyfin version mentioned in this README;
    4. after merging, release `v12.3.0.0`.

## License

[GPL-3.0](LICENSE), same as the original plugin and Jellyfin.
