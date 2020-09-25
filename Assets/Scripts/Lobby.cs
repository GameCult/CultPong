using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class Lobby : MonoBehaviour
{
	public TextMeshProUGUI LogText;
	public TextMeshProUGUI UserList;
	public TMP_InputField ChatInput;
	public TextMeshProUGUI CasualButtonLabel;
	public Button CasualButton;

    
	//public Button AcceptButton;
	//public Button ChallengeButton;

	//private TMP_TextEventHandler _userListEvent;
	private int _hoverLine;
	//private List<ChatEntry> _chatEntries = new List<ChatEntry>();
	private List<LobbyUser> _users = new List<LobbyUser>();

	private bool _waiting;
   


    
	void Start ()
	{
		//CasualButton.gameObject.SetActive(true);
		
		// _userListEvent = UserList.GetComponent<TMP_TextEventHandler>();
		// _userListEvent.onLineSelection.AddListener((text, index, length) =>
		// {
		// 	if (_hoverLine == index) return;
		// 	
		// 	_hoverLine = index;
		// 	RefreshUserList();
		// });
		
		ChatInput.onEndEdit.AddListener(chat =>
		{
			CloudManager.Send("Chat", chat);
			ChatInput.text = "";
		});
		
		_users.Clear();
		CasualButtonLabel.text = "Ready";
		CasualButton.interactable = false;
		
		string playerName;
		var sidePref = PlayerPrefs.GetInt("PreferredSide",0)==0?"LeftName":"RightName";
		if (PlayerPrefs.HasKey(sidePref))
			playerName = PlayerPrefs.GetString(sidePref);
		else
		{
			playerName = GameManager.RandomName;
			PlayerPrefs.SetString(sidePref, playerName);
		}
		
		CloudManager.AddMessageListener("Chat", message => LogText.text += $"\n{message.GetString(0)}: {message.GetString(1)}");
		CloudManager.AddMessageListener("PlayerJoined",
			message =>
			{
				var newUser = new LobbyUser {ID = message.GetString(0), Name = message.GetString(1)};
				if(_users.Any(u=>u.ID==newUser.ID))
					Debug.LogError($"User {newUser.ID}, \"{newUser.Name}\" joined but already exists! WTF?");
				_users.Add(newUser);
				RefreshUserList();
			});
		CloudManager.AddMessageListener("PlayerLeft",
			message =>
			{
				_users.RemoveAll(user => user.ID == message.GetString(0));
				RefreshUserList();
			});
		CloudManager.AddMessageListener("Name", message =>
		{
			var user = _users.FirstOrDefault(u => u.ID == message.GetString(0));
			if (user != null)
				user.Name = message.GetString(1);
		});
		CloudManager.AddMessageListener("ConfirmReady", message =>
		{
			_waiting = true;
			CasualButtonLabel.text = "Cancel";
			LogText.text += "\nYou have been entered into the matchmaking queue.";
		});
		CloudManager.AddMessageListener("ConfirmCancel", message =>
		{
			_waiting = false;
			CasualButtonLabel.text = "Ready";
			LogText.text += "\nYou have left the matchmaking queue.";
		});
		CloudManager.AddMessageListener("Match", message =>
		{
			_waiting = false;
			CasualButtonLabel.text = "Connecting...";
			CasualButton.interactable = false;
		});
	}

	public void Ready()
	{
		CloudManager.Send(_waiting?"Cancel":"Ready");
	}

	void RefreshUserList()
	{
		var builder = new StringBuilder();
		for (var i = 0; i < _users.Count; i++)
		{
			if (i == _hoverLine)
				builder.Append("<u><b>");
			
			var user = _users[i];
			builder.Append(user.Name);
			
			if (i == _hoverLine)
				builder.Append("</u></b>");
			
			builder.AppendLine();
		}
		UserList.text = builder.ToString();
	}
	
	void Update () {
	}
}

public class LobbyUser
{
	public string ID;
	public string Name;
}

/*
public class ChatEntry
{
	public string Text = "";
	public string Source = "";
}*/
