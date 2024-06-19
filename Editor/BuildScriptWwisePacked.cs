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

using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace AK.Wwise.Unity.WwiseAddressables
{
	/*
	 * The wwise build script ONLY builds the bundles automatically generated groupes (e.g. WwiseData_[platform] and  WwiseData_[platform]_InitBank) for the target build platform.
	 */
	[CreateAssetMenu(fileName = "BuildScriptWwisePacked.asset", menuName = "Addressables/Content Builders/Wwise Build Script")]
	public class BuildScriptWwisePacked : BuildScriptPackedMode
	{
		/// <inheritdoc />
		public override string Name
		{
			get
			{
				return "Wwise Build Script";
			}
		}
		/// <inheritdoc />
		/// 
		protected override string ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
		{
			if (assetGroup == null)
				return string.Empty;

			var buildTarget = (BuildTarget) Enum.Parse(typeof(BuildTarget), aaContext.runtimeData.BuildTarget);
			IncludePlatformSpecificBundles(buildTarget);

			foreach (var schema in assetGroup.Schemas)
			{
				var errorString = ProcessGroupSchema(schema, assetGroup, aaContext);
				if (!string.IsNullOrEmpty(errorString))
					return errorString;
			}

			return string.Empty;
		}

		private void IncludePlatformSpecificBundles(UnityEditor.BuildTarget target)
		{
			var wwisePlatform = AkAddressablesEditorUtilities.GetWwisePlatformNameFromBuildTarget(target);

			var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;

			if (addressableSettings == null)
			{
				UnityEngine.Debug.LogWarningFormat("[Addressables] settings file not found.\nPlease go to Menu/Window/Asset Management/Addressables/Groups, then click 'Create Addressables Settings' button.");
				return;
			}

			foreach (var group in addressableSettings.groups)
			{
				var include = false;

				if (group.Name.Contains("WwiseData"))
				{
					if (group.Name.Contains(wwisePlatform))
					{
						include = true;
					}

					var bundleSchema = group.GetSchema<BundledAssetGroupSchema>();
					if (bundleSchema != null)
						bundleSchema.IncludeInBuild = include;
				}
			}
		}
	}
}
#endif