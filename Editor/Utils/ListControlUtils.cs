using UnityEngine;
using UnityEditor;

namespace ArteHacker.UITKEditorAid.Utils
{
    /// <summary>
    /// Some utilities to create custom <see cref="ListControl"/> child classes.
    /// </summary>
    public static class ListControlUtils
    {
        /// <summary>
        /// If your custom list is backed by a SerializedProperty, you can call this method inside
        /// <see cref="ListControl.OnReorderDragPerformed(int, int)"/> to implement it.
        /// </summary>
        /// <param name="arrayProp">The serialized array</param>
        /// <param name="draggedIndex">The item that was dragged</param>
        /// <param name="dropIndex">The place where the item was dropped</param>
        public static void ReorderSerializedArray(SerializedProperty arrayProp, int draggedIndex, int dropIndex)
        {
            if (arrayProp == null || !arrayProp.isArray || arrayProp.propertyType == SerializedPropertyType.String)
            {
                Debug.LogError("arrayProp must be a valid SerializedProperty that points to an array or a list");
                return;
            }

            arrayProp.serializedObject.Update();

            int size = arrayProp.arraySize;
            if (draggedIndex < 0 || draggedIndex >= size || dropIndex < 0 || dropIndex > size)
                return;

            if (dropIndex > draggedIndex)
                dropIndex--;

            if (dropIndex != draggedIndex)
            {
                arrayProp.MoveArrayElement(draggedIndex, dropIndex);
                arrayProp.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}