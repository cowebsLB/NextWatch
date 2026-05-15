# Versioning

NextWatch uses [Semantic Versioning](https://semver.org/): `MAJOR.MINOR.PATCH`.

| Source | Location |
|--------|----------|
| Canonical version | [`VERSION`](../VERSION) and `Directory.Build.props` → `<Version>` |
| Changelog | [`CHANGELOG.md`](../CHANGELOG.md) |
| Git tags | `v0.1.0`, `v0.2.0`, … |
| GitHub Releases | Tag push runs [release.yml](../.github/workflows/release.yml) |

## Release checklist

1. Bump `VERSION`, `Directory.Build.props`, and `CHANGELOG.md`.
2. Commit: `chore: release vX.Y.Z`
3. Tag: `git tag -a vX.Y.Z -m "vX.Y.Z"`
4. Push: `git push origin main --tags`
5. GitHub Actions attaches `NextWatch-win-x64.zip` to the release.

## Pre-1.0

While `0.x`, MINOR bumps may include breaking changes. After `1.0.0`, follow strict semver.
