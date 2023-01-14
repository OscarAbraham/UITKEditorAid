using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// A UI Toolkit element that handles fields with the <see cref="SerializeReference"/> attribute.
    /// It contains a <see cref="PropertyField"/> and rebinds it when the managed reference changes type.
    /// It sends a <see cref="ReferenceTypeChangeEvent"/> when the field changes type.
    /// </summary>
    /// <example>
    /// A basic example. See <see cref="ArrayPropertyField"/> for a way to use it with reorderable lists.
    /// <code>
    /// class ACustomEditor : Editor
    /// {
    ///     public override VisualElement CreateInspectorGUI()
    ///     {
    ///         var root = new VisualElement();
    ///         // "managed" would be a field with the [SerializeReference] attribute.
    ///         SerializedProperty managedProp = serializedObject.FindProperty("managed");
    ///         root.Add(new ManagedReferenceField(managedProp));
    ///         return root;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Unity 2022 adds support for Managed References in UI Toolkit, but it's currently a bit buggy in some edge cases
    /// (for example, it doesn't like when two classes contain fields with the same name but different type).
    /// Use this element in 2022 to go around those bugs or to take advantage of the <see cref="ReferenceTypeChangeEvent"/>.
    /// </remarks>
    public class ManagedReferenceField : VisualElement
    {
        /// <summary> USS class name of elements of this type. </summary>
        public static readonly string ussClassName = "editor-aid-managed-reference-field";
        /// <summary> USS class name for the ManagedReferenceTypeTracker inside. </summary>
        public static readonly string trackerUssClassName = ussClassName + "__tracker";
        /// <summary> USS class name for the PropertyField inside. </summary>
        public static readonly string propertyFieldUssClassName = ussClassName + "__property-field";

        private static readonly HashSet<SerializedObject> m_SerializedObjectsUpdatedRecently = new HashSet<SerializedObject>();

        private string m_Path;
        private SerializedObject m_SerializedObject;
        private string m_ReferenceType;

        private long m_PeriodicalUpdateInterval = 5024;
        private readonly IVisualElementScheduledItem m_UpdateSchedule;

        /// <summary>
        /// In addition to checking the reference's type when things change in the editor, there's a periodical check to
        /// catch changes made from runtime. By default it's done around every five seconds, but the interval can be changed
        /// through this property.
        /// </summary>
        public long periodicalUpdateInterval
        {
            get => m_PeriodicalUpdateInterval;
            set
            {
                m_PeriodicalUpdateInterval = System.Math.Max(value, 100);
                m_UpdateSchedule.Every(m_PeriodicalUpdateInterval);
            }
        }

        /// <summary>
        /// Constructor. Receives a SerializedProperty for a field that has the <see cref="SerializeReference"/> attribute.
        /// It still needs to be bound to work properly.
        /// </summary>
        /// <param name="property"> A SerializedProperty for a field with the <see cref="SerializeReference"/> attribute. </param>
        public ManagedReferenceField(SerializedProperty property) : this(property, null) { }

        /// <summary>
        /// Constructor. Receives a SerializedProperty for a field that has the <see cref="SerializeReference"/> attribute.
        /// It still needs to be Bound to work properly.
        /// </summary>
        /// <param name="property"> A SerializedProperty for a field with the <see cref="SerializeReference"/> attribute. </param>
        /// <param name="label"> Optional string to override the property label. </param>
        public ManagedReferenceField(SerializedProperty property, string label)
        {
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;

            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                Debug.LogError("property must be a ManagedReference");
                return;
            }

            m_Path = property.propertyPath;
            m_SerializedObject = property.serializedObject;
            m_ReferenceType = property.managedReferenceFullTypename;

            var propertyField = new PropertyField(property, label);
            propertyField.AddToClassList(propertyFieldUssClassName);
            Add(propertyField);

            m_UpdateSchedule = schedule.Execute(Update).Every(m_PeriodicalUpdateInterval);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void Update()
        {
            // Avoid acting on a property that doesn't exist anymore and would cause error.
            // NOTE: This generates a little garbage, maybe we should instead use reflection to get the SerializedProperty.isValid internal property?
            var prop = m_SerializedObject.FindProperty(m_Path);
            if (prop == null || prop.propertyType != SerializedPropertyType.ManagedReference)
                return;

            // This generates garbage.
            string newType = prop.managedReferenceFullTypename;

            // This might help with performance in some extreme cases.
            prop.Dispose();

            if (m_ReferenceType != newType)
            {
                using (ReferenceTypeChangeEvent e = ReferenceTypeChangeEvent.GetPooled(m_ReferenceType, newType))
                {
                    e.target = this;
                    SendEvent(e);
                }
                m_ReferenceType = newType;
                this.Bind(m_SerializedObject);
            }
        }

        private void ReactToEditorChange()
        {
            UpdateSerializedObjectIfNeeded();
            Update();
        }

        private void UpdateSerializedObjectIfNeeded()
        {
            // We keep a record of Objects that have been updated this frame to avoid the expensive cost of redundant updates.
            if (m_SerializedObjectsUpdatedRecently.Contains(m_SerializedObject))
                return;

            m_SerializedObject.Update();

            bool isTheFirstAddition = m_SerializedObjectsUpdatedRecently.Count == 0;
            m_SerializedObjectsUpdatedRecently.Add(m_SerializedObject);

            // We clear the HashSet on the next frame so the Objects can be updated again later.
            if (isTheFirstAddition)
                EditorApplication.delayCall += ClearSerializedObjectsUpdatedRecently;

            // Assigning a static method instead of an instance method to the delay delgate avoids creating garbage.
            static void ClearSerializedObjectsUpdatedRecently() => m_SerializedObjectsUpdatedRecently.Clear();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Undo.undoRedoPerformed -= ReactToEditorChange;
            Undo.undoRedoPerformed += ReactToEditorChange;
            Undo.postprocessModifications -= OnPropertyModification;
            Undo.postprocessModifications += OnPropertyModification;

            ReactToEditorChange();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Undo.undoRedoPerformed -= ReactToEditorChange;
            Undo.postprocessModifications -= OnPropertyModification;
        }

        private UndoPropertyModification[] OnPropertyModification(UndoPropertyModification[] modifications)
        {
            // We optimize editing a single Object, which may be the most common case if not the only one,
            // as these fields aren't very usable when editing multiple Objects.
            if (!m_SerializedObject.isEditingMultipleObjects)
            {
                foreach (var mod in modifications)
                {
                    if (ReactToModificationIfItMatches(mod.previousValue, m_SerializedObject.targetObject))
                        return modifications;
                }
            }
            else
            {
                foreach (var target in m_SerializedObject.targetObjects)
                {
                    foreach (var mod in modifications)
                    {
                        if (ReactToModificationIfItMatches(mod.previousValue, target))
                            return modifications;
                    }
                }
            }

            return modifications;

            bool ReactToModificationIfItMatches(PropertyModification modification, Object target)
            {
                if (modification.target == target)
                {
                    ReactToEditorChange();
                    return true;
                }
                return false;
            }
        }
    }
}