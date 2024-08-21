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

using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using UnityEngine;

using UnityEditor.AssetImporters;

namespace AK.Wwise.Unity.WwiseAddressables
{
	[ScriptedImporter(1, "bnk")]
	public class WwiseBankImporter : ScriptedImporter
	{
		public override void OnImportAsset(AssetImportContext ctx)
		{
			string assetName = Path.GetFileNameWithoutExtension(ctx.assetPath);

			string platform;
			string language;
			AkAddressablesEditorUtilities.ParseAssetPath(ctx.assetPath, out platform, out language);

			if (platform == null)
			{
				Debug.LogWarning($"Skipping {ctx.assetPath} as its platform couldn't be determined. Make sure it is placed in the appropriate platform folder.");
				return;
			}

			var soundbankInfos = AkAddressablesEditorUtilities.ParsePlatformSoundbanksXML(platform, assetName);

			if (soundbankInfos == null)
			{
				Debug.LogWarning($"Skipping {ctx.assetPath}. SoundbanksInfo.xml could not be parsed.");
				return;
			}

			if (!soundbankInfos.ContainsKey(assetName))
			{
				Debug.LogWarning($"Skipping {ctx.assetPath} as it was not parsed in SoundbanksInfo.xml. Perhaps this bank no longer exists in the wwise project?");
				return;
			}
			WwiseSoundBankAsset dataAsset = ScriptableObject.CreateInstance<WwiseSoundBankAsset>();
			dataAsset.RawData = File.ReadAllBytes(Path.GetFullPath(ctx.assetPath));
			dataAsset.language = language;
			var eventNames = soundbankInfos[assetName][language].events;
			if (language !="SFX" && soundbankInfos[assetName].ContainsKey("SFX"))
			{
				eventNames.AddRange(soundbankInfos[assetName]["SFX"].events);
			}
			dataAsset.eventNames = eventNames;
			byte[] hash = MD5.Create().ComputeHash(dataAsset.RawData);
			dataAsset.hash = hash;
			ctx.AddObjectToAsset(string.Format("WwiseBank_{0}{1}_{2}", platform, language, assetName), dataAsset);
			ctx.SetMainObject(dataAsset);
		}
	}
}
#endif