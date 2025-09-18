using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;


namespace AspNetCore.WebSocket.RESTfulAPI.TestClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ClientWebSocket _ws;
        private readonly Thread _wsReceiving;
        private readonly ObservableCollection<string> _messages = [];

        public MainWindow()
        {
            InitializeComponent();
            MessagesList.ItemsSource = _messages;
            _ws = new ClientWebSocket();
            _wsReceiving = new Thread(ReceivingTest);
        }
        
        private void AddMessage(string message)
        {
            _messages.Add(message);
        }

        void ReceivingTest()
        {
            try
            {
                Receiving().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
            }
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(UserName.Text))
                {
                    _ws.Options.SetRequestHeader("UserName", UserName.Text);
                    _ws.Options.SetRequestHeader("UserId", Guid.NewGuid().ToString());
                    if (_ws.State != WebSocketState.Open)
                    {
                        _ws.ConnectAsync(new Uri("ws://localhost:5000/WSMessenger"), CancellationToken.None).GetAwaiter().GetResult();
                    }

                    if (_ws.State == WebSocketState.Open)
                    {
                        _wsReceiving.Start();
                        ConnectButton.IsEnabled = false;
                        SendButton.IsEnabled = true;
                        UserInfoButton.IsEnabled = true;
                        DisconnectButton.IsEnabled = true;
                    }
                    else
                    {
                        AddMessage("Connection failed");
                    }
                }
                else
                {
                    MessageBox.Show("First enter access token");
                }
            }
            catch (Exception ex)
            {
                AddMessage(ex.Message);
            }
        }

        private async void disconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test", CancellationToken.None);

                if (_ws.State == WebSocketState.Closed)
                {
                    AddMessage("WebSocket disconnected");
                    ConnectButton.IsEnabled = true;
                    SendButton.IsEnabled = false;
                    DisconnectButton.IsEnabled = false;
                }
                else
                {
                    AddMessage("Disconnecting socket failed");
                }
            }
            catch (Exception ex)
            {
                AddMessage(ex.Message);
            }
        }

        private async void sendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var message = Message.Text;
                if (!string.IsNullOrEmpty(message))
                {
                    if (Guid.TryParse(UserId.Text, out Guid userId))
                    {
                        await Sending("Chat.Message", ("message", message), ("userId", userId.ToString()));
                    }
                    else
                    {
                        await Sending("Chat.MessageToAll", ("message", message));
                    }
                }
                else
                {
                    MessageBox.Show("First enter access token");
                }
            }
            catch (Exception ex)
            {
                AddMessage(ex.Message);
            }
        }

       const string UserInfoKey = "User.Info";
       
        private async void userInfoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Sending(UserInfoKey);
            }
            catch (Exception ex)
            {
                AddMessage(ex.Message);
            }
        }

        private async Task Sending(string method, params (string key, object value)[] parameters)
        {
            var pairs = new Dictionary<string, object>();
            foreach (var paramItem in parameters)
                pairs.Add(paramItem.key, paramItem.value);

            var request = new RequestModel()
            {
                Id = Guid.NewGuid().ToString(),
                Method = method,
                Params = pairs
            };

            var jsonObject = request.SerializeObject();
            ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonObject));
            await _ws.SendAsync(bytesToSend, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        private async Task Receiving()
        {
            var buffer = new byte[1024 * 4];

            while (true)
            {
                if (_ws.State == WebSocketState.Open)
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            var responseMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            var response = RequestModel.DeserializeJsonObject(responseMessage);
                            string dataToAddMessageList = response.Method + ", result: ";
                            if (response.ErrorId == 0)
                            {
                                if (response.Method == "WSConnected")
                                {
                                    dataToAddMessageList += "Connection started";
                                }
                                else
                                {
                                    var responseResult = response.Result ?? response.Data;
                                    if (response.Method == UserInfoKey && responseResult is JObject jObject)
                                    {
                                        var userId = jObject["id"]?.ToString();
                                        if (!string.IsNullOrEmpty(userId))
                                        {
                                            Clipboard.SetText(userId);
                                            
                                            AddMessage("User Id copied to clipboard!");
                                        }
                                    }
                                    dataToAddMessageList += responseResult?.ToString();
                                }
                            }
                            else
                            {
                                var error = "Error: " + response.Error + ", ErrorId: " + response.ErrorId;
                                switch (response.Method)
                                {
                                    case "ConnectionAborted":
                                        dataToAddMessageList += error + ", " + result.CloseStatusDescription;
                                        break;
                                    case "User.UnAuth":
                                        dataToAddMessageList += error;
                                        break;
                                    default:
                                        dataToAddMessageList += error;
                                        break;
                                }
                            }

                            AddMessage(dataToAddMessageList);
                        });
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            AddMessage(result.CloseStatusDescription);
                        });
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }
}
