using UnityEngine;

namespace AK.Wwise.Unity.WwiseAddressables
{
	[PreferBinarySerialization]
	public class WwiseStreamingMediaAsset : WwiseAsset
	{
		public override string GetFilename()
		{
			return name+ ".wem";
		}

		public string GetName()
		{
			return name;
		}
	}
}
