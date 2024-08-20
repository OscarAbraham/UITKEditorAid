using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// Utility element that listens for changes in a bound <see cref="SerializedProperty"/>.
    /// </summary>
    /// 
    /// <typeparam name="TValue">
    /// The type of the property. It doesn't work with custom types. Enum types should use <see cref="int"/> or <see cref="string"/>.
    /// Types that inherit from <see cref="Object"/> should use Object as the type.
    /// </typeparam>
    /// 
    /// <remarks>
    /// It needs to be added to a panel and bound to work. You can use <see cref="BindableElement.bindingPath"/> to set the
    /// property that is to be bound.
    /// 
    /// <para>
    /// Many of the uses for this element are covered by
    /// <see cref="BindingExtensions.TrackPropertyValue">TrackPropertyValue</see>, but there's a bug with issue number UUM-74316
    /// that's stopping us from being able to utilize those extension methods fully.
    /// </para>
    /// 
    /// </remarks>
    public class ValuePropertyTracker<TValue> : BindableElement, INotifyValueChanged<TValue>, IMixedValueSupport
    {
        private TValue m_Value;
        private bool m_ShowMixedValue;

        /// <summary>
        /// Delegate called when the value from the bound property changes.
        /// It receives the old value and the new value as parameters, in that order.
        /// </summary>
        public Action<TValue, TValue> valueChanged { get; set; }

        /// <summary>
        /// Delegate called when the mixed value status from the bound property changes.
        /// It's used to represent a serialized property's <see cref="SerializedProperty.hasMultipleDifferentValues"/>.
        /// </summary>
        public Action<bool> isMixedValueChanged { get; set; }

        /// <summary>
        /// The current value stored in this tracker. When the element is bound and it's attached to a panel,
        /// setting its value also set's the value on the bound <see cref="SerializedProperty"/>.
        /// </summary>
        public TValue value
        {
            get => m_Value;
            set
            {
                if (EqualityComparer<TValue>.Default.Equals(m_Value, value))
                    return;

                TValue prevValue = m_Value;
                SetCurrentValue(value);

                if (panel != null)
                {
                    using (ChangeEvent<TValue> evt = ChangeEvent<TValue>.GetPooled(prevValue, m_Value))
                    {
                        evt.target = this;
                        SendEvent(evt);
                    }
                }
            }
        }

        /// <summary> Represents the bound property's <see cref="SerializedProperty.hasMultipleDifferentValues"/>. </summary>
        public bool isMixedValue => m_ShowMixedValue;

        bool IMixedValueSupport.showMixedValue
        {
            get => m_ShowMixedValue;
            set
            {
                if (m_ShowMixedValue != value)
                {
                    m_ShowMixedValue = value;
                    isMixedValueChanged?.Invoke(m_ShowMixedValue);
                }
            }
        }

        /// <summary> Constructor. </summary>
        public ValuePropertyTracker()
        {
            style.display = DisplayStyle.None;
            pickingMode = PickingMode.Ignore;
        }

        /// <summary> Convenience constructor that setups the ValuePropertyTracker.</summary>
        /// <param name="propertyPath">
        /// Path of the property to be bound to this element. It's the same as setting the <see cref="BindableElement.bindingPath"/>.
        /// </param>
        /// <param name="valueChangedCallback">
        /// Set's the <see cref="valueChanged"/> callback of this element.
        /// </param>
        /// <param name="initialValue">
        /// The initial value of the property.
        /// Use it to avoid triggering the <see cref="valueChanged"/> callback when the element the first time is synced with its property.
        /// </param>
        public ValuePropertyTracker(
            string propertyPath,
            Action<TValue, TValue> valueChangedCallback = null,
            TValue initialValue = default)
        {
            value = initialValue;
            bindingPath = propertyPath;
            valueChanged = valueChangedCallback;
        }

        /// <summary> Convenience constructor that setups the ValuePropertyTracker.</summary>
        /// <param name="property">
        /// The property to be bound to this element.
        /// </param>
        /// <param name="valueChangedCallback">
        /// Set's the <see cref="valueChanged"/> callback of this element.
        /// </param>
        /// <param name="initialValue">
        /// The initial value of the property.
        /// Use it to avoid triggering the <see cref="valueChanged"/> callback when the element the first time is synced with its property.
        /// </param>
        public ValuePropertyTracker(
            SerializedProperty property,
            Action<TValue, TValue> valueChangedCallback = null,
            TValue initialValue = default)
            : this(property?.propertyPath, valueChangedCallback, initialValue) { }

        void INotifyValueChanged<TValue>.SetValueWithoutNotify(TValue newValue)
        {
            SetCurrentValue(newValue);
        }

        private void SetCurrentValue(TValue newValue)
        {
            if (EqualityComparer<TValue>.Default.Equals(m_Value, newValue))
                return;

            var prevValue = m_Value;
            m_Value = newValue;
            // We call this here instead of calling it in the value property setter because Unity uses
            // SetValueWithoutNotify when there's a mixed value, but we still want a notification then.
            valueChanged?.Invoke(prevValue, newValue);
        }
    }
}