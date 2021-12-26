using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nomnom.ProjectWindowExtensions.Editor {
	internal static class PreferenceWindow {
		private const string DEF_COPY_PASTE = "NOM_PROJECT_COPY_PASTE";
		private const string DEF_MORE_FILES = "NOM_PROJECT_MORE_FILES";

		private static ProjectWindowSettingsHandler.Settings _currentSettings;
		
		[SettingsProvider]
		public static SettingsProvider CreateProvider() {
			_currentSettings = ProjectWindowSettingsHandler.GetEditorSettings();
			
			var provider = new SettingsProvider("Preferences/Project Window Extensions", SettingsScope.User) {
				label = "Project Window Extensions",

				guiHandler = searchContext => {
					ProjectWindowGUI.Draw(_currentSettings);

					if (GUILayout.Button("Apply")) {
						ProjectWindowSettingsHandler.SetEditorSettings(_currentSettings);
						
						// update preprocessors
						string definesString =
							PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
						List<string> allDefines = definesString.Split(';').ToList();

						bool containsCopyPaste = allDefines.Contains(DEF_COPY_PASTE);
						bool containsMoreFiles = allDefines.Contains(DEF_MORE_FILES);
						
						if (containsCopyPaste && !_currentSettings.UseCopyPaste) {
							allDefines.Remove(DEF_COPY_PASTE);
						} else if (!containsCopyPaste && _currentSettings.UseCopyPaste) {
							allDefines.Add(DEF_COPY_PASTE);
						}
						
						if (containsMoreFiles && !_currentSettings.UseAdditionalFiles) {
							allDefines.Remove(DEF_MORE_FILES);
						} else if (!containsMoreFiles && _currentSettings.UseAdditionalFiles) {
							allDefines.Add(DEF_MORE_FILES);
						}
						
						PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", allDefines));
						AssetDatabase.Refresh();
					}
				},

				// Keywords for the search bar in the Unity Preferences menu
				keywords = new HashSet<string>(new[] {"Project", "Window", "Extensions"})
			};

			return provider;
		}
	}

	internal sealed class ProjectWindowSettingsHandler {
		private const string KEY_USE_COPY_PASTE = "nomnom.project-window-extensions.use-copy-paste";
		private const string KEY_USE_ADDITIONAL_FILES = "nomnom.project-window-extensions.use-additional-files";

		public static Settings GetEditorSettings() {
			return new Settings {
				UseCopyPaste = EditorPrefs.GetBool(KEY_USE_COPY_PASTE, true),
				UseAdditionalFiles = EditorPrefs.GetBool(KEY_USE_ADDITIONAL_FILES, true),
			};
		}

		public static void SetEditorSettings(Settings settings) {
			EditorPrefs.SetBool(KEY_USE_COPY_PASTE, settings.UseCopyPaste);
			EditorPrefs.SetBool(KEY_USE_ADDITIONAL_FILES, settings.UseAdditionalFiles);
		}

		internal sealed class Settings {
			public bool UseCopyPaste;
			public bool UseAdditionalFiles;
		}
	}

	internal static class ProjectWindowGUI {
		private static GUIContent _useCopyPasteText = new GUIContent("Use Copy/Paste");
		private static GUIContent _useAdditionalFilesText = new GUIContent("Use Additional Files");


		public static void Draw(ProjectWindowSettingsHandler.Settings settings) {
			EditorGUI.indentLevel++;
			settings.UseCopyPaste = EditorGUILayout.ToggleLeft(_useCopyPasteText, settings.UseCopyPaste);
			settings.UseAdditionalFiles = EditorGUILayout.ToggleLeft(_useAdditionalFilesText, settings.UseAdditionalFiles);
			EditorGUI.indentLevel--;
		}
	}
}