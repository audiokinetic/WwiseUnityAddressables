using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[PreferBinarySerialization]
public abstract class WwiseAsset : ScriptableObject
{
	[SerializeField]
	[HideInInspector]
	public byte[] RawData;

	[SerializeField]
	[HideInInspector]
	public byte[] hash;

	abstract public string GetFilename();

	public int AssetSize
	{
		get
		{
			return RawData.Length;
		}
	}
}
