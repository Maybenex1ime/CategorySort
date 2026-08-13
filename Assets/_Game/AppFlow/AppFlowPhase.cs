namespace WordStack.Meta.AppFlow
{
    /// <summary>
    /// App đang ở giai đoạn nào. Manager dùng để chặn intent sai ngữ cảnh
    /// (ví dụ không cho StartGameplay khi đang ở Splash).
    /// </summary>
    public enum AppFlowPhase
    {
        None = 0,
        Boot = 1,
        Splash = 2,
        MainMenu = 3,
        Gameplay = 4,
        Result = 5,
    }
}
