#if NOM_PROJECT_MORE_FILES
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nomnom.ProjectWindowExtensions.Editor {
	internal static class CreateAdditionalTextFiles {
		private const string ROOT_FOLDER = "Packages/com.nomnom.project-window-extensions/Editor/TextFilePresets/";

		private static string _lastAssetPath;
		private static double _renameTime;
		private static bool _enableRename;

		[MenuItem("Assets/Create/Text Files/Text File", false, 81)]
		private static void CreateTXT() {
			string path = CollectPath("NewFile.txt");
			string text = LoadPreset("TxtPreset");
			CreateAsset(text, path);
		}

		[MenuItem("Assets/Create/Text Files/JSON File", false, 81)]
		private static void CreateJSON() {
			string path = CollectPath("NewFile.json");
			string text = LoadPreset("JsonPreset");
			CreateAsset(text, path);
		}

		[MenuItem("Assets/Create/Text Files/XML File", false, 81)]
		private static void CreateXML() {
			string path = CollectPath("NewFile.xml");
			string text = LoadPreset("XmlPreset");
			CreateAsset(text, path);
		}

		[MenuItem("Assets/Create/Text Files/CSV File", false, 81)]
		private static void CreateCSV() {
			string path = CollectPath("NewFile.csv");
			string text = LoadPreset("CSVPreset");
			CreateAsset(text, path);
		}

		private static string CollectPath(string fileName) {
			if (!Selection.activeObject) {
				return $"Assets/{fileName}";
			}

			return $"{AssetDatabase.GetAssetPath(Selection.activeObject)}/{fileName}";
		}

		private static string LoadPreset(string preset) {
			return AssetDatabase.LoadAssetAtPath<TextAsset>($"{ROOT_FOLDER}{preset}.txt").text;
		}

		private static void CreateAsset(string content, string path) {
			string absolutePath = $"{Application.dataPath}{path.Substring(6)}";
			using (StreamWriter streamWriter = new StreamWriter(absolutePath)) {
				streamWriter.Write(content);
			}

			_lastAssetPath = path;

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			_renameTime = EditorApplication.timeSinceStartup + 0.2d;
			_enableRename = true;
			EditorApplication.update += EngageRenameMode;
		}

		private static void EngageRenameMode() {
			if (EditorApplication.timeSinceStartup >= _renameTime) {
				if (!_enableRename) {
					EditorApplication.update -= EngageRenameMode;
					EditorExtensions.GetFocusedWindow("General/Project").SendEvent(new Event {
						keyCode = KeyCode.F2,
						type = EventType.KeyDown
					});
					return;
				}

				Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(_lastAssetPath);
				_enableRename = false;
				_renameTime = EditorApplication.timeSinceStartup + 0.2d;
			}
		}
	}
}
#endif