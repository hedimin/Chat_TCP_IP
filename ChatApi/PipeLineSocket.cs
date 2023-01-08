namespace ChatApi;

public sealed class PipeLineSocket
{
    private readonly Pipe _outputPipe;
    private readonly Pipe _inputPipe;
    public Socket Socket { get; }

    public PipeWriter OutputPipe => _outputPipe.Writer;
    public PipeReader InputPipe => _inputPipe.Reader;

    private readonly TaskCompletionSource<object> _completion;

    public IPEndPoint RemoteEndPoit { get; }

    private void Close(Exception? exception = null)
    {
        if (exception != null)
        {
            if (_completion.TrySetException(exception))
                _inputPipe.Writer.Complete(exception);
        }
        else
        {
            if (_completion.TrySetResult(null!))
                _inputPipe.Writer.Complete();
        }

        try
        {
            Socket.Shutdown(SocketShutdown.Both);
            Socket.Close();
        }
        catch
        {
        }
    }

    public PipeLineSocket(Socket connectedSocket, uint maxMessageSize = 65536)
    {
        Socket = connectedSocket;
        RemoteEndPoit = (IPEndPoint)connectedSocket.RemoteEndPoint!;
        MaxMessageSize = maxMessageSize;
        _outputPipe = new Pipe();
        _inputPipe = new Pipe(new PipeOptions(pauseWriterThreshold: maxMessageSize + 4));
        _completion = new TaskCompletionSource<object>();

        FromPipelineToSocketAsync(_outputPipe.Reader, Socket);
        FromSocketToPipeLineAsync(_inputPipe.Writer, Socket);
    }

    public uint MaxMessageSize { get; }

    private async void FromPipelineToSocketAsync(PipeReader pipeReader, Socket socket)
    {
        try
        {
            while (true)
            {
                ReadResult result = await pipeReader.ReadAsync();
                var buffer = result.Buffer;
                while (true)
                {
                    var memory = buffer.First;
                    if (memory.IsEmpty)
                        break;
                    var bytesSend = await socket.SendAsync(memory, SocketFlags.None);
                    buffer = buffer.Slice(bytesSend);
                    if (bytesSend == memory.Length)
                        break;
                }

                pipeReader.AdvanceTo(buffer.Start);
                if (result.IsCompleted)
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
    private async void FromSocketToPipeLineAsync(PipeWriter pipeWriter, Socket socket)
    {
        try
        {
            while (true)
            {
                var buffer = pipeWriter.GetMemory();
                var bytesRead = await socket.ReceiveAsync(buffer, SocketFlags.None);
                if (bytesRead == 0)
                {
                    Close();
                    break;
                }

                pipeWriter.Advance(bytesRead);
                await pipeWriter.FlushAsync();
            }
        }
        catch (Exception e)
        {
            Close(e);
        }
    }
}
