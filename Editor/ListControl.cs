using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// Base class to create Reorderable List UIToolkit elements.
    /// </summary>
    public abstract class ListControl : VisualElement
    {
        /// <summary> USS class name of elements of this type. </summary>
        public static readonly string ussClassName = "editor-aid-list-control";
        /// <summary> USS class name of Lists drawn inside a box. </summary>
        public static readonly string boxedUssClassName = "editor-aid-list-control--boxed";
        /// <summary> USS class name of Lists that are folded (hidden except for the header). </summary>
        public static readonly string foldedUssClassName = "editor-aid-list-control--folded";
        /// <summary> USS class name of Lists with a header. </summary>
        public static readonly string withHeaderUssClassName = "editor-aid-list-control--with-header";
        /// <summary> USS class name of Lists with a footer. </summary>
        public static readonly string withFooterUssClassName = "editor-aid-list-control--with-footer";
        /// <summary> USS class name of Lists in the middle of a drag operation. </summary>
        public static readonly string draggingListUssClassName = "editor-aid-list-control--dragging";

        /// <summary> USS class name of the drop indicator bar for Drag and Drop. </summary>
        public static readonly string dropIndicatorUssClassName = "editor-aid-list-control__drop-indicator";
        /// <summary> USS class name of a message to indicate the list is empty. </summary>
        public static readonly string emptyMessageUssClassName = "editor-aid-list-control__empty-message";
        /// <summary> USS class name of the list's header. </summary>
        public static readonly string headerUssClassName = "editor-aid-list-control__header";
        /// <summary> USS class name of the list's footer. </summary>
        public static readonly string footerUssClassName = "editor-aid-list-control__footer";

        /// <summary> USS class name of the list's items. </summary>
        public static readonly string itemUssClassName = "editor-aid-list-control__item";
        /// <summary> USS class name of an item that's being dragged. </summary>
        public static readonly string draggedItemUssClassName = "editor-aid-list-control__item--dragged";

        private readonly List<VisualElement> m_Items = new List<VisualElement>();
        private readonly VisualElement m_DropBar = new VisualElement();
        private readonly VisualElement m_Header = new VisualElement();
        private readonly VisualElement m_Footer = new VisualElement();
        private int m_DraggedIndex = -1;

        private readonly Label m_EmptyListLabel = new Label("List is Empty");
        private string m_EmptyListMessage = "List is Empty";

        /// <summary> A message to indicate that the list is empty. Set it to null to hide it.</summary>
        public string emptyListMessage
        {
            get => m_EmptyListMessage;
            set
            {
                if (m_EmptyListMessage == value)
                    return;
                m_EmptyListMessage = value;

                if (GetListSize() == 0 && !string.IsNullOrEmpty(value))
                    m_EmptyListLabel.style.display = StyleKeyword.Null;
                else
                    m_EmptyListLabel.style.display = DisplayStyle.None;
            }
        }

        /// <summary>Whether the list is drawn inside a box.</summary>
        public bool boxed
        {
            get => ClassListContains(boxedUssClassName);
            set
            {
                if (value)
                    AddToClassList(boxedUssClassName);
                else
                    RemoveFromClassList(boxedUssClassName);
            }
        }

        /// <summary> ListControl constructor.</summary>
        protected ListControl()
        {
            AddToClassList(ussClassName);
            EditorAidResources.ApplyCurrentTheme(this);
            styleSheets.Add(EditorAidResources.listControlStyle);

            m_Header.AddToClassList(headerUssClassName);
            Add(m_Header);
            m_EmptyListLabel.AddToClassList(emptyMessageUssClassName);
            Add(m_EmptyListLabel);
            m_Footer.AddToClassList(footerUssClassName);
            Add(m_Footer);

            m_DropBar.AddToClassList(dropIndicatorUssClassName);
            m_DropBar.style.visibility = Visibility.Hidden;
            m_DropBar.pickingMode = PickingMode.Ignore;
            Add(m_DropBar);

            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<DragLeaveEvent>(OnDragEnd);
            RegisterCallback<DragExitedEvent>(OnDragEnd);
        }

        /// <summary> Start dragging an item to reorder the list. Only call this from mouse events where a button is pressed to avoid errors.</summary>
        /// <param name="index">Index of the item to drag.</param>
        public void StartDraggingItem(int index)
        {
            if (!IsReorderable())
                return;
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = System.Array.Empty<Object>();
            DragAndDrop.paths = null;
            DragAndDrop.SetGenericData("DraggedList", this);
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            DragAndDrop.StartDrag("");
            m_DraggedIndex = index;
        }

        /// <summary> Get the item count of the list.</summary>
        /// <returns>The item count</returns>
        public int GetListSize() => m_Items.Count;

        /// <summary> Set the number of items in the list. It will call <see cref="CreateItemForIndex(int)"/> to create items if needed.</summary>
        /// <param name="size">The number of items.</param>
        protected void SetListSize(int size)
        {
            if (size < m_Items.Count)
            {
                for (int i = m_Items.Count - 1; i >= size; i--)
                {
                    var item = m_Items[i];
                    m_Items.RemoveAt(i); // In this order to remove from m_Items even if there's an error after removing from hierarchy.
                    item.RemoveFromHierarchy();
                }
            }
            else if (size > m_Items.Count)
            {
                for (int i = m_Items.Count; i < size; i++)
                {
                    var item = CreateItemForIndex(i) ?? new VisualElement();
                    item.AddToClassList(itemUssClassName);
                    Add(item);
                    m_Items.Add(item);
                }

                m_DropBar.BringToFront();
            }

            if (size == 0 && !string.IsNullOrEmpty(emptyListMessage))
                m_EmptyListLabel.style.display = StyleKeyword.Null;
            else
                m_EmptyListLabel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Get an item from the list.
        /// </summary>
        /// <param name="index">The item's index</param>
        /// <returns>The item</returns>
        protected VisualElement GetListItem(int index) => m_Items[index];

        /// <summary>
        /// Set whether to show the list's header.
        /// </summary>
        /// <param name="visible">The header's visibility</param>
        protected void SetHeaderVisibility(bool visible)
        {
            EnableInClassList(withHeaderUssClassName, visible);
        }

        /// <summary>
        /// Set the header's content.
        /// </summary>
        /// <param name="content">The header's content</param>
        protected void SetHeaderContent(VisualElement content)
        {
            m_Header.Clear();
            m_Header.Add(content);
        }

        /// <summary>
        /// Set whether to show the list's footer.
        /// </summary>
        /// <param name="visible">The footer's visibility</param>
        protected void SetFooterVisibility(bool visible)
        {
            EnableInClassList(withFooterUssClassName, visible);
        }

        /// <summary>
        /// Set the footer's content.
        /// </summary>
        /// <param name="content">The footer's content</param>
        protected void SetFooterContent(VisualElement content)
        {
            m_Footer.Clear();
            m_Footer.Add(content);
        }

        /// <summary>
        /// Implement this method to create the list's items.
        /// </summary>
        /// <param name="index">The item index</param>
        /// <returns>A new item</returns>
        protected abstract VisualElement CreateItemForIndex(int index);

        /// <summary>
        /// Override this method to define whether a list is reorderable.
        /// </summary>
        /// <returns>Whether the list is reorderable</returns>
        protected virtual bool IsReorderable() => true;

        /// <summary>
        /// Implement this method to do the actual reordering of the data backing the list control.
        /// </summary>
        /// <param name="draggedIndex">The index of the dragged item</param>
        /// <param name="dropIndex">The index where the item is being dropped</param>
        protected virtual void OnReorderDragPerformed(int draggedIndex, int dropIndex) { }

        /// <summary>
        /// Implement this method to support dragging other than list reordering (i.e. to insert objects).
        /// Return anything other than <see cref="DragAndDropVisualMode.Rejected"/> and <see cref="DragAndDropVisualMode.None"/> to support custom dragging.
        /// Use it together with <see cref="OnCustomDragPerformed(int)"/>.
        /// </summary>
        /// <param name="dropIndex">The place where the an item is to be droped.</param>
        /// <returns></returns>
        protected virtual DragAndDropVisualMode VerifyCustomDrag(int dropIndex) => DragAndDropVisualMode.Rejected;

        /// <summary>
        /// Called when the mouse is released and <see cref="VerifyCustomDrag(int)"/> returns a valid mode.
        /// </summary>
        /// <param name="dropIndex">The place where the item is dropped.</param>
        protected virtual void OnCustomDragPerformed(int dropIndex) { }

        private bool VerifyReorderDrag() => IsReorderable() && DragAndDrop.GetGenericData("DraggedList") == this;

        private void OnDragUpdated(DragUpdatedEvent e)
        {
            int dropIndex = GetDropIndex(e);
            var dragMode = VerifyCustomDrag(dropIndex);

            // If it's none or rejected.
            if ((dragMode & ~DragAndDropVisualMode.Rejected) == 0) 
            {
                if (!VerifyReorderDrag())
                    return;
                else
                    dragMode = DragAndDropVisualMode.Move;
            }

            DragAndDrop.visualMode = dragMode;
            UpdateDropBar(dropIndex);
            AddToClassList(draggingListUssClassName);

            if (m_DraggedIndex >= 0 && m_DraggedIndex < m_Items.Count)
                m_Items[m_DraggedIndex].AddToClassList(draggedItemUssClassName);
        }

        private void OnDragPerform(DragPerformEvent e)
        {
            OnDragEnd(e);
            int dropIndex = GetDropIndex(e);

            // If it isn't none or rejected.
            bool isCustomDrag = (VerifyCustomDrag(dropIndex) & ~DragAndDropVisualMode.Rejected) != 0;
            if (!isCustomDrag && !VerifyReorderDrag())
                return;

            DragAndDrop.AcceptDrag();

            if (isCustomDrag)
                OnCustomDragPerformed(dropIndex);
            else
                OnReorderDragPerformed(m_DraggedIndex, dropIndex);
        }

        private void OnDragEnd(IMouseEvent e)
        {
            m_DropBar.style.visibility = Visibility.Hidden;
            RemoveFromClassList(draggingListUssClassName);

            if (m_DraggedIndex >= 0 && m_DraggedIndex < m_Items.Count)
                m_Items[m_DraggedIndex].RemoveFromClassList(draggedItemUssClassName);
        }

        private void UpdateDropBar(int index)
        {
            if (index > m_Items.Count || index < 0)
            {
                m_DropBar.style.visibility = Visibility.Hidden;
                return;
            }

            if (index == m_Items.Count)
            {
                var itemRect = this.WorldToLocal(m_Items[index-1].worldBound);
                m_DropBar.transform.position = new Vector2(0, itemRect.yMax);
            }
            else
            {
                var itemRect = this.WorldToLocal(m_Items[index].worldBound);
                m_DropBar.transform.position = new Vector2(0, itemRect.yMin);
            }
            m_DropBar.style.visibility = Visibility.Visible;
        }

        private int GetDropIndex(IMouseEvent e)
        {
            if (m_Items.Count <= 0)
                return -1;

            Vector2 worldPos = e.mousePosition;
            for (int i = 0; i < m_Items.Count; i++)
            {
                var itemRect = m_Items[i].worldBound;
                if (itemRect.Contains(worldPos))
                    return worldPos.y < itemRect.center.y ? i : i + 1;
            }

            if (worldPos.y < m_Items[0].worldBound.yMin)
                return 0;
            else if (worldPos.y > m_Items[m_Items.Count-1].worldBound.yMax)
                return m_Items.Count;

            return -1;
        }
    }
}