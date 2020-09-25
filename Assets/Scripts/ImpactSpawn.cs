using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ImpactSpawn : MonoBehaviour
{
	public Transform Prefab;
	public SpawnOrientation Orientation;

	private void OnCollisionEnter(Collision other)
	{
		if (!other.contacts.Any())
		{
			Debug.Log("Collision without contact! WTF?");
			return;
		}
		var contactPoint = other.contacts.FirstOrDefault();

		Instantiate(Prefab,
				contactPoint.point,
				Orientation == SpawnOrientation.Ignore
					? Quaternion.identity
					: Quaternion.FromToRotation(Vector3.forward,
						Orientation == SpawnOrientation.Normal ? contactPoint.normal : Vector3.Reflect(other.relativeVelocity, contactPoint.normal))).gameObject
			.SetActive(true);
	}
}

public enum SpawnOrientation
{
	Ignore, Normal, Reflect
}