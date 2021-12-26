using UnityEditor;

namespace Nomnom.ProjectWindowExtensions.Editor {
	internal static class EditorExtensions {
		public static EditorWindow GetFocusedWindow(string window) {
			FocusOnWindow(window);
			return EditorWindow.focusedWindow;
		}

		public static void FocusOnWindow(string window) {
			EditorApplication.ExecuteMenuItem("Window/" + window);
		}
	}
}