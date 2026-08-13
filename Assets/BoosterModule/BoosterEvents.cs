namespace BoosterModule
{
    public struct BoosterAddedEvent
    {
        public BoosterId Id;
        public int Count;

        public BoosterAddedEvent(BoosterId id, int count)
        {
            Id = id;
            Count = count;
        }
    }

    public struct BoosterUseEvent
    {
        public BoosterId Id;

        public BoosterUseEvent(BoosterId id)
        {
            Id = id;
        }
    }

    public struct BoosterActivatedEvent
    {
        public BoosterId Id;

        public BoosterActivatedEvent(BoosterId id)
        {
            Id = id;
        }
    }

    public struct BoosterInventoryChangedEvent
    {
        public BoosterId Id;
        public int CurrentCount;

        public BoosterInventoryChangedEvent(BoosterId id, int currentCount)
        {
            Id = id;
            CurrentCount = currentCount;
        }
    }
    
    public struct BoosterExhaustedEvent
    {
        public BoosterId Id;
        public int CurrentCount;

        public BoosterExhaustedEvent(BoosterId id, int currentCount)
        {
            Id = id;
            CurrentCount = currentCount;
        }
    }
}
