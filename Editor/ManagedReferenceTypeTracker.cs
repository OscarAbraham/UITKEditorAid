using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// An element that tracks the type of a <see cref="serializedProperty"/> with the <see cref="SerializeReference"/> attribute.
    /// It's to be used in combination with <see cref="Rebinder"/> to update the UI when the type changes.
    /// <seealso cref="ManagedReferenceField"/>
    /// </summary>
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