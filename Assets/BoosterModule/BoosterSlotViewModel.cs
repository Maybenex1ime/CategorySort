using System;
using LogosSDK.Core.Events;

namespace BoosterModule
{
    public class BoosterSlotViewModel : IDisposable
    {
        public readonly BoosterId Id;
        public int Count { get; private set; }
        public bool IsUsable => Count > 0;

        public event Action<int> OnCountChanged;

        public BoosterSlotViewModel(BoosterId id)
        {
            Id = id;
            // Đọc count hiện tại ngay lúc dựng — không chờ event "changed" đầu tiên,
            // nếu không HUD hiện 0 sau restart dù save có booster.
            Count = BoosterManager.Instance != null ? BoosterManager.Instance.GetCount(id) : 0;
            Bus.Global.On<BoosterInventoryChangedEvent>(HandleInventoryChanged);
        }

        public void RequestUse()
        {
            if (IsUsable)
            {
                Bus.Global.Fire(new BoosterUseEvent(Id));
            }
            else
            {
                // Clicked while empty: let the game react (e.g. open a purchase flow).
                Bus.Global.Fire(new BoosterExhaustedEvent(Id, Count));
            }
        }

        private void HandleInventoryChanged(BoosterInventoryChangedEvent evt)
        {
            if (evt.Id == Id)
            {
                Count = evt.CurrentCount;
                OnCountChanged?.Invoke(Count);
            }
        }

        public void Dispose()
        {
            Bus.Global.Off<BoosterInventoryChangedEvent>(HandleInventoryChanged);
        }
    }
}
