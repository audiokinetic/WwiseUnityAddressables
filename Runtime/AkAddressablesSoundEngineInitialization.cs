using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES && (WWISE_ADDRESSABLES_23_1_OR_LATER || WWISE_ADDRESSABLES_POST_2023)

public class AkAddressablesSoundEngineInitialization : AkSoundEngineInitialization
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
			m_Instance = new AkAddressablesSoundEngineInitialization();
			m_Instance.initializationDelegate = copyInitialize;
#if WWISE_ADDRESSABLES_24_1_OR_LATER
			m_Instance.reInitializationDelegate = copyReInitialize;
#endif
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