#!/bin/sh
# 数萌AI服务器 (winintel) / Alex YANG · GPL-3.0-or-later
set -eu
here=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
assets="${1:?Usage: sh build.sh /path/to/offline-assets [output-directory]}"
output="${2:-$here/build}"
cmake -S "$here/../portable" -B "$output/engine" -DCMAKE_BUILD_TYPE=Release
cmake --build "$output/engine" --parallel
ctest --test-dir "$output/engine" --output-on-failure
mkdir -p "$output/package"
cp "$output/engine/libwinintel-engine.so" "$here/winintel.py" "$here/install.sh" "$output/package/"
for f in dictionary.bin character-readings.tsv name-parts.tsv english-words.tsv industry-words.tsv hot-chinese.tsv; do
  cp "$assets/$f" "$output/package/"
done
cp "$here/../LICENSE" "$output/package/"
cp "$here/../THIRD_PARTY_NOTICES.md" "$output/package/"
cp "$here/../data/rime-ice/provenance.json" "$output/package/dictionary-provenance.json"
cp "$here/../windows/Legal/Dictionary-notice.md" "$output/package/"
tar -czf "$output/winintel-ime-linux-0.5.16-$(uname -m).tar.gz" -C "$output/package" .
