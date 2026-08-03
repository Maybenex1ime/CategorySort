#!/usr/bin/env bash
# Compile-check toàn bộ script C# NGOÀI Unity, bằng Roslyn đi kèm Unity Hub.
# Nhanh hơn nhiều so với chờ Editor import + compile, và chạy được cả khi Unity đang đóng.
#
#   ./compilecheck.sh
#
# Tách làm 2 assembly đúng như Unity: Assembly-CSharp (domain + view, cần DOTween) và
# Assembly-CSharp-Editor (tool level, cần UnityEditor). Không tách thì xung khắc reference:
# DOTween.dll build theo mscorlib, còn UnityEditor.dll theo netstandard.
set -euo pipefail
cd "$(dirname "$0")"

UNITY_VERSION="$(tr -d '\r' < ProjectSettings/ProjectVersion.txt | sed -n 's/^m_EditorVersion: //p')"
DATA="/c/Program Files/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Data"
[ -d "$DATA" ] || { echo "Không thấy Unity $UNITY_VERSION: $DATA"; exit 1; }

DOTNET="$DATA/NetCoreRuntime/dotnet.exe"
CSC="$DATA/DotNetSdkRoslyn/csc.dll"
MAN="$DATA/Managed/UnityEngine"
API="$DATA/MonoBleedingEdge/lib/mono/4.7.1-api"
NS="$DATA/NetStandard"
OUT="$PWD/Temp/compilecheck"
mkdir -p "$OUT"

# Unity.InputSystem.dll chỉ có sau khi Editor import package. Worktree thường chưa có
# Library/ riêng → mượn của repo chính (cùng manifest, cùng version).
INPUTSYS="$PWD/Library/ScriptAssemblies/Unity.InputSystem.dll"
[ -f "$INPUTSYS" ] || INPUTSYS="$(git rev-parse --git-common-dir)/../Library/ScriptAssemblies/Unity.InputSystem.dll"
[ -f "$INPUTSYS" ] || { echo "Không thấy Unity.InputSystem.dll — mở project bằng Unity một lần cho nó import package."; exit 1; }

w() { cygpath -w "$1"; }

# ---- Assembly-CSharp: domain + view (+ DOTween) ----
{
  echo "-nologo"; echo "-target:library"; echo "-langversion:latest"; echo "-nostdlib"
  echo "-define:UNITY_EDITOR;UNITY_5_3_OR_NEWER"
  echo "-out:\"$(w "$OUT/game.dll")\""
  for n in mscorlib System System.Core System.Xml; do echo "-r:\"$(w "$API/$n.dll")\""; done
  echo "-r:\"$(w "$API/Facades/netstandard.dll")\""      # cầu nối giữa hai thế giới ref
  for f in "$MAN"/UnityEngine*.dll; do echo "-r:\"$(w "$f")\""; done
  echo "-r:\"$(w "$INPUTSYS")\""
  echo "-r:\"$(w "$PWD/Assets/Plugins/Demigiant/DOTween/DOTween.dll")\""
  # mọi .cs dưới Assets/Prototype trừ Editor/ — đúng cách Unity gom Assembly-CSharp
  find "$PWD/Assets/Prototype" -name '*.cs' -not -path '*/Editor/*' | while read -r f; do
    echo "\"$(w "$f")\""
  done
} > "$OUT/game.rsp"

# ---- Assembly-CSharp-Editor: tool xếp level ----
{
  echo "-nologo"; echo "-target:library"; echo "-langversion:latest"; echo "-nostdlib"
  echo "-define:UNITY_EDITOR;UNITY_5_3_OR_NEWER"
  echo "-out:\"$(w "$OUT/editor.dll")\""
  echo "-r:\"$(w "$NS/ref/2.1.0/netstandard.dll")\""
  for f in "$NS"/compat/2.1.0/shims/netstandard/*.dll; do [ -f "$f" ] && echo "-r:\"$(w "$f")\""; done
  for f in "$MAN"/UnityEngine*.dll "$MAN"/UnityEditor*.dll; do echo "-r:\"$(w "$f")\""; done
  echo "\"$(w "$PWD/Assets/Prototype/PrototypeDomain.cs")\""
  echo "\"$(w "$PWD/Assets/Prototype/Editor/LevelEditorWindow.cs")\""
} > "$OUT/editor.rsp"

fail=0
for a in game editor; do
  rm -f "$OUT/$a.dll"
  if "$DOTNET" "$CSC" "@$(w "$OUT/$a.rsp")"; then
    echo "  $a.dll OK  ($(stat -c %s "$OUT/$a.dll") bytes)"
  else
    echo "  $a FAIL"; fail=1
  fi
done
exit $fail
