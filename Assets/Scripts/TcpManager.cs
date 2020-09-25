using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using MessagePack;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class TcpManager : MonoBehaviour
{
	public static TcpManager Instance
	{
		get
		{
			if (_instance != null)
				return _instance;

			_instance = (TcpManager)FindObjectOfType(typeof(TcpManager));
			return _instance;
		}
	}
	private static TcpManager _instance;
	
	private TcpClient _client;
	private BinaryReader _binaryReader;
	private BinaryWriter _binaryWriter;

	private TcpListener _server;
	
	private readonly Dictionary<string, List<Action<TcpMessage>>> _messageCallbacks = new Dictionary<string, List<Action<TcpMessage>>>();
	private Action<string> _logger;
	private Action _onConnect;

	public void AddMessageListener(string messageType, Action<TcpMessage> callback)
	{
		if(!_messageCallbacks.ContainsKey(messageType))
			_messageCallbacks[messageType] = new List<Action<TcpMessage>>();
		_messageCallbacks[messageType].Add(callback);
		Debug.Log($"Registered \"{messageType}\" listener");
	}
	
	public void ClearMessageListeners()
	{
		_messageCallbacks.Clear();
	}

	private void Start ()
	{
	}

	public void Host(Action<string> logger, Action onConnect)
	{
		_logger = s => Observable.NextFrame(FrameCountType.Update).Subscribe(_ => logger(s));
		_server = new TcpListener(IPAddress.Any, PlayerPrefs.GetInt("Port", 7777));
		_server.Start();
		_onConnect = () => Observable.NextFrame(FrameCountType.Update).Subscribe(_ => onConnect());
	}

	public void Connect(string ip, Action<string> logger, Action onConnect)
	{
		_logger = s => Observable.NextFrame(FrameCountType.Update).Subscribe(_ => logger(s));
		_client = new TcpClient();
		_client.BeginConnect(IPAddress.Parse(ip), PlayerPrefs.GetInt("Port", 7777),
			result =>
			{
				TcpClient tcpClient = (TcpClient) result.AsyncState;
				tcpClient.EndConnect(result);

				if (tcpClient.Connected)
				{
					logger("Client connected");
					_binaryReader = new BinaryReader(tcpClient.GetStream());
					_binaryWriter = new BinaryWriter(tcpClient.GetStream());
				}
				else
				{
					logger("Client connection refused");
				}

				Observable.NextFrame(FrameCountType.Update).Subscribe(_ => onConnect());
			}, _client);
	}

	private void Update () {
		if(_client!=null)
			if (_client.Connected)
			{
				if (_client.GetStream().DataAvailable)
				{
					var clientStream = _client.GetStream();
					var inBuffer = new byte[65535];
	
	
					var binaryFormatter = new BinaryFormatter();
					MemoryStream memoryStream;
					using (memoryStream = new MemoryStream())
					{
						do
						{
							var readBytes = clientStream.Read(inBuffer, 0, inBuffer.Length);
							memoryStream.Write(inBuffer, 0, readBytes);
						} while (clientStream.DataAvailable);
					}
					var byteMessage = memoryStream.ToArray();
					
					var message = MessagePackSerializer.Deserialize<TcpMessage>(byteMessage);
					
					if(_messageCallbacks.ContainsKey(message.Type))
						foreach (var act in _messageCallbacks[message.Type])
							act(message);
					
					_logger($"Message received of type {message.Type} with content [{message.Content.Aggregate("",(s, o) => $"{s}({o.GetType()}:{o.ToString()})")}]");
				}
			}
		
		if(_server!=null)
			if (_server.Pending())
			{
				_logger("New Pending Connection");
				_server.BeginAcceptTcpClient(result =>
					{
						TcpListener tcpListener = (TcpListener) result.AsyncState;
						_client = tcpListener.EndAcceptTcpClient(result);
						if (_client.Connected)
						{
							_logger("Accepted new connection");

							_binaryReader = new BinaryReader(_client.GetStream());
							_binaryWriter = new BinaryWriter(_client.GetStream());
						}
						else
						{
							_logger("Refused new connection");
						}

						_onConnect();
					},
					_server);
			}
	}
	

	public void Send(TcpMessage message)
	{
		_binaryWriter.Write(MessagePackSerializer.Serialize(message));
	}

	public void Send(string type, params object[] content)
	{
		_binaryWriter.Write(MessagePackSerializer.Serialize(new TcpMessage{Type = type,Content = content}));
	}
}

[MessagePackObject]
public class TcpMessage
{
	[Key(0)]
	public string Type;
	[Key(1)]
	public object[] Content;

	public float GetFloat(int i)
	{
		if (Content[i] is float)
			return (float) Content[i];
		else
		{
			Debug.LogError($"Attempted to GetFloat on {Content[i].GetType()} in {Type} message.");
			return 0;
		}
	}

	public string GetString(int i)
	{
		if (Content[i] is string)
			return (string) Content[i];
		else
		{
			Debug.LogError($"Attempted to GetFloat on {Content[i].GetType()} in {Type} message.");
			return "";
		}
	}
}
