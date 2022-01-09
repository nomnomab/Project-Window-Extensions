using System;
using System.IO;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Nomnom.ProjectWindowExtensions.Editor.Folder {
	internal enum FilterType {
		Extension,
		Type,
		//AssetLabel
	}
	
	[Serializable]
	internal class FolderFilter {
		public FilterType Type;
		public string Selector;
		public string ObjectType;
		// public List<string> AssetLabels = new List<string>();

		public bool Find(string absoluteProjectPath, string absoluteAssetPath, string assetPath, ProcessorHeading heading) {
			return Type switch {
				FilterType.Extension => ExtensionFilter.Find(absoluteProjectPath, absoluteAssetPath, assetPath, Selector, heading),
				FilterType.Type => TypeFilter.Find(absoluteProjectPath, absoluteAssetPath, assetPath, ObjectType, heading),
				//FilterType.AssetLabel => AssetLabelFilter.Find(absoluteProjectPath, absoluteAssetPath, assetPath, AssetLabels, heading),
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		internal enum ProcessorHeading {
			Pre,
			Post
		}
	}
	
	internal static class ExtensionFilter {
		public static bool Find(string absoluteProjectPath, string absoluteAssetPath, string assetPath, string filter, FolderFilter.ProcessorHeading heading) {
			if (heading != FolderFilter.ProcessorHeading.Pre) {
				return false;
			}
			
			filter = $"*.{filter}";
			string[] find = Directory.GetFiles(Path.GetDirectoryName(absoluteProjectPath), filter, SearchOption.AllDirectories);

			foreach (string s in find) {
				if (s == absoluteAssetPath) {
					return true;
				}
			}

			return false;
		}
	}
	
	internal static class TypeFilter {
		public static bool Find(string absoluteProjectPath, string absoluteAssetPath, string assetPath, string filter, FolderFilter.ProcessorHeading heading) {
			if (heading != FolderFilter.ProcessorHeading.Post) {
				return false;
			}

			Type filterType = Type.GetType(filter);

			if (filterType == null || filterType == typeof(Object)) {
				return false;
			}
			
			Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
			bool validAsset = filterType.IsInstanceOfType(asset);

			return validAsset;
		}
	}

	// internal static class AssetLabelFilter {
	// 	public static bool Find(string absoluteProjectPath, string absoluteAssetPath, string assetPath, List<string> filter,
	// 		FolderFilter.ProcessorHeading heading) {
	// 		if (heading != FolderFilter.ProcessorHeading.Post) {
	// 			return false;
	// 		}
	//
	// 		Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
	// 		string[] labels = AssetDatabase.GetLabels(asset);
	//
	// 		foreach (string label in labels) {
	// 			foreach (string s in filter) {
	// 				if (label == s) {
	// 					return true;
	// 				}
	// 			}
	// 		}
	//
	// 		return false;
	// 	}
}