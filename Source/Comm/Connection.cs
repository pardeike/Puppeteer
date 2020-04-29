using System;
using System.Security.Authentication;
using WebSocketSharp;

namespace Puppeteer
{
	public class Connection
	{
		public const string tokenFilename = "PuppeteerToken.txt";
		public static string token = ReadToken();

		public WebSocket ws;
		readonly string endpoint;
		readonly ICommandProcessor processor;

		public bool isConnected = false;
		DateTime nextRetry = new DateTime(0);

		public Connection(ICommandProcessor processor)
		{
			this.processor = processor;
			endpoint = Tools.IsLocalDev ? "ws://localhost:3000" : "wss://puppeteer.rimworld.live";
			TryConnect();
		}

		public void TryConnect()
		{
			ws?.Close();

			var token = ReadToken();
			if (token.Length == 0)
			{
				Tools.LogWarning("No token found");
				return;
			}

			ws = new WebSocket(endpoint + "/connect") { Compression = CompressionMethod.Deflate };
			ws.OnOpen += Ws_OnOpen;
			ws.OnMessage += Ws_OnMessage;
			ws.OnError += Ws_OnError;
			ws.OnClose += Ws_OnClose;
			ws.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12;

			Connect();
		}

		void Connect()
		{
			nextRetry = new DateTime().AddSeconds(10);
			var token = ReadToken();
			if (token.Length == 0)
			{
				Tools.LogWarning("No token found");
				return;
			}

			ws.SetCookie(new WebSocketSharp.Net.Cookie("id_token", token));
			Tools.LogWarning("Token found, connecting...");
			ws.ConnectAsync();
		}

		static string ReadToken()
		{
			var tokenContent = tokenFilename.ReadConfig();
			if (tokenContent == null)
				return "";

			var parts = tokenContent.Split('.');
			if (parts.Length != 3)
				return "";

			return tokenContent;
		}

		public void Send<T>(JSONConvertable<T> obj, Action<bool> callback = null)
		{
			if (callback == null)
				callback = delegate { };

			if (ws?.ReadyState == WebSocketState.Closed)
			{
				if (DateTime.Now < nextRetry)
				{
					callback(false);
					return;
				}
				Connect();
			}
			if (ws?.ReadyState != WebSocketState.Open)
			{
				callback(false);
				return;
			}

			OutgoingRequests.Add(obj.type, obj.GetData(), callback);
		}

		public void Disconnect()
		{
			if (ws != null && ws.ReadyState == WebSocketState.Open)
				ws.CloseAsync(CloseStatusCode.Normal);
		}

		private void Ws_OnOpen(object sender, EventArgs e)
		{
			isConnected = true;
			Tools.LogWarning("Connected!");
			Send(new Hello());
		}

		private void Ws_OnClose(object sender, CloseEventArgs e)
		{
			isConnected = false;
			OutgoingRequests.Clear();
			Tools.LogWarning(ErrorDescription(e.Code));
		}

		private void Ws_OnMessage(object sender, MessageEventArgs e)
		{
			processor.Message(e.RawData);
		}

		private void Ws_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
		{
			Tools.LogWarning(e.Exception.ToString());
		}

		static string ErrorDescription(int code)
		{
			if (code == 1000)
				return "Connection closed";
			else if (code == 1001)
				return "Server gone";
			else if (code == 1002)
				return "Protocol error";
			else if (code == 1003)
				return "Bad data";
			else if (code == 1005)
				return "Disconnected";
			else if (code == 1006)
				return "Closed abnormally";
			else if (code == 1007)
				return "Malformed data";
			else if (code == 1008)
				return "Policy violation";
			else if (code == 1009)
				return "Message too large";
			else if (code == 1010)
				return "Handshake failed";
			else if (code == 1011)
				return "Unexpected condition";
			else if (code == 1015)
				return "TLS failed";
			return $"Unknown({code})";
		}
	}
}