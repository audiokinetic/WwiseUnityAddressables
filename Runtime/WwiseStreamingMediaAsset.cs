using System.IO;
using UnityEngine;

namespace AK.Wwise.Unity.WwiseAddressables
{
	[PreferBinarySerialization]
	public class WwiseStreamingMediaAsset : WwiseAsset
	{
		public override string GetRelativeFilePath()
		{
			return language == "SFX" ? name + ".wem" : Path.Combine(language, name + ".wem");
		}

		public string GetName()
		{
			return name;
		}
	}
}
