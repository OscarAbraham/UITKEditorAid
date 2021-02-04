using UnityEngine;
using UnityEditor;

namespace ArteHacker.UITKEditorAid.Utils
{
    public static class ListControlUtils
    {
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