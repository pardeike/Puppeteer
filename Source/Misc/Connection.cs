using System;
using Verse;
using WebSocketSharp;

namespace Puppeteer
{
	public class Connection
	{
		public const string tokenFilename = "PuppeteerToken.txt";
		public static string token = ReadToken();

		public readonly WebSocket ws;
		public readonly Action<string, string> action;
		readonly string endpoint;
		readonly ICommandProcessor processor;

		public bool isConnected = false;
		DateTime nextRetry = new DateTime(0);

		public Connection(bool localDev, ICommandProcessor processor)
		{
			this.processor = processor;
			endpoint = localDev ? "ws://localhost:3000" : "wss://puppeteer-central.herokuapp.com";

			ws = new WebSocket(endpoint + "/connect") { Compression = CompressionMethod.Deflate };
			ws.OnOpen += Ws_OnOpen;
			ws.OnMessage += Ws_OnMessage;
			ws.OnError += Ws_OnError;
			ws.OnClose += Ws_OnClose;

			action = FileWatcher.AddListener((action, file) =>
			{
				if (file == tokenFilename)
					ws.Close();
			});

			Connect();
		}

		void Connect()
		{
			var token = ReadToken();
			if (token != null && token.Length > 0)
				ws.SetCookie(new WebSocketSharp.Net.Cookie("id_token", token));
			ws.ConnectAsync();
			nextRetry = new DateTime().AddSeconds(5);
		}

		static string ReadToken()
		{
			var tokenContent = tokenFilename.ReadConfig();
			if (tokenContent == null)
				return "";

			var parts = tokenContent.Split('.');
			if (parts.Length != 3)
				return "";

			//var json = parts[1].Base64Decode();
			//var token = TokenJSON.Create(json);
			// Log.Warning($"Token {token}");
			return tokenContent;
		}

		public void Send<T>(JSONConvertable<T> obj, Action<bool> callback = null)
		{
			if (callback == null)
				callback = delegate { };

			/*if (isConnected == false)
			{
				callback(false);
				return;
			}*/

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

			ws?.SendAsync(obj.GetData(), callback);
		}

		public void Disconnect()
		{
			if (ws != null && ws.ReadyState == WebSocketState.Open)
				ws.CloseAsync(CloseStatusCode.Normal);

			FileWatcher.RemoveListener(action);
		}

		private void Ws_OnOpen(object sender, EventArgs e)
		{
			isConnected = true;

			ws.SendAsync("{\"type\":\"hello\"}", null);
		}

		private void Ws_OnClose(object sender, CloseEventArgs e)
		{
			isConnected = false;

			// 1005 = server closed, was connectable
			// 1006 server did not send close, probably no connection
			if (e.Code == 1005) { }
			if (e.Code == 1006) { }
		}

		private void Ws_OnMessage(object sender, MessageEventArgs e)
		{
			processor.Message(e.RawData);
		}

		private void Ws_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
		{
			Log.Warning($"# Error: {e.Message}");
		}
	}
}
