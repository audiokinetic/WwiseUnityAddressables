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
Copyright (c) 2025 Audiokinetic Inc.
*******************************************************************************/

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

	static AddressableBrowserTreeView()
	{
	    AkWwiseTreeView.wwiseBrowserColumnDelegate += DrawAddressableHeaderColumn;
	    AkWwiseTreeView.wwiseBrowserCellDelegate += DrawAddressableCell;
	    
	    AddressableSettings = AddressableAssetSettingsDefaultObject.Settings;
	    AddressableSettingsIsValid = AddressableSettings != null;
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
	    if (wwiseTreeViewCellInfo.Column == AkWwiseTreeView.ObjectColumns.AddressableGroup)
	    {
    		if (!AddressableSettingsIsValid)
    		{
    			return;
    		}
    		if (wwiseTreeViewCellInfo.Item.objectType == WwiseObjectType.Event)
    		{
    			var wwiseObjectReference = WwiseObjectReference.FindOrCreateWwiseObject(wwiseTreeViewCellInfo.Item.objectType, wwiseTreeViewCellInfo.Item.name, wwiseTreeViewCellInfo.Item.objectGuid);
    			if (wwiseObjectReference == null)
    			{
    				return;
    			}
    			var eventReference = wwiseObjectReference as WwiseEventReference;
    			if (eventReference == null)
    			{
    				return;
    			}
    			if (eventReference.AutoBank != null)
    			{
    				int indexOfCurrentLanguage = GetAddressableAssetIndex(eventReference.AutoBank, wwiseTreeViewCellInfo.CellRect);
    				if (indexOfCurrentLanguage == -1)
    				{
    					return;
    				}
    				DisplayAddressableEntry(eventReference.AutoBank.CurrentPlatformAssets.LocalizedBanksValues[indexOfCurrentLanguage].editorAsset, wwiseTreeViewCellInfo.CellRect);	
    			}
    			else
    			{
    				UnityEngine.GUI.Label(wwiseTreeViewCellInfo.CellRect, "Not an Autobank");
    			}
    				
    		}
    		else if (wwiseTreeViewCellInfo.Item.objectType == WwiseObjectType.Soundbank)
    		{
    			var wwiseObjectReference = WwiseObjectReference.FindOrCreateWwiseObject(wwiseTreeViewCellInfo.Item.objectType, wwiseTreeViewCellInfo.Item.name, wwiseTreeViewCellInfo.Item.objectGuid);
    			if (wwiseObjectReference == null)
    			{
    				return;
    			}
    			var bankReference = wwiseObjectReference as WwiseBankReference;
    			if (bankReference == null)
    			{
    				return;
    			}

    			int indexOfCurrentLanguage = GetAddressableAssetIndex(bankReference.AddressableBank, wwiseTreeViewCellInfo.CellRect);
    			if (indexOfCurrentLanguage == -1)
    			{
    				return;
    			}
    			
    			DisplayAddressableEntry(bankReference.AddressableBank.CurrentPlatformAssets.LocalizedBanksValues[indexOfCurrentLanguage].editorAsset, wwiseTreeViewCellInfo.CellRect);
    		}
	    }
	}
	private static void DisplayAddressableEntry(WwiseSoundBankAsset soundBankAsset, UnityEngine.Rect cellRect)
	{
		var assetPath = UnityEditor.AssetDatabase.GetAssetPath(soundBankAsset);
		AddressableAssetEntry entry = AddressableSettings.FindAssetEntry(UnityEditor.AssetDatabase.AssetPathToGUID(assetPath));
		if (entry == null)
		{
			UnityEngine.GUI.Label(cellRect, "Addressable entry not found for bank {item.name}");	
		}
		else
		{
			var parentGroupName = entry.parentGroup.name;
			UnityEngine.GUI.Label(cellRect, parentGroupName);	
		}
	}

	private static int GetAddressableAssetIndex(WwiseAddressableSoundBank addressableSoundBank, UnityEngine.Rect cellRect)
	{
		string currentLanguage = AkWwiseInitializationSettings.ActivePlatformSettings.InitialLanguage;
		int indexOfAsset = System.Array.IndexOf(addressableSoundBank.CurrentPlatformAssets.LocalizedBankKeys, currentLanguage);
		if (indexOfAsset != -1)
		{
			return indexOfAsset;
		}
		//If not a localized asset
		indexOfAsset = System.Array.IndexOf(addressableSoundBank.CurrentPlatformAssets.LocalizedBankKeys, "SFX");
		if (indexOfAsset == -1)
		{
			UnityEngine.GUI.Label(cellRect, $"Asset for {AkWwiseInitializationSettings.ActivePlatformSettings.InitialLanguage} not found");
		}
		return indexOfAsset;
	}
}