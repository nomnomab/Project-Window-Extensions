using System.Collections.Generic;
using System.Linq;
using Nomnom.EasierCustomPreferences.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Nomnom.ProjectWindowExtensions.Editor {
	[PreferencesName("Project Window Extensions", "Nomnom")]
	[PreferencesKeywords("Project", "Window", "Extensions")]
	internal static class PreferenceWindow {
		private const string DEF_COPY_PASTE = "NOM_PROJECT_COPY_PASTE";
		private const string DEF_MORE_FILES = "NOM_PROJECT_MORE_FILES";
		
		private const string KEY_USE_COPY_PASTE = "nomnom.project-window-extensions.use-copy-paste";
		private const string KEY_USE_ADDITIONAL_FILES = "nomnom.project-window-extensions.use-additional-files";

		private static GUIContent _useCopyPasteText = new GUIContent("Use Copy/Paste");
		private static GUIContent _useAdditionalFilesText = new GUIContent("Use Additional Files");

		private static ReorderableList _reorderableList;

		[SettingsProvider]
		public static SettingsProvider CreateProvider() => CustomPreferences.GetProvider(typeof(PreferenceWindow), false);

		public static Settings OnDeserialize() {
			Settings obj = new Settings {
				UseCopyPaste = EditorPrefs.GetBool(KEY_USE_COPY_PASTE, false),
				UseAdditionalFiles = EditorPrefs.GetBool(KEY_USE_ADDITIONAL_FILES, false),
			};

			return obj;
		}

		public static void OnSerialize(Settings settings) {
			EditorPrefs.SetBool(KEY_USE_COPY_PASTE, settings.UseCopyPaste);
			EditorPrefs.SetBool(KEY_USE_ADDITIONAL_FILES, settings.UseAdditionalFiles);
		}
		
		public static void OnGUI(string searchContext, Settings obj) {
			GUI.enabled = !EditorApplication.isCompiling;

			if (!GUI.enabled) {
				EditorGUILayout.HelpBox("The Editor is currently recompiling...", MessageType.Info);
			}
			
			EditorGUI.indentLevel++;
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.HelpBox("Shows context options for copying and pasting assets in \"IO/*\".", MessageType.Info);
			obj.UseCopyPaste = EditorGUILayout.ToggleLeft(_useCopyPasteText, obj.UseCopyPaste);
			EditorGUILayout.HelpBox("Shows context options for creating additional text file types in \"Assets/Create/Text Files/*\".", MessageType.Info);
			obj.UseAdditionalFiles = EditorGUILayout.ToggleLeft(_useAdditionalFilesText, obj.UseAdditionalFiles);

			if (EditorGUI.EndChangeCheck()) {
				OnSerialize(obj);
				UpdateProcessors(obj);
			}
			EditorGUI.indentLevel--;
		}

		private static void UpdateProcessors(Settings obj) {
			// update preprocessors
			string definesString =
				PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
			List<string> allDefines = definesString.Split(';').ToList();

			bool containsCopyPaste = allDefines.Contains(DEF_COPY_PASTE);
			bool containsMoreFiles = allDefines.Contains(DEF_MORE_FILES);
						
			if (containsCopyPaste && !obj.UseCopyPaste) {
				allDefines.Remove(DEF_COPY_PASTE);
			} else if (!containsCopyPaste && obj.UseCopyPaste) {
				allDefines.Add(DEF_COPY_PASTE);
			}
						
			if (containsMoreFiles && !obj.UseAdditionalFiles) {
				allDefines.Remove(DEF_MORE_FILES);
			} else if (!containsMoreFiles && obj.UseAdditionalFiles) {
				allDefines.Add(DEF_MORE_FILES);
			}
						
			PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", allDefines));
			AssetDatabase.Refresh();
		}
		
		internal sealed class Settings {
			public bool UseCopyPaste;
			public bool UseAdditionalFiles;
		}
	}
}