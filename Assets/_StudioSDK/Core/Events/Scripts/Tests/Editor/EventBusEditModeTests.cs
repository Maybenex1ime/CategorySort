using System.Threading.Tasks;
using LogosSDK.Core.Events;
using NUnit.Framework;

namespace EventBus.Tests.Editor
{
    public class EventBusEditModeTests
    {
        private struct TestEvent
        {
            public int Value;
        }

        [Test]
        public void Bus_Global_IsNotNull()
        {
            // Assert
            Assert.IsNotNull(Bus.Global);
        }

        [Test]
        public void GlobalBus_HasListeners_ReturnsFalseWhenNoListeners()
        {
            // Assert
            Assert.IsFalse(Bus.Global.HasListeners<TestEvent>());
        }

        [Test]
        public void GlobalBus_ListenerCount_ReturnsZeroWhenNoListeners()
        {
            // Assert
            Assert.AreEqual(0, Bus.Global.ListenerCount<TestEvent>());
        }

        [Test]
        public void GlobalBus_On_RegistersHandler()
        {
            // Arrange
            void Handler(TestEvent evt) { }

            // Act
            Bus.Global.On<TestEvent>(Handler);

            // Assert
            Assert.IsTrue(Bus.Global.HasListeners<TestEvent>());
            Assert.AreEqual(1, Bus.Global.ListenerCount<TestEvent>());

            // Cleanup
            Bus.Global.Off<TestEvent>(Handler);
        }

        [Test]
        public void GlobalBus_OnAsync_RegistersAsyncHandler()
        {
            // Arrange
            async ValueTask AsyncHandler(TestEvent evt) { await Task.Yield(); }

            // Act
            Bus.Global.OnAsync<TestEvent>(AsyncHandler);

            // Assert
            Assert.IsTrue(Bus.Global.HasListeners<TestEvent>());
            Assert.AreEqual(1, Bus.Global.ListenerCount<TestEvent>());

            // Cleanup
            Bus.Global.OffAsync<TestEvent>(AsyncHandler);
        }

        [Test]
        public void GlobalBus_Off_UnregistersHandler()
        {
            // Arrange
            void Handler(TestEvent evt) { }
            Bus.Global.On<TestEvent>(Handler);

            // Act
            Bus.Global.Off<TestEvent>(Handler);

            // Assert
            Assert.IsFalse(Bus.Global.HasListeners<TestEvent>());
            Assert.AreEqual(0, Bus.Global.ListenerCount<TestEvent>());
        }

        [Test]
        public void GlobalBus_OffAsync_UnregistersAsyncHandler()
        {
            // Arrange
            async ValueTask AsyncHandler(TestEvent evt) { await Task.Yield(); }
            Bus.Global.OnAsync<TestEvent>(AsyncHandler);

            // Act
            Bus.Global.OffAsync<TestEvent>(AsyncHandler);

            // Assert
            Assert.IsFalse(Bus.Global.HasListeners<TestEvent>());
            Assert.AreEqual(0, Bus.Global.ListenerCount<TestEvent>());
        }

        [Test]
        public void GlobalBus_On_ChainableInterface()
        {
            // Arrange
            void HandlerA(TestEvent evt) { }
            void HandlerB(TestEvent evt) { }

            // Act
            Bus.Global
                .On<TestEvent>(HandlerA)
                .On<TestEvent>(HandlerB);

            // Assert
            Assert.AreEqual(2, Bus.Global.ListenerCount<TestEvent>());

            // Cleanup
            Bus.Global.Off<TestEvent>(HandlerA);
            Bus.Global.Off<TestEvent>(HandlerB);
        }

        [Test]
        public void GlobalBus_OnAsync_ChainableInterface()
        {
            // Arrange
            async ValueTask AsyncHandlerA(TestEvent evt) { await Task.Yield(); }
            async ValueTask AsyncHandlerB(TestEvent evt) { await Task.Yield(); }

            // Act
            Bus.Global
                .OnAsync<TestEvent>(AsyncHandlerA)
                .OnAsync<TestEvent>(AsyncHandlerB);

            // Assert
            Assert.AreEqual(2, Bus.Global.ListenerCount<TestEvent>());

            // Cleanup
            Bus.Global.OffAsync<TestEvent>(AsyncHandlerA);
            Bus.Global.OffAsync<TestEvent>(AsyncHandlerB);
        }

        [Test]
        public void GlobalBus_On_WithNullHandler_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<System.ArgumentNullException>(() => Bus.Global.On<TestEvent>(null));
        }

        [Test]
        public void GlobalBus_OnAsync_WithNullHandler_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<System.ArgumentNullException>(() => Bus.Global.OnAsync<TestEvent>(null));
        }

        [Test]
        public void GlobalBus_Off_WithNullHandler_DoesNotThrow()
        {
            // Assert
            Assert.DoesNotThrow(() => Bus.Global.Off<TestEvent>(null));
        }

        [Test]
        public void GlobalBus_OffAsync_WithNullHandler_DoesNotThrow()
        {
            // Assert
            Assert.DoesNotThrow(() => Bus.Global.OffAsync<TestEvent>(null));
        }
    }
} 