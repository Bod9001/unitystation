using System;
using System.Collections.Generic;
using System.Text;
using DatabaseAPI;
using IngameDebugConsole;
using Initialisation;
using Logs;
using Managers;
using Newtonsoft.Json;
using Shared.Managers;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

/*/

Write
mhelp/ahelp/prayer/achat ( send)
chat  ( send)
Kick   ( send)
Ban?  ( send)
ooc_mute/Global/player  ( send)


server Config (  set
	ServerName,
	WinDownload,
	OSXDownload,
	LinuxDownload,
	ConnectionPassword
	FPS
	BuildNumber ,
	GoodFileVersion)

Looking at Logs ( search )

Managing permissions (  remove add people to roles )

Read

playlist
chat/all the Ahelps
Read current ooc_mute/Global/player
playlist  (
	Character name
	job
	admin tag
	account ID
	IP,
	UUID,
	Is antagonist)

server Config ( Read
	ServerName,
	WinDownload,
	OSXDownload,
	LinuxDownload,
	ConnectionPassword
	FPS
	BuildNumber ,
	GoodFileVersion)
Looking at Logs
Managing permissions ( Read who has)

===================================================================

mhelp/ahelp/prayer/achat (Receive and send)
chat  (Receive and send)
Kick   ( send)
Ban?  ( send)
ooc_mute/Global/player  ( send)

playlist  (
	Character name
	job
	admin tag
	account ID
	IP,
	UUID,
	Is antagonist)

server Config ( Read? (for some) and set
	ServerName,
	WinDownload,
	OSXDownload,
	LinuxDownload,
	ConnectionPassword
	FPS
	BuildNumber ,
	GoodFileVersion)

Looking at Logs ( Receive and search )

Managing permissions ( Read who has, remove add people to roles )
/*/

public class RconManager : SingletonManager<RconManager>
{
	private HttpServer httpServer;

	private WebSocketServiceHost consoleHost;
	private WebSocketServiceHost monitorHost;
	private WebSocketServiceHost chatHost;
	private WebSocketServiceHost playerListHost;
	private Queue<string> rconChatQueue = new Queue<string>();
	private Queue<string> commandQueue = new Queue<string>();

	private ServerConfig config;

	float monitorUpdate = 0f;

	public override void Start()
	{
		base.Start();
		Instance.Init();
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		if (httpServer != null)
		{
			httpServer.Stop();
		}
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void Init()
	{
		Loggy.Info("Init RconManager", Category.Rcon);
		DontDestroyOnLoad(gameObject);

		if (ServerData.ServerConfig == null)
		{
			ServerData.serverDataLoaded += OnServerDataLoaded;
		}
		else
		{
			OnServerDataLoaded();
		}
	}

	private void OnServerDataLoaded()
	{
		if (gameObject == null) return;
		ServerData.serverDataLoaded -= OnServerDataLoaded;
		if (ServerData.ServerConfig == null)
		{
			Loggy.Info("No server config found: rcon", Category.Rcon);
			Destroy(gameObject);
		}
		else
		{
			config = ServerData.ServerConfig;
			if (string.IsNullOrEmpty(config.RconPass) || config.RconPort == 0)
			{
				Loggy.Info("Invalid Rcon config, please check your RconPass and RconPort values", Category.Rcon);
				Destroy(gameObject);
			}
			else
			{
				LoadManager.RegisterActionDelayed(StartServer, 500); //Maybe giving it a little bit of time to fix a crash?
			}
		}
	}

	private void StartServer()
	{
		if (httpServer != null)
		{
			Loggy.Info("Already Listening: WebSocket", Category.Rcon);
			return;
		}

		Loggy.Info("config loaded", Category.Rcon);

		if (GameData.IsHeadlessServer == false && Application.isEditor == false)
		{
			Loggy.Info("Dercon", Category.Rcon);
			Destroy(gameObject);
			return;
		}

		httpServer = new HttpServer(config.RconPort, false);
		//string certPath = Application.streamingAssetsPath + "/config/certificate.pfx";
		//httpServer.SslConfiguration.ServerCertificate =
		//	new X509Certificate2( certPath, config.certKey );
		httpServer.AddWebSocketService<RconSocket>("/rconconsole");
		httpServer.AddWebSocketService<RconMonitor>("/rconmonitor");
		httpServer.AddWebSocketService<RconChat>("/rconchat");
		httpServer.AddWebSocketService<RconPlayerList>("/rconplayerlist");
		httpServer.AuthenticationSchemes = AuthenticationSchemes.Digest;
		httpServer.Realm = "Admins";
		httpServer.UserCredentialsFinder = id =>
		{
			var name = id.Name;
			return name == config.RconPass ?
				new NetworkCredential("admin", null, "admin") :
				null;
		};

		//httpServer.SslConfiguration.ClientCertificateValidationCallback =
		//	( sender, certificate, chain, sslPolicyErrors ) => { return true; };
		httpServer.Start();

		//Get the service hosts:
		Instance.httpServer.WebSocketServices.TryGetServiceHost("/rconconsole", out consoleHost);
		Instance.httpServer.WebSocketServices.TryGetServiceHost("/rconmonitor", out monitorHost);
		Instance.httpServer.WebSocketServices.TryGetServiceHost("/rconchat", out chatHost);
		Instance.httpServer.WebSocketServices.TryGetServiceHost("/rconplayerlist", out playerListHost);

		if (httpServer.IsListening)
		{
			Loggy.Info().Format("Providing websocket services on port {0}.", Category.Rcon, httpServer.Port);
			foreach (var path in httpServer.WebSocketServices.Paths)
				Loggy.Info().Format("- {0}", Category.Rcon, path);
		}
		else
		{
			Loggy.Error("Failed to start Rcon server.", Category.Rcon);
			Destroy(gameObject);
		}
	}

	private void UpdateMe()
	{
		if (rconChatQueue.Count > 0)
		{
			var msg = rconChatQueue.Dequeue();
			msg = msg.Substring(1, msg.Length - 1);
			Chat.AddGameWideSystemMsgToChat("[SERVER] " + msg);
		}

		if (commandQueue.Count > 0)
		{
			ExecuteCommand(commandQueue.Dequeue());
		}

		if (monitorHost != null)
		{
			monitorUpdate += Time.deltaTime;
			if (monitorUpdate > 4f)
			{
				monitorUpdate = 0f;
				BroadcastToSessions(GetMonitorReadOut(), monitorHost.Sessions.Sessions);
			}
		}
	}

	public static void AddChatLog(string msg)
	{
		if(Instance.chatHost == null) return;
		string json = $"{{\"DateTime\":\"{DateTime.UtcNow}\",\"msg\":\"{EscapeJson(msg)}\"}}";

		AmendChatLog(json);
		Instance.chatHost.Sessions.Broadcast(json);
		BroadcastToSessions(json, Instance.chatHost.Sessions.Sessions);
	}

	private static string EscapeJson(string value)
	{
		return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
	}
	public static void AddLog(string msg)
	{
		string json = $"{{\"DateTime\":\"{DateTime.UtcNow}\",\"msg\":\"{EscapeJson(msg)}\"}}";
		AmendLog(json);
		if (Instance.consoleHost != null)
		{
			BroadcastToSessions(json, Instance.consoleHost.Sessions.Sessions);
		}
	}

	public static void UpdatePlayerListRcon()
	{
		if(Instance.playerListHost == null) return;
		var json = JsonConvert.SerializeObject(new Players());
		BroadcastToSessions(json, Instance.playerListHost.Sessions.Sessions);
	}

	//On worker thread from websocket:
	public void ReceiveRconChat(string data)
	{
		rconChatQueue.Enqueue(data);
	}

	public void ReceiveRconCommand(string cmd)
	{
		commandQueue.Enqueue(cmd);
	}

	private static void BroadcastToSessions(string msg, IEnumerable<IWebSocketSession> sessions)
	{
		foreach (var conn in sessions)
		{
			if (conn == null)
			{
				continue;
			}
			if (conn.ConnectionState != WebSocketState.Closing ||
				conn.ConnectionState != WebSocketState.Closed)
			{
				conn.Context.WebSocket.Send(msg);
			}
			else
			{
				Loggy.Info().Format("Do not broadcast to (connection not ready): {0}", Category.Rcon, conn.ID);
			}
		}
	}

	public class MonitorReadOut
	{
		public float FPS;
		public float FPSAverage;
		public int RCONAdmins;
		public double MBsUse;
	}

	//Monitoring:
	public static string GetMonitorReadOut()
	{
		var connectedAdmins = 0;
		foreach (var s in Instance.monitorHost.Sessions.Sessions)
		{
			if (s.ConnectionState == WebSocketState.Open)
			{
				connectedAdmins++;
			}
		}

		long bytesUsed = GC.GetTotalMemory(false);
		double mbUsed = bytesUsed / (1024.0 * 1024.0);

		//Removed GC Check for time being
		return JsonConvert.SerializeObject(new MonitorReadOut()
		{
			FPS = FPSMonitor.Instance.Current,
			FPSAverage = FPSMonitor.Instance.Average,
			RCONAdmins = connectedAdmins,
			MBsUse = mbUsed
		});
	}

	public static string GetLastLog()
	{
		return LastLog;
	}

	public static string GetFullLog()
	{
		var stringBuilder = new StringBuilder();
		stringBuilder.AppendJoin(',',ServerLog);
		var log = stringBuilder.ToString();
		if (log.Length > 5000)
		{
			log = log.Substring(4000);
		}
		return $"[{log}]";
	}

	public static string GetFullChatLog()
	{
		var stringBuilder = new StringBuilder();
		stringBuilder.AppendJoin(',',ChatLog);
		var log = stringBuilder.ToString();

		if (string.IsNullOrEmpty(log))
		{
			return "[\"No one has said anything yet..\"]";
		}

		if (log.Length > 10000)
		{
			log = log.Substring(9000);
		}
		return $"[{log}]";
	}

	#region RconConsole

	protected static List<string> serverLog = new List<string>(1000);
	protected static List<string> ServerLog => serverLog;
	protected static string LastLog { get; private set; }


	protected static List<string> chatLog = new List<string>(1000);
	protected static List<string> ChatLog => chatLog;
	protected static string ChatLastLog { get; private set; }

	protected static void AmendLog(string msg)
	{
		ServerLog.Add(msg);
		LastLog = msg;
	}

	protected static void AmendChatLog(string msg)
	{
		ChatLog.Add(msg);
		ChatLastLog = msg;
	}

	protected static void ExecuteCommand(string command)
	{
		command = command.Substring(1, command.Length - 1);
		DebugLogConsole.ExecuteCommand(command);
	}

	#endregion
}

public class RconSocket : WebSocketBehavior
{
	public class RequestData
	{

		public CommandType CommandType;
		public string Data;
	}

	public enum CommandType
	{
		lastlog,
		logfull,
		Command
	}

	protected override void OnMessage(MessageEventArgs e)
	{
		var Request = JsonConvert.DeserializeObject<RequestData>(e.Data);

		if (Request.CommandType == CommandType.lastlog)
		{
			Send(RconManager.GetLastLog());
		}

		if (Request.CommandType == CommandType.logfull)
		{
			Send(RconManager.GetFullLog());
		}

		if (Request.CommandType == CommandType.Command)
		{
			RconManager.Instance.ReceiveRconCommand(Request.Data);
		}
	}
}

public class RconMonitor : WebSocketBehavior
{
	protected override void OnOpen()
	{
		if (Context.User.Identity.IsAuthenticated)
		{
			Loggy.Info("admin logged in", Category.Rcon);
		}

		base.OnOpen();
	}

	protected override void OnClose(CloseEventArgs e)
	{
		if (Context.User.Identity.IsAuthenticated)
		{
			Loggy.Info("admin closed. reason: " + e.Reason, Category.Rcon);
		}

		base.OnClose(e);
	}
}

public class RconChat : WebSocketBehavior
{


	public class RequestData
	{

		public CommandType CommandType;
		public string Data;
	}

	public enum CommandType
	{
		chatfull,
		Chat
	}

	protected override void OnMessage(MessageEventArgs e)
	{
		var Request = JsonConvert.DeserializeObject<RequestData>(e.Data);

		if (Request.CommandType == CommandType.chatfull)
		{
			Send(RconManager.GetFullChatLog());
		}

		if (Request.CommandType == CommandType.Chat)
		{
			RconManager.Instance.ReceiveRconChat(Request.Data);
		}
	}
}

public class RconPlayerList : WebSocketBehavior
{
	protected override void OnMessage(MessageEventArgs e)
	{
		if (e == null) return;

		if (e.Data == "players")
		{
			var playerList = JsonConvert.SerializeObject(new Players());
			if (!string.IsNullOrEmpty(playerList))
			{
				Send(playerList);
			}
		}
	}
}

[Serializable]
public class Players
{
	public List<PlayerDetails> players = new List<PlayerDetails>();

	public Players()
	{
		if (PlayerList.Instance == null) return;
		for (int i = 0; i < PlayerList.Instance.InGamePlayers.Count; i++)
		{
			var player = PlayerList.Instance.InGamePlayers[i];
			var playerEntry = new PlayerDetails()
			{
				playerName = player.Name + $" {player.Job.ToString()} : Acc: {player.Username} {player.AccountId} {player.ConnectionIP} ",
					job = player.Job.ToString()
			};
			players.Add(playerEntry);
		}
	}
}

[Serializable]
public class PlayerDetails
{
	public string playerName;
	public ulong steamID;
	public string job;
}
