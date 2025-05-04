namespace ENet.NET
{

    public class ENetListNode<T>
    {
        public T value;
        public ENetListNode<T> next;
        public ENetListNode<T> previous;
    }
}