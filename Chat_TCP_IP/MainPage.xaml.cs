using System.Net.Sockets;
using ChatApi;
using ChatApi.Messages;

namespace Chat_TCP_IP;

public partial class MainPage : ContentPage
{
    private ChatConnection _chatConnection;

    public MainPage()
	{
		InitializeComponent();
	}
    private async void ConnectBtn_Pressed(object sender, EventArgs e)
    {
        var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await clientSocket.ConnectAsync("localhost", 33333);

        Log.Text += $"Connected to the {clientSocket.RemoteEndPoint}\n";

        _chatConnection = new ChatConnection(new PipeLineSocket(clientSocket));

        await ProcessSocketAsync(_chatConnection);
    }

    private void DisconectBtn_Pressed(object sender, EventArgs e)
    {
        _chatConnection?.Complete();
    }

    private async void ChangeNickNameBtn_Pressed(object sender, EventArgs e)
    {
        if (_chatConnection == null)
        {
            Log.Text += "Cant connect to the server\n";
        }
        else
        {
            var nickName = NickNameBox.Text;
            try
            {
                Log.Text += $"Sending nickname request for {nickName}\n";
                await _chatConnection.SetNickNameAsync(nickName);
                Log.Text += $"Successfully set nickname to {nickName}\n";
            }
            catch (Exception ex)
            {
                Log.Text += $"Unable set nickname to {nickName}: {ex.Message}";
            }
        }
        NickNameBox.Text = string.Empty;
    }

    private async void SendMessageBtn_Pressed(object sender, EventArgs e)
    {
        if (_chatConnection == null)
        {
            Log.Text += "Cant connect to the server\n";
        }
        else
        {
            await _chatConnection.SendMessageAsync(new ChatMessage($"{EnterMessageField.Text}"));
            Log.Text += $"Sent message '{EnterMessageField.Text}' to the server!\n";
        }
        EnterMessageField.Text = string.Empty;
    }

    private async Task ProcessSocketAsync(ChatConnection chatConnection)
    {
        try
        {
            await foreach (var message in chatConnection.InputMessages)
            {
                if (message is BroadcastMessage broadcastMessage)
                {
                    Chat.Text += $"{broadcastMessage.From} : {broadcastMessage.Text}\n";
                }
                else
                {
                    Log.Text += $"Got unknown message from the {chatConnection.RemoteEndPoint} server\n";
                }
            }
            Log.Text += $"Connection at {chatConnection.RemoteEndPoint} was disconnected\n";
        }
        catch (Exception e)
        {
            Log.Text += $"Exception from {chatConnection.RemoteEndPoint}: [{e.GetType().Name}] {e.Message}\n";
        }
    }
}

