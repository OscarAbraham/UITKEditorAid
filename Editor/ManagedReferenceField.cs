using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// A convenience UIToolkit element that contains a <see cref="PropertyField"/> and a <see cref="ManagedReferenceTypeTracker"/> to update it.
    /// It needs to be inside a <see cref="Rebinder"/> ancestor to work.
    /// WARNING: Do not use this directly with the property received in <see cref="PropertyDrawer.CreatePropertyGUI(SerializedProperty)"/>;
    /// it'll cause an infinite loop because the PropertyField inside will call the same property drawer again.
    /// </summary>
    public class ManagedReferenceField : VisualElement
    {
        /// <summary> USS class name of elements of this type. </summary>
        public static readonly string ussClassName = "editor-aid-managed-reference-field";
        /// <summary> USS class name for the ManagedReferenceTypeTracker inside. </summary>
        public static readonly string trackerUssClassName = ussClassName + "__tracker";
        /// <summary> USS class name for the PropertyField inside. </summary>
        public static readonly string propertyFieldUssClassName = ussClassName + "__property-field";

        public ManagedReferenceField(SerializedProperty property) : this(property, string.Empty) { }

        public ManagedReferenceField(SerializedProperty property, string label)
        {
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;

            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                Debug.LogError("property must be a ManagedReference");
                return;
            }

            var tracker = new ManagedReferenceTypeTracker(property);
            tracker.style.display = DisplayStyle.None;
            tracker.AddToClassList(trackerUssClassName);
            Add(tracker);

            var propertyField = new PropertyField(property, label);
            propertyField.AddToClassList(propertyFieldUssClassName);
            Add(propertyField);
        }
    }
}