using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PlayerIO.GameLibrary;

namespace MushroomsUnity3DExample {

	public class LobbyPlayer : BasePlayer {
	}

    [RoomType("Lobby")]
    public class Lobby : Game<LobbyPlayer>
    {
        private List<LobbyPlayer> _readyPlayers = new List<LobbyPlayer>();

        //This method is called when a room is created
        public override void GameStarted()
        {
            //Perform actions to initialize room here
            AddTimer(() =>
                {
                    if (_readyPlayers.Count > 1)
                    {
                        var room = Guid.NewGuid().ToString("n").Substring(0, 6);
                        _readyPlayers[0].Send("Match", room);
                        _readyPlayers[0].Send("Chat", "CultPong", $"You have been matched with {_readyPlayers[1].JoinData["name"]}.");
                        _readyPlayers[1].Send("Match", room);
                        _readyPlayers[1].Send("Chat", "CultPong", $"You have been matched with {_readyPlayers[0].JoinData["name"]}.");
                    }
                },
                5000);
        }

        //This method is called when the last player leaves the room.
        public override void GameClosed()
        {
            //Do any clean-up here such as saving statistics
        }

        // This method is called whenever a player joins the room
        public override void UserJoined(LobbyPlayer player)
        {
            //Notify other players that someone joined,
            //or inform the new player of the game state
            foreach (LobbyPlayer pl in Players.Where(pl => pl.ConnectUserId != player.ConnectUserId))
            {
                pl.Send("PlayerJoined", player.ConnectUserId, player.JoinData["name"]);
                player.Send("PlayerJoined", pl.ConnectUserId, pl.JoinData["name"]);
            }
        }

        // This method is called when a player leaves the room
        public override void UserLeft(LobbyPlayer player)
        {
            if (_readyPlayers.Contains(player))
                _readyPlayers.Remove(player);

            //Notify other players the someone left
            Broadcast("PlayerLeft", player.ConnectUserId);
        }

        //This method is called before a user joins a room.
        //If you return false, the user is not allowed to join.
        public override bool AllowUserJoin(LobbyPlayer player)
        {
            return true;
        }

        //This method is called whenever a player sends a message to the room
        public override void GotMessage(LobbyPlayer player, Message message)
        {
            //Handle all different message types here
            switch (message.Type)
            {
                case "Chat":
                    Broadcast("Chat", player.JoinData["name"], message.GetString(0));
                    break;
                case "Name":
                    player.JoinData["name"] = message.GetString(0);
                    Broadcast("Name", player.ConnectUserId, message.GetString(0));
                    break;
                case "Ready":
                    _readyPlayers.Add(player);
                    player.Send("ConfirmReady");
                    break;
                case "Cancel":
                    _readyPlayers.Remove(player);
                    player.Send("ConfirmCancel");
                    break;
            }
        }
    }

    public class SinglesPlayer : BasePlayer
    {
        public int Score;
        public int PreferredSide;
        public int Paddle;
        public bool PreferencesReceived;
        public bool Ready;
        public bool Flip;
    }

    [RoomType("Singles")]
	public class Singles : Game<SinglesPlayer>
    {
        private Stopwatch _time;
        //private DateTime _gameStartTime;
        private Random _random = new Random();
        private bool _gameOver;

        private float Time => (float) _time.Elapsed.TotalSeconds;
        private float _tardiness = 0;

        // This method is called when an instance of your the game is created
        public override void GameStarted() {
			// anything you write to the Console will show up in the 
			// output window of the development server
			Console.WriteLine("Game is started: " + RoomId);
            //_gameStartTime = DateTime.Now;
            _time = Stopwatch.StartNew();
		}

		// This method is called when the last player leaves the room, and it's closed down.
		public override void GameClosed() {
			Console.WriteLine("RoomId: " + RoomId);
		}

		// This method is called whenever a player joins the game
		public override void UserJoined(SinglesPlayer player) {
			foreach(SinglesPlayer pl in Players.Where(pl => pl.ConnectUserId != player.ConnectUserId)) {
				pl.Send("PlayerJoined", player.ConnectUserId);
				player.Send("PlayerJoined", pl.ConnectUserId);
                if(pl.PreferencesReceived)
                    player.Send("Preferences", pl.Paddle, pl.PreferredSide, pl.JoinData["name"]);
            }
		}

		// This method is called when a player leaves the game
		public override void UserLeft(SinglesPlayer player) {
			Broadcast("PlayerLeft", player.ConnectUserId);
            if(Players.Any()&&!_gameOver)
                DeclareVictory(Players.First());
		}

        private void DeclareVictory(SinglesPlayer player)
        {
            player.Send("Victory", true);
            foreach (SinglesPlayer pl in Players.Where(pl => pl.ConnectUserId != player.ConnectUserId))
                pl.Send("Victory", false);
        }

        private void DelayedLaunch()
        {
            var dir = (float)(_random.Next(0, 4) * 90 + 45);
            foreach (SinglesPlayer pl in Players)
                pl.Send("Launch", Time+2.5f-_tardiness, dir * (pl.Flip ? -1 : 1));
        }

		// This method is called when a player sends a message into the server code
		public override void GotMessage(SinglesPlayer player, Message message) {
			switch(message.Type) {
                case "Preferences":
			        player.PreferencesReceived = true;
			        player.Paddle = message.GetInt(0);
			        player.PreferredSide = message.GetInt(1);
			        foreach (SinglesPlayer pl in Players.Where(pl => pl.ConnectUserId != player.ConnectUserId))
			            pl.Send("Preferences", player.Paddle, player.PreferredSide, player.JoinData["name"]);

			        if (Players.Count() > 1 && Players.All(p => p.PreferencesReceived))
			        {
			            if (Players.Skip(1).All(p => p.PreferredSide == Players.First().PreferredSide))
			                Players.First().Flip = true;
                        PlayerIO.ErrorLog.WriteError("First player is " + (Players.First().Flip ? "Flipped" : "Not Flipped"));
			            Broadcast("Start");
			        }
			        break;
                case "Ready":
			        player.Ready = true;
			        if (Players.Count() > 1 && Players.All(p => p.Ready))
			            DelayedLaunch();
                    break;
                case "Ping":
			        player.Send("Ping", message.GetFloat(0), Time);
                    break;
                case "Pong":
                    foreach (SinglesPlayer pl in Players.Where(pl => pl.ConnectUserId != player.ConnectUserId))
                        pl.Send("Pong", message.GetFloat(0), message.GetFloat(1), message.GetFloat(2), Time);
                    break;
                case "MoveUp":
			        foreach (SinglesPlayer pl in Players.Where(pl => pl.ConnectUserId != player.ConnectUserId))
                        pl.Send("MoveUp", message.GetFloat(0));
                    break;
                case "MoveDown":
                    foreach (SinglesPlayer pl in Players.Where(pl => pl.ConnectUserId != player.ConnectUserId))
                        pl.Send("MoveDown", message.GetFloat(0));
                    break;
                case "StopMoving":
                    foreach (SinglesPlayer pl in Players.Where(pl => pl.ConnectUserId != player.ConnectUserId))
                        pl.Send("StopMoving", message.GetFloat(0), message.GetFloat(1));
                    break;
                case "Bash":
                    foreach (SinglesPlayer pl in Players.Where(pl => pl.ConnectUserId != player.ConnectUserId))
                        pl.Send("Bash", message.GetFloat(0));
                    break;
                case "Hit":
                    _tardiness += message.GetFloat(7);
			        foreach (SinglesPlayer pl in Players.Where(pl => pl.ConnectUserId != player.ConnectUserId))
			            pl.Send(
			                "Hit",
			                message.GetFloat(0),
			                message.GetFloat(1) * (pl.Flip ? -1 : 1),
			                message.GetFloat(2),
			                message.GetFloat(3) * (pl.Flip ? -1 : 1),
			                message.GetFloat(4),
			                message.GetFloat(5),
			                message.GetBoolean(6),
                            message.GetFloat(7));
                    break;
                case "Goal":
			        foreach (SinglesPlayer pl in Players.Where(pl => pl.ConnectUserId != player.ConnectUserId))
			        {
			            pl.Score++;
			            pl.Send("Goal", message.GetFloat(0), message.GetFloat(1));
                        if(pl.Score>=3)
                            DeclareVictory(pl);
                        else DelayedLaunch();
			        }
			        break;
                case "Desync":
                    foreach (SinglesPlayer pl in Players.Where(pl => pl.ConnectUserId != player.ConnectUserId))
                    {
                        pl.Send("Desync");
                        DelayedLaunch();
                    }
                    break;
            }
		}
	}
}