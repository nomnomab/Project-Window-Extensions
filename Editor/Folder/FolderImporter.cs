using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nomnom.ProjectWindowExtensions.Editor.Folder {
	[CreateAssetMenu(menuName = "Nomnom/Folder Importer", fileName = nameof(FolderImporter), order = int.MaxValue - 1)]
	internal class FolderImporter: ScriptableObject {
		public string projectPath {
			get {
				string assetPath = AssetDatabase.GetAssetPath(this);
				return assetPath;
			}
		}
		
		public string absoluteProjectPath {
			get {
				string assetPath = AssetDatabase.GetAssetPath(this);
				return $"{Application.dataPath.Substring(0, Application.dataPath.Length - 6)}{assetPath}";
			}
		}
		
		public FolderSelector[] Selectors = Array.Empty<FolderSelector>();

		public bool HasSelector(string newAsset, string assetPath, FolderFilter.ProcessorHeading heading, out FolderSelector selector) {
			foreach (FolderSelector folderSelector in Selectors) {
				if (folderSelector.Find(absoluteProjectPath, newAsset, assetPath, heading)) {
					selector = folderSelector;
					return true;
				}
			}

			selector = null;
			return false;
		}

		internal class Processor : AssetPostprocessor {
			public string absoluteAssetPath => 
				$"{Application.dataPath.Substring(0, Application.dataPath.Length - 6)}{assetPath}".Replace('/', '\\');

			private static List<string> _doneAssets = new List<string>();
			private static List<string> _reimported = new List<string>();

			[InitializeOnLoadMethod]
			private static void OnLoad() {
				_reimported = new List<string>();
			}

			private void OnPreprocessAsset() {
				if (assetImporter.importSettingsMissing) {
					// load all folder import assets
					string[] uuids =
						AssetDatabase.FindAssets($"t:{typeof(FolderImporter).FullName}");

					if (uuids.Length == 0) {
						return;
					}

					FolderImporter[] folderImporters = uuids.Select(uuid =>
						AssetDatabase.LoadAssetAtPath<FolderImporter>(AssetDatabase.GUIDToAssetPath(uuid)))
						.ToArray();

					foreach (FolderImporter folder in folderImporters) {
						if (!folder.HasSelector(absoluteAssetPath, assetPath, FolderFilter.ProcessorHeading.Pre, out FolderSelector selector)) {
							continue;
						}

						selector.Assign(assetImporter);
						_doneAssets.Add(assetPath);
					}
				}
			}

			private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
				string[] movedFromAssetPaths) {
				// check
				string[] uuids =
					AssetDatabase.FindAssets($"t:{typeof(FolderImporter).FullName}");

				if (uuids.Length == 0) {
					return;
				}

				List<string> newAssets = new List<string>();
				
				FolderImporter[] folderImporters = uuids.Select(uuid =>
						AssetDatabase.LoadAssetAtPath<FolderImporter>(AssetDatabase.GUIDToAssetPath(uuid)))
					.ToArray();
				
				foreach (string importedAsset in importedAssets) {
					if (_doneAssets.Contains(importedAsset) || _reimported.Contains(importedAsset)) {
						continue;
					}

					string absolutePath = $"{Application.dataPath.Substring(0, Application.dataPath.Length - 6)}{importedAsset}".Replace('/', '\\');
					
					foreach (FolderImporter folder in folderImporters) {
						if (!folder.HasSelector(absolutePath, importedAsset, FolderFilter.ProcessorHeading.Post, out FolderSelector selector)) {
							continue;
						}

						// use
						// Object obj = AssetDatabase.LoadAssetAtPath<Object>(importedAsset);
						AssetImporter importer = AssetImporter.GetAtPath(importedAsset);
						selector.Preset.ApplyTo(importer);
						
						newAssets.Add(importedAsset);
					}
				}
				
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();

				if (_reimported.Count > 0) {
					EditorApplication.delayCall += () => {
						foreach (string importedAsset in importedAssets) {
							if (_reimported.Contains(importedAsset)) {
								_reimported.Remove(importedAsset);
								continue;
							}

							_reimported.Add(importedAsset);
							AssetDatabase.ImportAsset(importedAsset);
						}
					};
				}

				_doneAssets.Clear();
			}
		}
	}
}