using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Nomnom.ProjectWindowExtensions.Editor.Folder {
	internal class AssetLabelWindow : EditorWindow {
		private const BindingFlags FLAGS = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod;
		
		private static string[] _labels;
		
		private string _search;
		private string[] _results;
		private List<string> _items;
		private Vector2 _scroll;
		private Action<List<string>> _onClicked;

		public static void Open(Vector2 mousePos, Vector2 size, List<string> items, Action<List<string>> onClicked) {
			AssetLabelWindow window = CreateInstance<AssetLabelWindow>();
			mousePos.y -= size.y;
			Rect rect = new Rect(mousePos, size);
			window._items = items;
			window._onClicked = onClicked;
			window.ShowAsDropDown(rect, size);
		}

		private void OnGUI() {
			if (_labels == null) {
				_labels = ((Dictionary<string, float>) typeof(AssetDatabase).InvokeMember("GetAllLabels", FLAGS, null, null,
					null)).Keys.ToArray();
			}

			Rect bgRect = position;
			bgRect.x = bgRect.y = 0;
			EditorGUI.DrawRect(bgRect, new Color32(45, 45, 45, 255));
			Rect fgRect = bgRect;
			const int OFFSET = 3;
			fgRect.x += OFFSET;
			fgRect.y += OFFSET;
			fgRect.width -= OFFSET * 2;
			fgRect.height -= OFFSET * 2;
			EditorGUI.DrawRect(fgRect, new Color32(60, 60, 60, 255));
  				
			GUILayout.BeginArea(new Rect(OFFSET, OFFSET, position.size.x - OFFSET * 2, position.size.y - OFFSET * 2));

			EditorGUI.BeginChangeCheck();
			_search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
			if (EditorGUI.EndChangeCheck()) {
				if (string.IsNullOrEmpty(_search)) {
					_results = null;
				} else {
					_results = _labels.Where(type => type.Contains(_search)).ToArray();
				}
			}
			
			_scroll = EditorGUILayout.BeginScrollView(_scroll);
			{
				foreach (string result in _results != null && _results.Length > 0 ? _results : _labels) {
					GUI.backgroundColor = _items.Contains(result) ? Color.green : Color.white;
					if (GUILayout.Button(result, "toolbarbutton",
						GUILayout.Width(position.size.x - 20))) {
						if (_items.Contains(result)) {
							_items.Remove(result);
						} else {
							_items.Add(result);
						}
						
						_onClicked?.Invoke(_items);
						return;
					}
				}
			}
			EditorGUILayout.EndScrollView();
  
			GUILayout.EndArea();
		}
	}
}