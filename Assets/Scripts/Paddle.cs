using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Paddle", menuName = "CultPong/Paddle", order = 1)]
public class Paddle : ScriptableObject
{
	public Transform Prefab;
	public float MovementSpeed;

	[Header("Bash Properties")]
	public AnimationCurve BashCurve;
	public AnimationCurve BashBoostCurve;
	public float BashBoostDuration, BashBoostPower, BashDuration, BashDistance, BashBoostSpeed;

	[Header("Glitch Properties")]
	public AnimationCurve GlitchCurve;
	public float GlitchDuration;
	public float GlitchAmplitudePower;
	public float DigitalGlitching;
	public float ScanlineGlitching;
	public float TrackingGlitching;
	public float ColorDriftGlitching;
	
	[Header("Camera Shake Properties")]
	public AnimationCurve CameraShakeCurve;
	public float CameraShakeDuration;
	public float CameraShakeAmplitudePower;
	//public float CameraShakeDurationPower;

	private float _maxBashVelocity = -1f;

	public float MaxBashVelocity
	{
		get
		{
			if (_maxBashVelocity > 0) return _maxBashVelocity;
			
			for (float f = 0; f < 1; f += GameManager.Instance.BashCurveIntegrationStepSize)
			{
				var diff = Mathf.Abs(BashCurve.Evaluate(f) - BashCurve.Evaluate(f + GameManager.Instance.BashCurveIntegrationStepSize));
				if (diff > _maxBashVelocity)
					_maxBashVelocity = diff;
			}
			return _maxBashVelocity;
		}
	}
}
