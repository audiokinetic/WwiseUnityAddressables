﻿#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES

namespace AK.Wwise.Unity.WwiseAddressables
{
	public class AkWwiseAddressablesInitializationSettings : AkWwiseInitializationSettings
	{
		private static AkWwiseAddressablesInitializationSettings m_Instance = null;

		public new static AkWwiseAddressablesInitializationSettings Instance
		{
			get
			{
				if (m_Instance == null)
				{
#if WWISE_ADDRESSABLES_POST_2023
					AkAddressablesSoundEngineInitialization.ResetInstance();
#endif
#if UNITY_EDITOR
					var name = typeof(AkWwiseInitializationSettings).Name;
					var className = typeof(AkWwiseAddressablesInitializationSettings).Name;
					m_Instance = ReplaceOrCreateAsset(className, name);
#else
					m_Instance = (AkWwiseAddressablesInitializationSettings) CreateInstance<AkWwiseAddressablesInitializationSettings>();
					UnityEngine.Debug.LogWarning("WwiseUnity: No platform specific settings were created. Default initialization settings will be used.");
#endif
				}

				return m_Instance;
			}
		}

#if !WWISE_ADDRESSABLES_POST_2023
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
			AkUtilities.CreateFolder(AkWwiseEditorSettings.WwiseScriptableObjectRelativePath);
			UnityEditor.AssetDatabase.CreateAsset(createdAsset, path);
			return createdAsset;
		}
#endif
	}
}
#endif // AK_WWISE_ADDRESSABLES
