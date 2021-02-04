using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.IO;


namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// A static class to access resources and style sheets used by this package.
    /// </summary>
    public static class EditorAidResources
    {
        /// <summary>
        /// Uss class that contains variables like theme colors to be applied to its hierarchy.
        /// It is added to rootElement when calling <see cref="ApplyCurrentTheme(VisualElement)"/>
        /// </summary>
        public static readonly string variablesContainerUssClassName = "editor-aid-variables";

        private static string s_FolderPath;
        private static StyleSheet s_DarkTheme;
        private static StyleSheet s_LightTheme;
        private static StyleSheet s_ListControlStyle;
        private static StyleSheet s_EditableLabelStyle;
        private static StyleSheet s_ListOfInspectorsStyle;

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
        /// Pass a custom root element to this method to use the appropiate USS variables for Unity's current skin.
        /// The relevant elements in this package already use this method, so you don't need to call it for them.
        /// </summary>
        /// <param name="rootElement">The custom root element that will contain the variables. </param>
        public static void ApplyCurrentTheme(VisualElement rootElement)
        {
            var style = EditorGUIUtility.isProSkin ? darkTheme : lightTheme;
            rootElement.styleSheets.Add(style);
            rootElement.AddToClassList(variablesContainerUssClassName);
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