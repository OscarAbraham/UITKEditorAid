using UnityEngine;
using UnityEditor;

namespace ArteHacker.UITKEditorAid.Utils
{
    /// <summary>
    /// Extension methods for <see cref="SerializedObject"/> that are specially useful in combination with UIToolkit.
    /// </summary>
    public static class SerializedObjectExtensions
    {
        /// <summary>
        /// Returns whether a <see cref="SerializedObject"/> is considered editable.
        /// It will return false if any of its <see cref="SerializedObject.targetObjects"/> are marked with <see cref="HideFlags.NotEditable"/>
        /// or if there's an asset that isn't open for editing in version control.
        /// It's specially useful in combination with <see cref="Disabler"/>.
        /// </summary>
        /// <param name="obj"> The object to check. </param>
        /// <returns> Whether object is editable. </returns>
        public static bool IsEditable(this SerializedObject obj)
        {
            return IsEditable(obj, StatusQueryOptions.UseCachedIfPossible);
        }

        public static bool IsEditable(this SerializedObject obj, StatusQueryOptions queryOptions)
        {
            if (obj == null) return false;

            if (!obj.isEditingMultipleObjects)
                return IsTargetEditable(obj.targetObject, queryOptions);

            foreach (var target in obj.targetObjects)
            {
                if (!IsTargetEditable(target, queryOptions))
                    return false;
            }
            return true;
        }

        private static bool IsTargetEditable(Object target, StatusQueryOptions queryOptions)
        {
            return target && (target.hideFlags & HideFlags.NotEditable) == 0
                && (!EditorUtility.IsPersistent(target) || AssetDatabase.IsOpenForEdit(target, queryOptions));
        }
    }
}