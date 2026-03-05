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

#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES && UNITY_EDITOR

using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using System.Xml;
using UnityEditor.AddressableAssets.Settings;

namespace AK.Wwise.Unity.WwiseAddressables
{
	public struct AddressableEntryInformation : IEquatable<AddressableEntryInformation>
	{
		public string AssetPath;
		public string Platform;
		public string Language;
		public string Type;

		public bool Equals(AddressableEntryInformation other)
		{
			return AssetPath == other.AssetPath && Platform == other.Platform && Language == other.Language && Type == other.Type;
		}

		public override bool Equals(object obj)
		{
			return obj is AddressableEntryInformation other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(AssetPath, Platform, Language, Type);
		}
	}
	
	[InitializeOnLoad]
	public class AkAddressablesEditorUtilities : MonoBehaviour
	{
		public static IDictionary<string, string> ValidPlatforms = new Dictionary<string, string>();
		static AkAddressablesEditorUtilities()
		{
			WwiseAddressableSoundBank.GetWwisePlatformNameFromBuildTarget = GetWwisePlatformNameFromBuildTarget;
			ValidPlatforms = AkUtilities.GetAllBankPaths(AkBasePathGetter.GetWwiseProjectPath());
			WwiseProjectDatabase.SoundBankDirectoryUpdated += OnSoundBankUpdate;
			EditorApplication.quitting += Cleanup;
		}
		
		private static void Cleanup()
		{
			WwiseProjectDatabase.SoundBankDirectoryUpdated -= OnSoundBankUpdate;
			EditorApplication.quitting -= Cleanup;
		}
#if WWISE_ADDRESSABLES_24_1_OR_LATER
		static void RefreshIsJsonFileMissing()
		{
			WwiseProjectDatabase.SoundBankDirectoryUpdated -= RefreshIsJsonFileMissing;
			isJsonFileMissing = false;
		}
		private static bool isJsonFileMissing = false;
#endif

		public static Dictionary<string, PlatformEntry> SoundbanksInfo = new Dictionary<string, PlatformEntry>();

		public class SoundBankInfo
		{
			public List<string> streamedFileIds = new List<string>();
			public List<string> events = new List<string>();
#if WWISE_ADDRESSABLES_24_1_OR_LATER
			public bool isUserBank = true;
#endif
		}

		public class SoundBankEntry : Dictionary<string, SoundBankInfo>
		{
		}

		public class PlatformEntry : Dictionary<(string,string), SoundBankEntry>
		{
			public long lastParseTime;
			public Dictionary<string, List<(string,string)>> eventToSoundBankMap = new Dictionary<string, List<(string,string)>>();
#if WWISE_ADDRESSABLES_24_1_OR_LATER
			public bool containsInvalidEntry = false;

			public PlatformEntry()
			{
				WwiseProjectDatabase.SoundBankDirectoryUpdated += ResetInvalidEntry;
			}

			private void ResetInvalidEntry()
			{
				containsInvalidEntry = false;
			}
#endif
		}
		
		private static void OnSoundBankUpdate()
		{
			ValidPlatforms = AkUtilities.GetAllBankPaths(AkBasePathGetter.GetWwiseProjectPath());
		}


		public static string GetWwisePlatformNameFromBuildTarget(BuildTarget platform)
		{
			return AkBuildPreprocessor.GetPlatformName(platform);
		}

		public static bool IsAutoBank(string assetPath)
		{
			var banksPath = GetFullSoundbanksPath() + Path.DirectorySeparatorChar;
			var assetsFullPath = Path.GetFullPath(assetPath);
			
			string directoryName = Path.GetDirectoryName(assetsFullPath);
			var pathContent = directoryName.Split(Path.DirectorySeparatorChar).ToList();
			return pathContent.Contains("Event");
		}

		public static AddressableEntryInformation  ParseAssetPath(string assetPath)
		{
			var entry = new AddressableEntryInformation() { AssetPath = assetPath, Platform = String.Empty, Language = "SFX", Type = "User" };
			
			if (ValidPlatforms == null)
			{
				return entry;
			}

			var r = new Regex(Regex.Escape("_WwiseIntegrationTemp"));
			assetPath  = r.Replace(assetPath, "", 1);

			var wrongSeparatorChar = System.IO.Path.DirectorySeparatorChar == '/' ? '\\' : '/';
			string normalizedPath = assetPath.Replace(wrongSeparatorChar, System.IO.Path.DirectorySeparatorChar);

			string[] segments = normalizedPath.Split(System.IO.Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
			
			int length = segments.Length;
			
			int platformIndex = -1;
			
			for (int i = length - 1; i >= 0; i--) 
			{
				if (ValidPlatforms.ContainsKey(segments[i]))
				{
					platformIndex = i;
					break;
				}
			}

			if (platformIndex == -1)
			{
				return entry;
			}

			entry.Platform = segments[platformIndex];

			if (segments.Length > (platformIndex + 2))
			{
				if (segments[platformIndex + 1] == "Media" || segments[platformIndex + 1] == "Bus" || segments[platformIndex + 1] == "Event")
				{
					entry.Type = segments[platformIndex + 1];

					if (segments.Length > (platformIndex + 3))
					{
						entry.Language = segments[platformIndex +2];
					}
				}
				
				else
				{
					// Localized bank file; the sub-folder name is the locale string
					entry.Language = segments[platformIndex + 1];
				}
			}
			return entry;
		}

		public static string GetSoundbanksPath()
		{
			if (AkWwiseEditorSettings.Instance.GeneratedSoundbanksPath == null)
			{
				UnityEngine.Debug.LogError("Wwise Addressables: You need to set the GeneratedSoundbankPath in the Wwise Editor settings or assets will not be properly imported.");
				return string.Empty;
			}
			var path = Path.Combine("Assets", AkWwiseEditorSettings.Instance.GeneratedSoundbanksPath);
			return path.Replace("\\", "/");
		}

		private static string GetFullSoundbanksPath()
		{
			if (AkWwiseEditorSettings.Instance.GeneratedSoundbanksPath == null)
			{
				UnityEngine.Debug.LogError("Wwise Addressables: You need to set the GeneratedSoundbankPath in the Wwise Editor settings or assets will not be properly imported.");
				return string.Empty;
			}
			var path = Path.Combine("Assets", AkWwiseEditorSettings.Instance.GeneratedSoundbanksPath);
			return Path.GetFullPath(path);
		}


		public static void ClearSoundbankInfo()
		{
			SoundbanksInfo.Clear();
		}
#if WWISE_ADDRESSABLES_24_1_OR_LATER
		public static void AddSoundBank(string bankName, string bankLanguage, string type, ref PlatformEntry soundBankDict, WwiseSoundBankRef sbInfo)
		{
			bool isAutoBank = !sbInfo.IsUserBank;
			soundBankDict.TryAdd((bankName,type), new SoundBankEntry());
			soundBankDict[(bankName,type)][bankLanguage] = new SoundBankInfo();
			for (int i = 0; i < sbInfo.MediasCount; ++i)
			{
				if (!soundBankDict[(bankName, type)].ContainsKey(sbInfo.Medias[i].Language))
				{
					soundBankDict[(bankName,type)][sbInfo.Medias[i].Language] = new SoundBankInfo();
				}
				RecordMediaFile(soundBankDict, (bankName, type), sbInfo.Medias[i].ShortId.ToString(), sbInfo.Medias[i].Language); 

			}
			for (int i = 0; i < sbInfo.EventsCount; ++i)
			{
				RecordEvent(soundBankDict, (bankName,type), sbInfo.Language, sbInfo.Events[i].Name);
			}
			soundBankDict[(bankName,type)][sbInfo.Language].isUserBank = sbInfo.IsUserBank;
		}
		public static async Task<PlatformEntry> ExecuteUpdate(string platformName, string newBankName, string language, string type)
		{
			WwiseProjectDatabase.SetCurrentPlatform(platformName);
			WwiseProjectDatabase.SetCurrentLanguage(language);
			
			bool doUpdate = true;
			if (!SoundbanksInfo.ContainsKey(platformName))
			{
				SoundbanksInfo[platformName] = new PlatformEntry();
				WwisePlatformRef platformInfo = new WwisePlatformRef(platformName);
				if (platformInfo.Name == null)
				{ 
					WwiseProjectDatabase.Init(AkBasePathGetter.GetWwiseRootOutputPath(), platformName, language);
				}
			}
			if (SoundbanksInfo.ContainsKey(platformName) && SoundbanksInfo[platformName].containsInvalidEntry)
			{
				doUpdate = false;
			}
			if (doUpdate)
			{
				await UpdatePlatformEntry(SoundbanksInfo[platformName], newBankName, platformName, language, type);
			}

			return SoundbanksInfo[platformName];
		}

		public static async Task UpdatePlatformEntry(PlatformEntry soundBanks, string newBankName, string platformName, string language, string type)
		{
			WwiseSoundBankRef sbInfo = new WwiseSoundBankRef(newBankName, type);
			if (!sbInfo.IsValid)
			{
				WwiseProjectDatabase.Init(AkBasePathGetter.GetWwiseRootOutputPath(), platformName, language);
				sbInfo = new WwiseSoundBankRef(newBankName, type);
			}
			if (sbInfo.IsValid)
			{
				AddSoundBank(newBankName, language, type, ref soundBanks, sbInfo);
			}
			else
			{
				soundBanks.containsInvalidEntry = true;
			}
			soundBanks.lastParseTime = DateTime.Now.Ticks;
		}
#endif

		public static void AddSoundBank(string bankName, string bankLanguage, ref PlatformEntry soundBankDict)
		{
			if (!soundBankDict.ContainsKey((bankName, "User")))
			{
				soundBankDict.Add((bankName,"User"), new SoundBankEntry());
			}
			soundBankDict[(bankName,"User")][bankLanguage] = new SoundBankInfo();
		}
		public static PlatformEntry ExecuteParse(string platformName, string newBankName, string xmlFilename)
		{
			bool doParse = false;
			if (!SoundbanksInfo.ContainsKey(platformName))
			{
				doParse = true;
			}
			else if (SoundbanksInfo.ContainsKey(platformName) && !SoundbanksInfo[platformName].ContainsKey((newBankName,"User")))
			{
				doParse = true;
			}
			else
			{
				var fileModifiedTime = System.IO.File.GetLastWriteTime(xmlFilename);
				if (fileModifiedTime.Ticks > SoundbanksInfo[platformName].lastParseTime)
				{
					doParse = true;
				}
			}

			if (doParse)
			{
				var doc = new System.Xml.XmlDocument();
				PlatformEntry soundBanks;

				try
				{
					doc.Load(xmlFilename);
				}
				catch (XmlException e)
				{
					UnityEngine.Debug.LogError("Exception occurred while parsing SoundBanksInfo.xml. Cannot update project SoundBanks info: " + e);
					return null;
				}

				XmlElement root = doc.DocumentElement;
				if (!Int32.TryParse(root.GetAttribute("SchemaVersion"), out int schemaVersion))
				{
					Debug.LogError($"Could not parse SoundbanksInfo.xml for {platformName}. Check {xmlFilename} for possible corruption.");
					return null;
				}

				if (schemaVersion >= 16)
				{
					soundBanks = ParseSoundBanksInfoXmlv16(doc);
				}
				else
				{
					soundBanks = ParseSoundBanksInfoXmlv15(doc);
				}
				soundBanks.lastParseTime = DateTime.Now.Ticks;
				SoundbanksInfo[platformName] = soundBanks;
			}

			if (SoundbanksInfo[platformName].eventToSoundBankMap.Count == 0)
			{
				Debug.LogWarning($"Could not retrieve event data for {platformName} from SoundbanksInfo.xml. Check {xmlFilename} for possible corruption.");
			}

			return SoundbanksInfo[platformName];
		}

		private static PlatformEntry ParseSoundBanksInfoXmlv16(XmlDocument doc)
		{
			var soundBanks = new PlatformEntry();
			var soundBanksRootNode = doc.GetElementsByTagName("SoundBanks");
			for (var i = 0; i < soundBanksRootNode.Count; i++)
			{
				var soundBankNodes = soundBanksRootNode[i].SelectNodes("SoundBank");
				for (var j = 0; j < soundBankNodes.Count; j++)
				{
					var bankName = soundBankNodes[j].SelectSingleNode("ShortName").InnerText;
					var language = soundBankNodes[j].Attributes.GetNamedItem("Language").Value;

					AddSoundBank(bankName, language, ref soundBanks);

					if (bankName.Equals("Init"))
					{
						continue;
					}

					// First, record all streamed media contained in this bank.
					var mediaRootNode = soundBankNodes[j].SelectSingleNode("Media");
					if (mediaRootNode != null)
					{
						var fileNodes = mediaRootNode.SelectNodes("File");
						foreach (XmlNode fileNode in fileNodes)
						{
							RecordMediaFile(
								soundBanks,
								(bankName,"User"),
								fileNode.Attributes["Id"].Value,
								fileNode.Attributes["Language"].Value);
						}
					}

					// Then, record all events contained in the bank
					var includedEventsNode = soundBankNodes[j].SelectSingleNode("Events");
					if (includedEventsNode != null)
					{
						var eventNodes = includedEventsNode.SelectNodes("Event");
						foreach (XmlNode eventNode in eventNodes)
						{
							RecordEvent(soundBanks, (bankName,"User"), language, eventNode.Attributes["Name"].Value);
						}
					}
				}
			}

			return soundBanks;
		}

		private static PlatformEntry ParseSoundBanksInfoXmlv15(XmlDocument doc)
		{
			var soundBanks = new PlatformEntry();
			var soundBanksRootNode = doc.GetElementsByTagName("SoundBanks");
			for (var i = 0; i < soundBanksRootNode.Count; i++)
			{
				var soundBankNodes = soundBanksRootNode[i].SelectNodes("SoundBank");
				for (var j = 0; j < soundBankNodes.Count; j++)
				{
					var bankName = soundBankNodes[j].SelectSingleNode("ShortName").InnerText;
					var language = soundBankNodes[j].Attributes.GetNamedItem("Language").Value;

					AddSoundBank(bankName, language, ref soundBanks);

					if (bankName.Equals("Init"))
					{
						continue;
					}

					var includedEventsNode = soundBankNodes[j].SelectSingleNode("IncludedEvents");
					if (includedEventsNode != null)
					{
						var eventNodes = includedEventsNode.SelectNodes("Event");
						for (var e = 0; e < eventNodes.Count; e++)
						{
							RecordEvent(soundBanks, (bankName,"User"), language, eventNodes[e].Attributes["Name"].Value);

							var streamedFilesRootNode = eventNodes[e].SelectSingleNode("ReferencedStreamedFiles");
							if (streamedFilesRootNode != null)
							{
								var streamedFileNodes = streamedFilesRootNode.SelectNodes("File");
								if (streamedFileNodes.Count > 0)
								{
									for (var s = 0; s < streamedFileNodes.Count; s++)
									{
										RecordMediaFile(
											soundBanks,
											(bankName,"User"),
											streamedFileNodes[s].Attributes["Id"].Value,
											streamedFileNodes[s].Attributes.GetNamedItem("Language").Value);
									}
								}
							}
						}
					}
				}
			}

			return soundBanks;
		}

		public static PlatformEntry GetPlatformSoundbanks(string platformName)
		{
			return SoundbanksInfo[platformName];
		}
		//Parse soundbank xml file to get a dict of the streaming wem files
		public static async Task<PlatformEntry> ParsePlatformSoundbanks(string platformName, string newBankName, string language, string type)
		{
			if (platformName == null)
			{
				platformName = AkBasePathGetter.GetPlatformName();
			}

			if (ValidPlatforms == null || !ValidPlatforms.ContainsKey(platformName))
			{
				return null;
			}

			var sourceFolder = Path.GetFullPath(Path.Combine(AkBasePathGetter.GetWwiseProjectDirectory(), ValidPlatforms[platformName]));
			
#if WWISE_ADDRESSABLES_24_1_OR_LATER
			var jsonFilename = Path.Combine(sourceFolder, "SoundbanksInfo.json");
			if (File.Exists(jsonFilename))
			{
				return await ExecuteUpdate(platformName, newBankName, language, type);
			}
			if (!isJsonFileMissing && WwiseAddressableAdapter.Instance.IsAutoBankEnabled())
			{
				WwiseProjectDatabase.SoundBankDirectoryUpdated += RefreshIsJsonFileMissing;
				isJsonFileMissing = true;
				Debug.LogWarning($"Could not find SoundbanksInfo.json, falling back to SoundbanksInfo.xml. To fully benefit from AutoBanks, make sure Object GUID, Object Path and Generate JSON Metadata is checked in the WwiseProject. Then, clear {sourceFolder} and regenerate the Soundbanks.");
			}
#endif
			var xmlFilename = Path.Combine(sourceFolder, "SoundbanksInfo.xml");
			if (!File.Exists(xmlFilename))
			{
				Debug.LogWarning($"Could not find SoundbanksInfo.xml at {Path.Combine(AkWwiseEditorSettings.Instance.GeneratedSoundbanksPath, platformName)}. Check the Generated Soundbanks Path in the Unity Wwise project settings. Using the Wwise Project to find SoundbanksInfo.xml.");
				if (!AkBasePathGetter.GetSoundBankPaths(platformName, out sourceFolder, out string destinationFolder))
				{
					Debug.LogError($"Failed to import {newBankName}. Could not get SoundBank folder for {platformName} from Wwise Project {AkWwiseEditorSettings.Instance.WwiseProjectPath}.");
					return null;
				}
				
				xmlFilename = Path.Combine(sourceFolder, "SoundbanksInfo.xml");
				if(!File.Exists(xmlFilename))
				{
					Debug.LogError($"Failed to import {newBankName}. Could not find SoundbanksInfo for {platformName} platform. Make sure your SoundBanks are generated and that the setting \"Generate XML Metadata\" is enabled. Then, clear {sourceFolder} and regenerate the Soundbanks.");
					return null;
				}
			}
			return ExecuteParse(platformName, newBankName, xmlFilename);
		}

		public static void FindAndSetBankReference(WwiseAddressableSoundBank addressableBankAsset, string name)
		{
#if WWISE_ADDRESSABLES_24_1_OR_LATER
			if (addressableBankAsset.IsAutoBank)
			{
				WwiseEventReference.FindEventReferenceAndSetAddressableBank(addressableBankAsset, name);
				return;
			}
	#endif
			WwiseBankReference.FindBankReferenceAndSetAddressableBank(addressableBankAsset, name);
		}

		public static void EnsureInitBankAssetCreated()
		{
			var guids = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(WwiseInitBankReference).Name, new string[] { AkWwiseEditorSettings.WwiseScriptableObjectRelativePath });
			var InitBankAssetPath = Path.Combine(AkWwiseEditorSettings.WwiseScriptableObjectRelativePath, "InitBank.asset");
			if (guids.Length == 0)
			{
				try
				{
					AssetDatabase.StartAssetEditing();
					WwiseInitBankReference InitBankRef = UnityEngine.ScriptableObject.CreateInstance<WwiseInitBankReference>();
					UnityEditor.AssetDatabase.CreateAsset(InitBankRef, InitBankAssetPath);
				}
				finally
				{
					AssetDatabase.StopAssetEditing();
				}
			}
		}

		private static void RecordEvent(PlatformEntry soundBanks, (string,string) bankKey, string language, string eventName)
		{
			soundBanks[bankKey][language].events.Add(eventName);
		}

		private static void RecordMediaFile(PlatformEntry soundBanks, (string,string) bankKey, string id, string language)
		{
#if !WWISE_ADDRESSABLES_24_1_OR_LATER
			if (!soundBanks[bankKey].ContainsKey(language))
			{
				AddSoundBank(bankKey.Item1, language, ref soundBanks);
			}
#endif
			// Record that this bank "contains" this streamed media file
			soundBanks[bankKey][language].streamedFileIds.Add(id);

			// Record that this streamed media file is "contained" in this bank
			if (!soundBanks.eventToSoundBankMap.ContainsKey(id))
			{
				soundBanks.eventToSoundBankMap[id] = new List<(string,string)>();
			}
			soundBanks.eventToSoundBankMap[id].Add(bankKey);
		}
	}
}
#endif