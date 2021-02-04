using System.Collections.Generic;
using UnityEditor;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// Defines a VisualElement that can rebind itself to a serializedObject to update its contents.
    /// Most of the time, you can use the <see cref="Rebinder"/> element instead of implementing this interface yourself.
    /// </summary>
    public interface IRebinder
    {
        /// <summary> The <see cref="SerializedObject"/> that is to be bound.</summary>
        SerializedObject serializedObject { get; }
        /// <summary> Request a rebinding of this element and its contents. This call could be throttled for performance reasons.</summary>
        void RequestRebind();
    }

    /// <summary>
    /// Defines an <see cref="IRebinder"/> that polls IRebindingTriggers registered to itself in order to know if a rebind is required.
    /// Most of the time, you can use the <see cref="Rebinder"/> element instead of implementing this interface yourself.
    /// </summary>
    public interface ITriggereableRebinder :IRebinder
    {
        /// <summary>
        /// Tries to register an IRebindingTrigger to be polled by the rebinder.
        /// Usually, you can avoid calling this method by using <see cref="RebinderTriggerExtensions.SetUpRegisteringWithPoller{TTrigger}(TTrigger)"/>
        /// which automatically registers and unregisters the trigger with an ancestor Rebinder when it's attached and removed from the panel.
        /// </summary>
        /// <param name="trigger">The trigger to be registered.</param>
        /// <returns>Whether the trigger could be added.</returns>
        bool TryAddTrigger(IRebindingTrigger trigger);

        /// <summary>
        /// Unregisters an IRebindingTrigger from the rebinder.
        /// Usually, you can avoid calling this method by using <see cref="RebinderTriggerExtensions.SetUpRegisteringWithPoller{TTrigger}(TTrigger)"/>
        /// which automatically registers and unregisters the trigger with an ancestor Rebinder when it's attached and removed from the panel.
        /// </summary>
        /// <param name="trigger">The trigger to be unregistered.</param>
        void RemoveTrigger(IRebindingTrigger trigger);
    }

    public static class ElementUpdaterExtensions
    {
        public static bool PollTriggers(this ITriggereableRebinder poller, List<IRebindingTrigger> triggers)
        {
            var serializedObject = poller.serializedObject;
            // This could run after deleting an object, specially if deletion is behind a menu or a dialog that pauses the editor,
            // so we check that the targetObject stil exists.
            if (!serializedObject.targetObject)
                return false;

            serializedObject.Update();

            bool rebindRequested = false;

            // We go in reverse in case a trigger decides to be removed in PollForChanges. Although, is there
            // a good reason for that to happen? Are we reinforcing risky behavior by supporting this?
            for (int i = triggers.Count - 1; i >= 0; i--)
            {
                if (triggers[i] != null && triggers[i].PollForRebinding())
                    rebindRequested = true;
            }

            return rebindRequested;
        }
    }
}