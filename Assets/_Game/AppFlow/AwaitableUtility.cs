using UnityEngine;

namespace WordStack.Meta.AppFlow
{
    internal static class AwaitableUtility
    {
        /// <summary>
        /// Awaitable đã hoàn tất. Tạo mới mỗi lần gọi — Awaitable của Unity chỉ
        /// await được đúng một lần, cache lại dùng chung sẽ hỏng ở lần thứ hai.
        /// </summary>
        public static Awaitable Completed()
        {
            AwaitableCompletionSource completionSource = new AwaitableCompletionSource();
            completionSource.SetResult();
            return completionSource.Awaitable;
        }
    }
}
