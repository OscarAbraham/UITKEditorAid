﻿using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace ArteHacker.UITKEditorAid
{
    // TODO We could make this class work without ListControl by passing an onDragStart delegate to a constructor.
    /// <summary>
    /// Element to be used as an item inside a <see cref="ListControl"/>. It's used in <see cref="ArrayPropertyField"/>
    /// </summary>
    public class ListRow : VisualElement
    {
        /// <summary> USS class name of elements of this type. </summary>
        public static readonly string ussClassName = "editor-aid-list-row";
        /// <summary> USS class name of the drag handle. </summary>
        public static readonly string dragHandleUssClassName = "editor-aid-list-row__drag-handle";
        /// <summary> USS class name of the row's main content. </summary>
        public static readonly string contentUssClassName = "editor-aid-list-row__content";
        /// <summary> USS class name of the remove button. </summary>
        public static readonly string removeButtonUssClassName = "editor-aid-list-row__remove-button";
        /// <summary> USS class name of a row with an alternated background. </summary>
        public static readonly string alternatedUssClassName = "editor-aid-list-row--alternated";
        /// <summary> USS class name of a row with separators. </summary>
        public static readonly string withSeparatorsUssClassName = "editor-aid-list-row--with-separators";

        private readonly ListControl m_List;
        private readonly VisualElement m_DragHandle = new VisualElement();
        private readonly Button m_RemoveButton = new Button();
        private readonly int m_Index;
        private bool m_ReadyToDrag = false;

        /// <summary> The main content of the row. </summary>
        public VisualElement content { get; }

        /// <summary> ListRow constructor. </summary>
        /// <param name="list">The List that contains this row</param>
        /// <param name="content">The main content of this item</param>
        /// <param name="index">The index in the list</param>
        /// <param name="onRemove">Delegate called when clicking a remove button</param>
        public ListRow(
            ListControl list,
            VisualElement content,
            int index,
            Action<int> onRemove)
        {
            AddToClassList(ussClassName);
            SetAlternatedBackground(true);

            m_List = list;
            m_Index = index;

            m_DragHandle.AddToClassList(dragHandleUssClassName);
            Add(m_DragHandle);

            this.content = content ?? new VisualElement();
            this.content.AddToClassList(contentUssClassName);
            Add(this.content);

            m_RemoveButton.AddToClassList(removeButtonUssClassName);
            m_RemoveButton.style.backgroundImage = EditorGUIUtility.IconContent("Toolbar Minus").image as Texture2D;
            m_RemoveButton.clicked += () => onRemove?.Invoke(m_Index);
            Add(m_RemoveButton);

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
        }

        /// <summary> Enable/Disable alternated background to differentiate even and odd items.</summary>
        /// <param name="enable">Whether to enable alternated background.</param>
        public void SetAlternatedBackground(bool enable)
        {
            EnableInClassList(alternatedUssClassName, enable && m_Index % 2 == 1);
        }

        /// <summary> Enable/Disable separators before and after the row.</summary>
        /// <param name="enable">Whether to enable separators.</param>
        public void SetSeparators(bool enable)
        {
            EnableInClassList(withSeparatorsUssClassName, enable);
        }

        /// <summary> Show/hide a drag handle.</summary>
        /// <param name="enable">Whether to show a drag handle.</param>
        public void SetDragHandle(bool enable)
        {
            if (!enable)
                m_DragHandle.style.display = DisplayStyle.None;
            else
                m_DragHandle.style.display = StyleKeyword.Null;
        }

        /// <summary> Show/hide a remove button.</summary>
        /// <param name="enable">Whether to show a remove button.</param>
        public void SetRemoveButton(bool enable)
        {
            if (!enable)
                m_RemoveButton.style.display = DisplayStyle.None;
            else
                m_RemoveButton.style.display = StyleKeyword.Null;
        }

        /// <summary> Enable/Disable the row's features. </summary>
        /// <param name="removeButton">Show/hide remove button</param>
        /// <param name="dragHandle">Show/hide drag handle</param>
        /// <param name="alternatedBackground">Enable/disable alternated background</param>
        /// <param name="separators">Enable/disable separators</param>
        public void SetFeatures(bool removeButton = true, bool dragHandle = true, bool alternatedBackground = true, bool separators = false)
        {
            SetRemoveButton(removeButton);
            SetDragHandle(dragHandle);
            SetSeparators(separators);
            SetAlternatedBackground(alternatedBackground);
        }

        private void OnMouseDown(MouseDownEvent e)
        {
            if (m_ReadyToDrag)
            {
                e.StopImmediatePropagation();
                return;
            }
            if (e.button != 0 || e.commandKey || e.altKey || e.ctrlKey || e.shiftKey)
                return;

            m_ReadyToDrag = true;
            this.CaptureMouse();
            e.StopImmediatePropagation();
        }

        private void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_ReadyToDrag) return;

            m_List.StartDraggingItem(m_Index);

            m_ReadyToDrag = false;
            this.ReleaseMouse();

            e.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent e)
        {
            if (!m_ReadyToDrag) return;

            m_ReadyToDrag = false;
            this.ReleaseMouse();
            e.StopPropagation();
        }

        private void OnMouseCaptureOut(MouseCaptureOutEvent e) => m_ReadyToDrag = false;
    }
}