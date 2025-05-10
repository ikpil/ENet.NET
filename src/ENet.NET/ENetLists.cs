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
        
        public static T RemoveAndGet<T>(this LinkedListNode<T> position)
        {
            position.List.Remove(position);
            return position.Value;
        }

        public static LinkedListNode<T> AddAfter<T>(this LinkedListNode<T> position, T data)
        {
            return position.List.AddAfter(position, data);
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