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

using UnityEngine;
using System.IO;
using System.Security.Cryptography;

using UnityEditor.AssetImporters;

namespace AK.Wwise.Unity.WwiseAddressables
{
	[ScriptedImporter(1, "wem")]
	public class WwiseStreamingAssetImporter : ScriptedImporter
	{
		public override void OnImportAsset(AssetImportContext ctx)
		{
			string assetName = Path.GetFileNameWithoutExtension(ctx.assetPath);

			string platform;
			string language;
			AkAddressablesEditorUtilities.ParseAssetPath(ctx.assetPath, out platform, out language);

			if (platform == null)
			{
				return;
			}

			WwiseStreamingMediaAsset dataAsset = ScriptableObject.CreateInstance<WwiseStreamingMediaAsset>();
			dataAsset.RawData = File.ReadAllBytes(Path.GetFullPath(ctx.assetPath));
			byte[] hash = MD5.Create().ComputeHash(dataAsset.RawData);
			dataAsset.hash = hash;
			dataAsset.language = language;

			ctx.AddObjectToAsset(string.Format("WwiseSteamingMedia_{0}{1}_{2}", platform, language, assetName), dataAsset);
			ctx.SetMainObject(dataAsset);
		}
	}
}
#endif