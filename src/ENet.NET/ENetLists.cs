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
        
        public static T RemoveAndGetValue<T>(LinkedListNode<T> position)
        {
            position.List.Remove(position);
            return position.Value;
        }

        public static LinkedListNode<T> enet_list_insert<T>(LinkedListNode<T> position, T data)
        {
            return position.List.AddAfter(position, data);
        }

        public static T enet_list_remove<T>(LinkedListNode<T> position)
        {
            position.List.Remove(position);
            return position.Value;
        }

        public static LinkedListNode<T> enet_list_move<T>(LinkedListNode<T> position, LinkedListNode<T> first, LinkedListNode<T> last)
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

        public static int enet_list_size<T>(LinkedList<T> list)
        {
            return list.Count;
        }
    }
}