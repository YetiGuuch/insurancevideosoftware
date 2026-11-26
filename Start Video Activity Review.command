#!/bin/bash
set -euo pipefail
umask 077
export PATH=/usr/bin:/bin:/usr/sbin:/sbin

if [[ "$(uname -s)" != Darwin ]]; then
  echo "This launcher is for macOS." >&2
  exit 1
fi
version="$(sw_vers -productVersion)"
if [[ "${version%%.*}" -lt 14 ]]; then
  echo "Video Activity Review requires macOS Sonoma 14 or newer." >&2
  exit 1
fi

arch="$(uname -m)"
node_version=v24.11.1
case "$arch" in
  arm64) node_hash=b05aa3a66efe680023f930bd5af3fdbbd542794da5644ca2ad711d68cbd4dc35 ;;
  x86_64)
    arch=x64
    node_hash=096081b6d6fcdd3f5ba0f5f1d44a47e83037ad2e78eada26671c252fe64dd111 ;;
  *) echo "Unsupported Mac processor: $arch" >&2; exit 1 ;;
esac

app_dir="$(cd -- "$(dirname -- "$0")" && pwd -P)"
if [[ ! -f "$app_dir/server.mjs" || ! -f "$app_dir/engine.mjs" || ! -f "$app_dir/index.html" ]]; then
  echo "Extract the entire Video Activity Review ZIP and keep the launcher beside the app files." >&2
  exit 1
fi

data="$HOME/Library/Application Support/VideoActivityReview"
mkdir -p "$data"
node="$data/node-$node_version-$arch"
if [[ ! -x "$node" ]]; then
  staging="$(mktemp -d "$data/node-download.XXXXXXXX")"
  trap 'if [[ -n "${staging:-}" ]]; then rm -rf -- "$staging"; fi' EXIT
  archive="node-$node_version-darwin-$arch.tar.gz"
  echo "Downloading the local launcher runtime (first run only)..."
  curl --fail --location --retry 2 --connect-timeout 20 \
    "https://nodejs.org/dist/$node_version/$archive" --output "$staging/$archive"
  actual="$(shasum -a 256 "$staging/$archive" | awk '{ print $1 }')"
  if [[ "$actual" != "$node_hash" ]]; then
    echo "The launcher runtime download could not be verified." >&2
    exit 1
  fi
  tar -xzf "$staging/$archive" -C "$staging" -- "${archive%.tar.gz}/bin/node"
  mv -f "$staging/${archive%.tar.gz}/bin/node" "$node"
  rm -rf -- "$staging"
  staging=
fi

unset NODE_OPTIONS VIDEO_REVIEW_OLLAMA VIDEO_REVIEW_READY_FILE
export VIDEO_REVIEW_DATA="$data"
export VIDEO_REVIEW_PORT=0
export VIDEO_REVIEW_OPEN_BROWSER=1
echo "Keep this window open while reviewing. Press Control-C to exit."
exec "$node" "$app_dir/server.mjs"
