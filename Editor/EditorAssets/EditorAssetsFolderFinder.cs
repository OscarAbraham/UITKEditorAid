using UnityEditor;
using UnityEngine;
using System.IO;

namespace ArteHacker.UITKEditorAid
{
    // This is needed because this package could exist multiple times in the same project. That means
    // that there could be multiple copies of the same assets, which in turn means that common ways of
    // getting assets are not a good fit for us:
    //
    // - We can't reference them as assets by their GUID, because Unity will generate new GUIDs for copies.
    // - Referencing them by name implies having to rename each file in each copy of the package, and then
    //   either edit the EditorAidResources class with the new names, or creating a flimsy name scheme.
    //   That seems like a recipe for mistakes. Also, searching for a name can get slow on big projects.
    //
    // Additionally, using the CallerFilePath attribute to get the asset's folder doesn't work with local
    // packages stored outside the project, because Unity treats them as if they where inside the Packages
    // directory.
    internal class EditorAssetsFolderFinder : ScriptableObject
    {
        public static string GetFolderPath()
        {
            var finder = CreateInstance<EditorAssetsFolderFinder>();
            var script = MonoScript.FromScriptableObject(finder);

            string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(script));
            if (Path.DirectorySeparatorChar != '/')
                path = path.Replace(Path.DirectorySeparatorChar, '/');

            DestroyImmediate(finder);
            return path;
        }
    }
}
