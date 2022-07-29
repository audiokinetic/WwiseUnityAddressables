#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES && UNITY_EDITOR

using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

		public static void ParseAssetPath(string assetPath, out string platform, out string language)
		{
			platform = string.Empty;
			language = "SFX";

			var banksPath = $"{GetSoundbanksPath()}/";
			var assetRelPath = assetPath.Replace(banksPath, "");

			string[] parts = assetRelPath.Split('/');
			platform = parts[0];

			if (parts.Length > 2)
			{
				// Asset is stored in a sub-folder; we must identify the purpose of the sub-folder.
				if (parts[1] == "Media")
				{
					// Starting with Wwise 2022.1, loose media files are stored in a sub-directory named "Media".
					// These themselves can be in localized sub-folders.
					if (parts.Length > 3)
					{
						// The sub-sub folder name is the locale string
						language = parts[2];
					}
				}
				else if (parts[1] == "Bus" || parts[1] == "Media")
				{
					// Starting with 2022.1, there are auto-generated banks placed in these special sub-directories.
					UnityEngine.Debug.LogError("Wwise Unity Addressables: Auto Defined SoundBanks are not yet supported. Please turn off the option in Wwise under Project Settings -> SoundBanks");
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

		public static void ClearSoundbankInfo()
		{
			soundbanksInfo.Clear();
		}

		public static void AddSoundBank(string bankName, string bankLanguage, ref PlatformEntry soundBankDict)
		{
			if (!soundBankDict.ContainsKey(bankName))
			{
				soundBankDict.Add(bankName, new SoundBankEntry());
			}
			soundBankDict[bankName][bankLanguage] = new SoundBankInfo();
		}

		//Parse soundbank xml file to get a dict of the streaming wem files
		public static PlatformEntry ParsePlatformSoundbanksXML(string platformName, string newBankName)
		{
			if (platformName == null)
			{
				platformName = AkBasePathGetter.GetPlatformName();
			}

			if (!AkBasePathGetter.GetSoundBankPaths(platformName, out string sourceFolder, out string destinationFolder))
			{
				Debug.LogError($"Could not find containing folder for {newBankName} soundbank - platform: {platformName}. Check the Generated Soundbanks Path in the Unity Wwise project settings.");
				return null;
			}

			var xmlFilename = Path.Combine(sourceFolder, "SoundbanksInfo.xml");
			if (!File.Exists(xmlFilename))
			{
				Debug.LogError($"Could not find SoundbanksInfo for {platformName} platform. Check the Generated Soundbanks Path in the Unity Wwise project settings.");
				return null;
			}
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
				doc.Load(xmlFilename);

				XmlElement root = doc.DocumentElement;
				if (!Int32.TryParse(root.GetAttribute("SchemaVersion"), out int schemaVersion))
				{
					Debug.LogError($"Could not parse SoundBanksInfo.xml for {platformName}. Check {xmlFilename} for possible corruption.");
					return null;
				}

				PlatformEntry soundBanks;
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
							if (fileNode.Attributes["Streaming"].Value == "true")
							{
								RecordStreamedFile(
									soundBanks,
									bankName,
									fileNode.Attributes["Id"].Value,
									fileNode.Attributes["Language"].Value);
							}
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
										RecordStreamedFile(
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

		public static void FindAndSetBankReference(WwiseAddressableSoundBank addressableBankAsset, string name)
		{
			System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

			var assembly = Assembly.Load("AK.Wwise.Unity.API.WwiseTypes");
			var AkBankReferenceType = assembly.GetType("WwiseBankReference");
			bool success = (bool)AkBankReferenceType.GetMethod("FindBankReferenceAndSetAddressableBank").Invoke(null, new object[] { addressableBankAsset, name });
		}

		private static void RecordEvent(PlatformEntry soundBanks, string bankName, string language, string eventName)
		{
			soundBanks[bankName][language].events.Add(eventName);
		}

		private static void RecordStreamedFile(PlatformEntry soundBanks, string bankName, string id, string language)
		{
			if (!soundBanks[bankName].ContainsKey(language))
			{
				AddSoundBank(bankName, language, ref soundBanks);
			}

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