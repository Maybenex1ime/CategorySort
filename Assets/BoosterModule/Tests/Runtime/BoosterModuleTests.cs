using NUnit.Framework;
using UnityEngine;
using LogosSDK.Core.Events;

namespace BoosterModule.Tests
{
    // BoosterManager giờ là plain C# — test sync, không cần GameObject/yield.
    public class BoosterModuleTests
    {
        private BoosterManager _manager;

        [SetUp]
        public void Setup()
        {
            PlayerPrefs.DeleteKey("BoosterModule_Inventory");
            _manager = new BoosterManager();
        }

        [TearDown]
        public void Teardown()
        {
            _manager.Dispose();
            PlayerPrefs.DeleteKey("BoosterModule_Inventory");
        }

        [Test]
        public void Added_IncrementsInventory_AndFiresChanged()
        {
            int receivedCount = -1;
            System.Action<BoosterInventoryChangedEvent> handler = evt => receivedCount = evt.CurrentCount;
            Bus.Global.On(handler);
            try
            {
                Bus.Global.Fire(new BoosterAddedEvent(BoosterId.Hand, 5));

                Assert.AreEqual(5, receivedCount);
                Assert.AreEqual(5, _manager.GetCount(BoosterId.Hand));
            }
            finally
            {
                Bus.Global.Off(handler);
            }
        }

        [Test]
        public void Use_Decrements_AndFiresActivated()
        {
            Bus.Global.Fire(new BoosterAddedEvent(BoosterId.Hand, 1));

            bool activated = false;
            System.Action<BoosterActivatedEvent> handler = evt => activated = true;
            Bus.Global.On(handler);
            try
            {
                Bus.Global.Fire(new BoosterUseEvent(BoosterId.Hand));

                Assert.IsTrue(activated);
                Assert.AreEqual(0, _manager.GetCount(BoosterId.Hand));
            }
            finally
            {
                Bus.Global.Off(handler);
            }
        }

        [Test]
        public void Use_WhenEmpty_FiresExhausted_NotActivated()
        {
            bool exhausted = false, activated = false;
            System.Action<BoosterExhaustedEvent> onExhausted = evt => exhausted = true;
            System.Action<BoosterActivatedEvent> onActivated = evt => activated = true;
            Bus.Global.On(onExhausted);
            Bus.Global.On(onActivated);
            try
            {
                Bus.Global.Fire(new BoosterUseEvent(BoosterId.Hammer));

                Assert.IsTrue(exhausted);
                Assert.IsFalse(activated);
            }
            finally
            {
                Bus.Global.Off(onExhausted);
                Bus.Global.Off(onActivated);
            }
        }

        [Test]
        public void NewManager_LoadsPersistedInventory_AndSlotViewModelSeesIt()
        {
            Bus.Global.Fire(new BoosterAddedEvent(BoosterId.AddBelt, 3));
            _manager.Dispose();

            _manager = new BoosterManager();
            Assert.AreEqual(3, _manager.GetCount(BoosterId.AddBelt));

            // Initial sync: viewmodel dựng SAU manager phải thấy count ngay,
            // không chờ event changed đầu tiên.
            var vm = new BoosterSlotViewModel(BoosterId.AddBelt);
            try
            {
                Assert.AreEqual(3, vm.Count);
            }
            finally
            {
                vm.Dispose();
            }
        }
    }
}
