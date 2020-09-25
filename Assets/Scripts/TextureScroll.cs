using System.Collections;
using System.Collections.Generic;
using System.Xml;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[ExecuteInEditMode]
public class TextureScroll : MonoBehaviour
{
	public Vector2 TextureScrollSpeed;
	private MeshRenderer _targetMesh;

	private void OnWillRenderObject()
	{
		if(_targetMesh==null) _targetMesh = GetComponent<MeshRenderer>();
		
#if UNITY_EDITOR
		float time = (float) EditorApplication.timeSinceStartup;
#else
		float time = Time.time;
#endif
		
		// Application.isPlaying ? Time.time : (float) EditorApplication.timeSinceStartup;
		_targetMesh.sharedMaterial.mainTextureOffset = new Vector2(Mathf.Repeat(TextureScrollSpeed.x * time, 1), Mathf.Repeat(TextureScrollSpeed.y * time, 1));
	}
}
