using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Policy;
using Kino;
using NaughtyAttributes;
using Simplex;
using TMPro;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour {

	public static GameManager Instance
	{
		get
		{
			if (_instance != null)
				return _instance;

			_instance = (GameManager)FindObjectOfType(typeof(GameManager));
			return _instance;
		}
	}
	private static GameManager _instance;

	[Header("Prefabs")]
//	public Transform WallPrefab;
	[ReorderableList]
	public Transform[] HitPrefabs;
	[ReorderableList]
	public Transform[] BashPrefabs;

	[Header("Links")]
	public Transform Ball;
	public SphereCollider BallCollider;
	public MeshRenderer[] GlowingBallComponents;
	[ReorderableList]
	public GameObject[] EnableOnStart;
	public GameObject PauseMenuGameObject;
	public TextMeshPro VictoryText;

	[Header("Materials")]
	public float BoostGlow;
	public float BashGlow;
	public Shader GlowShader;

	[Header("Sounds")] 
	public AudioSource MusicSource;
	public AudioSource SoundSource;
	[ReorderableList] public AudioClip[] ImpactSounds, BounceSounds, BashSounds, CheerSounds, BattleMusic;
	public AudioClip MenuMusic;
	public AnimationCurve CheerCurve;
	public float CheerPower = 1f;
	public float CheerDuration;

	[Header("Mechanical Properties")]
	//public float MovementSpeed;
	public int ScoreLimit = 5;
	public float BallRadius = .9f;
	public float BallBackstep = 1.5f;
	public float LaunchSpeed;
	public float LaunchDelay;

	public float ArenaWidth, ArenaLength, GoalMargin, ArenaMargin;
	public float SpeedIncrementSpan = 3;
	public float SpeedSlope = 1f;
	public float SpeedPower = 1f;
	public bool StepSpeed = true;

	public Player Left, Right;
	public OfflineControl LeftInput, RightInput;

	[ReorderableList]
	public Paddle[] Paddles;

	[Header("Networking Properties")]
	public float DesyncMargin = 5f;
	public float CrossingPredictionRatio = .5f;
	public float CompensationPerFrame = .1f;
	public float TimeStretchMultiplier = 1;
	public int FramesPerPing = 50;
	public int PingHistorySize = 7;
	public int PingMedianCount = 3;
	public TextAsset Adjectives, Nouns;
	
	[Header("Animation Properties")]
	public float StartAnimationDuration = 1;
	public float VictoryAnimationDuration = 1;
	public float VictoryAnimationSpeed = 1;
	public float BashCurveIntegrationStepSize;
	
	[Header("Camera Shake Properties")]
	public Vector3 CameraPositionAmplitudes;
	public Vector3 CameraRotationEulerAmplitudes;

	[Header("Position Noise Properties")]
	public NoiseProperties PositionNoise;
	public NoiseProperties RotationNoise;

	private bool _gameStarted = false;
	private bool _suspended = true;
	private float _gameStartTime;
	private float _startAnimationStart;
	//private float _maxBashVelocity;
	//private readonly List<Vector2> _boosts = new List<Vector2>(); // x = time, y = power
	//private readonly List<Vector3> _shakes = new List<Vector3>(); // x = time, y = amp power, z = dur power
	private Noise3 _noisePos, _noiseRot;
	//private float _hitTime;
	//private Vector3 _cameraTargetPosition;
	private Vector3 _cameraTargetRotation;
	private float _glowEmission;
	private float _paddleEmission;
	private float _cameraHeight;
	private Vector2 _ballPosition;
	private Vector2 _ballDirection;

	private DigitalGlitch _digitalGlitch;
	private AnalogGlitch _analogGlitch;

	private readonly List<InputEvent> _inputEvents = new List<InputEvent>();
	//private readonly List<HitEvent> _hitEvents = new List<HitEvent>();
	
//	private List<Action<HitEvent>> _hitEventListeners = new List<Action<HitEvent>>();
	private Vector3 _leftStartPosition;
	private Quaternion _leftStartRotation;
	private Vector3 _rightStartPosition;
	private Quaternion _rightStartRotation;
	//private PostProcessingBehaviour _postProcessing;
	private Vector2 _directionCompensation = Vector2.zero;
	private Vector2 _positionCompensation = Vector2.zero;
	private int _frame;
	private OnlineControl _onlineControl;
	private bool _readyToStart;
	private float _serverTimeOffset;
	private OfflineControl _offlineControl;
	private bool _isReady;
	private bool _paused;
	private float _delayEstimate;
	
	private List<Ping> _pings = new List<Ping>();
	private List<Pong> _pongs = new List<Pong>();
	private bool _scored;

	public bool Online { get; private set; }

	public Func<float> TimeFunction { get; private set; } = () => Time.time;

//	public void AddHitEventListener(Action<HitEvent> listener)
//	{
//		_hitEventListeners.Add(listener);
//	}
//
//	public void ClearHitEventListeners()
//	{
//		_hitEventListeners.Clear();
//	}

	public float BallSpeed(float time) => LaunchSpeed + Mathf.Pow((StepSpeed ? (int) ((time-_gameStartTime) / SpeedIncrementSpan): (time-_gameStartTime)), SpeedPower) * SpeedSlope;

	public struct Ping
	{
		public float DepartureTime, ServerTime, ArrivalTime;
	}
	
	public struct Pong
	{
		public float ClientDepartureTime, ServerDepartureTime, ClientArrivalTime, ServerArrivalTime;
	}
	
	[Serializable]
	public struct NoiseProperties
	{
		public int Octaves;
		public float Lacunarity;
		public float Frequency;
		public float Gain;
		public float Amplitude;

		public NoiseProperties(int octaves = 3, int lacunarity = 2, float frequency = .3f, float gain = .5f, float amplitude = .5f)
		{
			Octaves = octaves;
			Lacunarity = lacunarity;
			Frequency = frequency;
			Gain = gain;
			Amplitude = amplitude;
		}
	}

	public class Noise3
	{
		FastNoise[] _noises;
		public float Frequency {set
		{
			foreach (var noise in _noises)
				noise.SetFrequency(value);
		}}
		public int Octaves {set
		{
			foreach (var noise in _noises)
				noise.SetFractalOctaves(value);
		}}
		public float Lacunarity {set
		{
			foreach (var noise in _noises)
				noise.SetFractalLacunarity(value);
		}}
		public float Gain {set
		{
			foreach (var noise in _noises)
				noise.SetFractalGain(value);
		}}

		public Noise3(int seed)
		{
			_noises = Enumerable.Range(seed, 3).Select(i => new FastNoise(i)).ToArray();
		}
		
		public Vector3 Value {
			get
			{
				return new Vector3(	_noises[0].GetSimplexFractal(Instance.TimeFunction(), 0),
									_noises[1].GetSimplexFractal(Instance.TimeFunction(), 0),
									_noises[2].GetSimplexFractal(Instance.TimeFunction(), 0));
			}}
	}
	
	public static string RandomName
	{
		get
		{
			var adjectives = Instance.Adjectives.text.Split('\n').Select(s => s.Trim()).Where(s => s.Length>0).ToArray();
			var nouns = Instance.Nouns.text.Split('\n').Select(s => s.Trim()).Where(s => s.Length>0).ToArray();

			var adj = adjectives[Random.Range(0, adjectives.Length)];
			var n = nouns[Random.Range(0, nouns.Length)];
			return char.ToUpper(adj[0]) + adj.Substring(1) + char.ToUpper(n[0]) + n.Substring(1);
		}
	}

	public void Quit()
	{
		Application.Quit();
	}

	public void PlayOffline()
	{
		Online = false;

		ResetGame();
		ResetBall();

		if (PlayerPrefs.HasKey("LeftName"))
			Left.Name = PlayerPrefs.GetString("LeftName");
		else
		{
			Left.Name = RandomName;
			PlayerPrefs.SetString("LeftName", Left.Name);
		}
		Left.Control = LeftInput;
		LeftInput.Player = Left;
		Left.NameText.text = Left.Name;

		if (PlayerPrefs.HasKey("RightName"))
			Right.Name = PlayerPrefs.GetString("RightName");
		else
		{
			Right.Name = RandomName;
			PlayerPrefs.SetString("RightName", Right.Name);
		}
		Right.Control = RightInput;
		RightInput.Player = Right;
		Right.NameText.text = Right.Name;
		
		StartGame();
	}

	public void PlayOnline()
	{
		Online = true;
		
		ResetGame();
		ResetBall();
		
		var side = PlayerPrefs.GetInt("PreferredSide", 0);
		if (side == (int) Side.Left)
		{
			Left.Name = PlayerPrefs.GetString("LeftName");
			Left.NameText.text = Left.Name;
			
			Left.Control = _offlineControl = LeftInput;
			LeftInput.Player = Left;
			
			Right.Control = _onlineControl = new OnlineControl(Right);
		}
		else
		{
			Right.Name = PlayerPrefs.GetString("RightName");
			Right.NameText.text = Right.Name;
			
			Right.Control = _offlineControl = RightInput;
			RightInput.Player = Right;
			
			Left.Control = _onlineControl = new OnlineControl(Left);
		}
		
		CloudManager.AddMessageListener("Hit",
			message =>
			{
				var hitEvent = new HitEvent
				{
					Time = message.GetFloat(0),
					BallPosition = new Vector2(message.GetFloat(1), message.GetFloat(2)),
					BallDirection = new Vector2(message.GetFloat(3), message.GetFloat(4)),
					Force = message.GetFloat(5),
					Player = _onlineControl.Player,
					TimeStretch = message.GetFloat(7)
				};

				if (message.GetBoolean(6))
					hitEvent.Player.Hit = hitEvent;

				if (hitEvent.Time <= TimeFunction())
				{
					Predict(hitEvent.Time, hitEvent.BallPosition, hitEvent.BallDirection);
				}

				//Predict(hitEvent.Time, hitEvent.BallPosition, hitEvent.BallDirection);
			});
		
		CloudManager.AddMessageListener("Ping",
			message =>
			{
				var ping = new Ping {DepartureTime = message.GetFloat(0), ServerTime = message.GetFloat(1), ArrivalTime = Time.time};
				_pings.Add(ping);
				CloudManager.Send("Pong", ping.DepartureTime, ping.ServerTime, ping.ArrivalTime);
			});
		
		CloudManager.AddMessageListener("Pong",
			message =>
			{
				var pong = new Pong
				{
					ClientDepartureTime = message.GetFloat(0),
					ServerDepartureTime = message.GetFloat(1),
					ClientArrivalTime = message.GetFloat(2),
					ServerArrivalTime = message.GetFloat(3)
				};
				_pongs.Add(pong);
				
				if (_pongs.Count >= PingHistorySize)
				{
					var pings = _pings.Skip(_pings.Count - PingHistorySize) // Take only the most recent n pings
						.OrderBy(p => p.ArrivalTime - p.DepartureTime) // Sort by round trip time
						.Skip((PingHistorySize - PingMedianCount) / 2) // Discard lowest pings
						.Take(PingMedianCount).ToArray(); // Discard highest pings
					
					var pongs = _pongs.Skip(_pongs.Count - PingHistorySize) // Take only the most recent n pings
						.OrderBy(p => p.ServerArrivalTime - p.ServerDepartureTime) // Sort by round trip time
						.Skip((PingHistorySize - PingMedianCount) / 2) // Discard lowest pings
						.Take(PingMedianCount).ToArray(); // Discard highest pings
					
					if (_readyToStart)
					{
						CloudManager.Send("Ready");
						TimeFunction = () => Time.time + _serverTimeOffset;
						_readyToStart = false;
						_isReady = true;
						
						// Estimated latency is half of the round trip time
						// Estimated server time at arrival is time from server plus latency
						// Estimated offset is estimated server time at arrival minus arrival time
						// Average together all the estimates
						_serverTimeOffset = pings.Sum(p => (p.ServerTime + (p.ArrivalTime - p.DepartureTime) / 2) - p.ArrivalTime) / pings.Length;
						
						// Pings use realtime but game loop uses time
						//_serverTimeOffset += Time.time - Time.realtimeSinceStartup;
					}
				
					// Estimated latency is half of the round trip time
					// Average together all the estimates
					var ourPing = pings.Sum(p => (p.ArrivalTime - p.DepartureTime) / 2) / pings.Length;
					var enemyPing = pongs.Sum(p => (p.ServerArrivalTime - p.ServerDepartureTime) / 2) / pongs.Length;
					
					// Delay is our ping plus the opponent's
					_delayEstimate = ourPing + enemyPing;

//					Debug.Log($"Ping number {_pings.Count}, sent {ping.DepartureTime}, arrived {ping.ArrivalTime}, server time {ping.ServerTime}\nOffset is {_serverTimeOffset}");
				}
			});
		
		CloudManager.AddMessageListener("Launch", 
			message =>
			{
				ResetBall();
				var delay = message.GetFloat(0) - TimeFunction();

				float timeBehind = 0;
				if (_scored)
				{
					timeBehind = _offlineControl.Player.Hit.TimeStretch;
					Time.timeScale =  (delay + timeBehind)/delay;
				}
				
				Observable.Timer(TimeSpan.FromSeconds(delay + timeBehind)).Subscribe(_ =>
				{
					_gameStartTime = message.GetFloat(0);
					_ballDirection = (Quaternion.AngleAxis(message.GetFloat(1), Vector3.up) * Vector3.forward).Flatland();
					Time.timeScale = 1;
				});
				
				//Predict(_gameStartTime, Vector2.zero, (Quaternion.AngleAxis(message.GetFloat(1), Vector3.up) * Vector3.forward).Flatland());
			});
		
		CloudManager.AddMessageListener("Preferences", 
			message =>
			{
				_onlineControl.Player.PaddleSettings = Paddles[message.GetInt(0)];
				RefreshPaddle(_onlineControl.Player);
				// Flip hits when remote player's apparent side doesn't match how he sees himself
				//_onlineControl.FlipHits = message.GetInt(1) != (int) SideOf(_onlineControl.Player); 
			});
		
		CloudManager.AddMessageListener("Start",
			message =>
			{
				StartGame();
			});
		
		CloudManager.AddMessageListener("Goal",
			message =>
			{
				Goal(_offlineControl.Player, message.GetFloat(0));
				_scored = true;
				
			});
		
		CloudManager.AddMessageListener("Victory",
			message =>
			{
				Victory(message.GetBoolean(0)?_offlineControl.Player:_onlineControl.Player);
			});
		
		CloudManager.AddMessageListener("Desync",
			message =>
			{
				ResetBall();
				VictoryText.gameObject.SetActive(true);
				VictoryText.text = "Desync";
				VictoryText.color = Color.black;
				VictoryText.fontSharedMaterial.SetColor("_ReflectFaceColor", Color.black);
				Observable.TimerFrame(20).Subscribe(_ => VictoryText.gameObject.SetActive(false));
			});
		
		CloudManager.Send("Preferences", PlayerPrefs.GetInt(side==(int) Side.Left?"LeftPaddle":"RightPaddle",0), side);
	}

	private void Predict(float time, Vector2 position, Vector2 direction)
	{
		for (; time < TimeFunction(); time += Time.fixedDeltaTime)
		{
			var boost = (Left.Hit?.Boost(time) ?? 0) + (Right.Hit?.Boost(time) ?? 0);
			
			var delta = direction.normalized * ((BallSpeed(time) + boost) * Time.fixedDeltaTime);
					
			var wallOverlap = Mathf.Abs((position + delta).y) - ArenaWidth / 2;
			// Perform Wall Collisions
			if (wallOverlap > 0)
			{
				direction.y = -direction.y;
				position.y = position.y - wallOverlap * Mathf.Sign(position.y);
				delta.y = -delta.y;
			}
					
			RaycastHit hit;
			if (Physics.SphereCast(position.Flatland()-delta.Flatland().normalized*BallBackstep, BallRadius, delta.normalized.Flatland(), out hit, BallBackstep + delta.magnitude))
			{
				var normal = hit.normal.Flatland();
				direction = Vector2.Reflect(direction, normal);
				delta = Vector2.Reflect(delta, normal);
			}
			position += delta;
		}

		_positionCompensation = _ballPosition - position;
		_ballPosition = position;
				
		_directionCompensation = _ballDirection - direction;
		_ballDirection = direction;
	}

	private float PredictCrossing(Vector2 position, Vector2 direction)
	{
		var goalSign = Mathf.Sign(position.x)*-1;

		// If the ball is heading away from the center, no prediction is possible
		if (!Mathf.Approximately(goalSign,Mathf.Sign(direction.x)))
			return -1;
		
		var time = TimeFunction();
		for (; position.x*goalSign<ArenaLength/2*CrossingPredictionRatio; time += Time.fixedDeltaTime)
		{
			var boost = (Left.Hit?.Boost(time) ?? 0) + (Right.Hit?.Boost(time) ?? 0);
			
			var delta = direction.normalized * ((BallSpeed(time) + boost) * Time.fixedDeltaTime);
					
			// Perform Wall Collisions
			var wallOverlap = Mathf.Abs((position + delta).y) - ArenaWidth / 2;
			if (wallOverlap > 0)
			{
				direction.y = -direction.y;
				position.y = position.y - wallOverlap * Mathf.Sign(position.y);
				delta.y = -delta.y;
			}
			
			position += delta;
		}

		return time;
	}

	private void ResetGame()
	{
		_inputEvents.Clear();
		_pings.Clear();
		_pongs.Clear();
		_gameStartTime = 0;
		Left.Position = .5f;
		Right.Position = .5f;
		Left.Hit = null;
		Right.Hit = null;
		_isReady = false;
	}

	void Start ()
	{
		_digitalGlitch = Camera.main.GetComponent<DigitalGlitch>();
		_analogGlitch = Camera.main.GetComponent<AnalogGlitch>();
		// _postProcessing = Camera.main.GetComponent<PostProcessingBehaviour>();
		// _postProcessing.profile.depthOfField.enabled = false;
		
		var radAngle = Camera.main.fieldOfView * Mathf.Deg2Rad;
		var radHFOV = (float) (2 * Math.Atan(Mathf.Tan(radAngle / 2) * Camera.main.aspect));
		var hFOV = Mathf.Rad2Deg * radHFOV;
		
		var plane = new Plane(Vector3.right,Vector3.zero);
		float dist;
		var ray = new Ray(Vector3.right*(ArenaLength/2+ArenaMargin), Quaternion.AngleAxis(hFOV / 2, Vector3.forward) * Vector3.up);
		if (plane.Raycast(ray, out dist))
		{
			_cameraHeight = ray.GetPoint(dist).y;
		}
		else
		{
			Debug.Log("Camera Ray missed center plane! Check your math!");
			_cameraHeight = 10;
		}
		
		//_cameraTargetPosition = Camera.main.transform.position;
		_cameraTargetRotation = Camera.main.transform.rotation.eulerAngles;
		
		_noisePos = new Noise3(Random.Range(0, 1 << 22))
		{
			Lacunarity = PositionNoise.Lacunarity,
			Frequency = PositionNoise.Frequency,
			Gain = PositionNoise.Gain,
			Octaves = PositionNoise.Octaves
		};

		_noiseRot = new Noise3(Random.Range(0, 1 << 22))
		{
			Lacunarity = RotationNoise.Lacunarity,
			Frequency = RotationNoise.Frequency,
			Gain = RotationNoise.Gain,
			Octaves = RotationNoise.Octaves
		};

//		for (float f = 0; f < 1; f += BashCurveIntegrationStepSize)
//		{
//			var diff = Mathf.Abs(BashCurve.Evaluate(f) - BashCurve.Evaluate(f + BashCurveIntegrationStepSize));
//			if (diff > _maxBashVelocity)
//				_maxBashVelocity = diff;
//		}
		
		Left.Score = Right.Score = 0;
		Left.Position = Right.Position = .5f;
		
		_leftStartPosition = Left.Paddle.position;
		_leftStartRotation = Left.Paddle.rotation;
		_rightStartPosition = Right.Paddle.position;
		_rightStartRotation = Right.Paddle.rotation;

		Left.PaddleSettings = Paddles[Mathf.Clamp(PlayerPrefs.GetInt("LeftPaddle"),0,Paddles.Length)];
		Right.PaddleSettings = Paddles[Mathf.Clamp(PlayerPrefs.GetInt("RightPaddle"),0,Paddles.Length)];
		RefreshPaddle(Left);
		RefreshPaddle(Right);
		
		_glowEmission = Left.Glow.material.GetFloat("_Emission");
		_paddleEmission = Left.Glow.material.GetFloat("_Emission");
		
		ResetBall();
		
		InitMenu();
	}

	void Update()
	{
		if(_gameStarted && Input.GetKeyDown(KeyCode.Escape))
			PauseMenu();
		
		if (_suspended)
			return;

		if (_isReady)
		{
			if (Left.Control!=null)
				_inputEvents.AddRange(Left.Control.ProcessInput());
		
			if (Right.Control!=null)
				_inputEvents.AddRange(Right.Control.ProcessInput());
		}
		
		var bashes = new List<HitEvent>();
		if(Left.Hit!=null)
			bashes.Add(Left.Hit);
		if(Right.Hit!=null)
			bashes.Add(Right.Hit);
		
		var boost = (Left.Hit?.Boost(TimeFunction()) ?? 0) + (Right.Hit?.Boost(TimeFunction()) ?? 0);
		
		foreach (var glowingBallComponent in GlowingBallComponents)
			glowingBallComponent.material.SetFloat("_Emission", _glowEmission + boost * _glowEmission * BoostGlow);

		UpdateGlow(Left);
		UpdateGlow(Right);
		
		Vector4 glitches = bashes.Aggregate(Vector4.zero,(v, b) => v + new Vector4(b.Player.PaddleSettings.DigitalGlitching,
			                                                           b.Player.PaddleSettings.ColorDriftGlitching,
			                                                           b.Player.PaddleSettings.TrackingGlitching,
			                                                           b.Player.PaddleSettings.ScanlineGlitching) * b.Glitch(TimeFunction()));

		_digitalGlitch.intensity = glitches.x;
		_analogGlitch.colorDrift = glitches.y;
		_analogGlitch.verticalJump = glitches.z;
		_analogGlitch.scanLineJitter = glitches.w;
		
		var shakeAmplitude = (Left.Hit?.Shake(TimeFunction()) ?? 0) + (Right.Hit?.Shake(TimeFunction()) ?? 0);

		Camera.main.transform.position = Vector3.up * _cameraHeight + Vector3.Scale(_noisePos.Value,
			                                 CameraPositionAmplitudes) * shakeAmplitude * PositionNoise.Amplitude;
		Camera.main.transform.rotation = Quaternion.Euler(_cameraTargetRotation + Vector3.Scale(_noiseRot.Value,
			                                                  CameraRotationEulerAmplitudes) * shakeAmplitude * RotationNoise.Amplitude);
	}
	
	void FixedUpdate ()
	{
		if (_suspended)
			return;

		_frame++;

		if (Online)
		{
			var newHit = _onlineControl.Player.Hit;
			if (newHit!=null && newHit.Time > TimeFunction() && newHit.Time < TimeFunction() + Time.fixedDeltaTime)
			{
				_ballDirection = newHit.BallDirection;
				_ballPosition = newHit.BallPosition;
			}
			
			if(_frame%FramesPerPing==0)
				CloudManager.Send("Ping", Time.time);
			
			//if(LastInput(_onlineControl.Player, InputEventType.StopMoving, TimeFunction()).Time > TimeFunction() - Time.fixedDeltaTime)
		}
		
		//Debug.Log($"Input events: {_inputEvents.Count}");
		var bashes = new List<HitEvent>();
		if(Left.Hit!=null)
			bashes.Add(Left.Hit);
		if(Right.Hit!=null)
			bashes.Add(Right.Hit);

		var boost = (Left.Hit?.Boost(TimeFunction()) ?? 0) + (Right.Hit?.Boost(TimeFunction()) ?? 0);

		var ballSpeed = BallSpeed(TimeFunction());
		if (float.IsNaN(ballSpeed))
		{
			Debug.Log($"Ball Speed for {TimeFunction()} is NaN. Start time is {_gameStartTime}. Server time offset is {_serverTimeOffset}");
			ballSpeed = 0;
		}
		var delta = (_ballDirection.normalized + _directionCompensation) * ((ballSpeed + boost) * Time.fixedDeltaTime);
		//Debug.Log($"Time: {TimeFunction()}, boost: {boost}, speed: {BallSpeed(TimeFunction())}");

		var wallOverlap = Mathf.Abs((_ballPosition + _positionCompensation + delta).y) - ArenaWidth / 2;
		// Perform Wall Collisions
		if (wallOverlap > 0)
		{
			_directionCompensation = Vector2.zero;
			_ballDirection.y = -_ballDirection.y;
			_ballPosition.y = _ballPosition.y - wallOverlap * Mathf.Sign(_ballPosition.y);
			delta.y = -delta.y;
			SoundSource.PlayOneShot(BounceSounds[Random.Range(0,BounceSounds.Length)]);
		}
		
		var xpos = ArenaLength / 2;
		var timeIntoBash = TimeFunction() - LastInput(Right, InputEventType.Bash).Time;
		if (timeIntoBash < Right.PaddleSettings.BashDuration)
		{
			var f = timeIntoBash / Right.PaddleSettings.BashDuration;
			xpos -= Right.PaddleSettings.BashCurve.Evaluate(f) * Right.PaddleSettings.BashDistance;
		}
		Right.Paddle.position = new Vector3(xpos,0,ArenaWidth*(Right.Position+Right.PositionDelta-.5f));
		Right.PositionDelta *= 1 - CompensationPerFrame;
		
		xpos = -ArenaLength / 2;
		timeIntoBash = TimeFunction() - LastInput(Left, InputEventType.Bash).Time;
		if (timeIntoBash < Left.PaddleSettings.BashDuration)
		{
			var f = timeIntoBash / Left.PaddleSettings.BashDuration;
			xpos += Left.PaddleSettings.BashCurve.Evaluate(f) * Left.PaddleSettings.BashDistance;
		}
		Left.Paddle.position = new Vector3(xpos,0,ArenaWidth*(Left.Position+Left.PositionDelta-.5f));
		Left.PositionDelta *= 1 - CompensationPerFrame;
		
		// Perform Paddle Collisions
		RaycastHit hit;
		if (Physics.SphereCast(Ball.position-delta.Flatland().normalized*BallBackstep, BallRadius, delta.normalized.Flatland(), out hit, BallBackstep + delta.magnitude))
		{
			if (!Online || hit.transform.root == _offlineControl.Player.Paddle)
			{
	//			Debug.Log($"Collision between ball and {hit.collider.gameObject.name}");
				var normal = hit.normal.Flatland();
				_ballDirection = Vector2.Reflect(_ballDirection, normal);
				delta = Vector2.Reflect(delta, normal);
				
				if(hit.transform.root==Left.Paddle)
					HitBall(Left,hit.point.Flatland(),_ballDirection);
				else if(hit.transform.root==Right.Paddle)
					HitBall(Right,hit.point.Flatland(),_ballDirection);
			}
		}
		_ballPosition += delta;
		Ball.position = _ballPosition.Flatland() + _positionCompensation.Flatland();
		
		_positionCompensation *= 1 - CompensationPerFrame;
		_directionCompensation *= 1 - CompensationPerFrame;
		
		// Perform Paddle Penetration Penalty
		var colliders = Physics.OverlapSphere(Ball.position, BallRadius);
		if (colliders.Any())
		{
			Vector3 direction;
			float distance;
			var coll = colliders.First();
			var player = coll.transform.root == Left.Paddle ? Left : Right;
			if (!Online || player.Control == _offlineControl)
			{
				Physics.ComputePenetration(BallCollider,
					Ball.position,
					Quaternion.identity,
					coll,
					coll.transform.position,
					coll.transform.rotation,
					out direction,
					out distance);
				
				Debug.Log($"Collision with {coll.gameObject.name} in direction {direction} and distance {distance}");
	
				if (distance > float.Epsilon)
				{
					_ballPosition = _ballPosition + (direction.Flatland() * distance);
					_ballDirection = Vector2.Reflect(_ballDirection, direction);
					//_ballDirection = direction.Flatland();
					//HitBall(player,Physics.ClosestPoint(Ball.position,coll,coll.transform.position,coll.transform.rotation),_ballDirection);
				}
			}
		}
		
		float cheer = bashes.Sum(b =>
		{
			var temp = CheerCurve.Evaluate((TimeFunction() - b.Time) / CheerDuration) * Mathf.Pow(b.Force, CheerPower);
			return float.IsNaN(temp) ? 0 : temp;
		});
		
		if (Ball.position.x < -ArenaLength / 2 - GoalMargin)
		{
			if(!Online || Online && Right.Control is OnlineControl)
				Goal(Right, cheer);
			if (Online && (Right.Control is OnlineControl))
				GoalOnline(cheer);
		}
		if(Ball.position.x > ArenaLength/2 + GoalMargin)
		{
			if(!Online || Online && Left.Control is OnlineControl)
				Goal(Left, cheer);
			if (Online && (Left.Control is OnlineControl))
				GoalOnline(cheer);
		}
		
		if(Online && Mathf.Abs(Ball.position.x) > ArenaLength / 2 + GoalMargin + DesyncMargin)
			CloudManager.Send("Desync");
		
		Move(Left);
		Move(Right);
	}

	public void GoalOnline(float power)
	{
		_scored = false;
		CloudManager.Send("Goal", power);
	}

	void UpdateGlow(Player player)
	{
		var timeIntoBash = TimeFunction() - LastInput(player, InputEventType.Bash).Time;
		if (timeIntoBash < player.PaddleSettings.BashDuration)
		{
			var f = timeIntoBash / player.PaddleSettings.BashDuration;
			var diff = Mathf.Clamp01((player.PaddleSettings.BashCurve.Evaluate(f) - player.PaddleSettings.BashCurve.Evaluate(f - BashCurveIntegrationStepSize)) / player.PaddleSettings.MaxBashVelocity);
			player.Glow.material.SetFloat("_Emission", _paddleEmission+diff*BashGlow);
		} else player.Glow.material.SetFloat("_Emission", _paddleEmission);
	}

	void Move(Player player)
	{
		var stoppedMoving = LastInput(player, InputEventType.StopMoving, TimeFunction());
		var movedUp = LastInput(player, InputEventType.MoveUp, TimeFunction());
		var movedDown = LastInput(player, InputEventType.MoveDown, TimeFunction());

		//Debug.Log($"{player.Paddle.name} stopped moving at {stoppedMoving}, moved up at {movedUp} and down at {movedDown}");

		if (movedUp.Time > stoppedMoving.Time && movedUp.Time > movedDown.Time)
			player.Position = Mathf.Clamp01(player.Position + player.PaddleSettings.MovementSpeed * Time.fixedDeltaTime);
		else if (movedDown.Time > stoppedMoving.Time && movedDown.Time > movedUp.Time)
			player.Position = Mathf.Clamp01(player.Position - player.PaddleSettings.MovementSpeed * Time.fixedDeltaTime);
		else
		{
			// Set the actual position to what the message stated, then set the delta to smoothly blend away the difference
			player.PositionDelta = (player.Position + player.PositionDelta) - stoppedMoving.Position;
			player.Position = stoppedMoving.Position;
		}
	}

	public void RefreshPaddle(Player p)
	{
		var oldPaddle = p.Paddle;
		var pos = oldPaddle.position;
		Destroy(oldPaddle.gameObject);
		p.Paddle = Instantiate(p.PaddleSettings.Prefab, pos, Quaternion.LookRotation(p==Right?Vector3.left:Vector3.right));
		p.Glow = p.Paddle.GetComponentsInChildren<MeshRenderer>().First(renderer => renderer.material.shader == GlowShader);
		p.Glow.material.SetColor("_EmissionColor", p.Color);
	}

	void UpdateScore()
	{
		Left.ScoreText.text = Left.Score.ToString();
		Right.ScoreText.text = Right.Score.ToString();
	}

	public void Goal(Player player, float power)
	{
		ResetBall();
		player.Score++;
		UpdateScore();
		SoundSource.PlayOneShot(CheerSounds[ Mathf.Clamp((int)(CheerSounds.Length * power), 0, CheerSounds.Length-1)]);
		if (player.Score >= ScoreLimit && !Online)
		{
			Victory(player);
		}
		else if (!Online)
			StartCoroutine(Pause());
	}

	void Victory(Player player)
	{
		VictoryText.gameObject.SetActive(true);
		VictoryText.text = player.Name + "\nWins";
		VictoryText.color = player.Color;
		VictoryText.fontSharedMaterial.SetColor("_ReflectFaceColor", player.Color);
		StartCoroutine(AnimateVictory());
	}

	public void HitBall(Player collidingPlayer, Vector2 position, Vector2 direction)
	{
		//Debug.Log("Hit!");
		//_hitTime = Time.time;
		
		foreach (var glowingBallComponent in GlowingBallComponents)
			glowingBallComponent.material.SetColor("_EmissionColor", collidingPlayer.Color);

		var timeIntoBash = TimeFunction() - LastInput(collidingPlayer, InputEventType.Bash).Time;
		var f = timeIntoBash / collidingPlayer.PaddleSettings.BashDuration;
		var diff = Mathf.Clamp01((collidingPlayer.PaddleSettings.BashCurve.Evaluate(f) - collidingPlayer.PaddleSettings.BashCurve.Evaluate(f - BashCurveIntegrationStepSize)) / collidingPlayer.PaddleSettings.MaxBashVelocity);
		
		var hitEvent = new HitEvent
		{
			Player = collidingPlayer,
			Time = TimeFunction(),
			Force = diff,
			BallPosition = Ball.position.Flatland(),
			BallDirection = direction
		};
		//Debug.Log($"New hit at time {hitEvent.Time}, force is {hitEvent.Force}");
		
		// Set the hit, unless there's a better one still active
		var setHit = collidingPlayer.Hit==null || !(TimeFunction() - collidingPlayer.Hit.Time < collidingPlayer.PaddleSettings.BashBoostDuration &&
		               collidingPlayer.Hit.Force > hitEvent.Force);
		if (setHit)
			collidingPlayer.Hit = hitEvent;

		
		if (Online && collidingPlayer.Control is OfflineControl)
		{
				//Debug.Log($"Time: {TimeFunction()}\nPredicted Crossing Time: {crossingTime}\nPrevious Crossing Time: {timeTaken}\nTime Stretch: {timeStretch}\nTime Scale: {Time.timeScale}");
			
			var crossingTime = PredictCrossing(position, direction) - TimeFunction();
			//var timeTaken = (TimeFunction() - _onlineControl.Player.Hit.Time);
			var timeStretch = _delayEstimate * TimeStretchMultiplier;// + (1 / _onlineControl.Player.Hit.TimeStretch * timeTaken - timeTaken);
			var visibleCrossingTime = crossingTime + timeStretch + (_onlineControl.Player.Hit?.TimeStretch ?? 0);
			Time.timeScale = crossingTime / visibleCrossingTime;
			Observable.Timer(TimeSpan.FromSeconds(crossingTime)).Subscribe(_ => Time.timeScale = 1);
			CloudManager.Send("Hit", TimeFunction(), position.x, position.y, direction.x, direction.y, hitEvent.Force, true, timeStretch);
			hitEvent.TimeStretch = Time.timeScale;
		}
		
		//_hitEvents.Add(hitEvent);
//		_hitEventListeners.ForEach(action => action(hitEvent));
		
		if (timeIntoBash < collidingPlayer.PaddleSettings.BashDuration)
		{
			try
			{
				collidingPlayer.Paddle.GetComponent<AudioSource>().PlayOneShot(BashSounds[(int) (BashSounds.Length * diff)]);
			}
			catch (Exception e)
			{
				Debug.Log($"WTF?! Time into bash is {timeIntoBash}, diff is {diff}");
			}
			//_boosts.Add(new Vector2(Time.time, Mathf.Pow((diff), BashEffectivenessPower)));
			//_shakes.Add(new Vector3(Time.time, Mathf.Pow((diff), CameraShakeAmplitudePower), Mathf.Pow((diff), CameraShakeDurationPower)));
			Instantiate(BashPrefabs[(int) (BashPrefabs.Length * diff)], position.Flatland(), Quaternion.identity).gameObject
				.SetActive(true);
		}
		else
		{
			//_shakes.Add(new Vector3(Time.time, Mathf.Pow(.05f, CameraShakeAmplitudePower), Mathf.Pow(.05f, CameraShakeDurationPower)));
			collidingPlayer.Paddle.GetComponent<AudioSource>().PlayOneShot(ImpactSounds[Random.Range(0,ImpactSounds.Length)]);
			Instantiate(HitPrefabs[Random.Range(0, HitPrefabs.Length)], position.Flatland(), Quaternion.identity).gameObject
				.SetActive(true);
		}
	}

	private InputEvent LastInput(Player player, InputEventType type)
	{
		for (int i = _inputEvents.Count - 1; i >= 0; i--)
		{
			var e = _inputEvents[i];
			if (e.Player == player && e.Type == type)
				return e;
		}
		return new InputEvent {Player = player, Time = float.MinValue, Type = type};
	}

	private InputEvent LastInput(Player player, InputEventType type, float time)
	{
		for (int i = _inputEvents.Count - 1; i >= 0; i--)
		{
			var e = _inputEvents[i];
			if (e.Player == player && e.Type == type && e.Time < time)
				return e;
		}
		return new InputEvent {Player = player, Time = float.MinValue, Type = type};
	}

	public void PauseMenu()
	{
		_paused = !_paused;
		if (!Online)
		{
			_suspended = _paused;
			Time.timeScale = _suspended ? 0 : 1;
		}
		// _postProcessing.profile.depthOfField.enabled = _paused;
		PauseMenuGameObject.SetActive(_paused);
	}

	public void StartGame()
	{
		MenuManager.Instance.HideMenu(() => StartCoroutine(AnimateStart()));
	}

	public void ExitToMenu()
	{
		Left.Hit = null;
		Right.Hit = null;

		foreach (var glowingBallComponent in GlowingBallComponents)
			glowingBallComponent.material.SetFloat("_Emission", _glowEmission);
		
		TimeFunction = () => Time.time;
		Time.timeScale = 1;
		PauseMenuGameObject.SetActive(false);
		// _postProcessing.profile.depthOfField.enabled = false;
		
		ResetBall();
		InitMenu();
		
		StartCoroutine(AnimateExit());
	}

	private void InitMenu()
	{
		MusicSource.clip = MenuMusic;
		MusicSource.Play();

		_gameStarted = false;
		_suspended = true;
		foreach (var o in EnableOnStart)
		{
			o.SetActive(false);
		}
		
		Left.NameText.gameObject.SetActive(false);
		Right.NameText.gameObject.SetActive(false);
		
		MenuManager.Instance.ShowMenu(Online?1:0);
	}

	IEnumerator AnimateVictory()
	{
		var animationStart = Time.time;

		while (Time.time - animationStart < VictoryAnimationDuration)
		{
			VictoryText.fontSharedMaterial.SetMatrix(ShaderUtilities.ID_EnvMatrix, Matrix4x4.Rotate(Quaternion.Euler(0,(Time.time - animationStart)*VictoryAnimationSpeed,0)));
			//VictoryText.ForceMeshUpdate();
			yield return null;
		}
		VictoryText.gameObject.SetActive(false);
		ExitToMenu();
	}

	IEnumerator AnimateExit()
	{
		var exitAnimationStart = Time.time;
		var ballStartPosition = Ball.position;
		
		while (Time.time - exitAnimationStart < StartAnimationDuration)
		{
			var tween = (Time.time - exitAnimationStart) / StartAnimationDuration;
			Ball.position = Vector3.Lerp(ballStartPosition, Vector3.zero, tween);
			
			Left.Paddle.position = Vector3.Lerp(new Vector3(-ArenaLength/2,0,ArenaWidth*(Left.Position-.5f)), _leftStartPosition, tween);
			//Left.Paddle.rotation = Quaternion.Slerp(Quaternion.LookRotation(Vector3.right, Vector3.up), _leftStartRotation, tween);
			
			Right.Paddle.position = Vector3.Lerp(new Vector3(ArenaLength/2,0,ArenaWidth*(Right.Position-.5f)), _rightStartPosition, tween);
			//Right.Paddle.rotation = Quaternion.Slerp(Quaternion.LookRotation(Vector3.left, Vector3.up), _rightStartRotation, tween);

			yield return null;
		}
	}

	IEnumerator AnimateStart()
	{
		_startAnimationStart = Time.time;
		var cameraStartPosition = Camera.main.transform.position;
		var cameraStartRotation = Camera.main.transform.rotation;

		while (Time.time - _startAnimationStart < StartAnimationDuration)
		{
			var tween = (Time.time - _startAnimationStart) / StartAnimationDuration;
			Camera.main.transform.position = Vector3.Lerp(cameraStartPosition, Vector3.up * _cameraHeight, tween);
			Camera.main.transform.rotation = Quaternion.Slerp(cameraStartRotation, Quaternion.LookRotation(Vector3.down, Vector3.forward), tween);
			
			Left.Paddle.position = Vector3.Lerp(_leftStartPosition, new Vector3(-ArenaLength/2,0,0), tween);
			//Left.Paddle.rotation = Quaternion.Slerp(_leftStartRotation, Quaternion.LookRotation(Vector3.right, Vector3.up), tween);
			
			Right.Paddle.position = Vector3.Lerp(_rightStartPosition, new Vector3(ArenaLength/2,0,0), tween);
			//Right.Paddle.rotation = Quaternion.Slerp(_rightStartRotation, Quaternion.LookRotation(Vector3.left, Vector3.up), tween);

			yield return null;
		}

		Left.ScoreText.color = Left.Color;
		Right.ScoreText.color = Right.Color;
		Left.Score = 0;
		Right.Score = 0;
		UpdateScore();
		
		MusicSource.clip = BattleMusic[Random.Range(0, BattleMusic.Length)];
		MusicSource.Play();

		_gameStarted = true;
		_suspended = false; 
		
		foreach (var o in EnableOnStart)
		{
			o.SetActive(true);
		}
		Left.NameText.gameObject.SetActive(true);
		Right.NameText.gameObject.SetActive(true);
		if (Online)
		{
			_readyToStart = true;
		}
		else
			StartCoroutine(Pause());
	}

	private void ResetBall()
	{
		GlowingBallComponents[0].material.SetColor("_EmissionColor", Left.Color);
		GlowingBallComponents[1].material.SetColor("_EmissionColor", Right.Color);
		_directionCompensation = Vector2.zero;
		_positionCompensation = Vector2.zero;
		_ballPosition = Vector2.zero;
		_ballDirection = Vector3.zero;
		BallCollider.radius = BallRadius;
	}
        
	IEnumerator Pause()
	{
		//_paused = true;
		_isReady = true;

		yield return new WaitForSeconds(LaunchDelay);
		
		_ballDirection = new Vector2(Random.Range(0, 2) == 0 ? -1 : 1, Random.Range(0, 2) == 0 ? -1 : 1);
		//_paused = false;
		_gameStartTime = TimeFunction();
	}

	public static Side SideOf(Player p) => Instance.Left == p ? Side.Left : Side.Right;
}

public class GameEvent
{
	public float Time;
}

public class InputEvent : GameEvent
{
	public Player Player;
	public InputEventType Type;
	public float Position;
}

public class HitEvent : GameEvent
{
	public Player Player;
	public float Force;
	public Vector2 BallDirection;
	public Vector2 BallPosition;
	public float TimeStretch;

	public float Boost(float time)
	{
		var temp = Mathf.Clamp01(Player.PaddleSettings.BashBoostCurve.Evaluate(Mathf.Clamp01((time - Time) / Player.PaddleSettings.BashBoostDuration))) *
		           Mathf.Pow(Force, Player.PaddleSettings.BashBoostPower) * Player.PaddleSettings.BashBoostSpeed;
		return float.IsNaN(temp) ? 0 : temp;
	}

	public float Shake(float time)
	{
		var temp = Mathf.Clamp01(Player.PaddleSettings.CameraShakeCurve.Evaluate(Mathf.Clamp01((time - Time) / Player.PaddleSettings.CameraShakeDuration))) *
		           Mathf.Pow(Force, Player.PaddleSettings.CameraShakeAmplitudePower);
		return float.IsNaN(temp) ? 0 : temp;
	}

	public float Glitch(float time)
	{
		var temp = Mathf.Clamp01(Player.PaddleSettings.GlitchCurve.Evaluate(Mathf.Clamp01((time - Time) / Player.PaddleSettings.GlitchDuration))) *
		           Mathf.Pow(Force, Player.PaddleSettings.GlitchAmplitudePower);
		return float.IsNaN(temp) ? 0 : temp;
	}
}

[Serializable]
public class Player
{
	public TextMeshPro ScoreText;
	public TextMeshPro NameText;
	public Transform Paddle;
	public Paddle PaddleSettings;
	public Color Color;

	[HideInInspector] public string Name;
	[HideInInspector] public MeshRenderer Glow;
	[HideInInspector] public int Score;
	[HideInInspector] public float Position;
	[HideInInspector] public float PositionDelta;
	[HideInInspector] public HitEvent Hit;

	//[HideInInspector]
	public PaddleControl Control;
	//public float BashTime;
}

[Serializable]
public abstract class PaddleControl
{
	[HideInInspector]
	public Player Player;

	protected PaddleControl(Player player)
	{
		Player = player;
	}
	public abstract IEnumerable<InputEvent> ProcessInput();
}

[Serializable]
public class OnlineControl : PaddleControl
{
	private List<InputEvent> _incomingQueue = new List<InputEvent>();
	//public bool FlipHits { get; set; }
//	private float _movementDelta;
	
	public override IEnumerable<InputEvent> ProcessInput()
	{
//		var compensation = _movementDelta * GameManager.Instance.CompensationPerFrame;
//		Player.Position += compensation;
//		_movementDelta -= compensation;
		
		var array = _incomingQueue.ToArray();
		_incomingQueue.Clear();
		return array;
	}

	public OnlineControl(Player player) : base(player)
	{
		CloudManager.AddMessageListener("MoveUp", message =>
		{
			var input = new InputEvent {Player = player, Time = message.GetFloat(0), Type = InputEventType.MoveUp};
			// Jump the actual position ahead by how much they would have moved had they started at the event time, then blend away the delta
//			var delta = (GameManager.Instance.TimeFunction() - input.Time) * player.PaddleSettings.MovementSpeed;
//			player.Position += delta;
//			player.PositionDelta -= delta;
			_incomingQueue.Add(input);
		});
		CloudManager.AddMessageListener("MoveDown", message =>
		{
			var input = new InputEvent {Player = player, Time = message.GetFloat(0), Type = InputEventType.MoveDown};
			// Jump the actual position ahead by how much they would have moved had they started at the event time, then blend away the delta
//			var delta = (GameManager.Instance.TimeFunction() - input.Time) * player.PaddleSettings.MovementSpeed;
//			player.Position -= delta;
//			player.PositionDelta += delta;
			_incomingQueue.Add(input);
		});
		CloudManager.AddMessageListener("Bash", message =>
		{
			var input = new InputEvent {Player = player, Time = message.GetFloat(0), Type = InputEventType.Bash};
			_incomingQueue.Add(input);
		});
		
		CloudManager.AddMessageListener("StopMoving", message =>
		{
			var input = new InputEvent {Player = player, Time = message.GetFloat(0), Type = InputEventType.StopMoving, Position = message.GetFloat(1)};
			_incomingQueue.Add(input);
		});
		
	}
}

[Serializable]
public class OfflineControl : PaddleControl
{
	public KeyCode UpKey, DownKey, BashKey;
	public override IEnumerable<InputEvent> ProcessInput()
	{
		var inputEvents = new List<InputEvent>();

		//Debug.Log("Processing input");
		if (Input.GetKeyDown(UpKey))
		{
			if(GameManager.Instance.Online)
				CloudManager.Send("MoveUp", GameManager.Instance.TimeFunction());
			inputEvents.Add(new InputEvent{Player = Player,Time = GameManager.Instance.TimeFunction(),Type = InputEventType.MoveUp});
		}
		if (Input.GetKeyDown(DownKey))
		{
			if(GameManager.Instance.Online)
				CloudManager.Send("MoveDown", GameManager.Instance.TimeFunction());
			inputEvents.Add(new InputEvent{Player = Player,Time = GameManager.Instance.TimeFunction(),Type = InputEventType.MoveDown});
		}
		if (Input.GetKeyDown(BashKey))
		{
			if(GameManager.Instance.Online)
				CloudManager.Send("Bash", GameManager.Instance.TimeFunction());
			inputEvents.Add(new InputEvent{Player = Player,Time = GameManager.Instance.TimeFunction(),Type = InputEventType.Bash});
		}

		if (Input.GetKeyUp(UpKey) && !Input.GetKey(DownKey) ||
		    Input.GetKeyUp(DownKey) && !Input.GetKey(UpKey))
		{
			if(GameManager.Instance.Online)
				CloudManager.Send("StopMoving", GameManager.Instance.TimeFunction(), Player.Position);
			inputEvents.Add(new InputEvent{Player = Player,Time = GameManager.Instance.TimeFunction(),Type = InputEventType.StopMoving});
		}

		return inputEvents;
	}

	public OfflineControl(Player player) : base(player)
	{
	}
}

public static class VectorExtensions
{
	public static Vector3 Flatland(this Vector2 vec)
	{
		return new Vector3(vec.x,0,vec.y);
	}

	public static Vector2 Flatland(this Vector3 vec)
	{
		return new Vector2(vec.x,vec.z);
	}
}

public enum InputEventType
{
	MoveUp = 0, MoveDown = 1, StopMoving = 2, Bash = 3
}

public enum Side
{
	Left = 0, Right = 1
}
