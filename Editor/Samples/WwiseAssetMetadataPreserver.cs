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

using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace AK.Wwise.Unity.WwiseAddressables
{
	/* 
	   This class provides the functionality preserve and restore Wwise addressable asset group and label metadata.
	   This information is lost when the assets are deleted, and clearing the Wwise addressable asset folder is 
	   the simplest way to repair broken WwiseAddressableSoundBanks.
	   This class should be seen as an example of how to implement this functionality and not a complete solution.
	*/
	[InitializeOnLoad]
	class WwiseAddressableAssetMetadataPreserver
	{
		static WwiseAddressableAssetMetadataPreserver()
		{
			if (AkAddressablesEditorSettings.Instance.UseSampleMetadataPreserver)
			{
				BindMetadataDelegate();
			}
		}
		
		//Sets the GetAssetMetadataDelegate on the WwiseBankPostProcess AssetPostProcessor (which handles importing of .bnk and .wem files)
		public static void BindMetadataDelegate()
		{
			WwiseBankPostProcess.GetAssetMetadataDelegate += GetMetadata;
		}

		//Unset the delegate (when the feature is disabled)
		public static void UnbindMetaDataDelegate()
		{
			WwiseBankPostProcess.GetAssetMetadataDelegate -= GetMetadata;
		}

		//Creates a AddressableMetadata asset for each Addressable wwise asset that keeps track of groups and labels.
		//Metadata assets are created in their own folder in a hierarchy matching the wwise assets
		//The folder hierarchy and asset name are used to match the assets on import
		[UnityEditor.MenuItem("Assets/Wwise/Addressables/Serialize addressable asset metadata")]
		public static void PreserveAllWwiseAssetMetadata()
		{
			AddressableAssetSettings addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
			foreach (AddressableAssetGroup group in addressableSettings.groups)
			{
				foreach (AddressableAssetEntry assetEntry in group.entries)
				{
					if (assetEntry.MainAsset)
					{
						if (assetEntry.MainAsset.GetType().IsSubclassOf(typeof(WwiseAsset)))
						{
							CreateMetadataAsset(assetEntry.AssetPath, assetEntry.labels, group.name);
						}
					}
				}
			}
		}

		[UnityEditor.MenuItem("Assets/Wwise/Addressables/Preserve addressable asset metadata", true)]
		public static bool ValidatePreserveAllWwiseAssetMetadata()
		{
			return AkAddressablesEditorSettings.Instance.UseSampleMetadataPreserver;
		}

		//Create the necessary subfolders and the metadata asset
		public static void CreateMetadataAsset(string assetPath, HashSet<string> assetLabels, string groupName)
		{
			string soundbankPath = assetPath.Replace(AkAssetUtilities.GetSoundbanksPath(), "");
			var splitBankPath = soundbankPath.Split('/');
			AddressableMetadata metaDataAsset = ScriptableObject.CreateInstance<AddressableMetadata>();
			metaDataAsset.labels = new List<string>(assetLabels);
			metaDataAsset.groupName = groupName;

			var rootPath = AkAddressablesEditorSettings.Instance.MetadataPath;

			if (!Directory.Exists(Path.Combine(Application.dataPath, rootPath)))
			{
				UnityEditor.AssetDatabase.CreateFolder("Assets", rootPath);
			}

			for (int i = 1; i < splitBankPath.Length - 1; i++)
			{
				if (!Directory.Exists(Path.Combine(Application.dataPath, Path.Combine(rootPath, splitBankPath[i]))))
				{
					AssetDatabase.CreateFolder(Path.Combine("Assets", rootPath), splitBankPath[i]);
				}
				rootPath = Path.Combine(rootPath, splitBankPath[i]);
			}

			string assetMetadataPath = Path.Combine("Assets", rootPath, Path.GetFileNameWithoutExtension(assetPath) + ".asset");
			AddressableMetadata oldAsset = AssetDatabase.LoadAssetAtPath<AddressableMetadata>(assetMetadataPath);
			if (oldAsset)
			{
				if (!metaDataAsset.IsDifferent(oldAsset))
				{
					return;
				}
			}

			UnityEditor.AssetDatabase.CreateAsset(metaDataAsset, assetMetadataPath);
		}

		//We know where the metadata asset should be located based on its platfrom and language.
		public static AddressableMetadata FindMetadataAsset(string assetName, string platformName, string languageName)
		{
			string MetadataAssetPath;
			if (languageName !="SFX")
			{
				MetadataAssetPath = Path.Combine("Assets", AkAddressablesEditorSettings.Instance.MetadataPath, platformName, languageName, Path.GetFileNameWithoutExtension(assetName) + ".asset");
			}
			else
			{
				MetadataAssetPath = Path.Combine("Assets", AkAddressablesEditorSettings.Instance.MetadataPath, platformName, Path.GetFileNameWithoutExtension(assetName) + ".asset");
			}
			return AssetDatabase.LoadAssetAtPath<AddressableMetadata>(MetadataAssetPath);
		}

		//Called when improting .bnk and .wem files. Will attempt to find an existing metadata object in the project and load it. 
		public static bool GetMetadata(string assetName, string platformName, string languageName, ref AddressableMetadata metaData )
		{
			AddressableMetadata asset = FindMetadataAsset(assetName, platformName, languageName);
			if ( asset )
			{
				metaData = asset;
				return true;
			}
			return false;
		}
	}
}

#endif