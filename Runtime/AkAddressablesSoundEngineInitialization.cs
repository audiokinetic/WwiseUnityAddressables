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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if WWISE_2026_OR_LATER
using AK.Wwise.Unity.Settings;
#endif

#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES && (WWISE_ADDRESSABLES_23_1_OR_LATER || WWISE_ADDRESSABLES_POST_2023)
#if WWISE_2024_OR_LATER
public class AkUnityAddressablesSoundEngineInitialization : AkUnitySoundEngineInitialization
#else
public class AkAddressablesSoundEngineInitialization : AkSoundEngineInitialization
#endif
{
	public static void ResetInstance()
	{
		WwiseAddressableAdapter.Instance.ResetInstance(ref m_Instance);
	}

	protected override void LoadInitBank()
	{
#if WWISE_2026_OR_LATER
		AK.Wwise.Unity.WwiseAddressables.AkAddressableBankManager.Instance.LoadInitBank(WwiseRuntimeSettings.LoadBanksAsynchronously);
#else
		AK.Wwise.Unity.WwiseAddressables.AkAddressableBankManager.Instance.LoadInitBank(AkWwiseInitializationSettings.Instance.LoadBanksAsynchronously);
#endif
	}
	
	protected override void ClearBanks()
	{
		AK.Wwise.Unity.WwiseAddressables.AkAddressableBankManager.Instance.UnloadAllBanks(clearBankDictionary: false);
		AK.Wwise.Unity.WwiseAddressables.AkAddressableBankManager.Instance.UnloadInitBank();
	}

	protected override void ResetBanks()
	{
		AK.Wwise.Unity.WwiseAddressables.AkAddressableBankManager.Instance.UnloadAllBanks(clearBankDictionary: false);
	}
}
#endif