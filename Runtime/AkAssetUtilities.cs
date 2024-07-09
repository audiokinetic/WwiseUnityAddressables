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
			if (AkWwiseEditorSettings.Instance.GeneratedSoundbanksPath == null)
			{
				UnityEngine.Debug.LogError("Wwise Addressables: You need to set the GeneratedSoundbankPath in the Wwise Editor settings or assets will not be properly imported.");
				return string.Empty;
			}
			var path = Path.Combine("Assets", AkWwiseEditorSettings.Instance.GeneratedSoundbanksPath);
			return path.Replace("\\", "/");
		}

		public static AssetReferenceWwiseAddressableBank GetAddressableBankAssetReference(string name)
		{
			var assetPath = System.IO.Path.Combine(GetSoundbanksPath(), name + ".asset");
			return new AssetReferenceWwiseAddressableBank(AssetDatabase.AssetPathToGUID(assetPath));
		}

		public static WwiseAddressableSoundBank GetAddressableBankAsset(string name)
		{
			var assetPath = System.IO.Path.Combine(GetSoundbanksPath(), name + ".asset");
			var asset = AssetDatabase.LoadAssetAtPath<WwiseAddressableSoundBank>(assetPath);
			if (asset == null)
			{
				Debug.LogError($"Could not find addressable bank asset : {assetPath}");
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

		public static bool UpdateWwiseFileIfNecessary(string wwiseFolder, WwiseAsset asset)
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
