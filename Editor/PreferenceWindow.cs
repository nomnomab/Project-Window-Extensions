using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nomnom.EasierCustomPreferences.Editor;
using Nomnom.ProjectWindowExtensions.Editor.Folder;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nomnom.ProjectWindowExtensions.Editor {
	[PreferencesName("Project Window Extensions", "Nomnom")]
	[PreferencesKeywords("Project", "Window", "Extensions")]
	internal static class PreferenceWindow {
		public static List<Assembly> FolderImportAssemblies { get; private set; }

		private const string DEF_COPY_PASTE = "NOM_PROJECT_COPY_PASTE";
		private const string DEF_MORE_FILES = "NOM_PROJECT_MORE_FILES";
		
		private const string KEY_USE_COPY_PASTE = "nomnom.project-window-extensions.use-copy-paste";
		private const string KEY_USE_ADDITIONAL_FILES = "nomnom.project-window-extensions.use-additional-files";
		private const string KEY_FOLDER_IMPORT_ASM = "nomnom.project-window-extensions.folder-import-asm";
		
		private static GUIContent _useCopyPasteText = new GUIContent("Use Copy/Paste");
		private static GUIContent _useAdditionalFilesText = new GUIContent("Use Additional Files");
		private static GUIContent _folderImportAssemblies = new GUIContent("Folder Import Assemblies");

		private static ReorderableList _reorderableList;

		[SettingsProvider]
		public static SettingsProvider CreateProvider() => CustomPreferences.GetProvider(typeof(PreferenceWindow), false);

		[InitializeOnLoadMethod]
		private static void OnLoad() {
			Settings settings = OnDeserialize();
			FolderImportAssemblies = ((List<string>)settings.Assemblies.list)
				.Select(Assembly.Load)
				.ToList();
		}

		public static Settings OnDeserialize() {
			string asmStr = EditorPrefs.GetString(KEY_FOLDER_IMPORT_ASM, null);
			List<string> list = string.IsNullOrEmpty(asmStr) 
				? new List<string> { typeof(Object).Assembly.FullName } 
				: JsonUtility.FromJson<ListWrapper>(asmStr).List;

			ReorderableList reorderList = new ReorderableList(list, typeof(string), true, true, true, true);

			Settings obj = new Settings {
				UseCopyPaste = EditorPrefs.GetBool(KEY_USE_COPY_PASTE, false),
				UseAdditionalFiles = EditorPrefs.GetBool(KEY_USE_ADDITIONAL_FILES, false),
				Assemblies = reorderList
			};

			reorderList.onAddCallback = reorderableList => {
				reorderableList.list.Add(typeof(Object).Assembly.FullName);
			};
			reorderList.drawHeaderCallback = r => {
				r.x -= 16;
				EditorGUI.LabelField(r, "Folder Importer Assemblies");
			};
			reorderList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				rect.y += 2f;
				rect.height = EditorGUIUtility.singleLineHeight;

				string val = (string) reorderList.list[index];
				
				if (GUI.Button(rect, val.Substring(0, val.IndexOf(',')), EditorStyles.toolbarDropDown)) {
					Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
					AssemblyWindow.Open(mousePos, new Vector2(400, 300), str => {
						reorderList.list[index] = str;
						OnSerialize(obj);
					});
				}
			};

			return obj;
		}

		public static void OnSerialize(Settings settings) {
			List<string> assemblies = new List<string>(settings.Assemblies.count);
			for (int i = 0; i < settings.Assemblies.count; i++) {
				assemblies.Add((string)settings.Assemblies.list[i]);
			}

			EditorPrefs.SetBool(KEY_USE_COPY_PASTE, settings.UseCopyPaste);
			EditorPrefs.SetBool(KEY_USE_ADDITIONAL_FILES, settings.UseAdditionalFiles);
			EditorPrefs.SetString(KEY_FOLDER_IMPORT_ASM, JsonUtility.ToJson(new ListWrapper { List = assemblies }));
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
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(18);
			obj.Assemblies.DoLayoutList();
			GUILayout.Space(3);
			EditorGUILayout.EndHorizontal();

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
			public ReorderableList Assemblies;
		}

		internal class ListWrapper {
			public List<string> List;
		}
	}
}