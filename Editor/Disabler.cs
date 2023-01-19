using System;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine.Internal;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// Element that disables and enables its content according to its <see cref="shouldDisable"/> callback. <mark>UXML support</mark>
    /// </summary>
    /// <remarks>
    /// This element is analogous to IMGUI's <see cref="EditorGUI.DisabledScope"/>. It can be used in combination with
    /// <see cref="Utils.SerializedObjectExtensions.IsEditable(SerializedObject)"/> to avoid editing objects that shouldn't be edited.
    /// </remarks>
    public class Disabler : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<Disabler> { }

        /// <summary> USS class name of elements of this type. </summary>
        public static readonly string ussClassName = "editor-aid-disabler";
        /// <summary> USS class name of the disabler's contentContainer. </summary>
        public static readonly string contentContainerUssClassName = "editor-aid-disabler__content-container";

        private readonly VisualElement m_Container = new VisualElement();

        [ExcludeFromDocs]
        public override VisualElement contentContainer => m_Container;

        /// <summary> Set this callback to indicate when to enable/disable contents. Elements will be disabled when it returns true.</summary>
        public Func<bool> shouldDisable { get; set; }

        public Disabler()
        {
            AddToClassList(ussClassName);
            m_Container.AddToClassList(contentContainerUssClassName);
            hierarchy.Add(m_Container);

            RegisterCallback<MouseDownEvent>(e => UpdateDisabledStatus(e), TrickleDown.TrickleDown);
            RegisterCallback<MouseUpEvent>(e => UpdateDisabledStatus(e), TrickleDown.TrickleDown);
            RegisterCallback<MouseOverEvent>(e => UpdateDisabledStatus(e), TrickleDown.TrickleDown);
            RegisterCallback<KeyDownEvent>(e => UpdateDisabledStatus(e), TrickleDown.TrickleDown);
            RegisterCallback<AttachToPanelEvent>(e => UpdateDisabledStatus(), TrickleDown.TrickleDown);
        }

        /// <param name="shouldDisable"> The callback that will be used to disable contents.</param>
        public Disabler(Func<bool> shouldDisable) : this()
        {
            this.shouldDisable = shouldDisable;
        }

        private void UpdateDisabledStatus(EventBase e = null)
        {
            bool disabled = shouldDisable != null ? shouldDisable() : false;

            if (disabled && e != null && e.target != this)
            {
                e.StopImmediatePropagation();
                e.PreventDefault();
            }
            m_Container.SetEnabled(!disabled);
        }
    }
}