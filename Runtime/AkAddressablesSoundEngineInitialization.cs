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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES && WWISE_ADDRESSABLES_POST_2023

public class AkAddressablesSoundEngineInitialization : AkSoundEngineInitialization
{
	public static void ResetInstance()
	{
		if(m_Instance != null)
		{
			InitializationDelegate copyInitialize = m_Instance.initializationDelegate;
			TerminationDelegate copyTerminate = m_Instance.terminationDelegate;
			m_Instance = new AkAddressablesSoundEngineInitialization();
			m_Instance.initializationDelegate = copyInitialize;
			m_Instance.terminationDelegate = copyTerminate;
		}
		else
		{
			m_Instance = new AkAddressablesSoundEngineInitialization();
		}
	}

	protected override void LoadInitBank()
	{
		AK.Wwise.Unity.WwiseAddressables.AkAddressableBankManager.Instance.LoadInitBank(AkWwiseInitializationSettings.Instance.LoadBanksAsynchronously);
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