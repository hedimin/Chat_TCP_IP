Console.WriteLine("Starting server...");

var connections = new ConnectionCollection();

var listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
listeningSocket.Bind(new IPEndPoint(IPAddress.Any, 33333));

Console.WriteLine("Listening...");
listeningSocket.Listen();
while (true)
{
    var connectedSocket = await listeningSocket.AcceptAsync();
    Console.WriteLine($"Got a connection from {connectedSocket.RemoteEndPoint} to {connectedSocket.LocalEndPoint}");

    ProcessSocketAsync(connectedSocket);
}

async void ProcessSocketAsync(Socket socket)
{
    var chatConnection = new ChatConnection(new PipeLineSocket(socket));
    var clientConnection = connections.Add(chatConnection);
    try
    {
        await foreach (var message in chatConnection.InputMessages)
        {
            if (message is ChatMessage chatMessage)
            {
                Console.WriteLine($"Got a message from {chatConnection.RemoteEndPoint} : {chatMessage.Text}");

                var currentConnections = connections.CurrentConnections;
                var from = clientConnection.NickName ?? chatConnection.RemoteEndPoint.ToString();
                var broadcastMessage = new BroadcastMessage(from, chatMessage.Text);
                var tasks = currentConnections
                    .Select(x => x.ChatConnection)
                    .Where(x => x != chatConnection)
                    .Select(connection =>
                        connection.SendMessageAsync(broadcastMessage));
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch
                {
                    //Ignore
                }
            }
            else if (message is SetNickNameMessage setNickNameMessage)
            {
                Console.WriteLine($"Got nickname request message from " +
                                  $"{chatConnection.RemoteEndPoint} : {setNickNameMessage.NickName}\n");
                if (connections.TrySetNickName(chatConnection, setNickNameMessage.NickName))
                {
                    await chatConnection.SendMessageAsync(new AckResponceMessage(setNickNameMessage.RequestId));
                }
                else
                {
                    await chatConnection.SendMessageAsync(new NakResponceMessage(setNickNameMessage.RequestId,
                        "NickName is already taken\n"));
                }
            }
            else
                Console.WriteLine($"Got unknown message from {chatConnection.RemoteEndPoint}\n");
        }

        Console.WriteLine($"Connection at {chatConnection.RemoteEndPoint} was disconnected\n");
    }
    catch (Exception e)
    {
        Console.WriteLine($"Exception from {chatConnection.RemoteEndPoint}: [{e.GetType().Name}] {e.Message}\n");
    }
    finally
    {
        connections.Remove(chatConnection);
    }
}