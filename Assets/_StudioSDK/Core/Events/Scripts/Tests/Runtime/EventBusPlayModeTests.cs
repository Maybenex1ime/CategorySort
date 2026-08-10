using System.Collections;
using System.Threading.Tasks;
using LogosSDK.Core.Events;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EventBus.Tests.Runtime
{
    public class EventBusPlayModeTests
    {
        private struct TestEvent
        {
            public int Value;
        }

        [TearDown]
        public void TearDown()
        {
            InvokerStore<TestEvent>.Invoker.Clear();
        }

        [Test]
        public void Fire_WithNoListeners_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => Bus.Global.Fire(new TestEvent { Value = 42 }));
        }

        [Test]
        public void Fire_WithOneListener_InvokesListener()
        {
            // Arrange
            int callCount = 0;
            int receivedValue = 0;
            void Handler(TestEvent evt)
            {
                callCount++;
                receivedValue = evt.Value;
            }

            Bus.Global.On<TestEvent>(Handler);

            // Act
            Bus.Global.Fire(new TestEvent { Value = 42 });

            // Assert
            Assert.AreEqual(1, callCount);
            Assert.AreEqual(42, receivedValue);

            // Cleanup
            Bus.Global.Off<TestEvent>(Handler);
        }

        [Test]
        public void Fire_WithTwoListeners_InvokesBoth()
        {
            // Arrange
            int callCount = 0;
            void HandlerA(TestEvent evt) => callCount++;
            void HandlerB(TestEvent evt) => callCount++;

            Bus.Global.On<TestEvent>(HandlerA);
            Bus.Global.On<TestEvent>(HandlerB);

            // Act
            Bus.Global.Fire(new TestEvent());

            // Assert
            Assert.AreEqual(2, callCount);

            // Cleanup
            Bus.Global.Off<TestEvent>(HandlerA);
            Bus.Global.Off<TestEvent>(HandlerB);
        }

        [Test]
        public void Off_RemovesListener()
        {
            // Arrange
            int callCount = 0;
            void Handler(TestEvent evt) => callCount++;

            Bus.Global.On<TestEvent>(Handler);
            Bus.Global.Off<TestEvent>(Handler);

            // Act
            Bus.Global.Fire(new TestEvent());

            // Assert
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void ListenerCount_ReturnsCorrectCount()
        {
            // Arrange
            void HandlerA(TestEvent evt) { }
            void HandlerB(TestEvent evt) { }

            // Act & Assert - Initial state
            Assert.AreEqual(0, Bus.Global.ListenerCount<TestEvent>());
            Assert.IsFalse(Bus.Global.HasListeners<TestEvent>());

            // Act & Assert - After adding listeners
            Bus.Global.On<TestEvent>(HandlerA);
            Assert.AreEqual(1, Bus.Global.ListenerCount<TestEvent>());
            Assert.IsTrue(Bus.Global.HasListeners<TestEvent>());

            Bus.Global.On<TestEvent>(HandlerB);
            Assert.AreEqual(2, Bus.Global.ListenerCount<TestEvent>());

            // Act & Assert - After removing listeners
            Bus.Global.Off<TestEvent>(HandlerA);
            Assert.AreEqual(1, Bus.Global.ListenerCount<TestEvent>());
            Assert.IsTrue(Bus.Global.HasListeners<TestEvent>());

            Bus.Global.Off<TestEvent>(HandlerB);
            Assert.AreEqual(0, Bus.Global.ListenerCount<TestEvent>());
            Assert.IsFalse(Bus.Global.HasListeners<TestEvent>());
        }

        [UnityTest]
        public IEnumerator OnAsync_InvokesAsyncHandler()
        {
            // Arrange
            bool handlerCalled = false;
            ValueTask AsyncHandler(TestEvent evt)
            {
                handlerCalled = true;
                return default;
            }

            Bus.Global.OnAsync<TestEvent>(AsyncHandler);

            // Act
            Bus.Global.Fire(new TestEvent());

            yield return null;

            // Assert
            Assert.IsTrue(handlerCalled);

            // Cleanup
            Bus.Global.OffAsync<TestEvent>(AsyncHandler);
        }

        [Test]
        public void EventOrder_DeterminesExecutionOrder()
        {
            // Arrange
            int executionOrder = 0;
            int firstHandlerOrder = 0;
            int secondHandlerOrder = 0;
            int thirdHandlerOrder = 0;

            void HighPriorityHandler(TestEvent evt) => firstHandlerOrder = ++executionOrder;
            void MediumPriorityHandler(TestEvent evt) => secondHandlerOrder = ++executionOrder;
            void LowPriorityHandler(TestEvent evt) => thirdHandlerOrder = ++executionOrder;

            // Register handlers in reverse order
            Bus.Global.On<TestEvent>(LowPriorityHandler, 30);
            Bus.Global.On<TestEvent>(MediumPriorityHandler, 20);
            Bus.Global.On<TestEvent>(HighPriorityHandler, 10);

            // Act
            Bus.Global.Fire(new TestEvent());

            // Assert
            Assert.AreEqual(1, firstHandlerOrder);
            Assert.AreEqual(2, secondHandlerOrder);
            Assert.AreEqual(3, thirdHandlerOrder);

            // Cleanup
            Bus.Global.Off<TestEvent>(HighPriorityHandler);
            Bus.Global.Off<TestEvent>(MediumPriorityHandler);
            Bus.Global.Off<TestEvent>(LowPriorityHandler);
        }
    }
} 