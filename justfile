set shell := ["/usr/bin/env", "bash", "-euo", "pipefail", "-c"]

game_dir := env("HOME") / "Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app"
game_data := game_dir / "Contents/Resources/data_sts2_macos_arm64"
mods_dir := game_dir / "Contents/MacOS/mods/KeyboardAccessibility"
decomp_dir := justfile_directory() / "decomp"

[default]
_default:
    @just -f {{justfile()}} --list

fmt:
    csharpier format .

build:
    dotnet build -c Debug

install: build
    mkdir -p "{{ mods_dir }}"
    cp bin/Debug/KeyboardAccessibility.dll "{{ mods_dir }}/"
    cp KeyboardAccessibility.json "{{ mods_dir }}/"

package: build
    #!/usr/bin/env bash
    set -euo pipefail
    PACKAGE_DIR="$(mktemp -d)"
    MOD_DIR="$PACKAGE_DIR/KeyboardAccessibility"
    mkdir -p "$MOD_DIR"
    mkdir -p dist/
    cp bin/Debug/KeyboardAccessibility.dll "$MOD_DIR/"
    cp KeyboardAccessibility.json "$MOD_DIR/"
    cp README.md "$MOD_DIR/"
    TAR_NAME="dist/KeyboardAccessibility-$(date +%Y%m%d).tar.gz"
    tar -czf "$TAR_NAME" -C "$PACKAGE_DIR" KeyboardAccessibility
    rm -rf "$PACKAGE_DIR"
    echo "Packaged: $TAR_NAME"

game-version:
    @strings "{{ game_data }}/sts2.dll" | grep -oE '[0-9]+\.[0-9]+\.[0-9]+\+[0-9a-f]{40}' | head -1

decompile:
    #!/usr/bin/env bash
    set -euo pipefail
    full=$(just -f {{justfile()}} game-version)
    if [[ -z "$full" ]]; then
        echo "error: could not detect game version from sts2.dll" >&2
        exit 1
    fi
    version=$(echo "$full" | sed 's/\(+[0-9a-f]\{8\}\).*/\1/')
    out="{{ decomp_dir }}/$version"
    if [[ -d "$out" ]]; then
        echo "Already decompiled at $out"
        exit 0
    fi
    ~/.dotnet/tools/ilspycmd -p -o "$out" "{{ game_data }}/sts2.dll"
    echo "Decompiled to $out"
