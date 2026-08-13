namespace WordStack.Meta.AppFlow
{
    /// <summary>
    /// Bắn mỗi khi app flow đổi phase. Cho phép view trong scene phản ứng theo
    /// giai đoạn mà không cần inject AppFlow — HUD dùng để tự ẩn/hiện, overlay
    /// debug dùng để hiển thị.
    ///
    /// Phải là struct: IEventBus ràng buộc `where T : struct`.
    /// </summary>
    public readonly struct AppFlowPhaseChangedEvent
    {
        public AppFlowPhase Phase { get; }

        public AppFlowPhaseChangedEvent(AppFlowPhase phase)
        {
            Phase = phase;
        }
    }
}
