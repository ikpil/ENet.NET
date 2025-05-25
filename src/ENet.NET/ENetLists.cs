using System.Collections.Generic;

namespace ENet.NET
{
    public static class ENetLists
    {
        // =======================================================================//
        // !
        // ! List
        // !
        // =======================================================================//
        public static bool IsEmpty<T>(this LinkedList<T> list)
        {
            return 0 >= list.Count;
        }

        public static LinkedListNode<T> Unlink<T>(this LinkedListNode<T> position)
        {
            if (null != position.List)
                position.List.Remove(position);

            return position;
        }

        public static void AddAfter<T>(this LinkedListNode<T> position, LinkedListNode<T> data)
        {
            position.List.AddAfter(position, data);
        }

        public static LinkedListNode<T> MoveBefore<T>(this LinkedListNode<T> position, LinkedListNode<T> first, LinkedListNode<T> last)
        {
            var list = position.List;
            var current = first;
            while (null != current)
            {
                var next = current.Next;
                list.Remove(current);
                list.AddBefore(position, current);

                if (current == last)
                    break;

                current = next;
            }

            return first;
        }
    }
}