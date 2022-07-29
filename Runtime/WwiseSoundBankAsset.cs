using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AK.Wwise.Unity.WwiseAddressables
{
	[PreferBinarySerialization]
	public class WwiseSoundBankAsset: WwiseAsset
	{
		[SerializeField]
		public List<string> eventNames;
		
		public override string GetRelativeFilePath()
		{
			return language == "SFX" ? name + ".bnk" : Path.Combine(language, name + ".bnk");
		}

		public string GetName()
		{
			return name;
		}
	}
}
