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
#endif

namespace AK.Wwise.Unity.WwiseAddressables
{
	public class AkAddressableBankManager
	{

		private ConcurrentDictionary<string, WwiseAddressableSoundBank> m_AddressableBanks =
			new ConcurrentDictionary<string, WwiseAddressableSoundBank>();

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
					m_wwiseMajorVersion = AkSoundEngine.GetMajorMinorVersion() >> 16;
				}
				return (uint)m_wwiseMajorVersion;
			}
		}

		public string WriteableMediaDirectory
		{
			get
			{
				string pdp = UnityEngine.Application.persistentDataPath;
				return WwiseMajorVersion >= 2022 ? Path.Combine(pdp, "Media") : pdp;
			}
		}

		private static WwiseAddressableSoundBank FindInitBank()
		{
			var foundBank = UnityEngine.MonoBehaviour.FindObjectsOfType<InitBankHolder>();
			if (foundBank.Count() == 0)
			{
				UnityEngine.Debug.LogError("Wwise Addressables : There is no InitBankHolder in the scene, please add one for Wwise to function properly.");
				return null;
			}

			if (foundBank.Count() > 1)
			{
				UnityEngine.Debug.LogError("Wwise Addressables : There is more than one InitBankHolder in the scene, which is not recommended.");
			}

			WwiseAddressableSoundBank InitBank = foundBank[0].GetAddressableInitBank();
			if (InitBank == null)
			{
				UnityEngine.Debug.LogError("Wwise Addressables : The InitBankHolder could not get a valid reference to the Init bank.");
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
			var m_banksToReload = new ConcurrentDictionary<string, WwiseAddressableSoundBank>(m_AddressableBanks);
			UnloadAllBanks();
			UnloadInitBank();
#if WWISE_ADDRESSABLES_POST_2023
			LoadInitBank(AkWwiseInitializationSettings.Instance.LoadBanksAsynchronously);
#else
			LoadInitBank();
#endif


			foreach (var bank in m_banksToReload.Values)
			{
				LoadBank(bank, bank.decodeBank, bank.saveDecodedBank);
			}
		}

		public void SetLanguageAndReloadLocalizedBanks(string language)
		{
			var banksToReload = new List<WwiseAddressableSoundBank>();
			foreach (var bank in m_AddressableBanks.Values)
			{
				if (bank.currentLanguage == "SFX" || bank.currentLanguage == language)
					continue;
				banksToReload.Add(bank);
			}
			if (banksToReload.Count == 0)
			{
				return;
			}
			foreach (var bank in banksToReload)
			{
				UnloadBank(bank, ignoreRefCount: true, removeFromBankDictionary: true);
			}
			UnloadInitBank();
			AkSoundEngine.SetCurrentLanguage(language);
			AkSoundEngine.RenderAudio();
#if WWISE_ADDRESSABLES_POST_2023
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
		public void LoadBank(WwiseAddressableSoundBank bank, bool decodeBank = false, bool saveDecodedBank = false, bool addToBankDictionary = true, bool loadAsync = true)
		{
			bank.decodeBank = decodeBank;
			bank.saveDecodedBank = saveDecodedBank;
			if (m_AddressableBanks.ContainsKey(bank.name))
			{
				m_AddressableBanks.TryGetValue(bank.name, out bank);
			}
			else if (addToBankDictionary)
			{
				m_AddressableBanks.TryAdd(bank.name, bank);
			}

			if (bank.loadState == BankLoadState.Unloaded || bank.loadState == BankLoadState.WaitingForInitBankToLoad)
			{
				if (!InitBankLoaded && bank.name != "Init")
				{
					UnityEngine.Debug.Log($"Wwise Addressable Bank Manager: {bank.name} bank will be loaded after the init bank is loaded");
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
				UnityEngine.Debug.LogError($"Wwise Addressable Bank Manager : {bank.name} could not be loaded - Bank reference not set");
				m_AddressableBanks.TryRemove(bank.name, out _);
				return;
			}

			AssetReferenceWwiseBankData bankData;
			if (bank.Data.ContainsKey("SFX"))
			{
				UnityEngine.Debug.Log($"Wwise Addressable Bank Manager: Loading {bank.name} bank");
				bankData = bank.Data["SFX"];
				bank.currentLanguage = "SFX";
			}
			else
			{
				var currentLanguage = AkSoundEngine.GetCurrentLanguage();
				if (bank.Data.ContainsKey(currentLanguage))
				{
					bankData = bank.Data[currentLanguage];
					bank.currentLanguage = currentLanguage;
					UnityEngine.Debug.Log($"Wwise Addressable Bank Manager: Loading {bank.name} - {currentLanguage}");
				}
				else
				{
					UnityEngine.Debug.LogError($"Wwise Addressable Bank Manager: {bank.name} could not be loaded in {currentLanguage} language ");
					m_AddressableBanks.TryRemove(bank.name, out _);
					return;
				}
			}

			LoadBankAsync(bank, bankData, loadAsync);
		}

		public async Task LoadBankAsync(WwiseAddressableSoundBank bank, AssetReferenceWwiseBankData bankData, bool loadAsync)
		{
			var asyncHandle = bankData.LoadAssetAsync<WwiseSoundBankAsset>();
			WwiseSoundBankAsset soundBankAsset;
#if UNITY_WEBGL && !UNITY_EDITOR
			// On WebGL, we MUST load asynchronously in order to yield back to the browser.
			// Failing to do so will result in the thread blocking forever and the asset will never be loaded.
			soundBankAsset = await asyncHandle.Task;
#else
			if (loadAsync)
			{
				soundBankAsset = await asyncHandle.Task;
			}
			else
			{
				soundBankAsset = asyncHandle.WaitForCompletion();
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

				var result = AkSoundEngine.LoadBankMemoryCopy(bank.GCHandle.AddrOfPinnedObject(), (uint)data.Length, out uint bankID);
				if (result == AKRESULT.AK_Success)
				{
					bank.soundbankId = bankID;
					bank.loadState = BankLoadState.Loaded;
				}
				else
				{
					bank.soundbankId = INVALID_SOUND_BANK_ID;
					bank.loadState = BankLoadState.LoadFailed;
					if ((int)result == 100) // 100 == AK_InvalidBankType (using the raw int value until this package only supports Wwise 22.1 and up)
					{
						UnityEngine.Debug.LogWarning($"Wwise Addressable Bank Manager : Bank {bank.name} is an auto-generated bank. The Unity Wwise Addressables package only supports user-defined banks. Avoid using auto-generated banks.");
					}
					else
					{
						UnityEngine.Debug.Log($"Wwise Addressable Bank Manager : Sound Engine failed to load {bank.name} SoundBank");
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
								UnityEngine.Debug.LogError($"Wwise Addressable Bank Manager: Streaming media asset referenced in {bank.name} SoundBank is null");
								continue;
							}
							assetKeys.Add(streamedAsset);
						}
					}
					if (assetKeys.Count > 0)
					{
#if UNITY_EDITOR
						if ((EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) != 0 || EditorApplication.isPlaying)
#endif
						{
							var streamingAssetAsyncHandle = Addressables.LoadAssetsAsync<WwiseStreamingMediaAsset>(assetKeys.AsEnumerable(), streamingMedia =>
							{
								AkAssetUtilities.UpdateWwiseFileIfNecessary(WriteableMediaDirectory, streamingMedia);
							}, Addressables.MergeMode.Union, false);
#if UNITY_WEBGL && !UNITY_EDITOR
							// On WebGL, we MUST load asynchronously in order to yield back to the browser.
							// Failing to do so will result in the thread blocking forever and the asset will never be loaded.
							await streamingAssetAsyncHandle.Task;
#else
							streamingAssetAsyncHandle.WaitForCompletion();
#endif

							Addressables.Release(streamingAssetAsyncHandle);
						}
					}
				}
			}
			else
			{
				UnityEngine.Debug.LogError($"Wwise Addressable Bank Manager : Failed to load {bank.name} SoundBank");
				bank.loadState = BankLoadState.LoadFailed;
			}

			// WG-60155 Release the bank asset AFTER streaming media assets are handled, otherwise Unity can churn needlessly if they are all in the same asset bundle!
			OnBankLoaded(bank);
			Addressables.Release(asyncHandle);

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

			if (bank.loadState == BankLoadState.Loading)
			{
				UnityEngine.Debug.Log($"Wwise Addressable Bank Manager: {bank.name} will be unloaded after it is done loading");
				m_banksToUnload.TryAdd(bank.name, bank.name);
				return;
			}

			if(bank.loadState == BankLoadState.Unloaded)
			{
				UnityEngine.Debug.Log($"Wwise Addressables Bank Manager: {bank.name} is already unloaded.");
				return;
			}

			if (bank.loadState == BankLoadState.Loaded)
			{
				UnityEngine.Debug.Log($"Wwise Addressable Bank Manager: Unloading {bank.name} sound bank - Bank ID : {bank.soundbankId}");
				AkSoundEngine.UnloadBank(bank.soundbankId, System.IntPtr.Zero);

			}

			m_banksToUnload.TryRemove(bank.name, out _);
			bank.soundbankId = 0;
			bank.refCount = 0;
			bank.loadState = BankLoadState.Unloaded;

			if (removeFromBankDictionary)
			{
				if (!m_AddressableBanks.TryRemove(bank.name, out _))
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
						UnityEngine.Debug.LogError($"Wwise Addressable Bank Manager: Unloaded {bank.name}, but it was not in the list of loaded banks");
					}
				}
			}
		}

		public bool LoadedBankContainsEvent(string eventName, uint eventId, object eventObject, string methodName, Type[] methodArgTypes, object[] methodArgs)
		{
			foreach (var bank in m_AddressableBanks.Values)
			{
				if (bank.loadState == BankLoadState.Loaded && bank.eventNames.Contains(eventName))
				{
					return true;
				}
			}

			UnityEngine.Debug.LogWarning($"Wwise Addressables : {eventName} will be delayed, because its soundbank has not been loaded.");
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

		private void OnBankLoaded(WwiseAddressableSoundBank bank)
		{
			if (bank.loadState == BankLoadState.Loaded)
			{
				UnityEngine.Debug.Log($"Wwise Addressable Bank Manager : Loaded {bank.name} bank -  Bank ID : {bank.soundbankId}");

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

				//Fire any events that were waiting on the bank load
				var eventsToRemove = new List<uint>();
				foreach (var e in m_eventsToFireOnBankLoad)
				{
					if (bank.eventNames.Contains(e.Value.eventName))
					{
						UnityEngine.Debug.Log($"Wwise Addressable Bank Manager: Triggering delayed event {e.Value.eventName}");
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
			AkSoundEngine.ClearBanks();
		}
	}
}

#endif // AK_WWISE_ADDRESSABLES