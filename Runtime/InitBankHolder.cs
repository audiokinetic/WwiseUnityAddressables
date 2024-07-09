/*******************************************************************************
The content of this file includes portions of the proprietary AUDIOKINETIC Wwise
Technology released in source code form as part of the game integration package.
The content of this file may not be used without valid licenses to the
AUDIOKINETIC Wwise Technology.
Note that the use of the game engine is subject to the Unity(R) Terms of
Service at https://unity3d.com/legal/terms-of-service
 
License Usage
 
Licensees holding valid licenses to the AUDIOKINETIC Wwise Technology may use
this file in accordance with the end user license agreement provided with the
software or, alternatively, in accordance with the terms contained
in a written agreement between you and Audiokinetic Inc.
Copyright (c) 2024 Audiokinetic Inc.
*******************************************************************************/

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