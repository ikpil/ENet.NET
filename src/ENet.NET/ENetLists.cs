namespace ENet.NET;

public static class ENetLists
{
    // =======================================================================//
    // !
    // ! List
    // !
    // =======================================================================//
    public static ENetListNode<T> enet_list_begin<T>(ref ENetList<T> list)
    {
        return list.sentinel.next;
    }

    public static ENetListNode<T> enet_list_end<T>(ref ENetList<T> list)
    {
        return list.sentinel;
    }

    public static bool enet_list_empty<T>(ref ENetList<T> list)
    {
        return enet_list_begin(ref list) == enet_list_end(ref list);
    }

    public static ENetListNode<T> enet_list_next<T>(ENetListNode<T> iterator)
    {
        return iterator.next;
    }

    public static ENetListNode<T> enet_list_previous<T>(ENetListNode<T> iterator)
    {
        return iterator.previous;
    }

    public static ref T enet_list_front<T>(ref ENetList<T> list)
    {
        return ref list.sentinel.next.value;
    }

    public static ref T enet_list_back<T>(ref ENetList<T> list)
    {
        return ref list.sentinel.previous.value;
    }


    public static void enet_list_clear<T>(ref ENetList<T> list)
    {
        list.sentinel.next = list.sentinel;
        list.sentinel.previous = list.sentinel;
    }

    public static ENetListNode<T> enet_list_insert<T>(ENetListNode<T> position, object data)
    {
        ENetListNode<T> result = (ENetListNode<T>)data;

        result.previous = position.previous;
        result.next = position;

        result.previous.next = result;
        position.previous = result;

        return result;
    }

    public static T enet_list_remove<T>(ENetListNode<T> position)
    {
        position.previous.next = position.next;
        position.next.previous = position.previous;

        return position.value;
    }

    public static ENetListNode<T> enet_list_move<T>(ENetListNode<T> position, object dataFirst, object dataLast)
    {
        ENetListNode<T> first = (ENetListNode<T>)dataFirst;
        ENetListNode<T> last = (ENetListNode<T>)dataLast;

        first.previous.next = last.next;
        last.next.previous = first.previous;

        first.previous = position.previous;
        last.next = position;

        first.previous.next = first;
        position.previous = last;

        return first;
    }

    public static int enet_list_size<T>(ref ENetList<T> list)
    {
        int size = 0;
        ENetListNode<T> position;

        for (position = enet_list_begin(ref list); position != enet_list_end(ref list); position = enet_list_next(position))
        {
            ++size;
        }

        return size;
    }
}