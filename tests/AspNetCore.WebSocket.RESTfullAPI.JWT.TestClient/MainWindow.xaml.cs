﻿using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;


namespace AspNetCore.WebSocket.RESTfullAPI.JWT.TestClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ClientWebSocket ws;
        Thread wsReceiving;
        public static string wsId = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            ws = new ClientWebSocket();
            wsReceiving = new Thread(new ThreadStart(ReceivingTest));
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
                    var userId = new Random().Next(5, 100);
                    var pairs = new List<KeyValuePair<string, string>>
                    {
                         new KeyValuePair<string, string>("UserName", UserName.Text),
                         new KeyValuePair<string, string>("UserId", userId.ToString()),
                    };
                    //var token = GetContext.PostRequest("http://localhost:5000/api/Account/Authorization", pairs).Result;
                    var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6IkJlbm9tIiwiVXNlcklkIjoiNDciLCJuYmYiOjE2MjExMDYzNTYsImV4cCI6MTYyMTE5Mjc1NiwiaWF0IjoxNjIxMTA2MzU2LCJpc3MiOiJXU1NlcnZlciIsImF1ZCI6IjlERDI4QUE3LTk2QjYtNDZBMy05RTQ0LUU3QTFDQjg0MzUwRCJ9.evZZdQwDXyCgKDwvWhLms-0JXpLD1gyqey_BnpX_Qk0";
                    ws.Options.SetRequestHeader("Authorization", "Bearer " + token);
                    if (ws.State != WebSocketState.Open)
                    {
                        ws.ConnectAsync(new Uri($"ws://localhost:5000/WSMessenger"), CancellationToken.None).GetAwaiter().GetResult();
                    }

                    if (ws.State == WebSocketState.Open)
                    {
                        wsReceiving.Start();
                        connectButton.IsEnabled = false;
                        sendButton.IsEnabled = true;
                        userInfoButton.IsEnabled = true;
                        deconnectButton.IsEnabled = true;
                    }
                    else
                    {
                        messagesList.Items.Add("Connection failed");
                    }
                }
                else
                {
                    MessageBox.Show("First enter access token");
                }
            }
            catch (Exception ex)
            {
                messagesList.Items.Add(ex.Message);
            }
        }

        private async void deconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test", CancellationToken.None);

                if (ws.State == WebSocketState.Closed)
                {
                    messagesList.Items.Add("WebSocket deconnected");
                    connectButton.IsEnabled = true;
                    sendButton.IsEnabled = false;
                    deconnectButton.IsEnabled = false;
                }
                else
                {
                    messagesList.Items.Add("Deconnection failed");
                }
            }
            catch (Exception ex)
            {
                messagesList.Items.Add(ex.Message);
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
                        await Sending("Chat.DirectWithFriend", ("message", message), ("userId", userId.ToString()));
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
                messagesList.Items.Add(ex.Message);
            }
        }

        private async void userInfoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Sending("User.Info");
            }
            catch (Exception ex)
            {
                messagesList.Items.Add(ex.Message);
            }
        }

        private async Task Sending(string method, params (string key, object value)[] parameters)
        {
            Dictionary<string, object> pairs = new Dictionary<string, object>();
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
            await ws.SendAsync(bytesToSend, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        private async Task Receiving()
        {
            var buffer = new byte[1024 * 4];

            while (true)
            {
                if (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            var responseMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            var response = RequestModel.DeserializeJsonObject(responseMessage);
                            object dataToAddMessageList = response.Method + ", result: ";
                            if (response.ErrorId == 0)
                            {
                                if (response.Method == "WSConnected")
                                {
                                    dataToAddMessageList += "Connection started";
                                }
                                else
                                {
                                    dataToAddMessageList += response.Result?.ToString() ?? response.Data?.ToString();
                                }
                            }
                            else
                            {
                                var error = "Error: " + response.Error?.ToString() + ", ErrorId: " + response.ErrorId;
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

                            messagesList.Items.Add(dataToAddMessageList);
                        });
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            messagesList.Items.Add(result.CloseStatusDescription);
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
