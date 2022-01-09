using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nomnom.ProjectWindowExtensions.Editor.Folder {
	[CustomPropertyDrawer(typeof(FolderFilter))]
	internal class CustomGUI : PropertyDrawer {
		private const string NOT_SAFE_TOOLTIP = "This item exists in a higher folder. As such, this item will be ignored for this folder.";
		
		private static readonly Dictionary<FolderFilter, bool> _filters = new Dictionary<FolderFilter, bool>();
		private static string[] _allFolderImporters;

		[InitializeOnLoadMethod]
		private static void OnLoad() {
			_allFolderImporters = AssetDatabase.FindAssets($"t:{typeof(FolderImporter).FullName}")
				.Select(AssetDatabase.GUIDToAssetPath)
				.ToArray();
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			FolderFilter filter = (FolderFilter)GetTargetObjectOfProperty(property);

			if (!_filters.ContainsKey(filter)) {
				_filters[filter] = ValidateInfo(property);
			}
			
			const int WIDTH = 90;
			bool isSafe = _filters[filter];

			if (!isSafe) {
				EditorGUI.DrawRect(new Rect(
					position.position.x - 28, position.position.y,
					position.size.x + 28, EditorGUIUtility.singleLineHeight), Color.red * 0.3f);
			}

			Rect lhs = position;
			lhs.width = WIDTH;
			Rect rhs = position;
			rhs.width -= WIDTH + (!isSafe ? 18 : 0);
			rhs.x += WIDTH;
			Rect conflict = rhs;
			conflict.width = 18;
			conflict.x += rhs.width + 2;

			SerializedProperty typeProp = property.FindPropertyRelative("Type");
			SerializedProperty selectorProp = property.FindPropertyRelative("Selector");
			SerializedProperty objectTypeProp = property.FindPropertyRelative("ObjectType");
			// SerializedProperty assetLabelsProp = property.FindPropertyRelative("AssetLabels");

			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField(lhs, typeProp, GUIContent.none);

			FilterType type = (FilterType) typeProp.enumValueIndex;
			switch (type) {
				case FilterType.Extension:
					EditorGUI.PropertyField(rhs, selectorProp, GUIContent.none);
					break;
				case FilterType.Type:
					if (string.IsNullOrEmpty(objectTypeProp.stringValue)) {
						objectTypeProp.stringValue = typeof(Object).AssemblyQualifiedName;
						objectTypeProp.serializedObject.ApplyModifiedProperties();
					}
					
					if (GUI.Button(rhs, objectTypeProp.stringValue.Substring(0, objectTypeProp.stringValue.IndexOf(',')),
						EditorStyles.toolbarDropDown)) {
						Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
						TypeWindow.Open(mousePos, new Vector2(400, 300), str => {
							objectTypeProp.stringValue = str;
							serialize();
						});
					}

					break;
				// case FilterType.AssetLabel:
				// 	_labelCache.Clear();
				// 	
				// 	for (int i = 0; i < assetLabelsProp.arraySize; i++) {
				// 		_labelCache.Add(assetLabelsProp.GetArrayElementAtIndex(i).stringValue);
				// 	}
				// 	
				// 	if (GUI.Button(rhs,  _labelCache.Count == 0 ? "Nothing selected" : string.Join(", ", _labelCache), EditorStyles.toolbarDropDown)) {
				// 		Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
				//
				// 		AssetLabelWindow.Open(mousePos, new Vector2(400, 300), _labelCache, list => {
				// 			assetLabelsProp.ClearArray();
				//
				// 			for (int i = 0; i < list.Count; i++) {
				// 				assetLabelsProp.InsertArrayElementAtIndex(i);
				// 				assetLabelsProp.GetArrayElementAtIndex(i).stringValue = list[i];
				// 			}
				//
				// 			serialize();
				// 		});
				// 	}
				// 	break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (!isSafe) {
				GUI.color = Color.red;
				GUI.Label(conflict, new GUIContent(" !", NOT_SAFE_TOOLTIP));
				GUI.color = Color.white;
			}

			if (EditorGUI.EndChangeCheck()) {
				// check higher SOs
				_filters[filter] = ValidateInfo(property);

				serialize();
			}

			void serialize() {
				_filters[filter] = ValidateInfo(property);
				
				typeProp.serializedObject.ApplyModifiedProperties();
				selectorProp.serializedObject.ApplyModifiedProperties();
				property.serializedObject.ApplyModifiedProperties();
			}
		}

		private bool ValidateInfo(SerializedProperty property) {
			SerializedProperty typeProp = property.FindPropertyRelative("Type");
			SerializedProperty selectorProp = property.FindPropertyRelative("Selector");
			SerializedProperty objectTypeProp = property.FindPropertyRelative("ObjectType");
			// SerializedProperty assetLabelsProp = property.FindPropertyRelative("AssetLabels");
			
			string ownerPath = AssetDatabase.GetAssetPath(property.serializedObject.targetObject);
			string ownerRelativePath = Path.GetDirectoryName(ownerPath);
			FilterType type = (FilterType) typeProp.enumValueIndex;

			FolderImporter[] allImports = _allFolderImporters
				.Where(path => path != ownerPath && ownerRelativePath.Contains(Path.GetDirectoryName(path)) || string.IsNullOrEmpty(path))
				.Select(AssetDatabase.LoadAssetAtPath<FolderImporter>)
				.ToArray();
			
			_filters.Clear();

			foreach (FolderImporter folderImporter in allImports) {
				if (!folderImporter) {
					OnLoad();
					return ValidateInfo(property);
				}
				
				foreach (FolderSelector selector in folderImporter.Selectors) {
					foreach (FolderFilter filter in selector.Filters) {
						if (type == filter.Type) {
							switch (type) {
								case FilterType.Extension:
									if (filter.Selector == selectorProp.stringValue) {
										return false;
									}
									break;
								case FilterType.Type:
									if (filter.ObjectType == objectTypeProp.stringValue) {
										return false;
									}
									break;
								// case FilterType.AssetLabel:
								// 	foreach (string filterAssetLabel in filter.AssetLabels) {
								// 		for (int i = 0; i < assetLabelsProp.arraySize; i++) {
								// 			if (filterAssetLabel == assetLabelsProp.GetArrayElementAtIndex(i).stringValue) {
								// 				return false;
								// 			}
								// 		}
								// 	}
								// 	break;
							}
						}
					}
				}
			}

			return true;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			return EditorGUIUtility.singleLineHeight;
		}
		
		/// <summary>
    /// Gets the object the property represents.
    /// https://forum.unity.com/threads/get-a-general-object-value-from-serializedproperty.327098/
    /// </summary>
		public static object GetTargetObjectOfProperty(SerializedProperty prop)
    {
        var path = prop.propertyPath.Replace(".Array.data[", "[");
        object obj = prop.serializedObject.targetObject;
        var elements = path.Split('.');
        foreach (var element in elements)
        {
            if (element.Contains("["))
            {
                var elementName = element.Substring(0, element.IndexOf("["));
                var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                obj = GetValue_Imp(obj, elementName, index);
            }
            else
            {
                obj = GetValue_Imp(obj, element);
            }
        }
        return obj;
    }
 
    private static object GetValue_Imp(object source, string name)
    {
        if (source == null)
            return null;
        var type = source.GetType();
 
        while (type != null)
        {
            var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (f != null)
                return f.GetValue(source);
 
            var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (p != null)
                return p.GetValue(source, null);
 
            type = type.BaseType;
        }
        return null;
    }

    private static object GetValue_Imp(object source, string name, int index) {
	    var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
	    if (enumerable == null) return null;
	    var enm = enumerable.GetEnumerator();
	    //while (index-- >= 0)
	    //    enm.MoveNext();
	    //return enm.Current;

	    for (int i = 0; i <= index; i++) {
		    if (!enm.MoveNext()) return null;
	    }

	    return enm.Current;
    }
	}
}