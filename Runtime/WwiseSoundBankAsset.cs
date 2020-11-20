using System.Collections.Generic;
using UnityEngine;

namespace AK.Wwise.Unity.WwiseAddressables
{
	[PreferBinarySerialization]
	public class WwiseSoundBankAsset: WwiseAsset
	{
		[SerializeField]
		public List<string> eventNames;
		
		public override string GetFilename()
		{
			return name + ".bnk";
		}

		public string GetName()
		{
			return name;
		}
	}
}
