using UnityEditor;
using UnityEngine;
using System.IO;

namespace ArteHacker.UITKEditorAid
{
    // This is needed because this package could exist multiple times in the same project with different
    // namespaces. That means that there could be multiple copies of the same assets, which means that
    // common ways of getting them are not a good fit for us:

    // - We can't reference them as assets by their GUID, because Unity will generate new GUIDs for copies.
    // - Referencing them by name implies having to rename each file in each copy of the package, and then
    //   either edit the EditorAidResources class with the new names, or creating a flimsy name scheme.
    //   That seems like a recipe for mistakes. Also, searching for a name can get slow on big projects.

    // Additionally, using the CallerFilePath attribute to get the asset's folder doesn't work with local
    // packages stored outside the project, because Unity treats them as if they where inside the Packages
    // directory.

    // Note that our solution works because this is editor code. That's due to using the AssetDatabase API.
    // Packages with assets that are used at runtime could adapt this solution by getting asset references
    // in the editor and then serializing them in a ScriptableObject asset, which could then be gotten at
    // runtime by type. I've only tested this idea a little, so don't take my word for it, but feel free to
    // hit me up if you need help.
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
