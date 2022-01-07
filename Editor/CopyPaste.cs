#if NOM_PROJECT_COPY_PASTE
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nomnom.ProjectWindowExtensions.Editor {
	internal static class CopyPaste {
		private const int PRIORITY = 0;
		
		private static Dictionary<string, List<AssetItem>> _tmpBuffer;
		private static Dictionary<string, List<AssetItem>> _copyBuffer;
		private static Dictionary<string, List<AssetItem>> _cutBuffer;
		private static List<string> _selectedThings;

		static CopyPaste() {
			EditorApplication.projectWindowItemOnGUI += OnProjectGUI;
		}

		[MenuItem("Assets/IO/Copy %c", false, PRIORITY)]
		private static void DoCopy() {
			// store all assets into a buffer
			// copy tmp buffer to copy buffer
			_copyBuffer = new Dictionary<string, List<AssetItem>>(_tmpBuffer);
			_cutBuffer = null;

			// now in copy state
		}
		
		[MenuItem("Assets/IO/Copy %c", true)]
		private static bool DoCopyValidate() {
			return DefaultValidation();
		}
		
		[MenuItem("Assets/IO/Paste %v", false, PRIORITY)]
		private static void DoPaste() {
			// get new folder
			var activeObject = Selection.activeObject;
			string objPath = AssetDatabase.GetAssetPath(activeObject).Replace('/', '\\');
			string objFolder = Path.HasExtension(objPath) ? Path.GetDirectoryName(objPath) : objPath;

			// go through copy buffer and copy files
			
			// check if we are only dealing with files
			bool inCopyMode = _copyBuffer != null && _copyBuffer.Count > 0;
			var buffer = inCopyMode ? _copyBuffer : _cutBuffer;
			if (buffer.FirstOrDefault(parent => parent.Value.FirstOrDefault(folder => folder.IsFolder) != null).Value == null) {
				foreach (var pair in buffer) {
					foreach (AssetItem assetItem in pair.Value) {
						if (inCopyMode) {
							AssetDatabase.CopyAsset(assetItem.Path, $"{objFolder}\\{Path.GetFileName(assetItem.Path)}");
						} else {
							AssetDatabase.MoveAsset(assetItem.Path, $"{objFolder}\\{Path.GetFileName(assetItem.Path)}");
						}
					}
				}

				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				
				buffer.Clear();
				_tmpBuffer.Clear();
				return;
			}
			
			// dealing with folders too
			foreach (var pair in buffer) {
				foreach (AssetItem assetItem in pair.Value) {
					if (inCopyMode) {
						AssetDatabase.CopyAsset(assetItem.Path, $"{objFolder}\\{Path.GetFileName(assetItem.Path)}");
					} else {
						AssetDatabase.MoveAsset(assetItem.Path, $"{objFolder}\\{Path.GetFileName(assetItem.Path)}");
					}
				}
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			
			buffer.Clear();
			_tmpBuffer.Clear();
		}
		
		[MenuItem("Assets/IO/Paste %v", true)]
		private static bool DoPasteValidate() {
			return DefaultValidation(false) && (_copyBuffer != null && _copyBuffer.Count > 0) || (_cutBuffer != null && _cutBuffer.Count > 0);
		}
		
		[MenuItem("Assets/IO/Cut %x", false, PRIORITY)]
		private static void DoCut() {
			// store all assets into a buffer
			// copy tmp buffer to cut buffer
			_copyBuffer = null;
			_cutBuffer = new Dictionary<string, List<AssetItem>>(_tmpBuffer);
			
			// now in cut state
		}
		
		[MenuItem("Assets/IO/Cut %x", true)]
		private static bool DoCutValidate() {
			return DefaultValidation();
		}

		[MenuItem("Assets/IO/Copy - Cancel %#c", false, PRIORITY)]
		private static void DoCopyCancel() {
			_tmpBuffer.Clear();
			_copyBuffer.Clear();
		}

		[MenuItem("Assets/IO/Copy - Cancel %#c", true)]
		private static bool DoCopyCancelValidate() {
			return DefaultValidation() && _copyBuffer != null && _copyBuffer.Count > 0;
		}
		
		[MenuItem("Assets/IO/Cut - Cancel %#x", false, PRIORITY)]
		private static void DoCutCancel() {
			_tmpBuffer.Clear();
			_cutBuffer.Clear();
		}
		
		[MenuItem("Assets/IO/Cut - Cancel %#x", true)]
		private static bool DoCutCancelValidate() {
			return DefaultValidation() && _cutBuffer != null && _cutBuffer.Count > 0;
		}

		private static bool DefaultValidation(bool restrictAssets = true) {
			return ValidateNotRestrictedPath(restrictAssets) && CollectAssetItems(ref _tmpBuffer);
		}
		
		private static bool ValidateNotRestrictedPath(bool restrictAssets = true) {
			string path = AssetDatabase.GetAssetPath(Selection.activeObject);

			if (restrictAssets && path.Equals("Assets")) {
				return false;
			}

			return !string.IsNullOrEmpty(path) && !path.StartsWith("Packages");
		}

		private static void OnProjectGUI(string guid, Rect selectionRect) {
			bool inCopyMode = _copyBuffer != null && _copyBuffer.Count > 0;
			bool inCutMode = _cutBuffer != null && _cutBuffer.Count > 0;

			if (_selectedThings == null || _selectedThings.Count == 0) {
				return;
			}

			if (_selectedThings.Contains(guid)) {
				if (inCopyMode) {
					EditorGUI.DrawRect(selectionRect, new Color(1, 0.92f, 0.016f, 0.3f));
				} else if (inCutMode) {
					EditorGUI.DrawRect(selectionRect, new Color(1, 0, 0, 0.3f));
				}
			}
		}

		private static bool CollectAssetItems(ref Dictionary<string, List<AssetItem>> outputItems) {
			_tmpBuffer ??= new Dictionary<string, List<AssetItem>>();
			_tmpBuffer.Clear();

			_selectedThings ??= new List<string>();
			_selectedThings.Clear();
			
			var objects = Selection.objects;
			outputItems = new Dictionary<string, List<AssetItem>>();
			bool hasFolderSelected = false;
			
			foreach (Object o in objects) {
				if (!AssetDatabase.IsMainAsset(o)) {
					continue;
				}
				
				string path = AssetDatabase.GetAssetPath(o).Replace('/', '\\');
				string[] parentSplit = path.Split('\\');
				string parent = Path.GetDirectoryName(path);

				if (string.IsNullOrEmpty(parent)) {
					continue;
				}
				
				AssetItem item = new AssetItem(path);

				if (item.IsFolder) {
					hasFolderSelected = true;
				}
				
				_selectedThings.Add(AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(o)).ToString());

				string currentParentPath = string.Empty;
				for (int i = 0; i < parentSplit.Length - 1; i++) {
					string s = parentSplit[i];
					currentParentPath += $"{s}";
					if (!outputItems.TryGetValue(currentParentPath, out List<AssetItem> items)) {
						items = new List<AssetItem>();
						outputItems[currentParentPath] = items;
					}

					currentParentPath += "\\";
				}

				outputItems[parent].Add(item);
			}

			if (hasFolderSelected) {
				string wantedParent = null;
				foreach (var pair in outputItems) {
					foreach (AssetItem assetItem in pair.Value) {
						string parent = Path.GetDirectoryName(assetItem.Path);
						
						if (assetItem.IsFolder) {
							if (string.IsNullOrEmpty(wantedParent)) {
								wantedParent = parent;
								continue;
							}
						}
						
						if (parent != wantedParent) {
							outputItems.Clear();
							return false;
						}
					}
				}
			}

			return true;
		}

		private class AssetItem {
			public string Path;
			public bool IsFolder;

			public AssetItem(string path) {
				Path = path;
				IsFolder = AssetDatabase.IsValidFolder(path);
			}
		}
	}
}
#endif