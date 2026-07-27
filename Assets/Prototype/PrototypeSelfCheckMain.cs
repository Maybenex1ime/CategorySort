// Entry point cho self-check chạy NGOÀI Unity (csc/dotnet). Unity bỏ qua nhờ #if.
#if !UNITY_5_3_OR_NEWER
using System;

static class PrototypeSelfCheckMain
{
    static int Main()
    {
        try
        {
            CategorySort.Prototype.SelfCheck.Run(Console.WriteLine);
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
