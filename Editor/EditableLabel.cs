using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// A label that transforms into a text field to allow editing. By default, it becomes editable with a double click.
    /// </summary>
    public class EditableLabel : BindableElement, INotifyValueChanged<string>
    {
        public new class UxmlFactory : UxmlFactory<EditableLabel, UxmlTraits> { }

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Value = new UxmlStringAttributeDescription { name = "value" };
            UxmlBoolAttributeDescription m_IsDelayed = new UxmlBoolAttributeDescription { name = "delayed", defaultValue = true };
            UxmlBoolAttributeDescription m_Multiline = new UxmlBoolAttributeDescription { name = "multiline" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var label = ve as EditableLabel;
                label.SetValueWithoutNotify(m_Value.GetValueFromBag(bag, cc));
                label.isDelayed = m_IsDelayed.GetValueFromBag(bag, cc);
                label.multiline = m_Multiline.GetValueFromBag(bag, cc);
            }
        }

        /// <summary> USS class name of elements of this type. </summary>
        public static readonly string ussClassName = "editor-aid-editable-label";
        /// <summary> USS class name of the TextField inside this element.</summary>
        public static readonly string textFieldUssClassName = ussClassName + "__text-field";

        private readonly TextField m_TextField = new TextField();
        private readonly VisualElement m_InnerTextInput;

        /// <summary> Whether to enable editing by double clicking the label. See <see cref="BeginEditing"/> for other ways to enable it. </summary>
        public bool editOnDoubleClick { get; set; } = true;

        /// <summary> Whether the TextField inside this label is multiline </summary>
        public bool multiline { get => m_TextField.multiline; set => m_TextField.multiline = value; }

        /// <summary> Whether the TextField inside this label is delayed </summary>
        public bool isDelayed { get => m_TextField.isDelayed; set => m_TextField.isDelayed = value; }

        /// <summary> The maximum character length of the TextField. -1 means no limit and it's the default.  </summary>
        public int maxLength { get => m_TextField.maxLength; set => m_TextField.maxLength = value; }

        /// <summary> The string value of this element.</summary>
        public string value { get => m_TextField.value; set => m_TextField.value = value; }

        /// <summary> EditableLabel constructor. </summary>
        public EditableLabel()
        {
            AddToClassList(ussClassName);
            EditorAidResources.ApplyCurrentTheme(this);
            styleSheets.Add(EditorAidResources.editableLabelStyle);

            m_TextField.SetEnabled(false);
            m_TextField.AddToClassList(textFieldUssClassName);
            Add(m_TextField);

            // We resend change events with this as target because bindings release themselves when a ChangeEvent has another target.
            m_TextField.RegisterValueChangedCallback(RetargetChangeEvent);

            // Focus doesn't work in TextField; we have to acces its child.
            m_InnerTextInput = m_TextField.Q(TextField.textInputUssName);
            m_InnerTextInput.RegisterCallback<BlurEvent>(_ => m_TextField.SetEnabled(false));

            isDelayed = true;
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);
            if (editOnDoubleClick && evt is MouseDownEvent mouseDown && mouseDown.clickCount >= 2)
                BeginEditing();
        }

        /// <summary> Call this method to put the label in edit mode. </summary>
        public void BeginEditing()
        {
            // Delay to let a previously focused element be unfocused first. Otherwise, focus doesn't take sometimes.
            EditorApplication.delayCall += EnableEditing;
        }

        private void EnableEditing()
        {
            m_TextField.SetEnabled(true);
            m_InnerTextInput.Focus();
        }

        /// <summary> Set the element's value without triggering a change event.</summary>
        /// <param name="newValue">The new value.</param>
        public void SetValueWithoutNotify(string newValue)
        {
            m_TextField.SetValueWithoutNotify(newValue);
        }

        private void RetargetChangeEvent(ChangeEvent<string> e)
        {
            e.StopPropagation();
            using (ChangeEvent<string> evt = ChangeEvent<string>.GetPooled(e.previousValue, e.newValue))
            {
                evt.target = this;
                SendEvent(evt);
            }
        }
    }
}