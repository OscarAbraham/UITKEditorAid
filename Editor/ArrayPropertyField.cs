using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using ArteHacker.UITKEditorAid.Utils;
using Object = UnityEngine.Object;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// Defines the way that an add button looks in a list.
    /// <para>
    /// See also: <see cref="ArrayPropertyField.addButtonMode"/>
    /// </para>
    /// </summary>
    public enum AddButtonMode
    {
        /// <summary> Hide add button. </summary>
        None = 0,
        /// <summary> Show a normal add button. </summary>
        Simple = 1,
        /// <summary> Show an add button with a little arrow. </summary>
        WithOptions = 2
    }

    // CONSIDER: Should we add a way to disable foldout binding?
    /// <summary>
    /// Defines behavior of a list header.
    /// <para>
    /// See also: <see cref="ArrayPropertyField.headerMode"/>
    /// </para>
    /// </summary>
    public enum ListHeaderMode
    {
        /// <summary>Hide the header.</summary>
        None = 0,
        /// <summary>Show a label in the header.</summary>
        Label = 1,
        /// <summary> Show a foldout in the header.</summary>
        Foldout = 2
    }

    /// <summary>
    /// Control for a serialized Array or List. It has many customization options.
    /// </summary>
    /// 
    /// <remarks>
    /// UI Toolkit's <see cref="ListView"/> covers many of ArrayPropertyField's use cases, but you may
    /// prefer this element sometimes. Use ArrayPropertyField when you need to go around bugs in ListView,
    /// or when you need the customization options. Use ListView when you need to take advantage of its
    /// virtualization system to handle thousands of elements with good performance.
    /// <para>
    /// More customizations are available by subclassing this element. See ArrayPropertyField's 
    /// <see cref="ListControl">parent class</see> for some of them (for example, to handle custom mouse drops).
    /// </para>
    /// </remarks>
    /// 
    /// <example>
    /// Basic Usage:
    /// <code language="csharp"><![CDATA[
    /// class ACustomEditor : Editor
    /// {
    ///     public override VisualElement CreateInspectorGUI()
    ///     {
    ///         var root = new VisualElement();
    ///         var list = new ArrayPropertyField(serializedObject.FindProperty("arrayField"));
    ///         root.Add(list);
    ///         return root;
    ///     }
    /// }
    /// ]]></code>
    /// Configuration Options:
    /// <code language="csharp"><![CDATA[
    /// class ACustomEditor : Editor
    /// {
    ///     public override VisualElement CreateInspectorGUI()
    ///     {
    ///         var root = new VisualElement();
    ///         var list = new ArrayPropertyField(serializedObject.FindProperty("field"));
    /// 
    ///         list.reorderable = false; // Make it non-reorderable.
    ///         list.boxed = false; // Remove the box around the list.
    /// 
    ///         list.label = "custom label"; // Set the header's text
    ///         list.headerMode = ListHeaderMode.Foldout; // Use a foldout in the header.
    ///         list.headerMode = ListHeaderMode.Label; // Use a label in the header.
    ///         list.headerMode = ListHeaderMode.None; // Hide the header.
    /// 
    ///         list.emptyListMessage = null; // Message for empty lists. Null hides it.
    ///         list.showDragHandles = false; // Hide drag handles.
    ///         list.showAlternatedBackgrounds = false; // Remove zebra-like backgrounds.
    ///         list.showSeparators = true; // Show separator lines between items.
    /// 
    ///         // Add a little arrow to the add button.
    ///         list.addButtonMode = AddButtonMode.WithOptions;
    ///         // Hide the add button in the footer.
    ///         list.addButtonMode = AddButtonMode.None;
    ///         // Hide the remove buttons on each item.
    ///         list.showRemoveButtons = false;
    /// 
    ///         // Show a button in the footer to remove the selected or the last item.
    ///         list.showMainRemoveButton = true;
    ///         // Enable item selection. Works great with the previous property.
    ///         list.supportItemSelection = true;
    ///         
    ///         list.onAdd = (Rect buttonPosition) => { }; // Customize add behavior.
    ///         list.onRemove = (int itemIndex) => { }; // Customize remove behavior.
    /// 
    ///         root.Add(list);
    ///         return root;
    ///     }
    /// }
    /// ]]></code>
    /// A custom example with <see cref="ManagedReferenceField">ManagedReferenceFields</see> for lists/arrays
    /// that use the <see cref="SerializeReference"/> attribute:
    /// <code language="csharp"><![CDATA[
    /// [System.Serializable] class ReferenceType1 { }
    /// 
    /// [System.Serializable] class ReferenceType2 { }
    /// 
    /// class ReferencesArrayContainer : ScriptableObject
    /// {
    ///     [SerializeReference]
    ///     object[] m_ReferencesArray;
    /// }
    /// 
    /// [CustomEditor(typeof(ReferencesArrayContainer))]
    /// class ReferencesArrayContainerEditor : Editor
    /// {
    ///     public override VisualElement CreateInspectorGUI()
    ///     {
    ///         var root = new VisualElement();
    /// 
    ///         var arrayProperty = serializedObject.FindProperty("m_ReferencesArray");
    ///         var list = new ArrayPropertyField
    ///         (
    ///             arrayProperty,
    ///             // This constructor parameter is an optional item creation method.
    ///             // We use it to return a ManagedReferenceField, which will update the
    ///             // item's UI if the backing property changes type.
    ///             i => new ManagedReferenceField(arrayProperty.GetArrayElementAtIndex(i))
    ///         );
    /// 
    ///         // Add a little arrow to the add button.
    ///         list.addButtonMode = AddButtonMode.WithOptions;
    ///         // We need custom add functionality, otherwise it'll just add null items.
    ///         list.onAdd = DisplayAddMenu;
    /// 
    ///         root.Add(list);
    ///         return root;
    ///     }
    /// 
    ///     private void DisplayAddMenu(Rect buttonPosition)
    ///     {
    ///         var menu = new GenericMenu();
    /// 
    ///         menu.AddItem(
    ///             new GUIContent("Add Reference Type 1"),
    ///             false,
    ///             () => AddItemToArray(new ReferenceType1()));
    /// 
    ///         menu.AddItem(
    ///             new GUIContent("Add Reference Type 2"),
    ///             false,
    ///             () => AddItemToArray(new ReferenceType2()));
    /// 
    ///         menu.DropDown(buttonPosition);
    ///     }
    /// 
    ///     private void AddItemToArray(object newItem)
    ///     {
    ///         serializedObject.Update();
    ///         var arrayProperty = serializedObject.FindProperty("m_ReferencesArray");
    /// 
    ///         arrayProperty.arraySize++;
    ///         var newItemProperty =
    ///             arrayProperty.GetArrayElementAtIndex(arrayProperty.arraySize - 1);
    ///         newItemProperty.managedReferenceValue = newItem;
    /// 
    ///         serializedObject.ApplyModifiedProperties();
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    public class ArrayPropertyField : ListControl
    {
        /// <summary> USS class name of elements of this type. </summary>
        public new static readonly string ussClassName = "editor-aid-array-property-field";
        /// <summary> USS class name of Lists with separators. </summary>
        public static readonly string withSeparatorsUSSClassName = "editor-aid-list-control--with-separators";
        /// <summary> USS class name of the footer content. </summary>
        public static readonly string footerContentUssClassName = "editor-aid-list-control__footer-content";
        /// <summary> USS class name of the add button. </summary>
        public static readonly string addButtonUssClassName = "editor-aid-list-control__add-button";
        /// <summary> USS class name of the remove button. </summary>
        public static readonly string removeButtonUssClassName = "editor-aid-list-control__remove-button";
        /// <summary> USS class name of the element that contains the header's label or foldout. </summary>
        public static readonly string headerContentUssClassName = "editor-aid-list-control__header-content";
        /// <summary> USS class name of the list's foldout. </summary>
        public static readonly string headerFoldoutUssClassName = "editor-aid-list-control__header-foldout";
        /// <summary> USS class name of the list's label. </summary>
        public static readonly string headerLabelUssClassName = "editor-aid-list-control__header-label";

        private readonly SerializedProperty m_ArrayProp;
        private readonly Button m_AddButton = new Button();
        private readonly Button m_RemoveButton = new Button
        {
            style = 
            {
                display = DisplayStyle.None,
                backgroundImage = EditorGUIUtility.IconContent("Toolbar Minus").image as Texture2D
            }
        };
        private readonly Label m_HeaderLabel = new Label();
        private readonly Foldout m_HeaderFoldout = new Foldout();
        private Func<int, VisualElement> m_MakeItem;

        private ListHeaderMode m_ListHeaderMode;
        private string m_label = null;
        private AddButtonMode m_AddButtonMode;
        private bool m_ShowRemoveButtons = true;
        private bool m_ShowDragHandles = true;
        private bool m_ShowAlternatedBackgrounds = true;

        /// <summary> Whether list items can be reordered by dragging. </summary>
        public bool reorderable { get; set; } = true;

        /// <summary> The text to show in the header. Set it to null to show the SerializedProperty's name.</summary>
        public string label
        {
            get
            {
                if(string.IsNullOrEmpty(m_label))
                    return m_ArrayProp.displayName;
                return m_label;
            }
            set
            {
                m_label = value;
                m_HeaderLabel.text = label;
                m_HeaderFoldout.text = label;
            }
        }

        /// <summary> The way to display the list's header. Default <see cref="ListHeaderMode.Foldout"/>.</summary>
        public ListHeaderMode headerMode
        {
            get => m_ListHeaderMode;
            set
            {
                if (m_ListHeaderMode == value) return;
                m_ListHeaderMode = value;
                SetHeaderVisibility(m_ListHeaderMode != ListHeaderMode.None);

                switch (m_ListHeaderMode)
                {
                    case ListHeaderMode.None:
                        m_HeaderLabel.style.display = DisplayStyle.None;
                        m_HeaderFoldout.style.display = DisplayStyle.None;
                        RemoveFromClassList(foldedUssClassName);
                        break;
                    case ListHeaderMode.Label:
                        m_HeaderLabel.style.display = DisplayStyle.Flex;
                        m_HeaderFoldout.style.display = DisplayStyle.None;
                        RemoveFromClassList(foldedUssClassName);
                        break;
                    case ListHeaderMode.Foldout:
                        m_HeaderLabel.style.display = DisplayStyle.None;
                        m_HeaderFoldout.style.display = DisplayStyle.Flex;
                        EnableInClassList(foldedUssClassName, !m_HeaderFoldout.value);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary> The way to display the add button. Default <see cref="AddButtonMode.Simple"/>.</summary>
        public AddButtonMode addButtonMode
        {
            get => m_AddButtonMode;
            set
            {
                m_AddButtonMode = value;
                m_AddButton.style.display = value != AddButtonMode.None ? DisplayStyle.Flex : DisplayStyle.None;

                switch (value)
                {
                    case AddButtonMode.Simple:
                        m_AddButton.style.backgroundImage = EditorGUIUtility.IconContent("Toolbar Plus").image as Texture2D;
                        break;
                    case AddButtonMode.WithOptions:
                        m_AddButton.style.backgroundImage = EditorGUIUtility.IconContent("Toolbar Plus More").image as Texture2D;
                        break;
                    case AddButtonMode.None:
                    default:
                        break;
                }

                SetFooterVisibility(value != AddButtonMode.None || showMainRemoveButton);
            }
        }

        /// <summary>
        /// Whether to show a footer button to remove selected items from the list. If nothing is selected,
        /// it removes the last item. Set <see cref="ListControl.supportItemSelection"/> true to enable item selection.
        /// </summary>
        public bool showMainRemoveButton
        {
            get => m_RemoveButton.style.display == DisplayStyle.Flex;
            set
            {
                m_RemoveButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                SetFooterVisibility(value || addButtonMode != AddButtonMode.None);
            }
        }

        /// <summary> Whether to show buttons to remove list items. Default true.</summary>
        public bool showRemoveButtons
        {
            get => m_ShowRemoveButtons;
            set
            {
                m_ShowRemoveButtons = value;
                ForEachItem(item => item.SetRemoveButton(value));
            }
        }

        /// <summary> Whether to show handles in the list's items. Default true.</summary>
        public bool showDragHandles
        {
            get => m_ShowDragHandles;
            set
            {
                m_ShowDragHandles = value;
                ForEachItem(item => item.SetDragHandle(value));
            }
        }

        /// <summary> Whether to show backgrounds to differentiate between even and odd items. Default true.</summary>
        public bool showAlternatedBackgrounds
        {
            get => m_ShowAlternatedBackgrounds;
            set
            {
                m_ShowAlternatedBackgrounds = value;
                ForEachItem(item => item.SetAlternatedBackground(value));
            }
        }

        /// <summary> Whether to show lines and small paddings between items. Default false.</summary>
        public bool showSeparators
        {
            get => ClassListContains(withSeparatorsUSSClassName);
            set
            {
                EnableInClassList(withSeparatorsUSSClassName, value);
                ForEachItem(item => item.SetSeparators(value));
            }
        }

        /// <summary> Set this callback to override what happens when clicking a remove button. It gets passed the item's index.</summary>
        public Action<int> onRemove { get; set; }

        /// <summary> Set this callback to override what happens when clicking the add button. It gets passed the button's position.</summary>
        public Action<Rect> onAdd { get; set; }

        /// <summary> Set this callback to populate context menu for each list item. It gets passed the event, the item's context and its index. </summary>
        public Action<ContextualMenuPopulateEvent, VisualElement, int> onPopulateItemMenu { get; set; }

        /// <summary>
        /// Constructor. It receives a list/array <see cref="SerializedProperty"/> and an optional makeItem delegate.
        /// It still needs to be bound to work properly.
        /// </summary>
        /// <param name="arrayProp"> A SerializedProperty that points to an array or a list</param>
        /// <param name="makeItem"> Optional callback to make custom list items. It receives the item's index as a parameter.</param>
        public ArrayPropertyField(SerializedProperty arrayProp, Func<int, VisualElement> makeItem = null)
        {
            AddToClassList(ussClassName);
            boxed = true;

            if (arrayProp == null || !arrayProp.serializedObject.targetObject || !arrayProp.isArray || arrayProp.propertyType == SerializedPropertyType.String)
            {
                Debug.LogError("arrayProp must be a valid SerializedProperty that points to an array or a list");
                return;
            }
            m_ArrayProp = arrayProp;
            m_MakeItem = makeItem;
            viewDataKey = arrayProp.serializedObject.targetObject.GetInstanceID() + "_" + arrayProp.propertyPath;

            label = null;
            m_HeaderLabel.AddToClassList(headerLabelUssClassName);
            m_HeaderFoldout.AddToClassList(headerFoldoutUssClassName);
            m_HeaderFoldout.bindingPath = arrayProp.propertyPath;
            m_HeaderFoldout.SetValueWithoutNotify(arrayProp.isExpanded);
            m_HeaderFoldout.RegisterValueChangedCallback(e =>
            {
                if (headerMode == ListHeaderMode.Foldout)
                    EnableInClassList(foldedUssClassName, !e.newValue);
            });

            var headerContent = new VisualElement();
            headerContent.AddToClassList(headerContentUssClassName);
            headerContent.Add(m_HeaderFoldout);
            headerContent.Add(m_HeaderLabel);
            SetHeaderContent(headerContent);
            headerMode = ListHeaderMode.Foldout;

            var footerContent = new VisualElement();
            footerContent.AddToClassList(footerContentUssClassName);
            SetFooterContent(footerContent);

            m_AddButton.AddToClassList(addButtonUssClassName);
            m_AddButton.clicked += () => AddItem(m_AddButton.worldBound);
            footerContent.Add(m_AddButton);
            addButtonMode = AddButtonMode.Simple;

            m_RemoveButton.AddToClassList(removeButtonUssClassName);
            m_RemoveButton.clicked += RemoveSelectedItem;
            footerContent.Add(m_RemoveButton);

            var sizeProp = m_ArrayProp.FindPropertyRelative("Array.size");
            Add(new ValueTracker<int>(sizeProp, OnSizeChange, sizeProp.intValue) { name = "Size Tracker" });

            SetListSize(m_ArrayProp.arraySize);
        }

        [RemoveFromDocs]
        protected override bool IsReorderable() => reorderable;

        [RemoveFromDocs]
        protected override void OnReorderDragPerformed(int draggedIndex, int dropIndex)
        {
            ListControlUtils.ReorderSerializedArray(m_ArrayProp, draggedIndex, dropIndex);
            selectedItem = dropIndex > draggedIndex ? dropIndex - 1 : dropIndex;
        }

        [RemoveFromDocs]
        protected override VisualElement CreateItemForIndex(int index)
        {
            VisualElement content = m_MakeItem?.Invoke(index) ?? new PropertyField(m_ArrayProp.GetArrayElementAtIndex(index));

            ListRow item = new ListRow(this, content, index, RemoveItem);
            item.SetFeatures(showRemoveButtons, showDragHandles, showAlternatedBackgrounds, showSeparators);
            item.AddManipulator(new ContextualMenuManipulator(e => PopulateMenu(e, item.content, index)));

            return item;
        }

        private void ForEachItem(Action<ListRow> action)
        {
            for (int i = 0; i < GetListSize(); i++)
            {
                var item = GetListItem(i) as ListRow;
                if (item != null)
                    action(item);
            }
        }

        private void OnSizeChange(ChangeEvent<int> e)
        {
            int prevListSize = GetListSize();
            SetListSize(m_ArrayProp.arraySize);

            // NOTE: Should we bind only the added elements for better performance?
            if (GetListSize() > prevListSize)
                this.Bind(m_ArrayProp.serializedObject);
        }

        private void RemoveSelectedItem()
        {
            var index = selectedItem;
            var size = GetListSize();

            if (index < 0 || index >= size)
                index = size - 1;

            if (index >= 0)
                RemoveItem(index);
        }

        private void RemoveItem(int index)
        {
            if (onRemove != null)
                onRemove(index);
            else
            {
                m_ArrayProp.serializedObject.Update();

                int initialArraySize = m_ArrayProp.arraySize;
                if (index < 0 || index >= initialArraySize)
                    return;

                m_ArrayProp.DeleteArrayElementAtIndex(index);
                // When items are object references and they are not null, deleting them just sets them to null.
                // This is fixed in 2021, but we need to fix it ourselves for previous versions.
                if(m_ArrayProp.arraySize == initialArraySize)
                    m_ArrayProp.DeleteArrayElementAtIndex(index);

                m_ArrayProp.serializedObject.ApplyModifiedProperties();
            }
        }

        private void AddItem(Rect buttonRect)
        {
            if (onAdd != null)
                onAdd(buttonRect);
            else
            {
                m_ArrayProp.serializedObject.Update();
                m_ArrayProp.arraySize++;
                m_ArrayProp.serializedObject.ApplyModifiedProperties();
            }
        }

        private void PopulateMenu(ContextualMenuPopulateEvent evt, VisualElement itemContent, int itemIndex)
        {
            onPopulateItemMenu?.Invoke(evt, itemContent, itemIndex);
        }
    }
}