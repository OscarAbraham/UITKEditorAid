using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// Element that prevents binding its children from higher in the hierarchy. <c>UXML support</c>
    /// </summary>
    /// <remarks>
    /// It's still possible to bind the children, just not from outside this element.
    /// <para>
    /// Use this element to keep fields bound to different Objects than the rest of the UI.
    /// For example, it's used by <see cref="ListOfInspectors"/> to ensure inspector headers aren't
    /// bound to the parent inspector's Object.
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

        [RemoveFromDocs]
        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt.GetType() != s_SerializedObjectBindEventType)
                return;

            evt.StopPropagation();
        }
    }
}