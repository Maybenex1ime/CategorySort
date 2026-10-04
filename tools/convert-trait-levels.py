#!/usr/bin/env python3
"""Chuyển level định dạng "traits/tiles/keyTiles/board" (tool xuất level) sang định dạng game đang đọc
(LevelData.Parse: id/title/layout.stacks/meaning.groups). Ghi đè tại chỗ; file đã đúng định dạng thì bỏ qua.

    python tools/convert-trait-levels.py [thư mục level, mặc định Assets/_Game/Content/Levels]

Quy ước ánh xạ:
  - id = tên file (Level_3.json -> "Level_3"), title = "Level 3". levelId/title trong file nguồn bị bỏ
    (bản xuất hay để nguyên "Level 1").
  - trait -> group "g<traitId>", text = traitName. tile -> card (id = tileId, art = assetId).
  - keyTile {tileId, assetId, traitId, mergeFrom} = nhóm mergeFrom gom đủ thì gộp thành MỘT thẻ của nhóm traitId
    -> group g<mergeFrom> có "group": "g<traitId>" và "art": assetId (thẻ gộp mang mặt nhóm con).
  - board.columns[c].stacks[r] -> stack pos [c, r]; boxes giữ nguyên thứ tự (phần tử đầu = hộp trên cùng).
  - Không có "moves": runtime dùng mặc định; muốn khác thì thêm "moves" vào file đã chuyển.
"""
import glob
import json
import os
import re
import sys


def convert(src: dict, level_id: str, title: str) -> str:
    merge = {k["mergeFrom"]: k for k in src.get("keyTiles", [])}   # traitId con -> keyTile
    tiles_by_trait = {}
    for t in src["tiles"]:
        tiles_by_trait.setdefault(t["traitId"], []).append(t)

    def q(s):
        return json.dumps(s, ensure_ascii=False)

    out = ["{", f'  "id": {q(level_id)},', f'  "title": {q(title)},',
           '  "note": "Chuyen tu dinh dang traits/tiles bang tools/convert-trait-levels.py.",', "",
           '  "layout": {', '    "stacks": [']
    stacks = []
    for c, col in enumerate(src["board"]["columns"]):
        for r, st in enumerate(col["stacks"]):
            boxes = ",\n".join(
                '          { "slots": [' + ",".join("null" if x is None else q(x) for x in box) + "] }"
                for box in st["boxes"])
            stacks.append(f'      {{ "pos": [{c},{r}], "boxes": [\n{boxes}\n      ]}}')
    out.append(",\n".join(stacks))
    out += ["    ]", "  },", "", '  "meaning": {', '    "groups": [']

    groups = []
    for tr in src["traits"]:
        tid = tr["traitId"]
        head = f'      {{ "id":"g{tid}", "text":{q(tr["traitName"])}'
        if tid in merge:
            head += f', "art":{q(merge[tid]["assetId"])}, "group":"g{merge[tid]["traitId"]}"'
        cards = ",\n".join(f'          {{ "id":{q(t["tileId"])}, "art":{q(t["assetId"])} }}'
                           for t in tiles_by_trait.get(tid, []))
        groups.append(head + ', "cards": [\n' + cards + "\n      ]}")
    out.append(",\n".join(groups))
    out += ["    ]", "  }", "}", ""]
    return "\n".join(out)


def main():
    folder = sys.argv[1] if len(sys.argv) > 1 else os.path.join("Assets", "_Game", "Content", "Levels")
    for path in sorted(glob.glob(os.path.join(folder, "*.json"))):
        with open(path, encoding="utf-8") as f:
            src = json.load(f)
        if "traits" not in src or "board" not in src:
            continue   # đã đúng định dạng game
        stem = os.path.splitext(os.path.basename(path))[0]
        m = re.search(r"(\d+)$", stem)
        title = f"Level {int(m.group(1))}" if m else stem
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(convert(src, stem, title))
        print("converted", path)


if __name__ == "__main__":
    main()
