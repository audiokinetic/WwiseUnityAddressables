using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES

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
		AK.Wwise.Unity.WwiseAddressables.AkAddressableBankManager.Instance.LoadInitBank();
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
