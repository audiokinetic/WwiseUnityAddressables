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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
#endif

namespace AK.Wwise.Unity.WwiseAddressables
{
	public class AkAddressableBankManager
	{

		private ConcurrentDictionary<(string, bool), WwiseAddressableSoundBank> m_AddressableBanks =
			new ConcurrentDictionary<(string, bool), WwiseAddressableSoundBank>();

		private ConcurrentDictionary<string, string> m_banksToUnload =
			new ConcurrentDictionary<string, string>();

		private ConcurrentDictionary<uint, EventContainer> m_eventsToFireOnBankLoad =
			new ConcurrentDictionary<uint, EventContainer>();

		public const uint INVALID_SOUND_BANK_ID = 0;

		private WwiseAddressableSoundBank initBank;
		private WwiseAddressableSoundBank InitBank
		{
			get
			{
				if (initBank == null)
				{
					initBank = FindInitBank();
				}
				return initBank;
			}
		}
		private static AkAddressableBankManager instance;
		public static AkAddressableBankManager Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new AkAddressableBankManager();
				}
				return instance;
			}
			private set { Instance = value; }
		}
		
		private uint? m_wwiseMajorVersion = null;
		public uint WwiseMajorVersion
		{
			get
			{
				if (m_wwiseMajorVersion == null)
				{
#if WWISE_2024_OR_LATER
					m_wwiseMajorVersion = AkUnitySoundEngine.GetMajorMinorVersion() >> 16;
#else
					m_wwiseMajorVersion = AkSoundEngine.GetMajorMinorVersion() >> 16;
#endif
				}
				return (uint)m_wwiseMajorVersion;
			}
		}

		public string WriteableMediaDirectory
		{
			get
			{
				return Path.Combine(UnityEngine.Application.persistentDataPath, "Media");
			}
		}

		private static WwiseAddressableSoundBank FindInitBank()
		{
			var foundBank = UnityEngine.MonoBehaviour.FindObjectsOfType<InitBankHolder>();
			if (foundBank.Count() == 0)
			{
				if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
				{
					UnityEngine.Debug.LogError("Wwise Addressables : There is no InitBankHolder in the scene, please add one for Wwise to function properly.");
				}
				return null;
			}

			if (foundBank.Count() > 1)
			{
				if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
				{
					UnityEngine.Debug.LogError("Wwise Addressables : There is more than one InitBankHolder in the scene, which is not recommended.");
				}
			}

			WwiseAddressableSoundBank InitBank = foundBank[0].GetAddressableInitBank();
			if (InitBank == null)
			{
				if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
				{
					UnityEngine.Debug.LogError("Wwise Addressables : The InitBankHolder could not get a valid reference to the Init bank.");
				}
				return null;

			}

			return InitBank;
		}

		struct EventContainer
		{
			public string eventName;
			public object eventObject;
			public string methodName;
			public object[] methodArgs;
			public Type[] methodArgTypes;
		}

		bool InitBankLoaded
		{
			get { return (InitBank != null && InitBank.loadState == BankLoadState.Loaded); }
		}

		public void UnloadAllBanks(bool clearBankDictionary = true)
		{
			foreach (var bank in m_AddressableBanks.Values)
			{
				UnloadBank(bank, ignoreRefCount: true, removeFromBankDictionary: false); ;
			}
			if (clearBankDictionary)
			{
				m_AddressableBanks.Clear();
			}
		}

		public void ReloadAllBanks()
		{
			var m_banksToReload = new ConcurrentDictionary<(string, bool), WwiseAddressableSoundBank>(m_AddressableBanks);
			UnloadAllBanks();
			UnloadInitBank();
#if WWISE_ADDRESSABLES_23_1_OR_LATER || WWISE_ADDRESSABLES_POST_2023
			LoadInitBank(AkWwiseInitializationSettings.Instance.LoadBanksAsynchronously);
#else
			LoadInitBank();
#endif


			foreach (var bank in m_banksToReload.Values)
			{
				LoadBank(bank, bank.decodeBank, bank.saveDecodedBank);
			}
		}

		public void SetLanguageAndReloadLocalizedBanks(string language, bool parseBanks = true)
		{
			var banksToReload = new List<WwiseAddressableSoundBank>();
			if (parseBanks)
			{
				foreach (var bank in m_AddressableBanks.Values)
				{
					if (bank.currentLanguage == "SFX" || bank.currentLanguage == language)
						continue;
					banksToReload.Add(bank);
				}
				foreach (var bank in banksToReload)
				{
					UnloadBank(bank, ignoreRefCount: true, removeFromBankDictionary: true);
				}
			}
			UnloadInitBank();
			m_eventsToFireOnBankLoad.Clear();
#if WWISE_2024_OR_LATER
			AkUnitySoundEngine.SetCurrentLanguage(language);
			AkUnitySoundEngine.RenderAudio();
#else
			AkSoundEngine.SetCurrentLanguage(language);
			AkSoundEngine.RenderAudio();
#endif
#if WWISE_ADDRESSABLES_23_1_OR_LATER || WWISE_ADDRESSABLES_POST_2023
			LoadInitBank(AkWwiseInitializationSettings.Instance.LoadBanksAsynchronously);
#else
			LoadInitBank();
#endif
			foreach (var bank in banksToReload)
			{
				LoadBank(bank, bank.decodeBank, bank.saveDecodedBank);
			}
		}

		public void LoadInitBank(bool loadAsync = true)
		{
			if (InitBank != null)
			{
				LoadBank(InitBank, addToBankDictionary: false, loadAsync: loadAsync);
			}
		}

		public void UnloadInitBank()
		{
			if (InitBank != null)
			{
				UnloadBank(InitBank, ignoreRefCount: true, removeFromBankDictionary: false);
			}
		}
		//Todo : support decoding banks and saving decoded banks
		public async Task LoadBank(WwiseAddressableSoundBank bank, bool decodeBank = false, bool saveDecodedBank = false, bool addToBankDictionary = true, bool loadAsync = true)
		{
			bank.decodeBank = decodeBank;
			bank.saveDecodedBank = saveDecodedBank;
			if (m_AddressableBanks.ContainsKey((bank.name, bank.isAutoBank)))
			{
				m_AddressableBanks.TryGetValue((bank.name, bank.isAutoBank), out bank);
			}
			else if (addToBankDictionary)
			{
				m_AddressableBanks.TryAdd((bank.name, bank.isAutoBank), bank);
			}
			
			if (bank.loadState == BankLoadState.Unloaded || bank.loadState == BankLoadState.WaitingForInitBankToLoad)
			{
				if (!InitBankLoaded && bank.name != "Init")
				{
					if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
					{
						UnityEngine.Debug.Log($"Wwise Addressable Bank Manager: {bank.name} bank will be loaded after the init bank is loaded");
					}
					bank.loadState = BankLoadState.WaitingForInitBankToLoad;
					return;
				}
			}
			if (bank.loadState == BankLoadState.Loading)
			{
				bank.refCount += 1;
				return;
			}

			if (bank.loadState == BankLoadState.Loaded)
			{
				bank.refCount += 1;
				return;
			}

			bank.refCount += 1;
			bank.loadState = BankLoadState.Loading;

			if (bank.Data == null)
			{
				if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
				{
					UnityEngine.Debug.LogError($"Wwise Addressable Bank Manager : {bank.name} could not be loaded - Bank reference not set");
				}
				m_AddressableBanks.TryRemove((bank.name, bank.isAutoBank), out _);
				return;
			}

			AssetReferenceWwiseBankData bankData;
			if (bank.Data.ContainsKey("SFX"))
			{
				if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
				{
					UnityEngine.Debug.Log($"Wwise Addressable Bank Manager: Loading {bank.name} bank");
				}
				bankData = bank.Data["SFX"];
				bank.currentLanguage = "SFX";
			}
			else
			{
#if WWISE_2024_OR_LATER
				var currentLanguage = AkUnitySoundEngine.GetCurrentLanguage();
#else
				var currentLanguage = AkSoundEngine.GetCurrentLanguage();
#endif
				if (bank.Data.ContainsKey(currentLanguage))
				{
					bankData = bank.Data[currentLanguage];
					bank.currentLanguage = currentLanguage;
					if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
					{
						UnityEngine.Debug.Log($"Wwise Addressable Bank Manager: Loading {bank.name} - {currentLanguage}");
					}
				}
				else
				{
					if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
					{
						UnityEngine.Debug.LogError($"Wwise Addressable Bank Manager: {bank.name} could not be loaded in {currentLanguage} language ");
					}
					m_AddressableBanks.TryRemove((bank.name, bank.isAutoBank), out _);
					bank.loadState = BankLoadState.Unloaded;
					return;
				}
			}

			if (loadAsync)
			{
				await LoadBankAsync(bank, bankData, true);
			}
			else
			{
				LoadBankAsync(bank, bankData, false);
			}
		}
		
		public async Task LoadBankAsync(WwiseAddressableSoundBank bank, AssetReferenceWwiseBankData bankData, bool loadAsync)
		{
			AsyncOperationHandle asyncHandle = new AsyncOperationHandle<WwiseSoundBankAsset>();
			WwiseSoundBankAsset soundBankAsset;
			if (bankData.OperationHandle.IsValid())
			{
				soundBankAsset = (WwiseSoundBankAsset)bankData.Asset;
				asyncHandle = bankData.OperationHandle;
			}
			else
			{
				asyncHandle = bankData.LoadAssetAsync<WwiseSoundBankAsset>();
			}
#if UNITY_WEBGL && !UNITY_EDITOR
			// On WebGL, we MUST load asynchronously in order to yield back to the browser.
			// Failing to do so will result in the thread blocking forever and the asset will never be loaded.
			soundBankAsset = await asyncHandle.Task;
#else
			if (loadAsync)
			{
				soundBankAsset = (WwiseSoundBankAsset)await asyncHandle.Task;
			}
			else
			{
				soundBankAsset = (WwiseSoundBankAsset)asyncHandle.WaitForCompletion();
			}
#endif	
			//AsyncHandle gets corrupted in Unity 2021 but properly returns the loaded Asset as expected
#if UNITY_2021_1_OR_NEWER
			if (soundBankAsset)
#else
			if (asyncHandle.IsValid() && asyncHandle.Status == AsyncOperationStatus.Succeeded)
#endif
			{
				bank.eventNames = new HashSet<string>(soundBankAsset.eventNames);
				var data = soundBankAsset.RawData;
				bank.GCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

#if WWISE_2024_OR_LATER
				var result = AkUnitySoundEngine.LoadBankMemoryCopy(bank.GCHandle.AddrOfPinnedObject(), (uint)data.Length, out uint bankID, out uint bankType);
#else
				var result = AkSoundEngine.LoadBankMemoryCopy(bank.GCHandle.AddrOfPinnedObject(), (uint)data.Length, out uint bankID, out uint bankType);
#endif
				if (result == AKRESULT.AK_Success)
				{
					bank.soundbankId = bankID;
					bank.bankType = bankType;
					//Auto bank will set itself as loaded later
					if(!bank.isAutoBank)
					{
						bank.loadState = BankLoadState.Loaded;
					}
					else
					{
						bank.loadState = BankLoadState.WaitingForPrepareEvent;
					}
				}
				else if(result != AKRESULT.AK_BankAlreadyLoaded)
				{
					bank.soundbankId = INVALID_SOUND_BANK_ID;
					bank.loadState = BankLoadState.LoadFailed;
					if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
					{
						if ((int)result == 100) // 100 == AK_InvalidBankType (using the raw int value until this package only supports Wwise 22.1 and up)
						{
							UnityEngine.Debug.LogWarning($"Wwise Addressable Bank Manager : Bank {bank.name} is an auto-generated bank. The Unity Wwise Addressables package only supports user-defined banks. Avoid using auto-generated banks.");
						}
						else
						{
							UnityEngine.Debug.Log($"Wwise Addressable Bank Manager : Sound Engine failed to load {bank.name} SoundBank");
						}
					}
				}
				bank.GCHandle.Free();

				if (bank.StreamingMedia != null)
				{
					var assetKeys = new List<AssetReferenceStreamingMedia>();
					foreach (var language in bank.StreamingMedia.Keys)
					{
						foreach (var streamedAsset in bank.StreamingMedia[language].media)
						{
							if (streamedAsset == null)
							{
								if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
								{
									UnityEngine.Debug.LogError($"Wwise Addressable Bank Manager: Streaming media asset referenced in {bank.name} SoundBank is null");
								}
								continue;
							}
							assetKeys.Add(streamedAsset);
						}
					}
					if (assetKeys.Count > 0)
					{
#if UNITY_EDITOR
						if ((EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) != 0 || EditorApplication.isPlaying)
						{
							var startingSceneName = SceneManager.GetActiveScene().name;
#endif

							var streamingAssetAsyncHandle = Addressables.LoadAssetsAsync<WwiseStreamingMediaAsset>(assetKeys.AsEnumerable(), streamingMedia =>
							{
								AkAssetUtilities.UpdateStreamedFileIfNecessary(WriteableMediaDirectory, streamingMedia);
							}, Addressables.MergeMode.Union, false);

							await streamingAssetAsyncHandle.Task;
#if UNITY_EDITOR
							if (startingSceneName != SceneManager.GetActiveScene().name)
							{
								bank.loadState = BankLoadState.TimedOut;
							}
#endif
							Addressables.Release(streamingAssetAsyncHandle);
#if UNITY_EDITOR
						}
#endif
					}
				}
			}
			else
			{
				if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
				{
					UnityEngine.Debug.LogError($"Wwise Addressable Bank Manager : Failed to load {bank.name} SoundBank");
				}
				bank.loadState = BankLoadState.LoadFailed;
			}

			// WG-60155 Release the bank asset AFTER streaming media assets are handled, otherwise Unity can churn needlessly if they are all in the same asset bundle!
			if(bank.loadState != BankLoadState.TimedOut)
				OnBankLoaded(bank);
			if (asyncHandle.IsValid())
			{
				Addressables.Release(asyncHandle);
			}
		}
		public void UnloadBank(WwiseAddressableSoundBank bank, bool ignoreRefCount = false, bool removeFromBankDictionary = true)
		{
			if (!ignoreRefCount)
			{
				bank.refCount = Math.Max(0, bank.refCount - 1);
				if (bank.refCount != 0)
				{
					return;
				}
			}

			if (bank.loadState == BankLoadState.Loading || bank.loadState == BankLoadState.WaitingForPrepareEvent)
			{
				if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
				{
					UnityEngine.Debug.Log($"Wwise Addressable Bank Manager: {bank.name} will be unloaded after it is done loading");
				}
				m_banksToUnload.TryAdd(bank.name, bank.name);
				return;
			}

			if(bank.loadState == BankLoadState.Unloaded)
			{
#if WWISE_2024_OR_LATER
				AkUnitySoundEngine.PrepareEvent(AkPreparationType.Preparation_Unload, new string[] { bank.name }, 1);
#else
				AkSoundEngine.PrepareEvent(AkPreparationType.Preparation_Unload, new string[] { bank.name }, 1);
#endif
				if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
				{
					UnityEngine.Debug.Log($"Wwise Addressables Bank Manager: {bank.name} is already unloaded.");
				}
				return;
			}

			if (bank.loadState == BankLoadState.Loaded || bank.loadState == BankLoadState.TimedOut)
			{
				if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
				{
					UnityEngine.Debug.Log($"Wwise Addressable Bank Manager: Unloading {bank.name} sound bank - Bank ID : {bank.soundbankId}");
				}
				if (bank.bankType != 0)
				{
#if WWISE_2024_OR_LATER
					AkUnitySoundEngine.PrepareEvent(AkPreparationType.Preparation_Unload, new string[] { bank.name }, 1);
					AkUnitySoundEngine.UnloadBank(bank.soundbankId, System.IntPtr.Zero, bank.bankType);
#else
					AkSoundEngine.PrepareEvent(AkPreparationType.Preparation_Unload, new string[] { bank.name }, 1);
					AkSoundEngine.UnloadBank(bank.soundbankId, System.IntPtr.Zero, bank.bankType);
#endif
				}
				else
				{
#if WWISE_2024_OR_LATER
					AkUnitySoundEngine.UnloadBank(bank.soundbankId, System.IntPtr.Zero);
#else
					AkSoundEngine.UnloadBank(bank.soundbankId, System.IntPtr.Zero);
#endif
				}
			}

			m_banksToUnload.TryRemove(bank.name, out _);
			bank.soundbankId = 0;
			bank.refCount = 0;
			bank.loadState = BankLoadState.Unloaded;

			if (removeFromBankDictionary)
			{
				if (!m_AddressableBanks.TryRemove((bank.name, bank.isAutoBank), out _))
				{
#if UNITY_EDITOR
					// Don't unnecessarily log messages when caused by domain reload
					if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && !UnityEditor.EditorApplication.isPlaying)
					{
						return;
					}
#endif
					if (InitBank && bank.name != InitBank.name)
					{
						if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
						{
							UnityEngine.Debug.LogError($"Wwise Addressable Bank Manager: Unloaded {bank.name}, but it was not in the list of loaded banks");
						}
					}
				}
			}
		}

		public bool LoadedBankContainsEvent(string eventName, uint eventId, object eventObject, string methodName, Type[] methodArgTypes, object[] methodArgs)
		{
			foreach (var bank in m_AddressableBanks.Values)
			{
				if (bank.loadState == BankLoadState.Loaded && bank.eventNames != null && bank.eventNames.Contains(eventName))
				{
					return true;
				}
			}

			if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
			{
				UnityEngine.Debug.LogWarning($"Wwise Addressables : {eventName} will be delayed, because its soundbank has not been loaded.");
			}
			
			m_eventsToFireOnBankLoad.TryAdd(eventId, new EventContainer { eventName = eventName, eventObject = eventObject, methodName = methodName, methodArgTypes = methodArgTypes, methodArgs = methodArgs });
			return false;
		}

		private Type m_AkEventType;
		private Type EventType
		{
			get
			{
				if (m_AkEventType == null)
				{
					var assembly = Assembly.Load("AK.Wwise.Unity.API.WwiseTypes");
					m_AkEventType = assembly.GetType("AK.Wwise.Event");
				}
				return m_AkEventType;
			}
		}

		public void OnAutoBankLoaded(WwiseAddressableSoundBank bank)
		{
			if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
			{
				UnityEngine.Debug.Log($"Wwise Addressable Bank Manager : Loaded {bank.name} AutoBank -  Bank ID : {bank.soundbankId}");
			}
			bank.loadState = BankLoadState.Loaded;
			FireEventOnBankLoad(bank, false);
		}

		private void FireEventOnBankLoad(WwiseAddressableSoundBank bank, bool skipAutoBank)
		{
			//Fire any events that were waiting on the bank load
			var eventsToRemove = new List<uint>();
			foreach (var e in m_eventsToFireOnBankLoad)
			{
				if (bank.eventNames.Contains(e.Value.eventName))
				{
					if (skipAutoBank && bank.isAutoBank)
						continue;
					if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
					{
						UnityEngine.Debug.Log($"Wwise Addressable Bank Manager: Triggering delayed event {e.Value.eventName}");
					}
					MethodInfo handleEvent = EventType.GetMethod(e.Value.methodName, e.Value.methodArgTypes);
					handleEvent.Invoke(e.Value.eventObject, e.Value.methodArgs);
					eventsToRemove.Add(e.Key);
				}
			}

			foreach (var e in eventsToRemove)
			{
				m_eventsToFireOnBankLoad.TryRemove(e, out _);
			}
		}

		private void OnBankLoaded(WwiseAddressableSoundBank bank)
		{
			if (bank.loadState == BankLoadState.Loaded)
			{
				if (AkWwiseInitializationSettings.Instance.UserSettings.m_EngineLogging)
				{
					UnityEngine.Debug.Log($"Wwise Addressable Bank Manager : Loaded {bank.name} bank -  Bank ID : {bank.soundbankId}");
				}
				if (InitBankLoaded && bank.name == InitBank.name)
				{
					foreach (var b in m_AddressableBanks.Values)
					{
						if (b.loadState == BankLoadState.WaitingForInitBankToLoad)
						{
							LoadBank(b, b.decodeBank, b.saveDecodedBank);
						}
					}
				}

				FireEventOnBankLoad(bank, true);
			}
			
			else if (bank.loadState == BankLoadState.WaitingForPrepareEvent)
			{
				bank.BroadcastBankLoaded();
			}

			//Reset bank state if load failed
			if (bank.loadState == BankLoadState.LoadFailed)
			{
				UnloadBank(bank, ignoreRefCount : true);
			}
			
			if (m_banksToUnload.Keys.Contains(bank.name))
			{
				UnloadBank(bank);
			}
		}

		~AkAddressableBankManager()
		{
#if WWISE_2024_OR_LATER
			AkUnitySoundEngine.ClearBanks();
#else
			AkSoundEngine.ClearBanks();
#endif
		}
	}
}

#endif // AK_WWISE_ADDRESSABLES