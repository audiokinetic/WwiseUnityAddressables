#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AK.Wwise.Unity.WwiseAddressables
{
	[Serializable]
	public class WwiseAddressableSoundBank : ScriptableObject, ISerializationCallbackReceiver
	{
		[SerializeField]
		internal WwiseBankPerPlatformEntry[] m_dataPerPlatformList;

		[SerializeField]
		internal WwiseBankPerPlatformEntry currentPlatformAssets;

		[System.NonSerialized]
		internal BankLoadState loadState = BankLoadState.Unloaded;

		[System.NonSerialized]
		internal string currentLanguage;

		[System.NonSerialized]
		internal uint soundbankId = AkAddressableBankManager.INVALID_SOUND_BANK_ID;

		[System.NonSerialized]
		internal GCHandle GCHandle;

		[System.NonSerialized]
		internal bool decodeBank;

		[System.NonSerialized]
		internal bool saveDecodedBank;

		[System.NonSerialized]
		internal HashSet<string> eventNames;

		[System.NonSerialized]
		internal int refCount;

#if UNITY_EDITOR
		public delegate string GetWwisePlatformNameDelegate(BuildTarget target);
		public static GetWwisePlatformNameDelegate GetWwisePlatformNameFromBuildTarget;
#endif

		public Dictionary<string, AssetReferenceWwiseBankData> Data
		{
			get
			{
#if UNITY_EDITOR
				string wwisePlatform = AkBasePathGetter.GetPlatformName();
				return m_dataPerPlatformList.Where(x => x.WwisePlatform == wwisePlatform).Select(x => x.LocalizedBanks).FirstOrDefault();
#else
				return currentPlatformAssets.LocalizedBanks;
#endif
			}
		}

		public Dictionary<string, StreamingMediaList> StreamingMedia
		{
			get
			{
#if UNITY_EDITOR
				string wwisePlatform = AkBasePathGetter.GetPlatformName();
				return m_dataPerPlatformList.Where(x => x.WwisePlatform == wwisePlatform).Select(x => x.LocalizedStreamingMedia).FirstOrDefault();
#else
				return currentPlatformAssets.LocalizedStreamingMedia;
#endif
			}
		}

		public void OnAfterDeserialize()
		{
#if UNITY_EDITOR
			if (m_dataPerPlatformList != null)
			{
				foreach (var entry in m_dataPerPlatformList)
				{
					entry.Deserialize();
				}
			}
#else
			currentPlatformAssets.Deserialize();
#endif
		}

		public void OnBeforeSerialize()
		{
#if UNITY_EDITOR

			string wwisePlatform = GetWwisePlatformNameFromBuildTarget(EditorUserBuildSettings.activeBuildTarget);
			if (m_dataPerPlatformList != null)
			{
				foreach (var entry in m_dataPerPlatformList)
				{
					entry.LocalizedBankKeys = entry.LocalizedBanks.Keys.ToArray();
					entry.LocalizedBanksValues = entry.LocalizedBanks.Values.ToArray();
					entry.LocalizedStreamingMediaKeys = entry.LocalizedStreamingMedia.Keys.ToArray();
					entry.LocalizedStreamingMediaValues = new LocalizedStreamingMediaList();
					entry.LocalizedStreamingMediaValues.Add(entry.LocalizedStreamingMedia.Values.ToList());

					if (entry.WwisePlatform == wwisePlatform)
					{
						currentPlatformAssets = entry;
					}
				}
			}
#endif
			loadState = BankLoadState.Unloaded;
		}

#if UNITY_EDITOR
		public void AddOrUpdate(string wwisePlatform, string language, AssetReferenceWwiseBankData bankRef)
		{
			if (m_dataPerPlatformList == null)
			{
				m_dataPerPlatformList = Array.Empty<WwiseBankPerPlatformEntry>();
			}

			WwiseBankPerPlatformEntry foundEntry = null;
			foreach (var entry in m_dataPerPlatformList)
			{
				if (entry.WwisePlatform == wwisePlatform)
				{
					if (entry.WwisePlatform == wwisePlatform)
					{
						foundEntry = entry;
						break;
					}
				}
			}

			if (foundEntry != null)
			{
				foundEntry.LocalizedBanks[language] = bankRef;
			}
			else
			{
				foundEntry = new WwiseBankPerPlatformEntry { WwisePlatform = wwisePlatform };
				foundEntry.LocalizedBanks[language] = bankRef;
				ArrayUtility.Add(ref m_dataPerPlatformList, foundEntry);
			}
		}

		public void SetStreamingMedia(string wwisePlatform, string language, string platformDir, List<string> streamingMediaIds)
		{
			List<string> uniqueList = streamingMediaIds.Distinct().ToList();
			foreach (var entry in m_dataPerPlatformList)
			{
				if (entry.WwisePlatform == wwisePlatform)
				{

					entry.LocalizedStreamingMedia[language] = new StreamingMediaList();

					foreach (var Id in uniqueList)
					{
						var mediaPath = System.IO.Path.Combine(platformDir, Id + ".wem");
						entry.LocalizedStreamingMedia[language].Add(new AssetReferenceStreamingMedia(AssetDatabase.AssetPathToGUID(mediaPath), Id));
					}
					break;
				}
			}
		}

		public void UpdateLocalizationLanguages(string wwisePlatform, List<string> parsedLanguages)
		{
			if (m_dataPerPlatformList == null) return;
			foreach (var entry in m_dataPerPlatformList)
			{
				if (entry.WwisePlatform == wwisePlatform)
				{
					var toRemove = new List<string>();
					foreach (var lang in entry.LocalizedStreamingMedia.Keys)
					{
						if (!parsedLanguages.Contains(lang))
						{
							toRemove.Add(lang);
						}
					}

					foreach (var lang in toRemove)
					{
						entry.LocalizedStreamingMedia.Remove(lang);
					}

					toRemove.Clear();
					foreach (var lang in entry.LocalizedBanks.Keys)
					{
						if (!parsedLanguages.Contains(lang))
						{
							toRemove.Add(lang);
						}
					}

					foreach (var lang in toRemove)
					{
						entry.LocalizedBanks.Remove(lang);
					}
				}
			}
		}

		public bool TryRemoveMedia(string wwisePlatform, string language, string mediaGuid)
		{
			foreach (var entry in m_dataPerPlatformList)
			{
				if (entry.WwisePlatform == wwisePlatform)
				{
					if (!entry.LocalizedStreamingMedia.ContainsKey(language))
					{
						break;
					}
					AssetReferenceStreamingMedia mediaToRemove = null;

					foreach (var media in entry.LocalizedStreamingMedia[language].media)
					{
						if (media.AssetGUID == mediaGuid)
						{
							mediaToRemove = media;
							break;
						}
					}
					if (mediaToRemove != null)
					{
						entry.LocalizedStreamingMedia[language].media.Remove(mediaToRemove);
						return true;
					}
				}
			}
			return false;
		}

		public bool TryRemoveBank(string wwisePlatform, string language, string bankGuid)
		{
			foreach (var entry in m_dataPerPlatformList)
			{
				if (entry.WwisePlatform == wwisePlatform)
				{
					if (!entry.LocalizedBanks.ContainsKey(language))
					{
						break;
					}
					if (entry.LocalizedBanks[language].AssetGUID == bankGuid)
					{
						entry.LocalizedBanks.Remove(language);
						return true;

					}
				}
			}
			return false;
		}
#endif
	}

	[Serializable]
	public class AssetReferenceWwiseAddressableBank : AssetReferenceT<WwiseAddressableSoundBank>
	{
		public AssetReferenceWwiseAddressableBank(string guid)
		: base(guid)
		{
		}
	}

	[Serializable]
	public class AssetReferenceWwiseBankData : AssetReferenceT<WwiseSoundBankAsset>
	{
		public AssetReferenceWwiseBankData(string guid)
		: base(guid)
		{
		}
	}

	[Serializable]
	public class AssetReferenceStreamingMedia : AssetReferenceT<WwiseStreamingMediaAsset>
	{
		public string id;

		public AssetReferenceStreamingMedia(string guid, string id)
		: base(guid)
		{
			this.id = id;
		}
	}

	[Serializable]
	public class StreamingMediaList 
	{
		public List<AssetReferenceStreamingMedia> media = new List<AssetReferenceStreamingMedia>();
		public void Add(AssetReferenceStreamingMedia m)
		{
			media.Add(m);
		}


		public AssetReferenceStreamingMedia this[int key]
		{
			get
			{
				return media[key];
			}
			set
			{
				media[key] = value;
			}
		}
	}

	[Serializable]
	public class LocalizedStreamingMediaList 
	{
		public List<StreamingMediaList> localizedMediaList = new List<StreamingMediaList>();
		public void Add(StreamingMediaList mediaList)
		{
			localizedMediaList.Add(mediaList);
		}

		public void Add(List<StreamingMediaList> mediaList)
		{
			foreach (var l in mediaList)
			{
				localizedMediaList.Add(l);
			}
		}

		public StreamingMediaList this[int key]
		{
			get
			{
				return localizedMediaList[key];
			}
			set
			{
				localizedMediaList[key] = value;
			}
		}
	}

	[Serializable]
	public class WwiseBankPerPlatformEntry
	{
		public string WwisePlatform;
		[NonSerialized]
		public Dictionary<string, AssetReferenceWwiseBankData> LocalizedBanks = new Dictionary<string, AssetReferenceWwiseBankData>();
		[NonSerialized]
		public Dictionary<string, StreamingMediaList> LocalizedStreamingMedia = new Dictionary<string, StreamingMediaList>();

		public string[] LocalizedBankKeys;
		public AssetReferenceWwiseBankData[] LocalizedBanksValues;

		public string[] LocalizedStreamingMediaKeys;
		public LocalizedStreamingMediaList LocalizedStreamingMediaValues;

		public void Deserialize()
		{
			int idx = 0;
			foreach (var key in LocalizedBankKeys)
			{
				LocalizedBanks.Add(key, LocalizedBanksValues[idx]);
				idx++;
			}
			idx = 0;
			foreach (var key in LocalizedStreamingMediaKeys)
			{
				LocalizedStreamingMedia.Add(key, LocalizedStreamingMediaValues[idx]);
				idx++;
			}
		}
	}

	public enum BankLoadState
	{
		Unloaded,
		WaitingForInitBankToLoad,
		Loading,
		Loaded,
		LoadFailed
	}
}
#endif // AK_WWISE_ADDRESSABLES
