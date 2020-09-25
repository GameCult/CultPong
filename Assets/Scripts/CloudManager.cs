using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LiteNetLib;
using MessagePack;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

public static class CloudManager {
	public static Action<string> Logger = s => 
		MainThreadDispatcher.Post(_=>Debug.Log(s),null);
	
	private static bool _joinedroom = false;
	private static Dictionary<string, List<Action<Message>>> _messageCallbacks = new Dictionary<string, List<Action<Message>>>();
	private static Dictionary<string, List<Action<Message>>> _asyncMessageCallbacks = new Dictionary<string, List<Action<Message>>>();
	private static List<Action<string>> _userJoinedCallbacks = new List<Action<string>>();
	private static List<Action<string>> _userLeftCallbacks = new List<Action<string>>();
	private static Dictionary<string, Dictionary<string,List<Action<Message>>>> _userMessageCallbacks = new Dictionary<string, Dictionary<string, List<Action<Message>>>>();
	private static Dictionary<string, Dictionary<string,List<Action<Message>>>> _userAsyncMessageCallbacks = new Dictionary<string, Dictionary<string, List<Action<Message>>>>();
	private static string _infomsg = "";
	private static float _serverTimeDelta;
	private static bool _connected;
	private static NetManager _client;
	private static NetPeer _peer;

	public static string State => _peer?.ConnectionState.ToString() ?? "Not Connected";

	public static float ServerTime { get { return Time.time + _serverTimeDelta; } }

	public static void AddUserJoinedListener(Action<string> callback)
	{
		_userJoinedCallbacks.Add(callback);
	}

	public static void AddUserLeftListener(Action<string> callback)
	{
		_userLeftCallbacks.Add(callback);
	}

	public static void AddMessageListener(string messageType, Action<Message> callback)
	{
		if(!_messageCallbacks.ContainsKey(messageType))
			_messageCallbacks[messageType] = new List<Action<Message>>();
		_messageCallbacks[messageType].Add(callback);
		Debug.Log($"Registered \"{messageType}\" Listener");
	}

	public static void AddAsyncMessageListener(string messageType, Action<Message> callback)
	{
		if(!_asyncMessageCallbacks.ContainsKey(messageType))
			_asyncMessageCallbacks[messageType] = new List<Action<Message>>();
		_asyncMessageCallbacks[messageType].Add(callback);
		Debug.Log($"Registered Asynchronous \"{messageType}\" Listener");
	}

	public static void AddUserMessageListener(string user, string messageType, Action<Message> callback)
	{
		if (!_userMessageCallbacks.ContainsKey(user))
		{
			Debug.LogError($"Attempted to Register \"{messageType}\" Listener for nonexistent User \"{user}\". Message listeners should be added only after they join.");
			return;
		}
		if(!_userMessageCallbacks[user].ContainsKey(messageType))
			_userMessageCallbacks[user][messageType] = new List<Action<Message>>();
		_userMessageCallbacks[user][messageType].Add(callback);
		Debug.Log($"Registered \"{messageType}\" Listener");
	}

	public static void AddAsyncUserMessageListener(string user, string messageType, Action<Message> callback)
	{
		if (!_userAsyncMessageCallbacks.ContainsKey(user))
		{
			Debug.LogError($"Attempted to Register \"{messageType}\" Listener for nonexistent User \"{user}\". Message listeners should be added only after they join.");
			return;
		}
		if(!_userAsyncMessageCallbacks[user].ContainsKey(messageType))
			_userAsyncMessageCallbacks[user][messageType] = new List<Action<Message>>();
		_userAsyncMessageCallbacks[user][messageType].Add(callback);
		Debug.Log($"Registered Asynchronous \"{messageType}\" Listener for User \"{user}\"");
	}

	public static void ClearMessageListeners()
	{
		_messageCallbacks.Clear();
		_asyncMessageCallbacks.Clear();
		_userJoinedCallbacks.Clear();
		_userLeftCallbacks.Clear();
		_userMessageCallbacks.Clear();
		_userAsyncMessageCallbacks.Clear();
	}

	public static bool Connected => _connected;

	public static void Connect(string host = "localhost", int port = 3075, Action<string> errorCallback = null)
	{
		EventBasedNetListener listener = new EventBasedNetListener();
		_client = new NetManager(listener, "aetheria-cc65a44d")
		{
			UnsyncedEvents = true,
			MergeEnabled = true,
			//NatPunchEnabled = true
		};
		_client.Start(3074);
		_peer = _client.Connect(host, port);
		listener.NetworkReceiveEvent += (peer, reader) => 
		{
			var message = MessagePackSerializer.Deserialize<Message>(reader.Data);
			Logger($"Received {message.Type} message with content [{message.Content.Aggregate("", (s, o) => $"{s}({o})")}]");
			HandleMessage(message);
		};
		listener.PeerConnectedEvent += peer =>
		{
			Logger($"Peer {peer.EndPoint.Host}:{peer.EndPoint.Port} connected.");
			_peer = peer;
//			onConnect();
		};
		listener.PeerDisconnectedEvent += (peer, info) =>
		{
			Logger($"Peer {peer.EndPoint.Host}:{peer.EndPoint.Port} disconnected: {info.Reason}.");
			_peer = null;
		};
		listener.NetworkLatencyUpdateEvent += (peer, latency) => Logger($"Ping received: {latency} ms");
	}

	public static void Disconnect()
	{
	}
	
	static void HandleMessage(Message m)
	{
		var received = false;
		if (m.Type.StartsWith("Player"))
		{
			if (m.Type == "PlayerJoined")
			{
				_userAsyncMessageCallbacks[m.GetString(0)] = new Dictionary<string, List<Action<Message>>>();
				_userMessageCallbacks[m.GetString(0)] = new Dictionary<string, List<Action<Message>>>();
				MainThreadDispatcher.Post(_=>_userJoinedCallbacks.ForEach(l => l(m.GetString(0))), null);
				if (_userJoinedCallbacks.Any())
					received = true;
			}
			else if (m.Type == "PlayerLeft")
			{
				_userAsyncMessageCallbacks.Remove(m.GetString(0));
				_userMessageCallbacks.Remove(m.GetString(0));
				MainThreadDispatcher.Post(_=>_userLeftCallbacks.ForEach(l => l(m.GetString(0))), null);
				if(_userLeftCallbacks.Any())
					received = true;
			}
			else
			{
				if (!_userAsyncMessageCallbacks.ContainsKey(m.GetString(0)))
				{
					Observable.NextFrame().Subscribe(_ => Debug.LogError($"{m.Type} Message received for nonexistent player {m.GetString(0)}"));
					return;
				}
				if (_userAsyncMessageCallbacks[m.GetString(0)].ContainsKey(m.Type))
				{
					_userAsyncMessageCallbacks[m.GetString(0)][m.Type].ForEach(l => l(m));
					received = true;
				}
				if (_userMessageCallbacks[m.GetString(0)].ContainsKey(m.Type))
				{
					MainThreadDispatcher.Post(_ => _userMessageCallbacks[m.GetString(0)][m.Type].ForEach(l => l(m)), null);
					received = true;
				}
//				else _userMsgList.Add(m);
			}
		}
		else
		{
			if (_asyncMessageCallbacks.ContainsKey(m.Type))
			{
				_asyncMessageCallbacks[m.Type].ForEach(l => l(m));
				received = true;
			}
			if (_messageCallbacks.ContainsKey(m.Type))
			{
				MainThreadDispatcher.Post(_=>_messageCallbacks[m.Type].ForEach(l => l(m)),null);
				received = true;
			}
		}
		if (!received)
			Observable.NextFrame().Subscribe(_ => Debug.Log($"{m.Type} Message received, but no one was listening for it :("));
	}

	public static void Send(string type, params object[] payload)
	{
		if (_peer != null && _peer.ConnectionState == ConnectionState.Connected)
			_peer.Send(type, payload);
		else Debug.LogError($"Attempted to send {type} message, but we're not connected!");
	}
	
	public static void Send(Message message)
	{
		if (_peer != null && _peer.ConnectionState == ConnectionState.Connected)
			_peer.Send(message);
		else Debug.LogError($"Attempted to send {message.Type} message, but we're not connected!");
	}
}

public static class PeerExtensions
{
	public static void Send(this NetPeer peer, string type, params object[] content)
	{
		peer.Send(new Message {Type = type, Content = content});
	}
	public static void Send(this NetPeer peer, Message message)
	{
		peer.Send(MessagePackSerializer.Serialize(message), SendOptions.ReliableOrdered);
	}
}

[MessagePackObject]
public class Message
{
	[Key(0)]
	public string Type;
	[Key(1)]
	public object[] Content;
	
	public float GetFloat(int i)
	{
	    if (Content[i] is float)
	        return (float)Content[i];
	    Console.WriteLine($"Attempted to Get Float from index {i}:{Content[i].GetType()} in {Type} message.");
	    return 0;
	}

	public string GetString(int i)
	{
	    if (Content[i] is string)
	        return (string)Content[i];
	    Console.WriteLine($"Attempted to Get String from index {i}:{Content[i].GetType()} in {Type} message.");
	    return "";
	}

	public int GetInt(int i)
	{
		if (Content[i] is int)
			return (int)Content[i];
		Console.WriteLine($"Attempted to Get Integer from index {i}:{Content[i].GetType()} in {Type} message.");
		return 0;
	}

	public bool GetBoolean(int i)
	{
		if (Content[i] is bool)
			return (bool)Content[i];
		Console.WriteLine($"Attempted to Get Boolean from index {i}:{Content[i].GetType()} in {Type} message.");
		return false;
	}
}