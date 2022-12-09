#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES

using UnityEngine;

namespace AK.Wwise.Unity.WwiseAddressables
{
	public class InitBankHolder : MonoBehaviour
	{
		public WwiseInitBankReference InitBank;

		public WwiseAddressableSoundBank GetAddressableInitBank()
		{
			if (InitBank == null)
			{
#if UNITY_EDITOR
				var guids = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(WwiseInitBankReference).Name,
				new string[] {AkWwiseEditorSettings.WwiseScriptableObjectRelativePath});
				if (guids.Length >0)
				{
                    var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    InitBank = UnityEditor.AssetDatabase.LoadAssetAtPath<WwiseInitBankReference>(assetPath);
					if (InitBank)
					{
						return InitBank.AddressableBank;
					}
				}
#endif
				return null;
			}

			return InitBank.AddressableBank;
		}
	}
}

#endif //AK_WWISE_ADDRESSABLES