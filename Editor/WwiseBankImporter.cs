#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES

using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;

using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

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
			AkAssetUtilities.ParseAssetPath(ctx.assetPath, out platform, out language);

			if (platform == null)
			{
				return;
			}

			var soundbankInfos = AkAssetUtilities.ParsePlatformSoundbanksXML(platform, assetName);

			if (!soundbankInfos.ContainsKey(assetName))
			{
				Debug.LogWarning($"Could not properly parse soundbank at {ctx.assetPath} - skipping it.");
				return;
			}
			WwiseSoundBankAsset dataAsset = ScriptableObject.CreateInstance<WwiseSoundBankAsset>();
			dataAsset.RawData = File.ReadAllBytes(Path.GetFullPath(ctx.assetPath));
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