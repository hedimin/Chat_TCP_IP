namespace Server_TCP_IP
{
    public sealed class ConnectionCollection
    {
        private readonly object _mutex = new object();
        private readonly List<ClientChatConnection> _connections = new();

        public ClientChatConnection Add(ChatConnection connection)
        {
            lock (_mutex)
            {
                var clientConnection = new ClientChatConnection(connection);
                _connections.Add(clientConnection);
                return clientConnection;
            }
        }
        public void Remove(ChatConnection connection)
        {
            lock (_mutex)
            {
                _connections.RemoveAll(x => x.ChatConnection == connection);
            }
        }

        public bool TrySetNickName(ChatConnection connection, string nickName)
        {
            lock (_mutex)
            {
                var clientChatConnection = _connections.FirstOrDefault(x => x.ChatConnection == connection);
                if (clientChatConnection == null)
                    return false;

                if (clientChatConnection.NickName == nickName)
                    return true;

                if (_connections.Any(x => x.NickName == nickName))
                    return false;

                clientChatConnection.NickName = nickName;
                return true;
            }
        }

        public IReadOnlyCollection<ClientChatConnection> CurrentConnections
        {
            get
            {
                lock (_mutex)
                {
                    return _connections.ToList();
                }

            }
        }

    }
}
