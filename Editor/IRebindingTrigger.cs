using UnityEngine.UIElements;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// Defines a VisualElement that registers with an <see cref="ITriggereableRebinder"/> to be polled in order to indicate whether a rebind is needed.
    /// </summary>
    public interface IRebindingTrigger
    {
        /// <summary>
        /// The rebinder that registered this trigger.
        /// </summary>
        ITriggereableRebinder rebinder { get; set; }

        /// <summary>
        /// Indicates whether this trigger is compatible with a rebinder. It usually means that they act on the same SerializedObject.
        /// </summary>
        /// <param name="rebinder">A rebinder to be checked.</param>
        /// <returns>Whether rebinder is compatible.</returns>
        bool IsCompatibleWithRebinder(ITriggereableRebinder rebinder);

        /// <summary>
        /// Indicates that something changed that requires a rebinding to be updated.
        /// </summary>
        /// <returns>Whether a rebinding is requested.</returns>
        bool PollForRebinding();
    }

    /// <summary>
    /// Extension methods for the <see cref="IRebindingTrigger"/> interface.
    /// </summary>
    public static class RebinderTriggerExtensions
    {
        /// <summary>
        /// Sets up the trigger to automatically register and unregister itself with an ancestor rebinder when it's attached and removed from a panel.
        /// </summary>
        /// <typeparam name="TTrigger">A <see cref="VisualElement"/> that implements <see cref="IRebindingTrigger"/></typeparam>
        /// <param name="trigger">The trigger to be acted upon.</param>
        public static void SetUpRegisteringWithPoller<TTrigger>(this TTrigger trigger) where TTrigger : VisualElement, IRebindingTrigger
        {
            trigger.RegisterCallback<AttachToPanelEvent>(e => trigger.TryRegisterWithAncestorRebinder());
            trigger.RegisterCallback<DetachFromPanelEvent>(e => trigger.UnregisterFromRebinder());
        }

        private static bool TryRegisterWithAncestorRebinder<TTrigger>(this TTrigger trigger) where TTrigger : VisualElement, IRebindingTrigger
        {
            //TODO Should we print a warning when no ancestor is found?
            trigger.UnregisterFromRebinder();

            var rebinder = trigger.GetFirstAncestorOfType<ITriggereableRebinder>();
            if (rebinder == null || !trigger.IsCompatibleWithRebinder(rebinder) || !rebinder.TryAddTrigger(trigger))
                return false;

            trigger.rebinder = rebinder;
            return true;
        }

        private static void UnregisterFromRebinder(this IRebindingTrigger trigger)
        {
            var rebinder = trigger.rebinder;

            if (rebinder != null)
                rebinder.RemoveTrigger(trigger);

            trigger.rebinder = null;
        }
    }
}