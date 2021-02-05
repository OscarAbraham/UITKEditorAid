using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// A UIToolkit element that rebinds itself to a <see cref="SerializedObject"/> when it is requested directly or by polling
    /// <see cref="IRebindingTrigger"/>s like <see cref="ManagedReferenceTypeTracker"/>. Note that this element does
    /// not do the initial binding, so it's necessary to call <see cref="BindingExtensions.Bind(VisualElement, SerializedObject)"/>
    /// when inside a custom Editor Window. Unity Inspectors do the binding themselves.
    /// </summary>
    /// <remarks>
    /// There are times when elements need to be bound at later time than the rest of the hierarchy to be updated.
    /// This includes when a list of elements grow, or when a <see cref="PropertyField"/> with a <see cref="SerializeReference"/> 
    /// attribute changes type. The problem is that each element that is bound separately makes Unity deserialize and 
    /// check the target objects for changes, which can have a huge performace cost.
    /// 
    /// A solution now is to rebind the elements from a root VisualElement, even if most of the children don't need it.
    /// That way, Unity doesn't check the same objects multiple times when it updates the UI. This class helps in
    /// coordinating the needed rebinding automatically. It also throttles rebinding requests for better performance.
    /// </remarks>
    public class Rebinder : VisualElement, ITriggereableRebinder
    {
        /// <summary> USS class name of elements of this type. </summary>
        public static readonly string ussClassName = "editor-aid-element-updater";

        // The binding system updates every 100 ms. If we ran at the same rate, we'd often be polling in the frame
        // where the binding system runs. We would be more prone to performance spikes and, since we would poll
        // just before bindings are updated, we would always old values anyway. This case always happens when the
        // schedule is created at the same time than the panel; but even if we offset the execution somehow, any
        // pause in the system, including those caused by a context menu, would eliminate our offset.

        // A slightly higher throttleTime is the simplest way to avoid coinciding often with the binding system.
        // It also helps to run Update after multiple modifications are done in a row, because the binding system
        // will have time to run between the queueing of an update and its scheduled execution.
        private const int k_ThrottleTime = 117;

        private readonly List<IRebindingTrigger> m_Triggers = new List<IRebindingTrigger>();
        private bool m_PollPeriodically;
        private uint m_PeriodicalPollingTime;

        private readonly IVisualElementScheduledItem m_UpdateSchedule;

        private bool m_RebindRequested = false;
        private bool m_TriggerCheckRequested = false;

        /// <summary>
        /// The SerializedObject that is to be rebound to this element.
        /// </summary>
        public SerializedObject serializedObject { get; }

        /// <summary>
        /// Rebinder constructor.
        /// </summary>
        /// <param name="serializedObject">The SerializedObject that is to be rebound to the element.</param>
        /// <param name="pollTriggersPeriodically">
        /// Whether to poll <see cref="IRebindingTrigger"/>s periodically on top of polling every time there's a change in the editor.
        /// It's useful if your data changes from playmode code.
        /// </param>
        /// <param name="periodicalPollingTime">The time in milliseconds to be used if pollTriggersPeriodically is true.</param>
        public Rebinder(SerializedObject serializedObject, bool pollTriggersPeriodically = false, uint periodicalPollingTime = 5032)
        {
            pickingMode = PickingMode.Ignore; 
            this.serializedObject = serializedObject;

            m_PollPeriodically = pollTriggersPeriodically;
            m_PeriodicalPollingTime = System.Math.Max(k_ThrottleTime, periodicalPollingTime);

            // Scheduled Items generate a lot of garbage every time they are unscheduled. We keep it alive by executing in an interval.
            // The minimum interval has to be the periodicalPollingTime to be able to do periodical polling at the right pace.
            m_UpdateSchedule = schedule.Execute(Update).Every(m_PeriodicalPollingTime);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        /// <summary>
        /// Override this in subclasses to customize binding (i.e. to call bind in a parent element). 
        /// </summary>
        protected virtual void Rebind() => this.Bind(serializedObject);

        /// <summary>
        /// Requests a throttled rebinding to this rebinder's serializedObject.
        /// </summary>
        public void RequestRebind()
        {
            m_RebindRequested = true;
            QueueThrottledUpdate();
        }

        private void RequestTriggerCheck()
        {
            m_TriggerCheckRequested = true;
            QueueThrottledUpdate();
        }

        bool ITriggereableRebinder.TryAddTrigger(IRebindingTrigger updatable)
        {
            if (!m_Triggers.Contains(updatable))
                m_Triggers.Add(updatable);
            return true;
        }

        void ITriggereableRebinder.RemoveTrigger(IRebindingTrigger updatable)
        {
            m_Triggers.Remove(updatable);
        }

        private void OnAttachToPanel(EventBase evt = null)
        {
            RegisterToEditorChanges();
            if (panel != null)
                RequestTriggerCheck();
        }

        private void OnDetachFromPanel(EventBase evt = null)
        {
            UnregisterfromEditorChanges();
        }

        private UndoPropertyModification[] OnPropertyModification(UndoPropertyModification[] modifications)
        {
            foreach (var mod in modifications)
                foreach (var target in serializedObject.targetObjects)
                    if (mod.previousValue.target == target || mod.currentValue.target == target)
                    {
                        RequestTriggerCheck();
                        return modifications;
                    }
            return modifications;
        }

        private void RegisterToEditorChanges()
        {
            UnregisterfromEditorChanges();
            Undo.undoRedoPerformed += RequestTriggerCheck;
            Undo.postprocessModifications += OnPropertyModification;
        }

        private void UnregisterfromEditorChanges()
        {
            Undo.undoRedoPerformed -= RequestTriggerCheck;
            Undo.postprocessModifications -= OnPropertyModification;
        }

        private void QueueThrottledUpdate()
        {
            // This makes the next update happen in k_ThrottleTime ms from now; it doesn't matter if it was going to run sooner or later than that.
            // It also resumes execution if the scheduled item was paused.
            m_UpdateSchedule.ExecuteLater(k_ThrottleTime);
        }

        private void Update()
        {
            // If we are polling periodically, we allways check triggers in Update. If Update runs early because of a 
            // call to RequestRebind, the next Update will probably be later than expected; better sooner than later.
            m_TriggerCheckRequested |= m_PollPeriodically;

            if (m_TriggerCheckRequested && this.PollTriggers(m_Triggers))
                m_RebindRequested = true;

            bool needsRebind = m_RebindRequested; // Use local variable in case RequestRebind is called during Rebind.
            m_RebindRequested = false;
            m_TriggerCheckRequested = false;

            if (needsRebind)
                Rebind();
        }
    }
}