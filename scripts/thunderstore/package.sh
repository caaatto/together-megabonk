#!/bin/bash

# Builds a Thunderstore package that r2modman can import
# (Settings > Import local mod) or that can be uploaded to Thunderstore.
#
# Output: artifacts/thunderstore/<namespace>-MegabonkTogether-<version>.zip

# Exit on error
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$REPO_ROOT"

NAMESPACE="${THUNDERSTORE_NAMESPACE:-caaatto}"
PACKAGE_NAME="MegabonkTogether"
WEBSITE_URL="https://github.com/caaatto/together-megabonk"
BEPINEX_DEPENDENCY="BepInEx-BepInExPack_IL2CPP-6.0.738"

VERSION="${VERSION:-$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' src/plugin/MegabonkTogether.Plugin.csproj)}"
VERSION="${VERSION#v}"
if ! [[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Thunderstore needs a plain X.Y.Z version, got '$VERSION'"
    exit 1
fi

PYTHON="$(command -v python3 || command -v python || true)"
if [ -z "$PYTHON" ]; then
    echo "python is needed to write the zip"
    exit 1
fi

BUILD_DIR="artifacts/thunderstore/build"
STAGING_DIR="artifacts/thunderstore/staging"
ZIP="artifacts/thunderstore/$NAMESPACE-$PACKAGE_NAME-$VERSION.zip"

echo "Packaging $NAMESPACE-$PACKAGE_NAME $VERSION..."

rm -rf "$BUILD_DIR" "$STAGING_DIR" "$ZIP"
mkdir -p "$STAGING_DIR/$PACKAGE_NAME"

# THUNDERSTORE turns off the self updater, r2modman handles updates
export THUNDERSTORE_BUILD=true
dotnet build src/plugin/MegabonkTogether.Plugin.csproj -c Release -o "$BUILD_DIR"

cp "$BUILD_DIR"/*.dll "$STAGING_DIR/$PACKAGE_NAME/"
cp "$BUILD_DIR"/*.pdb "$STAGING_DIR/$PACKAGE_NAME/"
# Read next to the plugin dll for the in game changelog
cp src/plugin/CHANGELOG.toml "$STAGING_DIR/$PACKAGE_NAME/"

cp images/icon.png "$STAGING_DIR/icon.png"
cp README.md "$STAGING_DIR/README.md"

cat > "$STAGING_DIR/manifest.json" <<EOF
{
  "name": "$PACKAGE_NAME",
  "version_number": "$VERSION",
  "website_url": "$WEBSITE_URL",
  "description": "Play Megabonk over netplay!",
  "dependencies": [
    "$BEPINEX_DEPENDENCY"
  ]
}
EOF

# python's zipfile always writes forward slashes, which r2modman needs
(cd "$STAGING_DIR" && "$PYTHON" -m zipfile -c "$REPO_ROOT/$ZIP" manifest.json icon.png README.md "$PACKAGE_NAME")

echo "Package written to $ZIP"
