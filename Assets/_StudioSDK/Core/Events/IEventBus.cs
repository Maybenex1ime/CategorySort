using System;

namespace LogosSDK.Core.Events
{
    public interface IEventBus
    {
        void Fire<T>(T evt) where T : struct;
        void On<T>(Action<T> handler) where T : struct;
        void Off<T>(Action<T> handler) where T : struct;
    }
}
