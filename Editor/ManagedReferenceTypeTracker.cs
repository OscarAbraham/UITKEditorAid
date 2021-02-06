using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// A UIToolkit element that tracks the type of a <see cref="SerializedProperty"/> with the <see cref="SerializeReference"/> attribute.
    /// It rebinds the UI and also sends a <see cref="ReferenceTypeChangeEvent"/> when the tracked property changes type.
    /// It needs to be inside an ancestor <see cref="Rebinder"/> to work.
    /// <para>
    /// See also: <seealso cref="ManagedReferenceField"/>
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// class ACustomEditor : Editor
    /// {
    ///     public override VisualElement CreateInspectorGUI()
    ///     {
    ///         //ManagedReferenceTypeTracker needs to be in a Rebinder to work.
    ///         var root = new Rebinder(serializedObject);
    ///         
    ///         // "managed" would be a field with the [SerializeReference] attribute.
    ///         SerializedProperty managedProp = serializedObject.FindProperty("managed");
    ///         
    ///         root.Add(new PropertyField(managedProp));
    ///         // This will update the PropertyField when it changes type.
    ///         root.Add(new ManagedReferenceTypeTracker(managedProp));
    /// 
    ///         return root;
    ///     }
    /// }
    /// </code>
    /// </example>
    public class ManagedReferenceTypeTracker : VisualElement, IRebindingTrigger
    {
        /// <summary> USS class name of elements of this type. </summary>
        public static readonly string ussClassName = "editor-aid-managed-reference-type-tracker";
        private string m_Path;

        /// <summary> The tracked property. </summary>
        public SerializedProperty serializedProperty { get; }
        /// <summary> The name of the reference type from the last time this element was polled by the rebinder. </summary>
        public string referenceType { get; private set; }
        ITriggereableRebinder IRebindingTrigger.rebinder { get; set; }

        /// <summary>
        /// ManagedReferenceTypeTracker constructor.
        /// </summary>
        /// <param name="property"> The property to be tracked. It must be <see cref="SerializedPropertyType.ManagedReference"/></param>
        public ManagedReferenceTypeTracker(SerializedProperty property)
        {
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;

            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                Debug.LogError("property must be a ManagedReference");
                return;
            }

            serializedProperty = property;
            m_Path = property.propertyPath;
            referenceType = property.managedReferenceFullTypename;
            this.SetUpRegisteringWithPoller();
        }

        bool IRebindingTrigger.PollForRebinding()
        {
            // Avoid acting on a property that doesn't exist anymore and would cause error when trying to access it.
            // TODO, this generates a little garbage, maybe we should instead use reflection to get the SerializedProperty.isValid internal property?
            var sp = serializedProperty.serializedObject.FindProperty(m_Path);
            if (sp == null)
                return false;

            // This generates garbage.
            string newType = serializedProperty.managedReferenceFullTypename;

            if (referenceType != newType)
            {
                using (ReferenceTypeChangeEvent e = ReferenceTypeChangeEvent.GetPooled(referenceType, newType))
                {
                    e.target = this;
                    SendEvent(e);
                }
                referenceType = newType;
                return true;
            }

            return false;
        }

        bool IRebindingTrigger.IsCompatibleWithRebinder(ITriggereableRebinder rebinder)
        {
            return rebinder.serializedObject == serializedProperty.serializedObject;
        }
    }
}