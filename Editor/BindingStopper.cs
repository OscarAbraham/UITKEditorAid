using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// A VisualElement that prevents binding operations from higher in the hierarchy from reaching its
    /// children. It's still possible to bind the children directly. It's useful when there are fields
    /// that should be bound to a different object than their ancestors.
    /// <para>
    /// For example, this element is used inside <see cref="ListOfInspectors"/> so, when the list is
    /// itself nested in another inspector, its items are bound to the listed objects instead of being
    /// bound to the object from the parent inspector.
    /// </para>
    /// </summary>
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