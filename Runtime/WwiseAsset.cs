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

	[SerializeField]
	[HideInInspector]
	public string language;

	abstract public string GetRelativeFilePath();

	public int AssetSize
	{
		get
		{
			return RawData.Length;
		}
	}
}
