namespace Server_TCP_IP
{
    public sealed class ClientChatConnection
    {
        public ChatConnection ChatConnection { get; }

        public ClientChatConnection(ChatConnection chatConnection)
        {
            ChatConnection = chatConnection;
        }

        public string? NickName { get; set; }
    }
}
