using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Hierarchy = UnityEngine.UIElements.VisualElement.Hierarchy;

namespace ArteHacker.UITKEditorAid.Utils
{
    /// <summary>
    /// Extension methods for <see cref="VisualElement"/> and <see cref="VisualElement.Hierarchy"/>
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
        /// Adds a child element to a parent a frame after the parent has been attached to a panel.
        /// Useful to add elements to an inspector when they shouldn't be bound to the inspected object.
        /// </summary>
        /// <param name="parent">The parent element</param>
        /// <param name="child">The element to be added</param>
        public static void AddDelayed(this VisualElement parent, VisualElement child)
        {
            EventCallback<AttachToPanelEvent> onAttach = null;
            onAttach = e =>
            {
                EditorApplication.delayCall += () => parent.Add(child);
                parent.UnregisterCallback(onAttach);
            };
            parent.RegisterCallback(onAttach);
        }
    }
}