#!/usr/bin/env bash
# Chạy SelfCheck của luật bàn chơi NGOÀI Unity — vòng phản hồi nhanh nhất khi sửa luật.
# Dùng Roslyn đi kèm Unity Hub, không cần cài .NET SDK riêng, không cần mở Editor.
#
#   ./selfcheck.sh
#
# Fail thì in "SELFCHECK FAIL: <lý do>" và exit 1.
set -euo pipefail
cd "$(dirname "$0")"

UNITY_VERSION="$(tr -d '\r' < ProjectSettings/ProjectVersion.txt | sed -n 's/^m_EditorVersion: //p')"
DATA="/c/Program Files/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Data"
[ -d "$DATA" ] || { echo "Không thấy Unity $UNITY_VERSION trong Hub: $DATA"; exit 1; }

DOTNET="$DATA/NetCoreRuntime/dotnet.exe"
CSC="$DATA/DotNetSdkRoslyn/csc.dll"
REF="$(ls -d "$DATA"/NetCoreRuntime/shared/Microsoft.NETCore.App/* | head -1)"
RUNTIME_VERSION="$(basename "$REF")"
# Build vào Temp/ của repo: đã nằm trong .gitignore, và tránh Application Control
# policy chặn thực thi assembly từ %TEMP% của user.
OUT="$PWD/Temp/wordstack-selfcheck"
mkdir -p "$OUT"

# Response file: đường dẫn Unity có dấu cách nên KHÔNG truyền -r qua dòng lệnh được.
{
  echo "-nologo"; echo "-nostdlib"; echo "-langversion:latest"; echo "-target:exe"
  echo "-out:\"$(cygpath -w "$OUT/selfcheck.dll")\""
  for f in "$REF"/System.*.dll "$REF"/netstandard.dll "$REF"/mscorlib.dll; do
    case "$f" in *Native*.dll) continue ;; esac      # dll native, không có managed metadata
    [ -f "$f" ] && echo "-r:\"$(cygpath -w "$f")\""
  done
  # Cả thư mục Domain/ — nó cố ý không import UnityEngine nên csc nuốt được nguyên khối.
  find "$PWD/Assets/_Game/Board/Domain" -name '*.cs' | while read -r f; do
    echo "\"$(cygpath -w "$f")\""
  done
} > "$OUT/csc.rsp"

cat > "$OUT/selfcheck.runtimeconfig.json" <<EOF
{ "runtimeOptions": { "tfm": "net6.0", "framework": { "name": "Microsoft.NETCore.App", "version": "$RUNTIME_VERSION" } } }
EOF

"$DOTNET" "$CSC" "@$(cygpath -w "$OUT/csc.rsp")"
"$DOTNET" "$(cygpath -w "$OUT/selfcheck.dll")" "$(cygpath -w "$PWD/Assets/_Game/Content/Levels")"
