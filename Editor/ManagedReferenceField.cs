using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// A UIToolkit element that's like a PropertyField for a field with the <see cref="SerializeReference"/> attribute.
    /// It contains a <see cref="PropertyField"/> and rebinds it when the managed reference changes type.
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
    ///         var root = new VisualElement();
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

        // We wait for Unity to update the SerializedObject; it happens every 100ms, and we wait a bit more for good measure.
        private const int k_PollingDelay = 164;

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
                m_PeriodicalUpdateInterval = System.Math.Max(value, k_PollingDelay);
                m_UpdateSchedule.Every(m_PeriodicalUpdateInterval);
            }
        }

        public ManagedReferenceField(SerializedProperty property) : this(property, null) { }

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

        private void QueueUpdate()
        {
            // This makes the next update happen in k_PollingDelay ms from now; it doesn't matter if it was going to run sooner or later than that.
            // It also resumes execution if the scheduled item was paused.
            m_UpdateSchedule.ExecuteLater(k_PollingDelay);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Undo.undoRedoPerformed -= QueueUpdate;
            Undo.undoRedoPerformed += QueueUpdate;
            Undo.postprocessModifications -= OnPropertyModification;
            Undo.postprocessModifications += OnPropertyModification;

            // In case we just changed tabs.
            QueueUpdate();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Undo.undoRedoPerformed -= QueueUpdate;
            Undo.postprocessModifications -= OnPropertyModification;
        }

        private UndoPropertyModification[] OnPropertyModification(UndoPropertyModification[] modifications)
        {
            foreach (var mod in modifications)
                foreach (var target in m_SerializedObject.targetObjects)
                    // TODO Should we check that propertyPath is our path? Would it work even if multiple properties refer to the same object?
                    if (mod.previousValue.target == target || mod.currentValue.target == target)
                    {
                        QueueUpdate();
                        return modifications;
                    }
            return modifications;
        }
    }
}