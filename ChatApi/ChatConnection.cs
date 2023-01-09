namespace ChatApi;
//Main logic to handling connection and "convert" pipeline to chanel
public sealed class ChatConnection
{
    private readonly PipeLineSocket _pipeLineSocket;
    private readonly Channel<IMessage> _inputChannel;
    private readonly Channel<IMessage> _outputChannel;
    private readonly TimeSpan _keepaliveTimeSpan;
    private readonly Timer _timer;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource> _outstandingRequest = new();
    public ChatConnection(PipeLineSocket pipeLineSocket, TimeSpan keepaliveTimeSpan = default)
    {
        keepaliveTimeSpan = keepaliveTimeSpan == default ? TimeSpan.FromSeconds(5) : keepaliveTimeSpan;
        _pipeLineSocket = pipeLineSocket;
        _inputChannel = Channel.CreateBounded<IMessage>(4);
        _outputChannel = Channel.CreateBounded<IMessage>(4);
        _keepaliveTimeSpan = keepaliveTimeSpan;
        _timer = new Timer(_ => SendKeepaliveMessage(), null, keepaliveTimeSpan, Timeout.InfiniteTimeSpan);
        PipeLineToChannelAsync();
        ChannelToPipeLineAsync();
    }

    public Socket Socket => _pipeLineSocket.Socket;
    public IPEndPoint RemoteEndPoint => (IPEndPoint)_pipeLineSocket.RemoteEndPoit;
    public IAsyncEnumerable<IMessage> InputMessages => _inputChannel.Reader.ReadAllAsync();

    public void Complete() => _outputChannel.Writer.Complete();

    public Task SetNickNameAsync(string nickName)
    {
        var setNickNameMessage = new SetNickNameMessage(Guid.NewGuid(), nickName);
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _outstandingRequest.TryAdd(setNickNameMessage.RequestId, tcs);
        return SendMessageAndWaitForResponce();

        async Task SendMessageAndWaitForResponce()
        {
            await SendMessageAsync(setNickNameMessage);
            await tcs.Task;
        }
    }

    public async Task SendMessageAsync(IMessage message)
    {
        await _outputChannel.Writer.WriteAsync(message);
        _timer.Change(_keepaliveTimeSpan, Timeout.InfiniteTimeSpan);
    }

    private void SendKeepaliveMessage()
    {
        _outputChannel.Writer.TryWrite(new KeepaliveMessage());
        _timer.Change(_keepaliveTimeSpan, Timeout.InfiniteTimeSpan);
    }

    private void Close(Exception? exception = null)
    {
        _timer.Dispose();
        _inputChannel.Writer.TryComplete(exception);
        _pipeLineSocket.OutputPipe.CancelPendingFlush();
        _pipeLineSocket.OutputPipe.Complete();
    }

    private void WriteMessage(IMessage message)
    {
        var messageLengthPrefixValue = GetMessageLengthPrefixValue(message);
        var memory = _pipeLineSocket.OutputPipe.GetMemory(LengthPrefixLength + messageLengthPrefixValue);
        SpanWriter writer = new(memory.Span);
        writer.WriteMessageLengthPrefix((uint)messageLengthPrefixValue);
        writer.WriteMessage(message);
        _pipeLineSocket.OutputPipe.Advance(writer.Position);
    }

    private async void ChannelToPipeLineAsync()
    {
        try
        {
            await foreach (var message in _outputChannel.Reader.ReadAllAsync())
            {
                WriteMessage(message);

                var flushResult = await _pipeLineSocket.OutputPipe.FlushAsync();
                if (flushResult.IsCanceled)
                    break;
            }
            Close();
        }
        catch (Exception e)
        {
            Close(e);
        }
    }
    //Making channel of messages from pipeline

    private async void PipeLineToChannelAsync()
    {
        try
        {
            while (true)
            {
                var data = await _pipeLineSocket.InputPipe.ReadAsync();

                foreach (var parseMessage in ParseMessages(data.Buffer))
                {
                    if (parseMessage is KeepaliveMessage)
                    {
                    }
                    else if (parseMessage is AckResponceMessage ackResponceMessage)
                    {
                        if (!_outstandingRequest.TryRemove(ackResponceMessage.RequestId, out var tcs))
                        {
                            throw new InvalidOperationException("Protocol violation");
                        }
                        tcs.TrySetResult();
                    }
                    else if (parseMessage is NakResponceMessage nakResponceMessage)
                    {
                        if (!_outstandingRequest.TryRemove(nakResponceMessage.RequestId, out var tcs))
                        {
                            throw new InvalidOperationException("Protocol violation");
                        }
                        tcs.TrySetException(new Exception(nakResponceMessage.Message));
                    }
                    else
                    {
                        await _inputChannel.Writer.WriteAsync(parseMessage);
                    }
                }

                if (data.IsCompleted)
                {
                    Close();
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Close(e);
        }
    }

    //Method for parsing messages as stream of bytes at pipeline 
    private IReadOnlyList<IMessage> ParseMessages(ReadOnlySequence<byte> buffer)
    {
        var result = new List<IMessage>();
        var sequenceReader = new SequenceReader<byte>(buffer);

        while (sequenceReader.Remaining != 0)
        {
            var beginOfMessagePosition = sequenceReader.Position;

            if (!sequenceReader.TryReadMessage(_pipeLineSocket.MaxMessageSize, out var message))
            {
                _pipeLineSocket.InputPipe.AdvanceTo(beginOfMessagePosition, buffer.End);
                break;
            }
            if (message != null)
                result.Add(message);

            _pipeLineSocket.InputPipe.AdvanceTo(sequenceReader.Position);
        }
        return result;
    }
}