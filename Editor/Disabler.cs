using System;
using UnityEngine.UIElements;
using UnityEditor;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// Element that disables its content according to its <see cref="shouldDisable"/> callback. <c>UXML support</c>
    /// </summary>
    /// <remarks>
    /// This element is analogous to IMGUI's <see cref="EditorGUI.DisabledScope"/>. It can be used in combination with
    /// <see cref="Utils.SerializedObjectExtensions.IsEditable(SerializedObject)"/> to avoid editing objects that shouldn't be edited.
    /// </remarks>
#if !REMOVE_UXML_FACTORIES && UNITY_2023_3_OR_NEWER
    [UxmlElement]
#endif
    public partial class Disabler : VisualElement
    {
#if !REMOVE_UXML_FACTORIES && !UNITY_2023_3_OR_NEWER
        public new class UxmlFactory : UxmlFactory<Disabler, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlBoolAttributeDescription m_UpdatePeriodically =
                new UxmlBoolAttributeDescription { name = "update-periodically", defaultValue = false };
            UxmlLongAttributeDescription m_UpdateInterval =
                new UxmlLongAttributeDescription { name = "update-interval", defaultValue = k_DefaultUpdateInterval };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var disabler = ve as Disabler;
                disabler.updatePeriodically = m_UpdatePeriodically.GetValueFromBag(bag, cc);
                disabler.updateInterval = m_UpdateInterval.GetValueFromBag(bag, cc);
            }
        }
#endif

        /// <summary> USS class name of elements of this type. </summary>
        public static readonly string ussClassName = "editor-aid-disabler";
        /// <summary> USS class name of the disabler's contentContainer. </summary>
        public static readonly string contentContainerUssClassName = "editor-aid-disabler__content-container";

        private const long k_DefaultUpdateInterval = 1000;

        private readonly VisualElement m_Container = new VisualElement();
        private IVisualElementScheduledItem m_UpdateSchedule;
        private long m_UpdateInterval = k_DefaultUpdateInterval;

        [RemoveFromDocs]
        public override VisualElement contentContainer => m_Container;

        /// <summary> Set this callback to indicate when to enable/disable contents. Elements will be disabled when it returns true.</summary>
        public Func<bool> shouldDisable { get; set; }

        /// <summary>
        /// Enable this to update the disabled status periodically. Note that this element already updates its disabled status on events
        /// that could modify its contents, but this way the disabled status can be updated even when not interacting with the element.
        /// It's false by default.
        /// </summary>
#if !REMOVE_UXML_FACTORIES && UNITY_2023_3_OR_NEWER
        [UxmlAttribute]
#endif
        public bool updatePeriodically
        {
            get => m_UpdateSchedule?.isActive ?? false;
            set
            {
                if (value)
                {
                    m_UpdateSchedule ??= schedule.Execute(UpdateDisabledStatus).Every(updateInterval);
                    m_UpdateSchedule.Resume();
                }
                else
                {
                    m_UpdateSchedule?.Pause();
                }
            }
        }

        /// <summary>
        /// The interval in milliseconds used to update the disabled status periodically when <see cref="updatePeriodically"/> is true.
        /// </summary>
#if !REMOVE_UXML_FACTORIES && UNITY_2023_3_OR_NEWER
        [UxmlAttribute]
#endif
        public long updateInterval
        {
            get => m_UpdateInterval;
            set
            {
                m_UpdateInterval = Math.Max(value, 0);
                m_UpdateSchedule?.Every(m_UpdateInterval);
            }
        }

        public Disabler()
        {
            AddToClassList(ussClassName);
            m_Container.AddToClassList(contentContainerUssClassName);
            hierarchy.Add(m_Container);

            RegisterCallback<PointerDownEvent>(e => UpdateDisabledStatusOnEvent(e), TrickleDown.TrickleDown);
            RegisterCallback<PointerUpEvent>(e => UpdateDisabledStatusOnEvent(e), TrickleDown.TrickleDown);
            RegisterCallback<PointerOverEvent>(e => UpdateDisabledStatusOnEvent(e), TrickleDown.TrickleDown);
            RegisterCallback<KeyDownEvent>(e => UpdateDisabledStatusOnEvent(e), TrickleDown.TrickleDown);
            RegisterCallback<AttachToPanelEvent>(e => UpdateDisabledStatus(), TrickleDown.TrickleDown);
        }

        /// <param name="shouldDisable"> The callback that will be used to disable contents.</param>
        public Disabler(Func<bool> shouldDisable) : this()
        {
            this.shouldDisable = shouldDisable;
        }

        /// <summary>
        /// Call this method to update the disabled status manually.
        /// Note that this element already updates its disabled status on events that could modify its contents.
        /// </summary>
        public void UpdateDisabledStatus()
        {
            bool disabled = shouldDisable != null && shouldDisable();
            m_Container.SetEnabled(!disabled);
        }

        private void UpdateDisabledStatusOnEvent(EventBase e)
        {
            bool prevEnabledSelf = enabledSelf;
            UpdateDisabledStatus();

            // Stop propagation only if the disabled status changed. That way, we still stop events
            // that shouldn't reach children when their disabled status may not be updated yet, and
            // we allow special handling of disabled events that some Unity elements have, e.g. for
            // tooltips or when clicking the object's name in an ObjectField.
            if (!enabledSelf && prevEnabledSelf != enabledSelf && e.target != this)
            {
                e.StopImmediatePropagation();

#if UNITY_2023_2_OR_NEWER
                focusController?.IgnoreEvent(e);
#else
                e.PreventDefault();
#endif
            }
        }
    }
}