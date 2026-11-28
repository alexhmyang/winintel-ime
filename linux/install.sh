#!/bin/sh
# 数萌AI服务器 (winintel) / Alex YANG · GPL-3.0-or-later
set -eu
here=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
prefix="${XDG_DATA_HOME:-$HOME/.local/share}/winintel-ime"
test -f "$here/dictionary.bin" || { echo "Missing dictionary.bin"; exit 1; }
python3 -c 'import gi; gi.require_version("IBus","1.0"); gi.require_version("Gtk","3.0")'
mkdir -p "$prefix" "${XDG_DATA_HOME:-$HOME/.local/share}/ibus/component"
cp "$here/winintel.py" "$here/libwinintel-engine.so" "$here/"*.tsv "$here/dictionary.bin" "$prefix/"
cp "$here/LICENSE" "$prefix/"
for notice in THIRD_PARTY_NOTICES.md dictionary-provenance.json Dictionary-notice.md; do
  test ! -f "$here/$notice" || cp "$here/$notice" "$prefix/"
done
python3 - "$prefix" "${XDG_DATA_HOME:-$HOME/.local/share}/ibus/component/winintel.xml" <<'PY'
import sys
import xml.etree.ElementTree as ET
from xml.sax.saxutils import escape
prefix, output = sys.argv[1:]
c = ET.Element("component")
for key, value in {"name":"org.freedesktop.IBus.Winintel","description":"数萌 AI 输入法",
                   "exec":f'python3 "{prefix}/winintel.py" --ibus',
                   "version":"0.5.16","author":"Alex YANG","license":"GPL-3.0-or-later",
                   "homepage":"https://github.com/alexhmyang/winintel-ime"}.items():
    ET.SubElement(c,key).text=value
e=ET.SubElement(ET.SubElement(c,"engines"),"engine")
for key,value in {"name":"winintel","longname":"数萌 AI","description":"离线拼音 · 25候选",
                  "language":"zh","layout":"us","symbol":"萌"}.items():
    ET.SubElement(e,key).text=value
ET.ElementTree(c).write(output,encoding="utf-8",xml_declaration=True)
PY
echo "Installed. Restart IBus at your convenience, then add 数萌 AI in input settings."
