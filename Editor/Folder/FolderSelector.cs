using System.Collections.Generic;
using UnityEditor.Presets;
using UnityEngine;

namespace Nomnom.ProjectWindowExtensions.Editor.Folder {
	[System.Serializable]
	internal class FolderSelector {
		public List<FolderFilter> Filters = new List<FolderFilter>();
		public Preset Preset;

		public void Assign(Object asset) {
			Preset.ApplyTo(asset);
		}
		
		public bool Find(string absoluteProjectPath, string absoluteAssetPath, string assetPath, FolderFilter.ProcessorHeading heading) {
			if (!Preset) {
				return false;
			}
			
			foreach (FolderFilter filter in Filters) {
				if (filter.Find(absoluteProjectPath, absoluteAssetPath, assetPath, heading)) {
					return true;
				}
			}
			
			return false;
		}
	}
}