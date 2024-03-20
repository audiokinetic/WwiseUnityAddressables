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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES && (WWISE_ADDRESSABLES_23_1_OR_LATER || WWISE_ADDRESSABLES_POST_2023)
#if WWISE_2024_OR_LATER
public class AkUnityAddressablesSoundEngineInitialization : AkUnitySoundEngineInitialization
#else
public class AkAddressablesSoundEngineInitialization : AkSoundEngineInitialization
#endif
{
	public static void ResetInstance()
	{
		if(m_Instance != null)
		{
			InitializationDelegate copyInitialize = m_Instance.initializationDelegate;
#if WWISE_ADDRESSABLES_24_1_OR_LATER
			ReInitializationDelegate copyReInitialize = m_Instance.reInitializationDelegate;
#endif
			TerminationDelegate copyTerminate = m_Instance.terminationDelegate;
#if WWISE_2024_OR_LATER
			m_Instance = new AkUnityAddressablesSoundEngineInitialization();
#else
			m_Instance = new AkAddressablesSoundEngineInitialization();
#endif
			m_Instance.initializationDelegate = copyInitialize;
#if WWISE_ADDRESSABLES_24_1_OR_LATER
			m_Instance.reInitializationDelegate = copyReInitialize;
#endif
			m_Instance.terminationDelegate = copyTerminate;
		}
		else
		{
#if WWISE_2024_OR_LATER
			m_Instance = new AkUnityAddressablesSoundEngineInitialization();
#else
			m_Instance = new AkAddressablesSoundEngineInitialization();
#endif
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