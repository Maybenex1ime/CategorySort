using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LogosSDK.Core.Events;
using BoosterModule;

namespace BoosterModule.Tests
{
    public class BoosterModuleTests
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            PlayerPrefs.DeleteAll();
            // Clean up any existing instance
            if (BoosterManager.Instance != null)
                Object.DestroyImmediate(BoosterManager.Instance.gameObject);

            var go = new GameObject("BoosterManager");
            go.AddComponent<BoosterManager>();
            
            yield return null; // Wait for Awake/OnEnable
            
            Assert.IsNotNull(BoosterManager.Instance, "BoosterManager.Instance should be set after initialization");
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (BoosterManager.Instance != null)
                Object.DestroyImmediate(BoosterManager.Instance.gameObject);
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator BoosterAdded_IncrementsInventory()
        {
            int receivedCount = -1;
            Bus.Global.On<BoosterInventoryChangedEvent>(evt => 
            {
                receivedCount = evt.CurrentCount;
            });

            Bus.Global.Fire(new BoosterAddedEvent(BoosterId.Hand, 5));

            yield return null; // Ensure event is processed

            Assert.AreEqual(5, receivedCount);
        }

        [UnityTest]
        public IEnumerator UseBooster_DecrementsInventoryAndTriggers()
        {
            Bus.Global.Fire(new BoosterAddedEvent(BoosterId.AddQueue, 1));
            yield return null;

            bool triggered = false;
            Bus.Global.On<BoosterActivatedEvent>(evt => triggered = true);

            Bus.Global.Fire(new BoosterUseEvent(BoosterId.Hand));
            yield return null;

            Assert.IsTrue(triggered);
        }
    }
}
