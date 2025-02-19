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

using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;
#endif

namespace AK.Wwise.Unity.WwiseAddressables
{
	public static class AkAssetUtilities
	{

#if UNITY_EDITOR
		public delegate bool AddressableBankCreatedDelegate(WwiseAddressableSoundBank assetRef, string name);
		public static AddressableBankCreatedDelegate AddressableBankUpdated;

		public static string GetSoundbanksPath()
		{
			if (AkWwiseEditorSettings.Instance.RootOutputPath == null)
			{
				UnityEngine.Debug.LogError("Wwise Addressables: You need to set the RootOutputPath in the Wwise Editor settings or assets will not be properly imported.");
				return string.Empty;
			}
			var fullRootOutputPath = AkUtilities.GetFullPath(UnityEngine.Application.dataPath, AkWwiseEditorSettings.Instance.RootOutputPath);
			var streamingAssetPath = UnityEngine.Application.dataPath.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);
			streamingAssetPath = System.IO.Path.Combine(streamingAssetPath, "StreamingAssets");
			if (fullRootOutputPath.Contains(streamingAssetPath))
			{
				UnityEngine.Debug.LogWarning("Wwise Addressables: The RootOutputPath in the Wwise Editor settings is setting within the StreamingAssets folder. It is recommended to not set it in this folder.");
			}
			var path = Path.Combine("Assets", AkWwiseEditorSettings.Instance.RootOutputPath);
			return path.Replace("\\", "/");
		}

		public static AssetReferenceWwiseAddressableBank GetAddressableBankAssetReference(string name)
		{
			var assetPath = System.IO.Path.Combine(GetSoundbanksPath(), name + ".asset");
			return new AssetReferenceWwiseAddressableBank(AssetDatabase.AssetPathToGUID(assetPath));
		}
#if !WWISE_ADDRESSABLES_24_1_OR_LATER
		public static WwiseAddressableSoundBank GetAddressableBankAsset(string name)
		{
			//Unity Integration 2023.1 does not support Auto-Banks
			return GetAddressableBankAsset(name, false);
		}
#endif

		public static WwiseAddressableSoundBank GetAddressableBankAsset(string name, bool IsLookingForAutoBank)
		{
			var assetPath = System.IO.Path.Combine(GetSoundbanksPath(), name + ".asset");
			if (IsLookingForAutoBank)
			{
				assetPath = System.IO.Path.Combine(GetSoundbanksPath(), "Event", name + ".asset");
			}
			
			var asset = AssetDatabase.LoadAssetAtPath<WwiseAddressableSoundBank>(assetPath);
			if (asset == null)
			{
				if (IsLookingForAutoBank)
				{
					Debug.LogWarning($"Could not find addressable bank asset : {assetPath}. If the event is in an User Defined Soundbank, make sure" +
					                 " to check the \"Is In User Define SoundBank\" box in the editor.");
				}
				else
				{
					Debug.LogError($"Could not find addressable bank asset : {assetPath}");
				}
			}
			
			return asset;
		}
#endif
		public static bool AreHashesEqual(byte[] existingHash, byte[] newHash)
		{
			if (existingHash == null || newHash == null)
			{
				return false;
			}

			if (existingHash.Length != newHash.Length)
			{
				return false;
			}

			for (int i = 0; i < newHash.Length; i++)
			{
				if (existingHash[i] != newHash[i])
				{
					return false;
				}
			}

			return true;
		}

		public static bool UpdateStreamedFileIfNecessary(string wwiseFolder, WwiseAsset asset)
		{
			var filePath = Path.Combine(wwiseFolder, asset.GetRelativeFilePath());
			var hashPath = filePath + ".md5";
			if (File.Exists(hashPath))
			{
				var existingHash = File.ReadAllBytes(hashPath);

				if (!AreHashesEqual(existingHash, asset.hash))
				{
					// Different hash means file content has changed and needs to be updated
					WriteFile(filePath, hashPath, asset);
					return true;
				}
			}
			else
			{
				// No hash means we are downloading the file for the first time
				WriteFile(filePath, hashPath, asset);
				return true;
			}
			return false;
		}

		private static void WriteFile(string filePath, string hashPath, WwiseAsset asset)
		{
			var destinationDir = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(destinationDir))
			{
				Directory.CreateDirectory(destinationDir);
			}
			File.WriteAllBytes(filePath, asset.RawData);
			File.WriteAllBytes(hashPath, asset.hash);
		}
	}
}
#endif  // AK_WWISE_ADDRESSABLES
