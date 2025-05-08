namespace ENet.NET
{
    public class ENetDefaultAllocator : IENetAllocator
    {
        public T[] malloc<T>(int size) where T : new()
        {
            var array = new T[size];
            if (typeof(T).IsValueType)
                return array;

            for (int i = 0; i < size; ++i)
            {
                array[i] = new T();
            }

            return array;
        }

        public void free<T>(T memory)
        {
            // ..
        }

        public void no_memory()
        {
            // ..
        }
    }
}