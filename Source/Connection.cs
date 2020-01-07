using System;
using System.Timers;
using Verse;
using WebSocketSharp;

namespace Puppeteer
{
	public class Connection
	{
		public readonly WebSocket ws;
		readonly Timer timer = new Timer(10000) { AutoReset = true };
		readonly string endpoint;
		readonly ICommandProcessor processor;

		public bool isConnected = false;
		DateTime nextRetry = new DateTime(0);

		public Connection(bool localDev, ICommandProcessor processor)
		{
			this.processor = processor;
			endpoint = localDev ? "ws://localhost:3000" : "wss://puppeteer-central.herokuapp.com";

			timer.Elapsed += new ElapsedEventHandler((sender, args) => ws.SendAsync(new SimpleCmd() { type = "ping" }.GetJSON(), null));

			ws = new WebSocket(endpoint + "/connect") { Compression = CompressionMethod.Deflate };
			ws.OnOpen += Ws_OnOpen;
			ws.OnMessage += Ws_OnMessage;
			ws.OnError += Ws_OnError;
			ws.OnClose += Ws_OnClose;
			Connect();
		}

		void Connect()
		{
			var tokenContent = Tools.ReadConfig("PuppeteerToken.txt");
			if (tokenContent == null)
			{
				Log.Warning("Cannot read PuppeteerToken.txt");
				return;
			}
			
			var parts = tokenContent.Split('.');
			if (parts.Length == 3)
			{
				var json = Tools.Base64Decode(parts[1]);
				var token = TokenJSON.Create(json);
				Log.Warning("# Connecting (" + token.game + ")");
			}
			else
				Log.Warning("Invalid token format");

			Log.Warning("# Connecting");
			ws.SetCookie(new WebSocketSharp.Net.Cookie("id_token", tokenContent));
			ws.ConnectAsync();
			nextRetry = new DateTime().AddSeconds(5);
		}

		public void Send(string data, Action<bool> callback = null)
		{
			if (ws != null && ws.ReadyState == WebSocketState.Closed)
			{
				if (DateTime.Now < nextRetry)
				{
					callback(false);
					return;
				}
				Connect();
			}
			if (ws != null && ws.ReadyState == WebSocketState.Open)
				ws.SendAsync(data, callback);
			else
				callback(false);
		}

		public void Disconnect()
		{
			if (ws != null && ws.ReadyState == WebSocketState.Open)
				ws.CloseAsync(CloseStatusCode.Normal);
		}

		private void Ws_OnOpen(object sender, EventArgs e)
		{
			Log.Warning("# Open");
			isConnected = true;

			ws.SendAsync("{\"type\":\"hello\"}", null);
			timer.Start();
		}

		private void Ws_OnClose(object sender, CloseEventArgs e)
		{
			Log.Warning("# Closed " + e.Code);
			isConnected = false;

			timer.Stop();

			// 1005 = server closed, was connectable
			// 1006 server did not send close, probably no connection
			if (e.Code == 1005) { }
			if (e.Code == 1006) { }
		}

		private void Ws_OnMessage(object sender, MessageEventArgs e)
		{
			processor.Message(e.Data);
		}

		private void Ws_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
		{
			Log.Warning("# Error: " + e.Message);
		}
	}
}