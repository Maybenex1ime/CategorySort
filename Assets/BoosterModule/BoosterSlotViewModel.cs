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
            Bus.Global.On<BoosterInventoryChangedEvent>(HandleInventoryChanged);
        }

        public void RequestUse()
        {
            if (IsUsable)
            {
                Bus.Global.Fire(new BoosterUseEvent(Id));
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
