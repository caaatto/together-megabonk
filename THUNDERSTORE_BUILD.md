# Building for Thunderstore / r2modman

The Thunderstore build disables automatic update downloads while keeping update notifications, since r2modman handles the install.

## Package locally

```bash
bash scripts/thunderstore/package.sh
```

This builds the plugin with `THUNDERSTORE_BUILD=true` and writes `artifacts/thunderstore/caaatto-TogetherWeBonk-X.Y.Z.zip`, with the version taken from `<Version>` in `src/plugin/MegabonkTogether.Plugin.csproj`. Import it in r2modman with Settings > Profile > Import local mod. Set `THUNDERSTORE_NAMESPACE` to package under another namespace.

The zip contains `manifest.json`, `icon.png`, `README.md` and a `MegabonkTogether` folder with the plugin dlls and `CHANGELOG.toml`.

## Releases

`.github/workflows/release.yml` builds every package on each push to `main` and on each pull request. Pull request builds can be downloaded from the run's artifacts.

When a push to `main` carries a `<Version>` that has no tag yet, the workflow creates the GitHub release for it, with the release notes taken from `CHANGELOG.toml`. These packages are attached:

- `Megabonk-Together-X.Y.Z.zip`: manual install, also what the in game auto updater downloads
- `Megabonk-Together-Proton-X.Y.Z.zip`: Proton / Steam Deck
- `caaatto-TogetherWeBonk-X.Y.Z.zip`: r2modman / Thunderstore
- `MegabonkTogether-Server-win-x64-X.Y.Z.zip`: self hosted server

To release, bump `<Version>`, add the `CHANGELOG.toml` entry and merge to `main`.
