using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// A UIToolkit element that's like a PropertyField for a field with the <see cref="SerializeReference"/> attribute.
    /// It updates when the backing field changes type. It needs to be inside a <see cref="Rebinder"/> ancestor to work.
    /// It contains a <see cref="PropertyField"/> and a <see cref="ManagedReferenceTypeTracker"/> to update it.
    /// <para>
    /// WARNING: Do not use this directly with the property received in <see cref="PropertyDrawer.CreatePropertyGUI(SerializedProperty)"/>;
    /// it'll cause an infinite loop because the PropertyField inside will call the same property drawer again.
    /// </para>
    /// </summary>
    /// <example>
    /// A basic example. See <see cref="ArrayPropertyField"/> for a way to use it with reorderable lists.
    /// <code>
    /// class ACustomEditor : Editor
    /// {
    ///     public override VisualElement CreateInspectorGUI()
    ///     {
    ///         //ManagedReferenceField needs to be in a Rebinder to work.
    ///         var root = new Rebinder(serializedObject);
    ///         // "managed" would be a field with the [SerializeReference] attribute.
    ///         SerializedProperty managedProp = serializedObject.FindProperty("managed");
    ///         root.Add(new ManagedReferenceField(managedProp));
    ///         return root;
    ///     }
    /// }
    /// </code>
    /// </example>
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