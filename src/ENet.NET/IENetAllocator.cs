namespace ENet.NET
{
    public interface IENetAllocator
    {
        T[] malloc<T>(int size) where T : new();
        void free<T>(T memory);
        void no_memory();
    }
}