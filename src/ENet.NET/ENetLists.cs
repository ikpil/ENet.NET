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
        public static LinkedListNode<T> enet_list_begin<T>(LinkedList<T> list)
        {
            return list.First;
        }

        public static LinkedListNode<T> enet_list_end<T>(LinkedList<T> list)
        {
            return list.Last;
        }

        public static bool enet_list_empty<T>(LinkedList<T> list)
        {
            return enet_list_begin(list) == enet_list_end(list);
        }

        public static LinkedListNode<T> enet_list_next<T>(LinkedListNode<T> iterator)
        {
            return iterator.Next;
        }

        public static LinkedListNode<T> enet_list_previous<T>(LinkedListNode<T> iterator)
        {
            return iterator.Previous;
        }

        public static T enet_list_front<T>(LinkedList<T> list)
        {
            return list.First.Value;
        }

        public static T enet_list_back<T>(LinkedList<T> list)
        {
            return list.Last.Value;
        }


        public static void enet_list_clear<T>(LinkedList<T> list)
        {
            list.Clear();
        }

        public static LinkedListNode<T> enet_list_insert<T>(LinkedListNode<T> position, T data)
        {
            return position.List.AddAfter(position, data);
        }
        
        public static LinkedListNode<T> enet_list_insert<T>(LinkedListNode<T> position, LinkedListNode<T> data)
        {
            position.List.AddAfter(position, data);
            return data;
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