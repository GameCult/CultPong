using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class ScoreControl : MonoBehaviour
{
	public static ScoreControl Instance
	{
		get
		{
			if (_instance != null)
				return _instance;

			_instance = (ScoreControl)FindObjectOfType(typeof(ScoreControl));
			return _instance;
		}
	}
	private static ScoreControl _instance;

	public float ArenaWidth = 3;
    public Text Label;
    public Text Label2;
    public int Player1;
    public int Player2;

    void Start ()
    {
    }
	
	void Update () {
		
	}
	
    public void Player1Win() { }

    public void Player2Win() { }
}
