using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Hierarchy = UnityEngine.UIElements.VisualElement.Hierarchy;

namespace ArteHacker.UITKEditorAid.Utils
{
    /// <summary>
    /// Extension methods for VisualElements
    /// </summary>
    public static class UIToolkitExtensions
    {
        /// <summary> Get an element's layout rect in local space.</summary>
        /// <param name="ve">The VisualElement.</param>
        /// <returns>The local layout rect.</returns>
        public static Rect GetLocalRect(this VisualElement ve)
        {
            var layout = ve.layout;
            return new Rect(0, 0, layout.width, layout.height);
        }

        /// <summary>
        /// Get the direct children of a VisualElement filtered by type and an optional filter delegate. It stores them in a List parameter
        /// to avoid garbage allocations.
        /// Sometimes it's hard to work on children using UQuery without getting the parent too. Also, UQuery can have noticeable
        /// performance drawbacks in certain cases.
        /// </summary>
        /// <typeparam name="TElement">The type of the child elements</typeparam>
        /// <param name="ve">The parent element</param>
        /// <param name="results">A list to store the results.</param>
        /// <param name="filter">An optional filter callback.</param>
        public static void GetChildren<TElement>(this VisualElement ve, List<TElement> results, Func<TElement, bool> filter = null)
            where TElement : VisualElement
        {
            if (ve.contentContainer == ve)
                ve.hierarchy.GetChildren(results, filter);
            else
                ve.contentContainer.GetChildren(results, filter);
        }

        /// <summary>
        /// Get the direct children of a <see cref="Hierarchy"/> filtered by type and an optional filter delegate. It stores them in a List parameter
        /// to avoid garbage allocations.
        /// </summary>
        /// <typeparam name="TElement">The type of the child elements</typeparam>
        /// <param name="hierarchy">The parent hierarchy</param>
        /// <param name="results">A List to store the results</param>
        /// <param name="filter">An optional filter callback</param>
        public static void GetChildren<TElement>(this Hierarchy hierarchy, List<TElement> results, Func<TElement, bool> filter = null)
            where TElement : VisualElement
        {
            for (int i = 0; i < hierarchy.childCount; i++)
                if (hierarchy[i] is TElement element && (filter == null || filter(element)))
                    results.Add(element);
        }

        /// <summary>
        /// Get the first direct child with a certain type that passes an optional filter delegate.
        /// </summary>
        /// <typeparam name="TElement">The type of the child</typeparam>
        /// <param name="ve">The parent element</param>
        /// <param name="filter">An optional filter callback</param>
        /// <returns>A child that satisfies conditions or null.</returns>
        public static TElement GetFirstChild<TElement>(this VisualElement ve, Func<TElement, bool> filter = null)
            where TElement : VisualElement
        {
            if (ve.contentContainer == ve)
                return ve.hierarchy.GetFirstChild(filter);
            else
                return ve.contentContainer.GetFirstChild(filter);
        }

        /// <summary>
        /// Get the first direct child with a certain type that passes an optional filter delegate.
        /// </summary>
        /// <typeparam name="TElement">The type of the child</typeparam>
        /// <param name="hierarchy">The parent element</param>
        /// <param name="filter">An optional filter callback</param>
        /// <returns>A child that satisfies conditions or null.</returns>
        public static TElement GetFirstChild<TElement>(this Hierarchy hierarchy, Func<TElement, bool> filter = null)
            where TElement : VisualElement
        {
            for (int i = 0; i < hierarchy.childCount; i++)
                if (hierarchy[i] is TElement element && (filter == null || filter(element)))
                    return element;

            return null;
        }

        /// <summary>
        /// Execute an action on all direct children with a certain type.
        /// </summary>
        /// <typeparam name="TElement">The type of the child elements.</typeparam>
        /// <param name="ve">The parent element.</param>
        /// <param name="action">The action to execute.</param>
        public static void ForEachChild<TElement>(this VisualElement ve, Action<TElement> action)
            where TElement : VisualElement
        {
            if (ve.contentContainer == ve)
                ve.hierarchy.ForEachChild<TElement>(action);
            else
                ve.contentContainer.ForEachChild<TElement>(action);
        }

        /// <summary>
        /// Execute an action on all direct children with a certain type.
        /// </summary>
        /// <typeparam name="TElement">The type of the child elements.</typeparam>
        /// <param name="hierarchy">The parent hierarchy.</param>
        /// <param name="action">The action to execute.</param>
        public static void ForEachChild<TElement>(this Hierarchy hierarchy, Action<TElement> action)
            where TElement : VisualElement
        {
            for (int i = 0; i < hierarchy.childCount; i++)
                if (hierarchy[i] is TElement element)
                    action(element);
        }

        /// <summary>
        /// Gets the main label element of a Property Field when using a default property drawer.
        /// This can be useful to set its tooltip, or to change its text. It should be called after the
        /// field has been bound and attached to panel. Custom PropertyDrawers aren't really supported;
        /// usually, they can take care of their own labels.
        /// </summary>
        /// <param name="propertyField">The property field to search for a main Label Element.</param>
        /// <returns>The main Label Element, or null if it isn't found.</returns>
        public static Label GetLabelElement(this PropertyField propertyField)
        {
            var baseField = propertyField.GetFirstChild<VisualElement>(ve => ve.ClassListContains(BaseField<int>.ussClassName));
            var label = baseField?.GetFirstChild<Label>(l => l.ClassListContains(PropertyField.labelUssClassName));

            // If we couldn't find a basefield to get the label, try to get it from a foldout.
            if (baseField == null && !string.IsNullOrEmpty(propertyField.bindingPath))
            {
                // The names of property foldouts created by default end with the property's path.
                var foldout = propertyField.GetFirstChild<Foldout>(f => f.name?.EndsWith(propertyField.bindingPath) == true);
                var foldoutToggle = foldout?.Q<Toggle>(className: Foldout.toggleUssClassName);
                label = foldoutToggle?.Q<Label>(className: Toggle.textUssClassName);
            }

            return label;
        }

        private static Type s_CachedBindingType;
        private static FieldInfo s_CachedBoundPropertyField;

        /// <summary>
        /// Tries to get the bound SerializedProperty from a bindable element through reflection.
        /// Use it to get the property that is bound to a <see cref="VisualElement"/> if you really need it.
        /// </summary>
        /// <param name="bindable">A bindable element</param>
        /// <returns>A serialized property if it's found and reflection works, null otherwise.</returns>
        public static SerializedProperty GetBoundSerializedProperty(this IBindable bindable)
        {
            var binding = bindable.binding;
            if (binding == null)
                return null;

            var bindingType = binding.GetType();

            if (bindingType != s_CachedBindingType)
            {
                var propertyField = bindingType.GetField("boundProperty",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (propertyField?.FieldType != typeof(SerializedProperty))
                    return null;

                s_CachedBindingType = bindingType;
                s_CachedBoundPropertyField = propertyField;
            }

            return s_CachedBoundPropertyField.GetValue(binding) as SerializedProperty;
        }

        /// <summary>
        /// Short hand for setting flex properties in one go, like in the USS flex property.
        /// </summary>
        /// <param name="style">The style to modify</param>
        /// <param name="grow">The value for <see cref="IStyle.flexGrow"/></param>
        /// <param name="shrink">The value for <see cref="IStyle.flexShrink"/></param>
        /// <param name="basis">The value for <see cref="IStyle.flexBasis"/></param>
        public static void SetFlex(this IStyle style, StyleFloat grow, StyleFloat shrink, StyleLength basis)
        {
            style.flexShrink = shrink;
            style.flexGrow = grow;
            style.flexBasis = basis;
        }

        /// <summary>
        /// Short hand for setting margin properties in one go, like in the USS margin property.
        /// </summary>
        /// <param name="style">The style to modify</param>
        /// <param name="top">The value for <see cref="IStyle.marginTop"/></param>
        /// <param name="right">The value for <see cref="IStyle.marginRight"/></param>
        /// <param name="bottom">The value for <see cref="IStyle.marginBottom"/></param>
        /// <param name="left">The value for <see cref="IStyle.marginLeft"/></param>
        public static void SetMargin(this IStyle style, StyleLength top, StyleLength right, StyleLength bottom, StyleLength left)
        {
            style.marginTop = top;
            style.marginRight = right;
            style.marginBottom = bottom;
            style.marginLeft = left;
        }

        /// <summary>
        /// Short hand for setting padding properties in one go, like in the USS padding property.
        /// </summary>
        /// <param name="style">The style to modify</param>
        /// <param name="top">The value for <see cref="IStyle.paddingTop"/></param>
        /// <param name="right">The value for <see cref="IStyle.paddingRight"/></param>
        /// <param name="bottom">The value for <see cref="IStyle.paddingBottom"/></param>
        /// <param name="left">The value for <see cref="IStyle.paddingLeft"/></param>
        public static void SetPadding(this IStyle style, StyleLength top, StyleLength right, StyleLength bottom, StyleLength left)
        {
            style.paddingTop = top;
            style.paddingRight = right;
            style.paddingBottom = bottom;
            style.paddingLeft = left;
        }

        /// <summary>
        /// Short hand for setting a style's position coordinates.
        /// </summary>
        /// <param name="style">The style to modify</param>
        /// <param name="top">The value for <see cref="IStyle.top"/></param>
        /// <param name="right">The value for <see cref="IStyle.right"/></param>
        /// <param name="bottom">The value for <see cref="IStyle.bottom"/></param>
        /// <param name="left">The value for <see cref="IStyle.left"/></param>
        public static void SetPositionCoordinates(this IStyle style, StyleLength top, StyleLength right, StyleLength bottom, StyleLength left)
        {
            style.top = top;
            style.right = right;
            style.bottom = bottom;
            style.left = left;
        }

        /// <summary>
        /// Short hand for setting border width properties in one go, like in the USS border-width property.
        /// </summary>
        /// <param name="style">The style to modify</param>
        /// <param name="top">The value for <see cref="IStyle.borderTopWidth"/></param>
        /// <param name="right">The value for <see cref="IStyle.borderRightWidth"/></param>
        /// <param name="bottom">The value for <see cref="IStyle.borderBottomWidth"/></param>
        /// <param name="left">The value for <see cref="IStyle.borderLeftWidth"/></param>
        public static void SetBorderWidth(this IStyle style, StyleFloat top, StyleFloat right, StyleFloat bottom, StyleFloat left)
        {
            style.borderTopWidth = top;
            style.borderRightWidth = right;
            style.borderBottomWidth = bottom;
            style.borderLeftWidth = left;
        }

        /// <summary>
        /// Short hand for setting border radius properties in one go, like in the USS border-radius property.
        /// </summary>
        /// <param name="style">The style to modify</param>
        /// <param name="topLeft">The value for <see cref="IStyle.borderTopLeftRadius"/></param>
        /// <param name="topRight">The value for <see cref="IStyle.borderTopRightRadius"/></param>
        /// <param name="bottomRight">The value for <see cref="IStyle.borderBottomRightRadius"/></param>
        /// <param name="bottomLeft">The value for <see cref="IStyle.borderBottomLeftRadius"/></param>
        public static void SetBorderRadius(this IStyle style, StyleLength topLeft, StyleLength topRight, StyleLength bottomRight, StyleLength bottomLeft)
        {
            style.borderTopLeftRadius = topLeft;
            style.borderTopRightRadius = topRight;
            style.borderBottomRightRadius = bottomRight;
            style.borderBottomLeftRadius = bottomLeft;
        }
    }
}