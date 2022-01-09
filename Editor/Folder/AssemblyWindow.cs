using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nomnom.ProjectWindowExtensions.Editor.Folder {
	internal class AssemblyWindow : EditorWindow {
  			private static readonly List<string> _typeList = AppDomain.CurrentDomain.GetAssemblies()
	        .Select(asm => asm.FullName)
	        .ToList();
  
        private bool _firstPing;
  			private string _search;
  			private string[] _results;
  			private Vector2 _scroll;
  			private Action<string> _onClicked;
  
  			public static void Open(Vector2 mousePos, Vector2 size, Action<string> onClicked) {
	        AssemblyWindow window = CreateInstance<AssemblyWindow>();
  				mousePos.y -= size.y;
  				Rect rect = new Rect(mousePos, size);
  				window._onClicked = onClicked;
  				window.ShowAsDropDown(rect, size);
  			}
  
  			private void OnGUI() {
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
          GUI.SetNextControlName("search");
  				_search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
          
          if (!_firstPing) {
	          _firstPing = true;
				
	          GUI.FocusControl("search");
	          var te = (TextEditor) GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
	          te.cursorIndex = 1;
          }
          
  				if (EditorGUI.EndChangeCheck()) {
  					_results = _typeList.Where(type => type.Contains(_search)).ToArray();
  				}
  
  				if (_results == null) {
  					EditorGUILayout.LabelField("Input an assembly name to list assemblies", EditorStyles.miniBoldLabel);
  				} else {
  					_scroll = EditorGUILayout.BeginScrollView(_scroll);
  					{
  						foreach (string result in _results) {
  							if (GUILayout.Button(result.Substring(0, result.IndexOf(',')), "toolbarbutton",
  								GUILayout.Width(position.size.x - OFFSET * 2))) {
  								_onClicked?.Invoke(result);
  								Close();
  								return;
  							}
  						}
  					}
  					EditorGUILayout.EndScrollView();
  				}
  
  				GUILayout.EndArea();
  			}
  		}
}