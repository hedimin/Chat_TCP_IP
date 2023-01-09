namespace ChatApi;
internal static class MessageSerialization
{
    //Every message has several bytes (depends on type of it) at the beginning
    public const int LengthPrefixLength = sizeof(uint);
    private const int MessageTypeLength = sizeof(uint);
    private const int ShortStringLengthPrefix = sizeof(byte);
    private const int LongStringLengthPrefix = sizeof(ushort);
    private const int GuidLength = 16;

    public static int GetMessageLengthPrefixValue(IMessage message)
    {
        if (message is ChatMessage chatMessage)
        {
            return MessageTypeLength + LongStringFieldLength(chatMessage.Text);
        }
        else if (message is BroadcastMessage broadcastMessage)
        {
            return MessageTypeLength + ShortStringFieldLength(broadcastMessage.From)
                                     + LongStringFieldLength(broadcastMessage.Text);
        }
        else if (message is SetNickNameMessage setNickNameMessage)
        {
            return MessageTypeLength + GuidLength + ShortStringFieldLength(setNickNameMessage.NickName);
        }
        else if (message is AckResponceMessage)
        {
            return MessageTypeLength + GuidLength;
        }
        else if (message is NakResponceMessage nakResponceMessage)
        {
            return MessageTypeLength + GuidLength + LongStringFieldLength(nakResponceMessage.Message);
        }
        else if (message is KeepaliveMessage)
        {
            return MessageTypeLength;
        }
        else
        {
            throw new InvalidOperationException("Unknown message type.");
        }
        int ShortStringFieldLength(string value) => ShortStringLengthPrefix + 
            MessagePackSerializer.Serialize(value).Length;
        int LongStringFieldLength(string value) => LongStringLengthPrefix + 
            MessagePackSerializer.Serialize(value).Length;
    }
    public static bool TryReadLongString(ref this SequenceReader<byte> sequenceReader, 
        [NotNullWhen(true)] out string? value)
    {
        value = null;
        if (!sequenceReader.TryReadBigEndian(out short signedLength))
            return false;
        var length = (ushort)signedLength;
        if (!sequenceReader.TryReadByteArray(length, out var bytes))
            return false;
        value = MessagePackSerializer.Deserialize<string>(bytes);
        return true;
    }

    private static bool TryReadShortString(ref this SequenceReader<byte> sequenceReader,
        [NotNullWhen(true)] out string? value)
    {
        value = null;
        if (!sequenceReader.TryRead(out var length))
            return false;
        if (!sequenceReader.TryReadByteArray(length, out var bytes))
            return false;
        value = MessagePackSerializer.Deserialize<string>(bytes);
        return true;
    }

    private static bool TryReadGuid(ref this SequenceReader<byte> sequenceReader,
        [NotNullWhen(true)] out Guid? value)
    {
        value = null;
        if (!sequenceReader.TryReadByteArray(GuidLength, out var bytes))
            return false;

        value = new Guid(bytes);
        return true;
    }

    private static bool TryReadByteArray(ref this SequenceReader<byte> sequenceReader, int length,
        [NotNullWhen(true)] out byte[]? value)
    {
        value = new byte[length];
        if (!sequenceReader.TryCopyTo(value))
            return false;

        sequenceReader.Advance(length);
        return true;
    }

    public static bool TryReadMessage(ref this SequenceReader<byte> sequenceReader, 
        uint maxMessageSize, out IMessage? message)
    {
        message = null;
        if (!sequenceReader.TryReadLengthPrefix(out var lengthPrefix))
            return false;

        if (lengthPrefix > maxMessageSize)
            throw new InvalidCastException("Message is too big");

        if (sequenceReader.Remaining < lengthPrefix)
            return false;

        if (!sequenceReader.TryReadMessageType(out int messageType))
            return false;

        if (messageType == 0)
        {
            if (!sequenceReader.TryReadLongString(out var text))
                return false;

            message = new ChatMessage(text);
            return true;
        }
        else if (messageType == 1)
        {

            if (!sequenceReader.TryReadShortString(out var from))
                return false;

            if (!sequenceReader.TryReadLongString(out var text))
                return false;
            message = new BroadcastMessage(from, text);
            return true;
        }
        else if (messageType == 2)
        {
            message = new KeepaliveMessage();
            return true;
        }
        else if (messageType == 3)
        {
            if (!sequenceReader.TryReadGuid(out var requestId))
                return false;
            if (!sequenceReader.TryReadShortString(out var nickName))
                return false;
            message = new SetNickNameMessage(requestId.Value, nickName);
            return true;
        }
        else if (messageType == 4)
        {
            if (!sequenceReader.TryReadGuid(out var requestId))
                return false;
            message = new AckResponceMessage(requestId.Value);
            return true;
        }
        else if (messageType == 5)
        {
            if (!sequenceReader.TryReadGuid(out var requestId))
                return false;
            if (!sequenceReader.TryReadLongString(out var messageField))
                return false;
            message = new NakResponceMessage(requestId.Value, messageField);
            return true;
        }
        else
        {
            sequenceReader.Advance(lengthPrefix - MessageTypeLength);
            return true;
        }
    }

    public static bool TryReadMessageType(ref this SequenceReader<byte> sequenceReader,
        [NotNullWhen(true)] out int value) => sequenceReader.TryReadBigEndian(out value);

    public static bool TryReadLengthPrefix(ref this SequenceReader<byte> sequenceReader,
        [NotNullWhen(true)] out uint value)
    {
        var result = sequenceReader.TryReadBigEndian(out int signedValue);
        value = (uint)signedValue;
        return result;
    }


    public ref struct SpanWriter
    {
        private readonly Span<byte> _span;
        private int _position;

        public SpanWriter(Span<byte> span)
        {
            _span = span;
            _position = 0;
        }

        public int Position => _position;

        public void WriteMessage(IMessage message)
        {

            if (message is ChatMessage chatMessage)
            {
                WriteMessageType(0);
                WriteLongString(chatMessage.Text);
            }
            else if (message is BroadcastMessage broadcastMessage)
            {
                WriteMessageType(1);
                WriteShortString(broadcastMessage.From);
                WriteLongString(broadcastMessage.Text);
            }
            else if (message is SetNickNameMessage setNickNameMessage)
            {
                WriteMessageType(3);
                WriteGuid(setNickNameMessage.RequestId);
                WriteShortString(setNickNameMessage.NickName);
            }
            else if (message is KeepaliveMessage)
            {
                WriteMessageType(2);
            }
            else if (message is AckResponceMessage ackResponceMessage)
            {
                WriteMessageType(4);
                WriteGuid(ackResponceMessage.RequestId);
            }
            else if (message is NakResponceMessage nakResponceMessage)
            {
                WriteMessageType(5);
                WriteGuid(nakResponceMessage.RequestId);
                WriteLongString(nakResponceMessage.Message);
            }
            else
            {
                throw new InvalidOperationException("Unknown message type.");
            }
        }


        public void WriteGuid(Guid value) => WriteByteArray(value.ToByteArray());
        public void WriteMessageLengthPrefix(uint value) => WriteUint32BigEndian(value);
        private void WriteMessageType(uint value) => WriteUint32BigEndian(value);

        private void WriteUint32BigEndian(uint value)
        {
            BinaryPrimitives.WriteUInt32BigEndian(_span.Slice(_position, sizeof(uint)), value);
            _position += sizeof(uint);
        }

        private void WriteUint16BigEndian(ushort value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(_span.Slice(_position, sizeof(ushort)), value);
            _position += sizeof(ushort);
        }

        private void WriteLongString(string value)
        {
            var bytes = MessagePackSerializer.Serialize(value);
            if (bytes.Length > ushort.MaxValue)
                throw new InvalidOperationException("Long string field is too big");
            WriteUint16BigEndian((ushort)bytes.Length);
            WriteByteArray(bytes);
        }
        private void WriteShortString(string value)
        {
            var bytes = MessagePackSerializer.Serialize(value);
            if (bytes.Length > byte.MaxValue)
                throw new InvalidOperationException("Short string field is too big");
            WriteByte((byte)bytes.Length);
            WriteByteArray(bytes);
        }

        private void WriteByte(byte value) => _span[_position++] = value;

        private void WriteByteArray(ReadOnlySpan<byte> value)
        {
            value.CopyTo(_span.Slice(_position, value.Length));
            _position += value.Length;
        }
    }
}
