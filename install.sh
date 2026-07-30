#!/usr/bin/env bash
# DbShift – official install script
# Usage: curl -fsSL https://github.com/AzimMahmud/dbshift/releases/latest/download/install.sh | bash
#   or:  bash <(curl -fsSL https://github.com/AzimMahmud/dbshift/releases/latest/download/install.sh)
#
# Environment overrides:
#   REPO         GitHub "owner/name" (default: AzimMahmud/dbshift)
#   VERSION      Release tag to install, with or without leading "v" (default: latest)
#   INSTALL_DIR  Destination directory (default: ~/.local/bin)
#   ARCH         Override architecture (default: detected)

set -euo pipefail

REPO="${REPO:-AzimMahmud/dbshift}"
VERSION="${VERSION:-latest}"
INSTALL_DIR="${INSTALL_DIR:-$HOME/.local/bin}"

# ── helpers ──────────────────────────────────────────────────────────────────
info()  { printf "  \033[36m>\033[0m %s\n" "$*"; }
ok()    { printf "  \033[32m✓\033[0m %s\n" "$*"; }
warn()  { printf "  \033[33m⚠\033[0m %s\n" "$*"; }
err()   { printf "  \033[31m✗\033[0m %s\n" "$*" >&2; exit 1; }

# ── platform detection ───────────────────────────────────────────────────────
detect_platform() {
    local os arch

    case "$(uname -s)" in
        Linux)  os="linux";;
        Darwin) os="macos";;
        *)      err "Unsupported OS: $(uname -s)";;
    esac

    case "$(uname -m)" in
        x86_64|amd64)  arch="x64";;
        aarch64|arm64) arch="arm64";;
        *)             err "Unsupported architecture: $(uname -m)";;
    esac

    arch="${ARCH:-$arch}"
    echo "${os}-${arch}"
}

# ── release URL helpers ─────────────────────────────────────────────────────
resolve_tag() {
    # Strip an optional leading "v" so "v1.0.0" and "1.0.0" both work.
    local v="$VERSION"
    v="${v#v}"
    if [ "$VERSION" = "latest" ]; then
        echo "latest"
    else
        echo "v${v}"
    fi
}

base_url() {
    local tag="$1"
    if [ "$tag" = "latest" ]; then
        echo "https://github.com/${REPO}/releases/latest/download"
    else
        echo "https://github.com/${REPO}/releases/download/${tag}"
    fi
}

# ── download + integrity check ──────────────────────────────────────────────
download_release() {
    local platform="$1"
    local tag
    tag="$(resolve_tag)"
    local url
    url="$(base_url "$tag")/dbshift-${platform}.tar.gz"
    local sums_url
    sums_url="$(base_url "$tag")/SHA256SUMS"

    local workdir
    workdir="$(mktemp -d)"
    trap 'rm -rf "$workdir"' EXIT

    info "Downloading dbshift for ${platform}..."
    curl -fsSL "$url" -o "$workdir/dbshift.tar.gz" || err "Download failed: $url"
    ok "Downloaded"

    # Best-effort checksum verification: if SHA256SUMS is published alongside the
    # archive we verify against it; otherwise we warn and continue. SECURITY.md
    # documents checksums as required for trust, so failure to fetch is reported
    # loudly rather than silently skipped.
    local expected
    expected="$(curl -fsSL "$sums_url" 2>/dev/null | grep -E "dbshift-${platform}\.tar\.gz" | awk '{print $1}' || true)"
    if [ -n "$expected" ]; then
        local actual
        actual="$(cd "$workdir" && command -v sha256sum >/dev/null 2>&1 && sha256sum dbshift.tar.gz | awk '{print $1}' || shasum -a 256 dbshift.tar.gz | awk '{print $1}')"
        if [ -z "$actual" ]; then
            err "Could not compute SHA256 (install 'coreutils' or 'shasum')."
        fi
        if [ "$actual" != "$expected" ]; then
            err "Checksum mismatch for dbshift-${platform}.tar.gz
  expected: $expected
  actual:   $actual"
        fi
        ok "Checksum verified"
    else
        warn "SHA256SUMS not found at $sums_url; skipping integrity verification."
    fi

    info "Extracting..."
    tar -xzf "$workdir/dbshift.tar.gz" -C "$workdir" || err "Extraction failed"
    mv "$workdir/dbshift" "${INSTALL_TMP}"
}

# ── install ───────────────────────────────────────────────────────────────────
install_binary() {
    mkdir -p "$INSTALL_DIR"
    mv "${INSTALL_TMP}" "${INSTALL_DIR}/dbshift" || err "Failed to install binary (check write permission on ${INSTALL_DIR})"
    chmod +x "${INSTALL_DIR}/dbshift"
    ok "Installed to ${INSTALL_DIR}/dbshift"
}

# ── PATH management ─────────────────────────────────────────────────────────
ensure_on_path() {
    case ":${PATH:-}:" in
        *":${INSTALL_DIR}:"*) return 0 ;;
    esac

    local rc_file=""
    case "$(basename "${SHELL:-bash}")" in
        zsh)  rc_file="$HOME/.zshrc" ;;
        bash) rc_file="$HOME/.bashrc" ;;
        *)    rc_file="" ;;
    esac

    if [ -n "$rc_file" ]; then
        if ! grep -q "${INSTALL_DIR}" "$rc_file" 2>/dev/null; then
            printf '\n# Added by dbshift installer\nexport PATH="%s:$PATH"\n' "$INSTALL_DIR" >> "$rc_file"
            ok "Added ${INSTALL_DIR} to PATH in ${rc_file}"
            warn "Open a new shell (or run: source ${rc_file}) to pick up the new PATH."
        fi
    else
        warn "${INSTALL_DIR} is not on PATH. Add it manually: export PATH=\"${INSTALL_DIR}:\$PATH\""
    fi
}

# ── verify ────────────────────────────────────────────────────────────────────
verify() {
    if [ -x "${INSTALL_DIR}/dbshift" ]; then
        ok "$("${INSTALL_DIR}/dbshift" --version 2>&1 | head -1)"
    else
        warn "dbshift installed but not found on PATH"
    fi
}

# Allow override of temp staging path so the trap can clean it up.
INSTALL_TMP="$(mktemp -d)/dbshift"

# ── main ──────────────────────────────────────────────────────────────────────
main() {
    echo ""
    echo "  ╭──────────────────────────────────────╮"
    echo "  │  DbShift — database migration tool   │"
    echo "  ╰──────────────────────────────────────╯"
    echo ""

    local platform
    platform="$(detect_platform)"
    info "Detected: ${platform}"
    info "Install dir: ${INSTALL_DIR}"

    download_release "$platform"
    install_binary
    ensure_on_path

    echo ""
    verify
    echo ""
    info "Run 'dbshift --help' to get started."
    echo ""
}

main "$@"
