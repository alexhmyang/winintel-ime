#!/bin/sh
# 数萌AI服务器 (winintel) / Alex YANG · GPL-3.0-or-later
set -eu
here=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
assets="${1:?Usage: sh build.sh /path/to/offline-assets [output-directory]}"
output="${2:-$here/build}"
test "$(uname -s)" = Darwin || { echo "Build on macOS with Xcode Command Line Tools"; exit 1; }
test -f "$assets/dictionary.bin"
app="$output/Winintel.app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
cp "$here/Info.plist" "$app/Contents/Info.plist"
for asset in dictionary.bin character-readings.tsv name-parts.tsv english-words.tsv industry-words.tsv hot-chinese.tsv; do
  cp "$assets/$asset" "$app/Contents/Resources/"
done
cp "$here/../LICENSE" "$app/Contents/Resources/"
for notice in "$here/../THIRD_PARTY_NOTICES.md" "$assets/dictionary-provenance.json" "$assets/Dictionary-notice.md"; do
  test ! -f "$notice" || cp "$notice" "$app/Contents/Resources/"
done
xcrun clang++ -std=c++17 -fobjc-arc -arch arm64 -arch x86_64 -mmacosx-version-min=11.0 \
  "$here/Main.mm" "$here/../portable/Bridge.cpp" -framework Cocoa -framework InputMethodKit -framework QuartzCore \
  -o "$app/Contents/MacOS/Winintel"
codesign --force --deep --sign "${WININTEL_SIGN_IDENTITY:--}" "$app"
codesign --verify --deep --strict "$app"
ditto -c -k --keepParent "$app" "$output/winintel-ime-macos-0.5.16.zip"
echo "Built $app. Copy to ~/Library/Input Methods/ and log out/in, then add 数萌 AI in Keyboard settings."
