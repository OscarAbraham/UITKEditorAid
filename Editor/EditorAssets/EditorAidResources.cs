using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.IO;


namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// A static class for accessing resources used by this package.
    /// </summary>
    public static class EditorAidResources
    {
        /// <summary> USS applied by <see cref="ApplyCurrentTheme(VisualElement)"/> to add style variables. </summary>
        public static readonly string variablesContainerUssClassName = "editor-aid-variables";

        /// <summary> USS class applied by <see cref="ApplyCurrentTheme(VisualElement)"/> when the Editor is in dark mode. </summary>
        public static readonly string darkModeContainerUssClassName = "editor-aid-dark-mode";

        /// <summary> USS class applied by <see cref="ApplyCurrentTheme(VisualElement)"/> when the Editor is in light mode. </summary>
        public static readonly string lightModeContainerUssClassName = "editor-aid-light-mode";

        private static string s_FolderPath;
        private static StyleSheet s_DarkTheme;
        private static StyleSheet s_LightTheme;
        private static StyleSheet s_ListControlStyle;
        private static StyleSheet s_EditableLabelStyle;
        private static StyleSheet s_ListOfInspectorsStyle;
        private static StyleSheet s_TabbedViewStyle;

        /// <summary> StyleSheet for <see cref="ListControl"/> </summary>
        public static StyleSheet listControlStyle
        {
            get
            {
                if (!s_ListControlStyle)
                    s_ListControlStyle = GetAsset<StyleSheet>("ListControlStyle.uss");
                return s_ListControlStyle;
            }
        }

        /// <summary> StyleSheet for <see cref="EditableLabel"/> </summary>
        public static StyleSheet editableLabelStyle
        {
            get
            {
                if (!s_EditableLabelStyle)
                    s_EditableLabelStyle = GetAsset<StyleSheet>("EditableLabelStyle.uss");
                return s_EditableLabelStyle;
            }
        }

        /// <summary> StyleSheet for <see cref="ListOfInspectors"/> </summary>
        public static StyleSheet listOfInspectorsStyle
        {
            get
            {
                if (!s_ListOfInspectorsStyle)
                    s_ListOfInspectorsStyle = GetAsset<StyleSheet>("ListOfInspectorsStyle.uss");
                return s_ListOfInspectorsStyle;
            }
        }

        /// <summary> StyleSheet for <see cref="TabbedView"/> </summary>
        public static StyleSheet tabbedViewStyle
        {
            get
            {
                if (!s_TabbedViewStyle)
                    s_TabbedViewStyle = GetAsset<StyleSheet>("TabbedViewStyle.uss");
                return s_TabbedViewStyle;
            }
        }

        private static StyleSheet darkTheme
        {
            get
            {
                if (!s_DarkTheme)
                    s_DarkTheme = GetAsset<StyleSheet>("DarkThemeVariables.uss");
                return s_DarkTheme;
            }
        }

        private static StyleSheet lightTheme
        {
            get
            {
                if (!s_LightTheme)
                    s_LightTheme = GetAsset<StyleSheet>("LightThemeVariables.uss");
                return s_LightTheme;
            }
        }

        // This is the most practical way I found to not depend on specific file paths or GUIDs which change when assets are
        // duplicated. It makes it easier for the assets to exist multiple times in different places in the same project.
        private static string folderPath
        {
            get
            {
                if (s_FolderPath == null)
                {
                    s_FolderPath = EditorAssetsFolderFinder.GetFolderPath();
                }
                return s_FolderPath;
            }
        }

        /// <summary>
        /// Pass a custom root element to this method to use the appropiate USS variables and class names for Unity's current skin.
        /// </summary>
        /// <param name="rootElement">The custom root element that will contain the variables. </param>
        public static void ApplyCurrentTheme(VisualElement rootElement)
        {
            bool isDark = EditorGUIUtility.isProSkin;

            var style = isDark ? darkTheme : lightTheme;
            rootElement.styleSheets.Add(style);
            rootElement.AddToClassList(variablesContainerUssClassName);

            rootElement.EnableInClassList(darkModeContainerUssClassName, isDark);
            rootElement.EnableInClassList(lightModeContainerUssClassName, !isDark);
        }

        private static T GetAsset<T>(string relativePath) where T : Object
        {
            string path = Path.Combine(folderPath, relativePath);

            if (Path.DirectorySeparatorChar != '/')
                path = path.Replace(Path.DirectorySeparatorChar, '/');

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}