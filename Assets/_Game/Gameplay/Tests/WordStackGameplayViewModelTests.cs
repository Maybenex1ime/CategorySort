using LogosGame.Features.Gameplay.Flow;
using NUnit.Framework;
using R3;

namespace WordStack.Meta.Tests
{
    // Notify* trả Awaitable nhưng thân hàm chạy đồng bộ tới hết (xem Completed()
    // trong VM) — test gọi xong đọc CurrentValue ngay được, không cần await.
    public sealed class WordStackGameplayViewModelTests
    {
        [Test]
        public void ProgressBar_FollowsContentReadyAndEvaluation()
        {
            var vm = new WordStackGameplayViewModel();

            _ = vm.StartLevelAsync(new GameplayStartContext { StartingMoves = 10 });
            Assert.AreEqual(0, vm.GroupsCleared.CurrentValue, "vào màn: cleared phải là 0");
            Assert.AreEqual(0, vm.TotalGroups.CurrentValue, "vào màn: total 0 tới khi board báo");

            _ = vm.NotifyLevelContentReadyAsync(3);
            Assert.AreEqual(3, vm.TotalGroups.CurrentValue, "board báo total qua NotifyLevelContentReady");

            // Đi một nước, gom được 1 nhóm — đúng đường máy phase Playing → Evaluating.
            _ = vm.NotifyFirstInteractionAsync();
            _ = vm.NotifyPlayerActionCommittedAsync(new GameplayActionContext { RemainingMoves = 9 });
            _ = vm.NotifyEvaluationCompletedAsync(new GameplayEvaluationResult
            {
                RemainingMoves = 9,
                GroupsCleared = 1,
            });
            Assert.AreEqual(1, vm.GroupsCleared.CurrentValue, "evaluation phải đẩy cleared vào RP");
            Assert.AreEqual(3, vm.TotalGroups.CurrentValue, "total giữ nguyên giữa các nước");

            // Chơi lại cùng màn: reset 0/0 rồi chờ board báo total lần nữa.
            _ = vm.ResetLevelAsync(new GameplayStartContext { StartingMoves = 10 });
            Assert.AreEqual(0, vm.GroupsCleared.CurrentValue, "chơi lại: cleared về 0");
            Assert.AreEqual(0, vm.TotalGroups.CurrentValue, "chơi lại: total về 0 (bar ẩn) tới khi board báo");
        }
    }
}
