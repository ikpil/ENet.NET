namespace ENet.NET
{
    public static class ENetProtocolCommand
    {
        public const byte ENET_PROTOCOL_COMMAND_NONE = 0;
        public const byte ENET_PROTOCOL_COMMAND_ACKNOWLEDGE = 1;
        public const byte ENET_PROTOCOL_COMMAND_CONNECT = 2;
        public const byte ENET_PROTOCOL_COMMAND_VERIFY_CONNECT = 3;
        public const byte ENET_PROTOCOL_COMMAND_DISCONNECT = 4;
        public const byte ENET_PROTOCOL_COMMAND_PING = 5;
        public const byte ENET_PROTOCOL_COMMAND_SEND_RELIABLE = 6;
        public const byte ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE = 7;
        public const byte ENET_PROTOCOL_COMMAND_SEND_FRAGMENT = 8;
        public const byte ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED = 9;
        public const byte ENET_PROTOCOL_COMMAND_BANDWIDTH_LIMIT = 10;
        public const byte ENET_PROTOCOL_COMMAND_THROTTLE_CONFIGURE = 11;
        public const byte ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE_FRAGMENT = 12;
        public const byte ENET_PROTOCOL_COMMAND_COUNT = 13;

        public const byte ENET_PROTOCOL_COMMAND_MASK = 0x0F;
    }
}