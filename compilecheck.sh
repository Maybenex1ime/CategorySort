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
  # mọi .cs dưới Assets/Prototype trừ Editor/ — đúng cách Unity gom Assembly-CSharp.
  # Kèm Contracts vì BoardController báo kết quả màn qua LevelSignals. Contracts cố ý
  # KHÔNG phụ thuộc gì nên nhập thẳng vào thế giới mscorlib này được; kéo EventBus hay
  # WordStack.Meta vào đây thì hỏng (EventBus cần ValueTask — không có trong 4.7.1-api;
  # R3/Reflex là netstandard2.1, xung khắc DOTween). Đó là lý do có target meta riêng.
  find "$PWD/Assets/Prototype" "$PWD/Assets/_Game/Contracts" -name '*.cs' -not -path '*/Editor/*' | while read -r f; do
    echo "\"$(w "$f")\""
  done
} > "$OUT/game.rsp"

# ---- Assembly-CSharp-Editor: tool xếp level + dựng prefab ----
# Editor script giờ đụng tới view (PrefabBuilder dựng prefab từ TileView/BoxView/...), nên gom
# CẢ source runtime vào đây. Đã thử cách đúng-Unity hơn — tham chiếu game.dll thay vì source —
# nhưng game.dll build theo mscorlib còn ref set của UnityEditor là netstandard, csc đòi mscorlib
# cho mọi chữ ký mượn từ nó (CS0012). Rẻ hơn là compile hai lần trong CÙNG một thế giới ref.
{
  echo "-nologo"; echo "-target:library"; echo "-langversion:latest"; echo "-nostdlib"
  echo "-define:UNITY_EDITOR;UNITY_5_3_OR_NEWER"
  echo "-out:\"$(w "$OUT/editor.dll")\""
  for n in mscorlib System System.Core System.Xml; do echo "-r:\"$(w "$API/$n.dll")\""; done
  echo "-r:\"$(w "$API/Facades/netstandard.dll")\""
  for f in "$MAN"/UnityEngine*.dll "$MAN"/UnityEditor*.dll; do echo "-r:\"$(w "$f")\""; done
  echo "-r:\"$(w "$INPUTSYS")\""
  echo "-r:\"$(w "$PWD/Assets/Plugins/Demigiant/DOTween/DOTween.dll")\""
  find "$PWD/Assets/Prototype" "$PWD/Assets/_Game/Contracts" -name '*.cs' | while read -r f; do
    echo "\"$(w "$f")\""
  done
} > "$OUT/editor.rsp"

# ---- WordStack.Meta (+ .Editor): tầng meta và cả stack SDK/module nó dựa vào ----
# Đây là thế giới netstandard2.1 thuần (R3, Reflex, LogosSDK), KHÔNG trộn với hai
# assembly trên: chúng build theo mscorlib 4.7.1, còn R3.dll/Reflex.dll theo
# netstandard2.1 — trộn vào là CS1701 hàng loạt. Tách ra là cách rẻ nhất.
#
# Reflex, TMP, ugui, Addressables chỉ có dạng .dll sau khi Editor import package,
# nên mượn Library/ScriptAssemblies giống cách INPUTSYS ở trên. Chưa mở Unity lần
# nào thì bỏ qua phần này thay vì báo fail — Unity vẫn là bên kiểm cuối.
SA="$PWD/Library/ScriptAssemblies"
[ -d "$SA" ] || SA="$(git rev-parse --git-common-dir)/../Library/ScriptAssemblies"
NS_REF="$NS/ref/2.1.0/netstandard.dll"
PKG="$PWD/Assets/Packages"

meta_ready=1
for d in Reflex Unity.TextMeshPro UnityEngine.UI; do
  [ -f "$SA/$d.dll" ] || meta_ready=0
done

if [ "$meta_ready" = 1 ]; then
  {
    echo "-nologo"; echo "-target:library"; echo "-langversion:latest"; echo "-nostdlib"
    echo "-define:UNITY_EDITOR;UNITY_5_3_OR_NEWER;NET_STANDARD_2_1;NETSTANDARD2_1"
    echo "-nowarn:CS0649"                                   # [SerializeField] private — Unity gán, csc không biết
    echo "-out:\"$(w "$OUT/meta.dll")\""
    echo "-r:\"$(w "$NS_REF")\""
    for f in "$NS"/compat/2.1.0/shims/netfx/*.dll "$NS"/compat/2.1.0/shims/netstandard/*.dll; do
      echo "-r:\"$(w "$f")\""
    done
    for f in "$MAN"/UnityEngine*.dll "$MAN"/UnityEditor*.dll; do echo "-r:\"$(w "$f")\""; done
    echo "-r:\"$(w "$PWD/Assets/Plugins/Demigiant/DOTween/DOTween.dll")\""
    echo "-r:\"$(w "$PKG/R3.1.3.0/lib/netstandard2.1/R3.dll")\""
    echo "-r:\"$(w "$PKG/Microsoft.Bcl.TimeProvider.8.0.0/lib/netstandard2.0/Microsoft.Bcl.TimeProvider.dll")\""
    echo "-r:\"$(w "$PKG/Microsoft.Bcl.AsyncInterfaces.6.0.0/lib/netstandard2.1/Microsoft.Bcl.AsyncInterfaces.dll")\""
    echo "-r:\"$(w "$PKG/System.Threading.Channels.8.0.0/lib/netstandard2.1/System.Threading.Channels.dll")\""
    echo "-r:\"$(w "$PKG/System.ComponentModel.Annotations.5.0.0/lib/netstandard2.1/System.ComponentModel.Annotations.dll")\""
    for d in Reflex Unity.Addressables Unity.ResourceManager Unity.TextMeshPro UnityEngine.UI; do
      [ -f "$SA/$d.dll" ] && echo "-r:\"$(w "$SA/$d.dll")\""
    done
    # Newtonsoft là DLL tiền biên dịch của package, không đi qua ScriptAssemblies.
    NJ="$(ls "$SA/../PackageCache"/com.unity.nuget.newtonsoft-json*/Runtime/Newtonsoft.Json.dll 2>/dev/null | head -1)"
    [ -n "$NJ" ] && echo "-r:\"$(w "$NJ")\""
    # DOTween/Modules đi kèm vì CheatToastView dùng DOFade/DOAnchorPosY trên UI
    # Bỏ Tests/: chúng cần NUnit + TestRunner, chỉ Unity mới dựng nổi ref đó.
    find "$PWD/Assets/_StudioSDK" "$PWD/Assets/_Modules" "$PWD/Assets/_Game" \
         "$PWD/Assets/Plugins/Demigiant/DOTween/Modules" -name '*.cs' -not -path '*/Tests/*' | while read -r f; do
      echo "\"$(w "$f")\""
    done
  } > "$OUT/meta.rsp"
else
  echo "  meta BỎ QUA (chưa có Library/ScriptAssemblies — mở Unity một lần)"
fi

fail=0
for a in game editor $([ "$meta_ready" = 1 ] && echo meta); do
  rm -f "$OUT/$a.dll"
  if "$DOTNET" "$CSC" "@$(w "$OUT/$a.rsp")"; then
    echo "  $a.dll OK  ($(stat -c %s "$OUT/$a.dll") bytes)"
  else
    echo "  $a FAIL"; fail=1
  fi
done
exit $fail
