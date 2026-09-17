using LitMotion;
using UnityEngine;

namespace LogosSDK.Tween
{
    public static class MotionHandleCompat
    {
        // Thay AsyncWaitForCompletion của thư viện tween cũ: motion bị huỷ (AddTo — object bị destroy) thì await
        // vẫn trả về êm. ToAwaitable() mặc định lại ném OperationCanceledException.
        public static Awaitable WaitAsync(this MotionHandle handle)
        {
            return handle.ToAwaitable(CancelBehavior.None, cancelAwaitOnMotionCanceled: false);
        }
    }
}
