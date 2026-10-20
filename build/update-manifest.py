#!/usr/bin/env python3
"""Adds a release to manifest.json (the Jellyfin plugin repository file), newest first."""
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

MANIFEST = Path(__file__).resolve().parent.parent / "manifest.json"


def main(version: str, source_url: str, checksum: str, target_abi: str, changelog: str) -> None:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    plugin = manifest[0]
    versions = [v for v in plugin["versions"] if v["version"] != version]
    versions.insert(0, {
        "version": version,
        "changelog": changelog,
        "targetAbi": target_abi,
        "sourceUrl": source_url,
        "checksum": checksum,
        "timestamp": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
    })
    plugin["versions"] = versions
    MANIFEST.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    if len(sys.argv) != 6:
        sys.exit("usage: update-manifest.py <version> <sourceUrl> <md5> <targetAbi> <changelog>")
    main(*sys.argv[1:])
