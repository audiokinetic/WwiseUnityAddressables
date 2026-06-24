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
Copyright (c) 2026 Audiokinetic Inc.
*******************************************************************************/

#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES

namespace AK.Wwise.Unity.WwiseAddressables
{
	[System.Obsolete(AkUnitySoundEngine.Migrated_Setting_AkOption_2026_1_2)]
	public class AkWwiseAddressablesInitializationSettings : AkWwiseInitializationSettings
	{
		private static AkWwiseAddressablesInitializationSettings m_Instance = null;

		public new static AkWwiseAddressablesInitializationSettings Instance
		{
			get
			{
				if (m_Instance == null)
				{
#if WWISE_2026_OR_LATER
					m_Instance = CreateInstance<AkWwiseAddressablesInitializationSettings>();
					WwiseAddressableAdapter.Instance.WwiseWarning("AkWwiseInitializationSettings is deprecated (replaced with AkOption); returning default values.");
					return m_Instance;
#else
#if WWISE_ADDRESSABLES_POST_2023 || WWISE_ADDRESSABLES_23_1_OR_LATER
#if WWISE_2024_OR_LATER
					AkUnityAddressablesSoundEngineInitialization.ResetInstance();
#else
					AkAddressablesSoundEngineInitialization.ResetInstance();
#endif //WWISE_2024_OR_LATER
#endif //WWISE_ADDRESSABLES_POST_2023 || WWISE_ADDRESSABLES_23_1_OR_LATER
#if UNITY_EDITOR
					var name = typeof(AkWwiseInitializationSettings).Name;
					var className = typeof(AkWwiseAddressablesInitializationSettings).Name;
					m_Instance = ReplaceOrCreateAsset(className, name);
#else
					m_Instance = (AkWwiseAddressablesInitializationSettings) CreateInstance<AkWwiseAddressablesInitializationSettings>();
					WwiseAddressableAdapter.Instance.WwiseWarning("WwiseUnity: No platform specific settings were created. Default initialization settings will be used.");
#endif
#endif // WWISE_2026_OR_LATER
				}

				return m_Instance;
			}
		}

#if !(WWISE_ADDRESSABLES_POST_2023 || WWISE_ADDRESSABLES_23_1_OR_LATER)
		protected override void LoadInitBank()
		{
			AkAddressableBankManager.Instance.LoadInitBank();
		}

		protected override void ClearBanks()
		{
			AkAddressableBankManager.Instance.UnloadAllBanks(clearBankDictionary: false);
			AkAddressableBankManager.Instance.UnloadInitBank();
		}
#endif


#if UNITY_EDITOR
		public static AkWwiseAddressablesInitializationSettings ReplaceOrCreateAsset(string className, string fileName)
		{
			var path = System.IO.Path.Combine(AkWwiseEditorSettings.WwiseScriptableObjectRelativePath, fileName + ".asset");
			var assetExists = string.IsNullOrEmpty(UnityEditor.AssetDatabase.AssetPathToGUID(path));
			if (assetExists)
			{
				var loadedAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<AkWwiseAddressablesInitializationSettings>(path);
				if (loadedAsset != null)
				{
					return loadedAsset;
				}
				else //overwrite current InitializationSettings asset with the addressables one
				{
					UnityEditor.AssetDatabase.DeleteAsset(path);
					var newAsset = CreateInstance<AkWwiseAddressablesInitializationSettings>();
					UnityEditor.AssetDatabase.CreateAsset(newAsset, path);
					return newAsset;
				}
			}

			var guids = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(AkWwiseAddressablesInitializationSettings).Name);
			foreach (var assetGuid in guids)
			{
				var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
				var foundAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<AkWwiseAddressablesInitializationSettings>(assetPath);
				if (foundAsset)
					return foundAsset;
			}

			var createdAsset = CreateInstance<AkWwiseAddressablesInitializationSettings>();
			WwiseAddressableAdapter.Instance.CreateFolderFromAkUtilities(AkWwiseEditorSettings.WwiseScriptableObjectRelativePath);
			UnityEditor.AssetDatabase.CreateAsset(createdAsset, path);
			return createdAsset;
		}
#endif
	}
}
#endif // AK_WWISE_ADDRESSABLES
