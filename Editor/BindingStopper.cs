using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// An element that prevents binding operations from higher in the hierarchy from reaching its children. <i>UXML support.</i>
    /// </summary>
    /// <remarks>
    /// It's still possible to bind the children, just not from outside this element.
    /// <para>
    /// This element is useful for adding fields that must be bound to a different Object than the rest of the UI.
    /// For example, this is used by the <see cref="ListOfInspectors"/> so each item is bound to its respective
    /// Object instead of being bound to the Object from the parent inspector.
    /// </para>
    /// </remarks>
    public class BindingStopper : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<BindingStopper> { }

        private static readonly Type s_SerializedObjectBindEventType;

        /// <summary> USS class name of elements of this type. </summary>
        public static readonly string ussClassName = "editor-aid-binding-stopper";

        static BindingStopper()
        {
            s_SerializedObjectBindEventType = typeof(PropertyField).Assembly?.GetType("UnityEditor.UIElements.SerializedObjectBindEvent");
            if (s_SerializedObjectBindEventType == null)
            {
                Debug.LogError("BindingStopper won't work. There must have been a change in Unity's internals. Reflection must be updated.");
            }
        }

        public BindingStopper()
        {
            AddToClassList(ussClassName);
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt.GetType() != s_SerializedObjectBindEventType)
                return;

            evt.StopPropagation();
        }
    }
}