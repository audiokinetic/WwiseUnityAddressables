using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace AK.Wwise.Unity.WwiseAddressables
{
	public class AddressableMetadata : UnityEngine.ScriptableObject
	{
		public string groupName;
		public List<string> labels = new List<string>();

		public AddressableMetadata()
		{
			labels = new List<string>(labels);
		}

		public AddressableMetadata(List<string> inLabels, string inGroupName)
		{
			labels = inLabels;
			groupName = inGroupName;
		}

		public bool IsDifferent(AddressableMetadata other)
		{
			return other.groupName != groupName || other.labels != labels;
		}
	}
}