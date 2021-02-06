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
    /// Defines the way that an add button looks in a list control.
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

    // TODO Should we add a way to disable foldout binding?
    /// <summary>
    /// Defines the way that a list's header looks.
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
    /// A UIToolkit reorderable list for a given SerializedProperty. It's drawn inside a box by default. Put it inside a <see cref="Rebinder"/> for better performance.
    /// </summary>
    /// <example>
    /// Basic Usage:
    /// <code>
    /// class ACustomEditor : Editor
    /// {
    ///     public override VisualElement CreateInspectorGUI()
    ///     {
    ///         // ArrayPropertyFields don't need to be in a Rebinder, but it performs better.
    ///         var root = new Rebinder(serializedObject);
    ///         var list = new ArrayPropertyField(serializedObject.FindProperty("arrayField"));
    ///         root.Add(list);
    ///         return root;
    ///     }
    /// }
    /// </code>
    /// Configuration Options:
    /// <code>
    /// class ACustomEditor : Editor
    /// {
    ///     public override VisualElement CreateInspectorGUI()
    ///     {
    ///         // ArrayPropertyFields don't need to be in a Rebinder, but it performs better.
    ///         var root = new Rebinder(serializedObject);
    ///         var list = new ArrayPropertyField(serializedObject.FindProperty("field"));
    /// 
    ///         // Make it non-reorderable.
    ///         list.reorderable = false;
    ///         // Remove the box that is drawn around the list by default.
    ///         list.boxed = false;
    ///         // Set the header's text.
    ///         list.label = "custom label";
    ///         // Hide the header.
    ///         list.headerMode = ListHeaderMode.None;
    ///         // Set a custom empty message. Null to hide it.
    ///         list.emptyListMessage = null;
    ///         // Hide drag handles.
    ///         list.showDragHandles = false;
    ///         // Don't use zebra-like background to differentiate odd and even items.
    ///         list.showAlternatedBackgrounds = false;
    ///         // Hide remove buttons.
    ///         list.showRemoveButtons = false;
    ///         // Hide the add button.
    ///         list.addButtonMode = AddButtonMode.None;
    ///         // Customize remove behavior
    ///         list.onRemove = (int itemIndex) => { };
    ///         // Customize add behavior
    ///         list.onAdd = (Rect buttonPosition) => { };
    ///         // Show separator lines between items.
    ///         list.showSeparators = true;
    /// 
    ///         root.Add(list);
    ///         return root;
    ///     }
    /// }
    /// </code>
    /// A custom list with <see cref="ManagedReferenceField"/>s (with the <see cref="SerializeReference"/> attribute):
    /// <code>
    /// class ACustomEditor : Editor
    /// {
    ///     public override VisualElement CreateInspectorGUI()
    ///     {
    ///         // This time we do need a Rebinder for ManagedReferenceFields to work.
    ///         var root = new Rebinder(serializedObject);
    /// 
    ///         // "field" would be an array with the [SerializeReference] attribute.
    ///         var arrayProp = serializedObject.FindProperty("field");
    ///         var list = new ArrayPropertyField
    ///         (
    ///             arrayProp,
    ///             // The second constructor parameter is an optional item creation method.
    ///             // We use it to return a ManagedReferenceField, which will update the
    ///             // PropertyField if the backing property changes type. That can happen
    ///             // when the list is reordered, for example.
    ///             i => new ManagedReferenceField(arrayProp.GetArrayElementAtIndex(i))
    ///         );
    /// 
    ///         // Add a little arrow to the add button
    ///         list.addButtonMode = AddButtonMode.WithOptions;
    ///         // We need custom add functionality, otherwise it'll just add null objects.
    ///         list.onAdd = DisplayAddMenu;
    /// 
    ///         root.Add(list);
    ///         return root;
    ///     }
    /// 
    ///     private void DisplayAddMenu(Rect buttonPosition)
    ///     {
    ///         SerializedProperty array = serializedObject.FindProperty("field");
    ///         GenericMenu gm = new GenericMenu();
    /// 
    ///         gm.AddItem(new GUIContent("Add Object Type 1"), false, () =>
    ///         {
    ///             serializedObject.Update();
    /// 
    ///             array.arraySize++;
    ///             var lastItem = array.GetArrayElementAtIndex(array.arraySize - 1);
    ///             lastItem.managedReferenceValue = new ObjectType1();
    /// 
    ///             serializedObject.ApplyModifiedProperties();
    ///         });
    /// 
    ///         gm.AddItem(new GUIContent("Add Object Type 2"), false, () =>
    ///         {
    ///             serializedObject.Update();
    /// 
    ///             array.arraySize++;
    ///             var lastItem = array.GetArrayElementAtIndex(array.arraySize - 1);
    ///             lastItem.managedReferenceValue = new ObjectType2();
    /// 
    ///             serializedObject.ApplyModifiedProperties();
    ///         });
    /// 
    ///         gm.DropDown(buttonPosition);
    ///     }
    /// }
    /// </code>
    /// </example>
    public class ArrayPropertyField : ListControl
    {
        /// <summary> USS class name of elements of this type. </summary>
        public new static readonly string ussClassName = "editor-aid-array-property-field";
        /// <summary> USS class name of the add button. </summary>
        public static readonly string addButtonUssClassName = "editor-aid-list-control__add-button";
        /// <summary> USS class name of the list's foldout. </summary>
        public static readonly string headerFoldoutUssClassName = "editor-aid-list-control__header-foldout";
        /// <summary> USS class name of the list's label. </summary>
        public static readonly string headerLabelUssClasName = "editor-aid-list-control__header-label";

        private readonly SerializedProperty m_ArrayProp;
        //TODO It seems we could use the size tracker as a local variable.
        private readonly ValueTracker<int> m_SizeTracker = new ValueTracker<int>();
        private readonly Button m_AddButton = new Button();
        private readonly Label m_HeaderLabel = new Label();
        private readonly Foldout m_HeaderFoldout = new Foldout();
        private Func<int, VisualElement> m_MakeItem;
        private IRebinder m_AncestorRebinder;

        private ListHeaderMode m_ListHeaderMode;
        private string m_label = null;
        private AddButtonMode m_AddButtonMode;
        private bool m_ShowRemoveButtons = true;
        private bool m_ShowDragHandles = true;
        private bool m_ShowAlternatedBackgrounds = true;
        private bool m_ShowSeparators = false;

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

        /// <summary> The way to display the list's header. Default <see cref="ListHeaderMode.Foldout"/></summary>
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

        /// <summary> The way to display the add button. Default <see cref="AddButtonMode.Simple"/></summary>
        public AddButtonMode addButtonMode
        {
            get => m_AddButtonMode;
            set
            {
                m_AddButtonMode = value;
                SetFooterVisibility(value != AddButtonMode.None);
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
            get => m_ShowSeparators;
            set
            {
                m_ShowAlternatedBackgrounds = value;
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
        /// ArrayPropertyField Constructor. You still have to call Bind for it work. Also, only bind to the Object that owns the arrayProp to avoid ugly problems.
        /// </summary>
        /// <param name="arrayProp"> A SerializedProperty that points to an array or a list</param>
        /// <param name="makeItem"> Set this callback to make custom list items. It receives the item's index as a parameter.</param>
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
            m_HeaderLabel.AddToClassList(headerLabelUssClasName);
            m_HeaderFoldout.AddToClassList(headerFoldoutUssClassName);
            m_HeaderFoldout.bindingPath = arrayProp.propertyPath;
            m_HeaderFoldout.SetValueWithoutNotify(arrayProp.isExpanded);
            m_HeaderFoldout.RegisterValueChangedCallback(e =>
            {
                if (headerMode == ListHeaderMode.Foldout)
                    EnableInClassList(foldedUssClassName, !e.newValue);
            });

            var headerContent = new VisualElement();
            headerContent.Add(m_HeaderFoldout);
            headerContent.Add(m_HeaderLabel);
            SetHeaderContent(headerContent);
            headerMode = ListHeaderMode.Foldout;

            m_AddButton.AddToClassList(addButtonUssClassName);
            m_AddButton.clicked += () => AddItem(m_AddButton.worldBound);
            SetFooterContent(m_AddButton);
            addButtonMode = AddButtonMode.Simple;

            var sizeProp = m_ArrayProp.FindPropertyRelative("Array.size");
            m_SizeTracker.SetUp(sizeProp, OnSizeChange, sizeProp.intValue);
            Add(m_SizeTracker);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            SetListSize(m_ArrayProp.arraySize);
        }

        protected override bool IsReorderable() => reorderable;

        protected override void OnReorderDragPerformed(int draggedIndex, int dropIndex)
        {
            ListControlUtils.ReorderSerializedArray(m_ArrayProp, draggedIndex, dropIndex);
        }

        protected override VisualElement CreateItemForIndex(int index)
        {
            VisualElement content = m_MakeItem?.Invoke(index) ?? new PropertyField(m_ArrayProp.GetArrayElementAtIndex(index));

            ListRow item = new ListRow(this, content, index, RemoveItem);
            item.SetFeatures(showRemoveButtons, showDragHandles, showAlternatedBackgrounds, showSeparators);
            item.AddManipulator(new ContextualMenuManipulator(e => PopulateMenu(e, item.content, index)));

            return item;
        }

        private void OnAttachToPanel(AttachToPanelEvent e)
        {
            m_AncestorRebinder = GetFirstAncestorOfType<IRebinder>();
            if (m_AncestorRebinder != null && m_AncestorRebinder.serializedObject != m_ArrayProp.serializedObject)
                m_AncestorRebinder = null;
        }

        private void Rebind()
        {
            if (m_AncestorRebinder != null)
                m_AncestorRebinder.RequestRebind();
            else
                this.Bind(m_ArrayProp.serializedObject);
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
            // In case the SerializedProperty hasn't been updated yet. It happens when seting a field's value directly.
            m_ArrayProp.arraySize = e.newValue;
            m_ArrayProp.serializedObject.ApplyModifiedProperties();
            SetListSize(m_ArrayProp.arraySize);

            if (GetListSize() > prevListSize)
                Rebind();
        }

        private void RemoveItem(int index)
        {
            if (onRemove != null)
                onRemove(index);
            else
            {
                m_ArrayProp.serializedObject.Update();
                if (index < 0 || index >= m_ArrayProp.arraySize)
                    return;
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