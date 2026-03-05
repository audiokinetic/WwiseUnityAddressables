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
#if ADDRESSABLES_API_BROWSER_TREE_VIEW
using System.Collections.Generic;
using AK.Wwise.Unity.WwiseAddressables;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.IMGUI.Controls;

[InitializeOnLoad]
public class AddressableBrowserTreeView
{
	static readonly AddressableAssetSettings AddressableSettings;
	static readonly bool AddressableSettingsIsValid;
	private static readonly Dictionary<System.Guid, WwiseObjectReference> BankInfoCache;
	private const string ADDRESSABLE_ENTRY_NOT_FOUND_MESSAGE = "Addressable entry not found";

	static AddressableBrowserTreeView()
	{
		AkWwiseTreeView.wwiseBrowserColumnDelegate += DrawAddressableHeaderColumn;
		AkWwiseTreeView.wwiseBrowserCellDelegate += DrawAddressableCell;
	    WwiseProjectDatabase.SoundBankDirectoryUpdated += ClearCache;
	    
	    AddressableSettings = AddressableAssetSettingsDefaultObject.Settings;
	    AddressableSettingsIsValid = AddressableSettings != null;
	    BankInfoCache = new Dictionary<System.Guid, WwiseObjectReference>();
	}

	static void ClearCache()
	{
		BankInfoCache.Clear();
	}

	static void DrawAddressableHeaderColumn(List<MultiColumnHeaderState.Column> columns)
	{
	    columns.Add(
		    new MultiColumnHeaderState.Column
		    {
			    headerContent = new UnityEngine.GUIContent("Addressable Group"),
			    headerTextAlignment = UnityEngine.TextAlignment.Left,
			    sortedAscending = true,
			    sortingArrowAlignment = UnityEngine.TextAlignment.Center,
			    width = 300,
			    minWidth = 100,
			    autoResize = true,
			    allowToggleVisibility = false,
			    canSort = false
		    }
	    );
	}
	
	static void DrawAddressableCell(AkWwiseTreeView.AkWwiseTreeViewCellInfo wwiseTreeViewCellInfo)
	{
		if (wwiseTreeViewCellInfo.Column != AkWwiseTreeView.ObjectColumns.AddressableGroup || !AddressableSettingsIsValid)
		{
			return;
		}

		if (!UnityEngine.Device.Application.isPlaying && !BankInfoCache.ContainsKey(wwiseTreeViewCellInfo.Item.objectGuid))
		{
			UpdateBankInfoCache(wwiseTreeViewCellInfo);
		}
        
		RenderAddressableGroup(wwiseTreeViewCellInfo);
	}
	
	private static void UpdateBankInfoCache(AkWwiseTreeView.AkWwiseTreeViewCellInfo wwiseTreeViewCellInfo)
	{
		if (wwiseTreeViewCellInfo.Item.objectType != WwiseObjectType.Event && wwiseTreeViewCellInfo.Item.objectType != WwiseObjectType.Soundbank)
		{
			return;
		}

		var wwiseObjectReference = WwiseObjectReference.FindOrCreateWwiseObject(
			wwiseTreeViewCellInfo.Item.objectType, 
			wwiseTreeViewCellInfo.Item.name, 
			wwiseTreeViewCellInfo.Item.objectGuid
		);

		if (wwiseObjectReference != null)
		{
			BankInfoCache.Add(wwiseTreeViewCellInfo.Item.objectGuid, wwiseObjectReference);
		}
	}
	
	private static void RenderAddressableGroup(AkWwiseTreeView.AkWwiseTreeViewCellInfo wwiseTreeViewCellInfo)
    {
        if (!BankInfoCache.TryGetValue(wwiseTreeViewCellInfo.Item.objectGuid, out var wwiseObjectReference))
        {
            return;
        }

        if (wwiseTreeViewCellInfo.Item.objectType == WwiseObjectType.Event)
        {
			var eventReference = wwiseObjectReference as WwiseEventReference;
            if (eventReference == null)
            {
	            return;
            }
            if (eventReference.IsInUserDefinedSoundBank)
            {
	            UnityEngine.GUI.Label(wwiseTreeViewCellInfo.CellRect, "Not an Autobank");
                return;
            }

            int indexOfCurrentLanguage = GetAddressableAssetIndex(eventReference.AutoBank, wwiseTreeViewCellInfo.CellRect);
            if (indexOfCurrentLanguage != -1)
            {
                DisplayAddressableEntry(eventReference.AutoBank.CurrentPlatformAssets.LocalizedBanksValues[indexOfCurrentLanguage].editorAsset, wwiseTreeViewCellInfo.CellRect,wwiseTreeViewCellInfo.Item.name);
            }
        }
        else if (wwiseTreeViewCellInfo.Item.objectType == WwiseObjectType.Soundbank)
        {
            var bankReference = wwiseObjectReference as WwiseBankReference;
            if (bankReference?.AddressableBank == null)
            {
	            UnityEngine.GUI.Label(wwiseTreeViewCellInfo.CellRect, ADDRESSABLE_ENTRY_NOT_FOUND_MESSAGE);
                return;
            }

            int indexOfCurrentLanguage = GetAddressableAssetIndex(bankReference.AddressableBank, wwiseTreeViewCellInfo.CellRect);
            if (indexOfCurrentLanguage != -1)
            {
                DisplayAddressableEntry(bankReference.AddressableBank.CurrentPlatformAssets.LocalizedBanksValues[indexOfCurrentLanguage].editorAsset, wwiseTreeViewCellInfo.CellRect, wwiseTreeViewCellInfo.Item.name);
            }
        }
    }
	
	private static void DisplayAddressableEntry(WwiseSoundBankAsset soundBankAsset, UnityEngine.Rect cellRect, string itemName)
	{
		var assetPath = AssetDatabase.GetAssetPath(soundBankAsset);
		AddressableAssetEntry entry = AddressableSettings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
       
		if (entry == null)
		{
			UnityEngine.GUI.Label(cellRect, ADDRESSABLE_ENTRY_NOT_FOUND_MESSAGE);   
		}
		else
		{
			var parentGroupName = entry.parentGroup.name;
			UnityEngine.GUI.Label(cellRect, parentGroupName);  
		}
	}

	private static int GetAddressableAssetIndex(WwiseAddressableSoundBank addressableSoundBank, UnityEngine.Rect cellRect)
	{
		string currentLanguage = AkWwiseInitializationSettings.Instance.UserSettings.m_StartupLanguage;
		int indexOfAsset = System.Array.IndexOf(addressableSoundBank.CurrentPlatformAssets.LocalizedBankKeys, currentLanguage);
		if (indexOfAsset != -1)
		{
			return indexOfAsset;
		}
		//If not a localized asset
		indexOfAsset = System.Array.IndexOf(addressableSoundBank.CurrentPlatformAssets.LocalizedBankKeys, "SFX");
		if (indexOfAsset == -1)
		{
			UnityEngine.GUI.Label(cellRect, $"Asset for {currentLanguage} not found");
		}
		return indexOfAsset;
	}
}
#endif
