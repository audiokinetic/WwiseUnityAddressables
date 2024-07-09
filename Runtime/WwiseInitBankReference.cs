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

using System.Collections;
using System.Collections.Generic;
using AK.Wwise.Unity.WwiseAddressables;
using UnityEditor;
using UnityEngine;

namespace AK.Wwise.Unity.WwiseAddressables
{
	public class WwiseInitBankReference : ScriptableObject
	{
		public WwiseAddressableSoundBank AddressableBank;
		public const string InitBankName = "Init";

#if UNITY_EDITOR

		public void OnEnable()
		{
			AkAssetUtilities.AddressableBankUpdated += UpdateAddressableBankReference;
		}

		public void SetAddressableBank(WwiseAddressableSoundBank asset)
		{
			if (asset != null)
			{
				AddressableBank = asset;
				EditorUtility.SetDirty(this);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
		}

		public bool UpdateAddressableBankReference(WwiseAddressableSoundBank asset, string name)
		{
			if (this == null)
			{
				return false;
			}
			if (name == InitBankName)
			{
				SetAddressableBank(asset);
				return true;
			}

			return false;
		}


		public static bool FindInitBankReferenceAndSetAddressableBank(WwiseAddressableSoundBank addressableAsset,
			string name)
		{
			WwiseInitBankReference asset;
			var guids = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(WwiseInitBankReference).Name,
				new string[] {AkWwiseEditorSettings.WwiseScriptableObjectRelativePath});
			foreach (var assetGuid in guids)
			{
				var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
				asset = UnityEditor.AssetDatabase.LoadAssetAtPath<WwiseInitBankReference>(assetPath);
				if (asset)
				{
					asset.SetAddressableBank(addressableAsset);
				}
			}

			return guids.Length > 0;
		}

		public void OnDestroy()
		{
			AkAssetUtilities.AddressableBankUpdated -= UpdateAddressableBankReference;
		}
#endif
    }
}
#endif