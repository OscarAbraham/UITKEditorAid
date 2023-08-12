using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace ArteHacker.UITKEditorAid.Utils
{
    /// <summary>
    /// Some utilities to create custom <see cref="ListControl"/> child classes.
    /// </summary>
    public static class ListControlUtils
    {
        /// <summary>
        /// If your custom <see cref="ListControl"/> is backed by a SerializedProperty, you can call this method inside
        /// <see cref="ListControl.OnReorderDragPerformed(int, int)"/> to implement it.
        /// </summary>
        /// <param name="arrayProp">The serialized array</param>
        /// <param name="draggedIndex">The item that was dragged</param>
        /// <param name="dropIndex">The place where the item was dropped</param>
        public static void ReorderSerializedArray(SerializedProperty arrayProp, int draggedIndex, int dropIndex)
        {
            if (arrayProp == null || !arrayProp.isArray || arrayProp.propertyType == SerializedPropertyType.String)
            {
                Debug.LogError("arrayProp must be a valid SerializedProperty that points to an array or a list");
                return;
            }

            arrayProp.serializedObject.Update();

            int size = arrayProp.arraySize;
            if (draggedIndex < 0 || draggedIndex >= size || dropIndex < 0 || dropIndex > size)
                return;

            if (dropIndex > draggedIndex)
                dropIndex--;

            if (dropIndex != draggedIndex)
            {
                arrayProp.MoveArrayElement(draggedIndex, dropIndex);
                arrayProp.serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Call this with an item for a custom <see cref="ListControl"/>'s <see cref="ListControl.CreateItemForIndex(int)"/>
        /// method to handle dragging and selecting the item. It's used by <see cref="ListRow"/>.
        /// </summary>
        /// <param name="listControl">The ListControl that owns the item.</param>
        /// <param name="item">The item element.</param>
        /// <param name="itemIndex">The item's index in the ListControl.</param>
        public static void HandleListControlItemEvents(ListControl listControl, VisualElement item, int itemIndex)
        {
            // We don't need to do anything with the handler; it's like a closure in the shape of a class because it's cleaner.
            new ListControlItemEventsHandler(listControl, item, itemIndex);
        }

        private class ListControlItemEventsHandler
        {
            private readonly ListControl m_ListControl;
            private readonly VisualElement m_Item;
            private readonly int m_ItemIndex;

            private bool m_ReadyToDrag = false;

            public ListControlItemEventsHandler(ListControl listControl, VisualElement item, int itemIndex)
            {
                m_ListControl = listControl;
                m_ItemIndex = itemIndex;
                m_Item = item;

                item.RegisterCallback<PointerDownEvent>(OnPointerDown);
                item.RegisterCallback<MouseDownEvent>(OnMouseDown);
                item.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                item.RegisterCallback<MouseUpEvent>(OnMouseUp);
                item.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
            }

            private void OnPointerDown(PointerDownEvent e)
            {
                if (m_ReadyToDrag)
                {
                    e.StopImmediatePropagation();
                    return;
                }
                if (e.button != 0 || e.commandKey || e.altKey || e.ctrlKey || e.shiftKey)
                    return;

                // We stop propagation here because we do intend to use this user action, but we
                // wait until MouseDown because in some Unity versions pointer events are propagated
                // completely before sending mouse events. So, if we capture the mouse here, a child
                // of m_Item could miss the MouseDown event.
                if (e.pointerId == PointerId.mousePointerId)
                    e.StopPropagation();
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

                if (!TargetIsInDescendantCollectionView(e))
                {
                    m_ReadyToDrag = true;
                    m_Item.CaptureMouse();
                    m_ListControl.selectedItem = m_ItemIndex;
                }

                e.StopPropagation();
            }

            private void OnMouseMove(MouseMoveEvent e)
            {
                if (!m_ReadyToDrag) return;

                // We check if the mouse is still dragging to protect against edge cases where the
                // mouse is released without us receiving any event that sets m_ReadyToDrag false.
                if ((e.pressedButtons & 1) != 0)
                    m_ListControl.StartDraggingItem(m_ItemIndex);

                m_ReadyToDrag = false;
                m_Item.ReleaseMouse();

                e.StopPropagation();
            }

            private void OnMouseUp(MouseUpEvent e)
            {
                if (!m_ReadyToDrag) return;

                m_ReadyToDrag = false;
                m_Item.ReleaseMouse();
                e.StopPropagation();
            }

            private void OnMouseCaptureOut(MouseCaptureOutEvent e)
            {
                m_ReadyToDrag = false;
            }

            // This ensures reorderable CollectionViews (e.g. ListViews) work when nested in a ListControl item.
            // Unity's ListView dragging implementation doesn't play very nice with other elements:
            //
            // - It uses pointer events instead of mouse events, that's not a problem in itself, but it's relevant.
            // - It prepares for reordering on PointerDownEvent, but it doesn't stop propagation, and it doesn't capture the pointer.
            //   Not capturing here makes sense in some Unity versions, as they propagate pointer events before sending mouse events,
            //   so child elements would miss the mouseDownEvent. I'm not sure why they don't stop propagation.
            // - It doesn't capture or stop propagation of PointerMoveEvents immediately; it waits for the pointer to move a certain amount.
            // - It doesn't check if the dragging operation is still valid when reacting to PointerMoveEvents.
            //
            // So, if we don't use this method, our ListControl will try to reorder when dragging from inside the ListView. Then,
            // the ListView gets into a weird unsupported state where it tries to drag an element when just hovering the mouse over it.
            //
            // An alternative solution would be to use a similar strategy ourselves, maybe also stop propagation of DownEvents.
            // But it isn't very practical; Unity's implementation changes a lot. Even something small, like changing the distance threshold
            // for beggining to process PointerMoveEvents, could make our implementation stop working.
            private bool TargetIsInDescendantCollectionView(EventBase e)
            {
                if (e.target is VisualElement veTarget)
                {
                    var ancestor = veTarget.parent;
                    while (ancestor != null && ancestor != m_Item)
                    {
                        if (ancestor is BaseVerticalCollectionView)
                            return true;
                        ancestor = ancestor.parent;
                    }
                }

                return false;
            }
        }
    }
}