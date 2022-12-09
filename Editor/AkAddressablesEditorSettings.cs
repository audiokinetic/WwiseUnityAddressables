#if UNITY_EDITOR
#if AK_WWISE_ADDRESSABLES && UNITY_ADDRESSABLES
using UnityEngine;
using System.Xml;

namespace AK.Wwise.Unity.WwiseAddressables
{
	[System.Serializable]
	public class AkAddressablesSettings
	{
		public const string Filename = "WwiseAddressablesSettings.xml";

		public static string Path
		{
			get { return System.IO.Path.Combine(UnityEngine.Application.dataPath, Filename); }
		}

		public static bool Exists { get { return System.IO.File.Exists(Path); } }

		public bool UseSampleMetadataPreserver;
		public string MetadataPath;


		internal static AkAddressablesSettings LoadSettings()
		{
			var settings = new AkAddressablesSettings();

			try
			{
				var path = Path;
				if (System.IO.File.Exists(path))
				{
					var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(AkAddressablesSettings));
					using (var xmlFileStream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
						settings = xmlSerializer.Deserialize(xmlFileStream) as AkAddressablesSettings;
				}
				else
				{
					var projectDir = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);
					var foundWwiseProjects = System.IO.Directory.GetFiles(projectDir, "*.wproj", System.IO.SearchOption.AllDirectories);

					//WG-61124 In order to avoid addressables not being updated when removing the streaming option, enable the RemoveUnusedGeneratedFiles feature
					foreach (var projects in foundWwiseProjects)
					{
						var doc = new System.Xml.XmlDocument();
						doc.Load(projects);
						var RemoveUnusedGeneratedFilesNode = doc.SelectSingleNode("//Property[@Name='RemoveUnusedGeneratedFiles']");
						if (RemoveUnusedGeneratedFilesNode != null)
                        {
							XmlAttribute valueAttribute = RemoveUnusedGeneratedFilesNode.Attributes["Value"];
							if (valueAttribute != null && valueAttribute.Value != null && valueAttribute.Value != "True")
							{
								valueAttribute.Value = "True";
								doc.Save(projects);
							}
						}

					}



					settings.MetadataPath = "WwiseAddressablesMetadata";
					settings.UseSampleMetadataPreserver = false;
				}
			}
			catch (System.Exception exception)
			{
				Debug.LogWarning("Could not load Wwise Addressables settings");
				Debug.LogWarning(exception);
			}

			if (string.IsNullOrEmpty(settings.MetadataPath))
			{
				settings.MetadataPath = "WwiseAddressablesMetadata";
			}
			return settings;
		}

		public void SaveSettings()
		{
			try
			{
				var xmlDoc = new System.Xml.XmlDocument();
				var xmlSerializer = new System.Xml.Serialization.XmlSerializer(GetType());
				using (var xmlStream = new System.IO.MemoryStream())
				{
					var streamWriter = new System.IO.StreamWriter(xmlStream, System.Text.Encoding.UTF8);
					xmlSerializer.Serialize(streamWriter, this);
					xmlStream.Position = 0;
					xmlDoc.Load(xmlStream);
					xmlDoc.Save(Path);
				}
			}
			catch
			{
				UnityEngine.Debug.LogErrorFormat("WwiseUnity: Unable to save addressables settings to file <{0}>. Please ensure that this file path can be written to.", Path);
			}
		}
	}

	public class AkAddressablesEditorSettings
	{
		private static AkAddressablesSettings s_Instance;

		public static AkAddressablesSettings Instance
		{
			get
			{
				if (s_Instance == null)
					s_Instance = AkAddressablesSettings.LoadSettings();
				return s_Instance;
			}
		}

		public static void Reload()
		{
			s_Instance = AkAddressablesSettings.LoadSettings();
		}

		#region GUI
		class SettingsProvider : UnityEditor.SettingsProvider
		{
			class Styles
			{
				public static string AddressablesSettings = "Asset metadata preservation";

				public static UnityEngine.GUIContent UseSampleMetadataPreserver = new UnityEngine.GUIContent("Use Sample Metadata Preserver", "Use the sample metadata preserver to preserve Wwise addressable asset groups and labels when they are deleted.");

				public static UnityEngine.GUIContent MetadataPath = new UnityEngine.GUIContent("Wwise Asset Metadata Path", "Location to create the assets that will contain the addressable asset metadata.");


				private static UnityEngine.GUIStyle textField;
				public static UnityEngine.GUIStyle TextField
				{
					get
					{
						if (textField == null)
							textField = new UnityEngine.GUIStyle("textfield");
						return textField;
					}
				}
			}

			private static bool Ellipsis()
			{
				return UnityEngine.GUILayout.Button("...", UnityEngine.GUILayout.Width(30));
			}

			private SettingsProvider(string path) : base(path, UnityEditor.SettingsScope.Project) { }

			[UnityEditor.SettingsProvider]
			public static UnityEditor.SettingsProvider CreateMyCustomSettingsProvider()
			{
				return new SettingsProvider("Project/Wwise Addressables") { keywords = GetSearchKeywordsFromGUIContentProperties<Styles>() };
			}

			public override void OnGUI(string searchContext)

			{
				bool changed = false;

				var labelWidth = UnityEditor.EditorGUIUtility.labelWidth;
				UnityEditor.EditorGUIUtility.labelWidth += 100;

				var settings = Instance;

				UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);
				UnityEngine.GUILayout.Label(Styles.AddressablesSettings, UnityEditor.EditorStyles.boldLabel);
				UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);

				UnityEditor.EditorGUI.BeginChangeCheck();

				using (new UnityEngine.GUILayout.VerticalScope("box"))
				{
					bool newValue = UnityEditor.EditorGUILayout.Toggle(Styles.UseSampleMetadataPreserver, settings.UseSampleMetadataPreserver);
					if (settings.UseSampleMetadataPreserver != newValue)
					{
						settings.UseSampleMetadataPreserver = newValue;
						if (settings.UseSampleMetadataPreserver)
						{
							WwiseAddressableAssetMetadataPreserver.BindMetadataDelegate();
						}
						else
						{
							WwiseAddressableAssetMetadataPreserver.UnbindMetaDataDelegate();
						}
					}
					UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);

					using (new UnityEngine.GUILayout.HorizontalScope())
					{


						if (settings.UseSampleMetadataPreserver)
						{
							UnityEditor.EditorGUILayout.PrefixLabel(Styles.MetadataPath);
							UnityEditor.EditorGUILayout.SelectableLabel(settings.MetadataPath, Styles.TextField, UnityEngine.GUILayout.Height(17));
							if (Ellipsis())
							{
								var OpenInPath = System.IO.Path.GetDirectoryName(AkUtilities.GetFullPath(UnityEngine.Application.dataPath, settings.MetadataPath));
								var MetadataPathNew = UnityEditor.EditorUtility.OpenFolderPanel("Select your metadata Project", OpenInPath, "WwiseAddressableMetadata");
								if (MetadataPathNew.Length != 0)
								{
									settings.MetadataPath = AkUtilities.MakeRelativePath(UnityEngine.Application.dataPath, MetadataPathNew);
									changed = true;
								}
							}
						}
					}
				}


				if (UnityEditor.EditorGUI.EndChangeCheck())
					changed = true;

				UnityEngine.GUILayout.Space(UnityEditor.EditorGUIUtility.standardVerticalSpacing);

				UnityEditor.EditorGUIUtility.labelWidth = labelWidth;

				if (changed)
					settings.SaveSettings();
			}
			#endregion
		}
	}
}
#endif //Addressables
#endif // UNITY_EDITOR
