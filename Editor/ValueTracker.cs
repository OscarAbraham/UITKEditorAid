using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using System;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// Utility element that listens for changes in a <see cref="SerializedProperty"/>.
    /// </summary>
    /// 
    /// <typeparam name="TValue">
    /// The type of the property, it doesn't seem to work if it isn't one mentioned in <see cref="SerializedPropertyType"/>
    /// </typeparam>
    /// 
    /// <remarks>
    /// It needs to be added to a panel and bound to work. Many of the uses for this element are covered in Unity 2021 by the
    /// <see cref="BindingExtensions.TrackPropertyValue">TrackPropertyValue</see> and
    /// <see cref="BindingExtensions.TrackSerializedObjectValue">TrackSerializedObjectValue</see> extension methods.
    /// </remarks>
    /// 
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// class ACustomEditor : Editor
    /// {
    ///     public override VisualElement CreateInspectorGUI()
    ///     {
    ///         var root = new VisualElement();
    ///         var intTracker = new ValueTracker<int>();
    ///         root.Add(intTracker);
    /// 
    ///         // You can pass a property path relative to the object that will be bound.
    ///         intTracker.SetUp("intProperty", e => Debug.Log($"new value: {e.newValue}"));
    /// 
    ///         // You can pass a serialized property instead of the property path:
    ///         var intProp = serializedObject.FindProperty("intProperty");
    ///         intTracker.SetUp(intProp, e => Debug.Log($"value changed to {e.newValue}"));
    /// 
    ///         // An optional third value argument sets the initial value of the tracker, 
    ///         // this is to avoid receiving a callback when the tracker is bound.
    ///         intTracker.SetUp(
    ///             intProp,
    ///             e => Debug.Log($"value changed to {e.newValue}"),
    ///             intProp.intValue);
    /// 
    ///         // You can set up all this from the constructor:
    ///         var intTracker2 = new ValueTracker<int>(
    ///             intProp,
    ///             e => Debug.Log($"new value: {e.newValue}"),
    ///             intProp.intValue);
    ///         root.Add(intTracker2);
    /// 
    ///         return root;
    /// 
    ///         // Remember that if we are not inside an inspector, or if we are not tracking
    ///         // a property of the editor's target, we have to bind it manually:
    ///         // root.Bind(serializedObject);
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    [Obsolete("Use ValuePropertyTracker instead.")]
    public class ValueTracker<TValue> : BindableElement, INotifyValueChanged<TValue>
    {
        /// <summary> USS class name of elements of this type. </summary>
        public static readonly string ussClassName = $"editor-aid-{typeof(TValue).Name}-value-tracker";
        private TValue m_Value;

        /// <summary> Delegate called when value changes. </summary>
        public EventCallback<ChangeEvent<TValue>> valueChangedCallback {get; set;}

        /// <summary> Constructor. </summary>
        public ValueTracker()
        {
            pickingMode = PickingMode.Ignore;
            style.display = DisplayStyle.None;
            AddToClassList(ussClassName);
        }

        /// <summary> Convenience constructor that setups the ValueTracker.</summary>
        /// <param name="propertyPath">Path of the property to be tracked</param>
        /// <param name="callback">Set a callback to be called when the property changes</param>
        /// <param name="initialValue">The initial value of the property. Set it to avoid triggering the callback when the element is bound.</param>
        public ValueTracker(
            string propertyPath,
            EventCallback<ChangeEvent<TValue>> callback = null,
            TValue initialValue = default) : this()
        {
            SetUp(propertyPath, callback, initialValue);
        }

        /// <summary> Convenience constructor that setups the ValueTracker.</summary>
        /// <param name="property">Property to be tracked</param>
        /// <param name="callback">Set a callback to be called when the property changes</param>
        /// <param name="initialValue">The initial value of the property. Set it to avoid triggering the callback when the element is bound.</param>
        public ValueTracker(
            SerializedProperty property,
            EventCallback<ChangeEvent<TValue>> callback = null,
            TValue initialValue = default) : this()
        {
            SetUp(property, callback, initialValue);
        }

        /// <summary>
        /// Sets the <see cref="IBindable.bindingPath"/> of the tracker, registers a callback and sets an initial value.
        /// </summary>
        /// <param name="propertyPath">Path of the property to be tracked</param>
        /// <param name="callback">Set a callback to be called when the property changes</param>
        /// <param name="initialValue">The initial value of the property. Set it to avoid triggering the callback when the element is bound.</param>
        public void SetUp(string propertyPath, EventCallback<ChangeEvent<TValue>> callback, TValue initialValue = default)
        {
            SetValueWithoutNotify(initialValue);
            bindingPath = propertyPath;
            valueChangedCallback = callback;

            // There's a issue present at least in Unity 2020.2.7f1, maybe also in older/newer versions, where ChangeEvents are fired on every
            // binding, even if the value hasn't changed. We'll submit a bug report, but on the chance that Unity considers this an expected
            // behavior, here's a fix for us.
            this.RegisterValueChangedCallback(e =>
            {
                if (!EqualityComparer<TValue>.Default.Equals(e.previousValue, e.newValue))
                    valueChangedCallback?.Invoke(e);
            });
        }

        /// <summary>
        /// Sets the <see cref="IBindable.bindingPath"/> of the tracker, registers a callback and sets an initial value.
        /// </summary>
        /// <param name="property">Property to be tracked</param>
        /// <param name="callback">Set a callback to be called when the property changes</param>
        /// <param name="initialValue">The initial value of the property. Set it to avoid triggering the callback when the element is bound.</param>
        public void SetUp(SerializedProperty property, EventCallback<ChangeEvent<TValue>> callback, TValue initialValue = default)
        {
            if (property == null)
            {
                Debug.LogError("Cannot bind tracker to a null SerializedProperty");
                return;
            }

            SetUp(property.propertyPath, callback, initialValue);
        }

        /// <summary>The value of the tracker updated by Unity with the property's value.</summary>
        public TValue value
        {
            get => m_Value;
            set
            {
                if (!EqualityComparer<TValue>.Default.Equals(m_Value, value))
                {
                    if (panel != null)
                    {
                        using (ChangeEvent<TValue> e = ChangeEvent<TValue>.GetPooled(m_Value, value))
                        {
                            e.target = this;
                            SetValueWithoutNotify(value);
                            SendEvent(e);
                        }
                    }
                    else
                    {
                        SetValueWithoutNotify(value);
                    }
                }
            }
        }

        /// <summary> Set the value of the tracker without updating the property or triggering callbacks.</summary>
        /// <param name="value">The new value</param>
        public void SetValueWithoutNotify(TValue value)
        {
            m_Value = value;
        }
    }
}