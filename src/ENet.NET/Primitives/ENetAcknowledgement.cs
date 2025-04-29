using ENet.NET.Protocols;

namespace ENet.NET.Primitives;

public struct ENetAcknowledgement
{
    public ENetListNode acknowledgementList;
    public uint sentTime;
    public ENetProtocol command;
}