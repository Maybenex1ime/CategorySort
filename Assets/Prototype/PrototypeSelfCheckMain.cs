// Entry point cho self-check chạy NGOÀI Unity (csc/dotnet). Unity bỏ qua nhờ #if.
// Đây là vòng phản hồi nhanh nhất khi sửa luật — không phải mở Editor.
// Chạy từ repo root:  <dotnet> <csc.dll> ... && <exe> [đường dẫn thư mục Levels]
#if !UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;

static class PrototypeSelfCheckMain
{
    static int Main(string[] args)
    {
        try
        {
            string levelsDir = args.Length > 0
                ? args[0]
                : Path.Combine("Assets", "_Game", "Content", "Levels");
            // Art KHÔNG đi theo thư mục level: runtime nạp Resources.Load("Art/" + key),
            // tức cố định Assets/Prototype/Resources/Art dù level đã dời sang _Game/Content.
            string artDir = Path.Combine("Assets", "Prototype", "Resources", "Art");

            if (!Directory.Exists(levelsDir))
                throw new Exception("không thấy thư mục level: " + Path.GetFullPath(levelsDir) +
                                    " (chạy từ repo root, hoặc truyền đường dẫn làm tham số)");

            var files = new List<string>(Directory.GetFiles(levelsDir, "*.json"));
            files.Sort(StringComparer.Ordinal);
            var jsons = files.ConvertAll(File.ReadAllText);
            Console.WriteLine("Level: " + string.Join(", ", files.ConvertAll(Path.GetFileName).ToArray()));

            WordStack.Prototype.SelfCheck.Run(
                Console.WriteLine,
                jsons,
                key => File.Exists(Path.Combine(artDir, key + ".png")));
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine("SELFCHECK FAIL: " + e.Message);
            return 1;
        }
    }
}
#endif
