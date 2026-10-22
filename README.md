# CultPong

Competitive arcade Pong with expressive paddle shapes, violent momentum swings and a bash mechanic that turns defense into a kill shot (1v1 and 2v2). A Unity prototype from September 2020. It is dormant, and the initial commit is marked "broken due to version update": it is not known to open or run today.

No successor repo exists. The public description is the project page in `GameCult/gamecult-site` (`GameCult/Projects/CultPong.md`).

## Where to look

- `Assets/MultiPong.unity`: the main scene. `Assets/Scripts/`: gameplay and UI scripts (`Ball.cs`, `CloudManager.cs`, `DragField.cs`, and others).
- `Assets/NetProtocol.txt`: the message protocol between lobby and game clients and the server, the most self-contained design document here.
- `Serverside Code/`: the Player.IO multiplayer backend: `Game Code/Game.cs`, `Player.IO Test Server Project/Startup.cs`, and the vendored `Multiplayer/Player.IO GameLibrary.dll` with a development server executable. Player.IO is a hosted multiplayer service; the dependency is external.
- `ProjectSettings/`: Unity 2020.1.6f1.

License: see `LICENSE`.
