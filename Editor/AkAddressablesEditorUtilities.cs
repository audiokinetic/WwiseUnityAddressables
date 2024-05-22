#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES && UNITY_EDITOR

using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using System.Xml;

namespace AK.Wwise.Unity.WwiseAddressables
{
	[InitializeOnLoad]
	public class AkAddressablesEditorUtilities : MonoBehaviour
	{
		static AkAddressablesEditorUtilities()
		{
			WwiseAddressableSoundBank.GetWwisePlatformNameFromBuildTarget = GetWwisePlatformNameFromBuildTarget;
		}

		public static Dictionary<string, PlatformEntry> soundbanksInfo = new Dictionary<string, PlatformEntry>();
		
		

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

		public class PlatformEntry : Dictionary<string, SoundBankEntry>
		{
			public long lastParseTime;
			public Dictionary<string, List<string>> eventToSoundBankMap = new Dictionary<string, List<string>>();

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

		public static void ParseAssetPath(string assetPath, out string platform, out string language)
		{
			platform = string.Empty;
			language = "SFX";

			var banksPath = GetFullSoundbanksPath() + Path.DirectorySeparatorChar;
			var assetsFullPath = Path.GetFullPath(assetPath);

			// TODO Use Path.RelativePath as soon as Unity uses a .NET version that includes it (i.e 2021.3)
			var assetRelPath = assetsFullPath.Replace(banksPath, "");

			string[] parts = assetRelPath.Split(Path.DirectorySeparatorChar);
			platform = parts[0];

			if (parts.Length > 2)
			{
				// Asset is stored in a sub-folder; we must identify the purpose of the sub-folder.
				if (parts[1] == "Media" || parts[1] == "Bus" || parts[1] == "Event")
				{
					// Starting with Wwise 2022.1, loose media files are stored in a sub-directory named "Media".
					// These themselves can be in localized sub-folders.
					if (parts.Length > 3)
					{
						// The sub-sub folder name is the locale string
						language = parts[2];
					}
				}
				else
				{
					// Localized bank file; the sub-folder name is the locale string
					language = parts[1];
				}
			}
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
			soundbanksInfo.Clear();
		}

#if WWISE_ADDRESSABLES_24_1_OR_LATER
		public static void AddSoundBank(string bankName, string bankLanguage, ref PlatformEntry soundBankDict, AkWwiseCRefSoundBank sbInfo)
		{
			soundBankDict.TryAdd(bankName, new SoundBankEntry());
			soundBankDict[bankName][bankLanguage] = new SoundBankInfo();
			for (int i = 0; i < sbInfo.MediasCount; ++i)
			{
				RecordMediaFile(soundBankDict, bankName, sbInfo.Medias[i].ShortId.ToString(), sbInfo.Medias[i].Language); 

			}
			for (int i = 0; i < sbInfo.EventSCount; ++i)
			{
				RecordEvent(soundBankDict, bankName, sbInfo.Language, sbInfo.Events[i].Name);
			}
			soundBankDict[bankName][sbInfo.Language].isUserBank = sbInfo.IsUserBank;
		}
		
		public static async Task<PlatformEntry> ExecuteUpdate(string platformName, string newBankName, string language)
		{
			AkProjectDB.SetCurrentPlatform(platformName);
			AkProjectDB.SetCurrentLanguage(language);
			
			bool doUpdate = false;
			if (!soundbanksInfo.ContainsKey(platformName))
			{
				soundbanksInfo[platformName] = new PlatformEntry();
				AkWwiseCRefPlatform platformInfo = new AkWwiseCRefPlatform(platformName);
				if (platformInfo.Name == null)
				{
					await AkProjectDB.Init(AkBasePathGetter.GetFullSoundBankPathEditor(), platformName, language);
				}
				doUpdate = true;
			}
			else if (!soundbanksInfo[platformName].ContainsKey(newBankName) || !soundbanksInfo[platformName][newBankName].ContainsKey(language))
			{
				doUpdate = true;
			}

			if (doUpdate)
			{
				await UpdatePlatformEntry(soundbanksInfo[platformName], newBankName, platformName, language);
			}

			return soundbanksInfo[platformName];
		}
		
		public static async Task UpdatePlatformEntry(PlatformEntry soundBanks, string newBankName, string platformName, string language)
		{
			AkWwiseCRefSoundBank sbInfo = new AkWwiseCRefSoundBank(newBankName);
			if (!sbInfo.IsValid)
			{
				await AkProjectDB.Init(AkBasePathGetter.GetFullSoundBankPathEditor(), platformName, language);
				sbInfo = new AkWwiseCRefSoundBank(newBankName);
			}
			if (sbInfo.IsValid)
			{
				AddSoundBank(newBankName, language, ref soundBanks, sbInfo);
			}
			soundBanks.lastParseTime = DateTime.Now.Ticks;
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

#else
		public static void AddSoundBank(string bankName, string bankLanguage, ref PlatformEntry soundBankDict)
		{
			if (!soundBankDict.ContainsKey(bankName))
			{
				soundBankDict.Add(bankName, new SoundBankEntry());
			}
			soundBankDict[bankName][bankLanguage] = new SoundBankInfo();
		}
		public static PlatformEntry ExecuteParse(string platformName, string newBankName, string xmlFilename)
		{
			bool doParse = false;
			if (!soundbanksInfo.ContainsKey(platformName))
			{
				doParse = true;
			}
			else if (soundbanksInfo.ContainsKey(platformName) && !soundbanksInfo[platformName].ContainsKey(newBankName))
			{
				doParse = true;
			}
			else
			{
				var fileModifiedTime = System.IO.File.GetLastWriteTime(xmlFilename);
				if (fileModifiedTime.Ticks > soundbanksInfo[platformName].lastParseTime)
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
					Debug.LogError($"Could not parse SoundBanksInfo.xml for {platformName}. Check {xmlFilename} for possible corruption.");
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
				soundbanksInfo[platformName] = soundBanks;
			}

			if (soundbanksInfo[platformName].eventToSoundBankMap.Count == 0)
			{
				Debug.LogWarning($"Could not retrieve event data for {platformName} from SoundBanksInfo.xml. Check {xmlFilename} for possible corruption.");
			}

			return soundbanksInfo[platformName];
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
								bankName,
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
							RecordEvent(soundBanks, bankName, language, eventNode.Attributes["Name"].Value);
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
							RecordEvent(soundBanks, bankName, language, eventNodes[e].Attributes["Name"].Value);

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
											bankName,
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
#endif
		//Parse soundbank xml file to get a dict of the streaming wem files
		public static async Task<PlatformEntry> ParsePlatformSoundbanksXML(string platformName, string newBankName, string language)
		{
			if (platformName == null)
			{
				platformName = AkBasePathGetter.GetPlatformName();
			}

			string sourceFolder = Path.Combine("Assets", AkWwiseEditorSettings.Instance.GeneratedSoundbanksPath, platformName);
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
					Debug.LogError($"Failed to import {newBankName}. Could not find SoundbanksInfo for {platformName} platform. Make sure your SoundBanks are generated and that the setting \"Generate XML Metadata\" is enabled.");
					return null;
				}
			}
#if WWISE_ADDRESSABLES_24_1_OR_LATER
			return await ExecuteUpdate(platformName, newBankName, language);
#else
			return ExecuteParse(platformName, newBankName, xmlFilename);
#endif
		}

		public static void FindAndSetBankReference(WwiseAddressableSoundBank addressableBankAsset, string name)
		{
			if (!WwiseBankReference.FindBankReferenceAndSetAddressableBank(addressableBankAsset, name))
			{
#if WWISE_ADDRESSABLES_24_1_OR_LATER
				WwiseEventReference.FindEventReferenceAndSetAddressableBank(addressableBankAsset, name);
#endif
			}
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

		private static void RecordEvent(PlatformEntry soundBanks, string bankName, string language, string eventName)
		{
			soundBanks[bankName][language].events.Add(eventName);
		}

		private static void RecordMediaFile(PlatformEntry soundBanks, string bankName, string id, string language)
		{
#if !WWISE_ADDRESSABLES_24_1_OR_LATER
			if (!soundBanks[bankName].ContainsKey(language))
			{
				AddSoundBank(bankName, language, ref soundBanks);
			}
#endif
			// Record that this bank "contains" this streamed media file
			soundBanks[bankName][language].streamedFileIds.Add(id);

			// Record that this streamed media file is "contained" in this bank
			if (!soundBanks.eventToSoundBankMap.ContainsKey(id))
			{
				soundBanks.eventToSoundBankMap[id] = new List<string>();
			}
			soundBanks.eventToSoundBankMap[id].Add(bankName);
		}
	}
}
#endif