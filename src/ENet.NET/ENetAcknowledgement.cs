

namespace ENet.NET;

public struct ENetAcknowledgement
{
    public ENetListNode acknowledgementList;
    public uint sentTime;
    public ENetProtocol command;
