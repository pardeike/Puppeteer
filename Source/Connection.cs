using System;
using System.Threading;
using Verse;
using WebSocketSharp;

namespace Puppeteer
{
	public static class ConnectionAsyncHelper
	{
		public static void ConnectAsServer(this WebSocket ws)
		{
			try
			{
				ws.ConnectAsync();
				ws.SendData("hello:server", 10, (success) =>
				{
					Log.Warning("Connection: " + success);
				});
			}
			catch (InvalidOperationException)
			{
				Log.Warning("Connecting failed");
			}
		}

		public static void SendData(this WebSocket ws, string data, int timeout = 10, Action<bool> callback = null)
		{
			var expiringDate = DateTime.Now.AddSeconds(timeout);
			while (ws.ReadyState != WebSocketState.Open)
			{
				if (ws.ReadyState == WebSocketState.Closed)
					ws.ConnectAsServer();
				else
				{
					Thread.Sleep(200);
					if (DateTime.Now > expiringDate)
					{
						callback?.Invoke(false);
						return;
					}
				}
			}
			ws.SendAsync(data, callback);
		}
	}

	class Connection
	{
		const string endpoint = "ws://localhost:3000"; //"wss://puppeteer-central.herokuapp.com";

		static Connection _instance;
		public readonly WebSocket ws;

		public static Connection Instance
		{
			get
			{
				if (_instance == null)
					_instance = new Connection();
				return _instance;
			}
		}

		Connection()
		{
			ws = new WebSocket(endpoint) { Compression = CompressionMethod.Deflate };
			ws.OnOpen += Ws_OnOpen;
			ws.OnMessage += Ws_OnMessage;
			ws.OnError += Ws_OnError;
			ws.OnClose += Ws_OnClose;
			ws.ConnectAsServer();
		}

		private void Ws_OnOpen(object sender, EventArgs e)
		{
			Log.Warning("Opened! " + ws.ReadyState);
		}

		private void Ws_OnClose(object sender, CloseEventArgs e)
		{
			Log.Warning("Closed: " + e.Reason + " [" + e.Code + "]");
			ws.ConnectAsync();
		}

		private void Ws_OnMessage(object sender, MessageEventArgs e)
		{
			Log.Warning("Response: " + e.Data);
		}

		private void Ws_OnError(object sender, ErrorEventArgs e)
		{
			Log.Warning("Error: " + e.Message);
		}
	}
}