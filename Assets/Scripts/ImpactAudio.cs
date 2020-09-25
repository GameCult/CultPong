using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImpactAudio : MonoBehaviour
{
	public AudioClip[] Sounds;
	private AudioSource _source;

	private void OnCollisionExit(Collision other)
	{
		_source.PlayOneShot(Sounds[Random.Range(0,Sounds.Length)]);
	}

	private void Start()
	{
		_source = GetComponent<AudioSource>();
	}
}
