using System;
using UnityEngine.UIElements;
using UnityEditor;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// UIToolkit element that disables and enables contents according to the <see cref="shouldDisable"/> callback. It's equivalent to
    /// <see cref="EditorGUI.DisabledScope"/>. This has a custom contentContainer, so make sure to add children using <b>VisualElement.Add</b>;
    /// children added with <b>VisualElement.hierarchy.Add</b> will not be handled by the Disabler. UXML shouldn't have this problem.
    /// <para>
    /// This element is specially useful in combination with <see cref="Utils.SerializedObjectExtensions.IsEditable(SerializedObject)"/>
    /// to avoid editing objects that shouldn't be edited. This helps in replicating what happens in IMGUI editors, which become disabled
    /// when an object is closed to edit by Version Control or HideFlags.
    /// </para>
    /// </summary>
    public class Disabler : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<Disabler> { }

        /// <summary> USS class name of elements of this type. </summary>
        public static readonly string ussClassName = "editor-aid-disabler";
        /// <summary> USS class name of the disabler's contentContainer. </summary>
        public static readonly string contentContainerUssClassName = "editor-aid-disabler__content-container";

        private readonly VisualElement m_Container = new VisualElement();

        public override VisualElement contentContainer => m_Container;

        /// <summary> Set this callback to indicate when to enable/disable contents. Elements will be disabled when it returns true.</summary>
        public Func<bool> shouldDisable { get; set; }

        /// <summary> Disabler constructor. </summary>
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

        /// <summary> Disabler constructor. </summary>
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