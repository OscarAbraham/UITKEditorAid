using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Presets;
using UnityEditorInternal;
using ArteHacker.UITKEditorAid.Manipulators;
using ArteHacker.UITKEditorAid.Utils;
using Object = UnityEngine.Object;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// A UI Toolkit list similar to the component list in a GameObject's inspector. It's typically used with a serialized list or
    /// array of ScriptableObject. It doesn't do well with multitarget SerializedObjects and Editors. Creating a derived class from
    /// this one allows for some customizations.
    /// </summary>
    /// <example>
    /// <code>
    /// class CustomEditorWithListOfInspectors : Editor
    /// {
    ///     public override VisualElement CreateInspectorGUI()
    ///     {
    ///         var root = new VisualElement();
    /// 
    ///         // The mylistOfObjects field should be a list or array of elements that derive from UnityEngine.Object.
    ///         // Usually, it's a list of ScriptableObjects that are embedded as subassets in this Editor's target.
    ///         var listOfObjectsProperty = serializedObject.FindProperty("mylistOfObjects");
    ///         var listOfInspectors = new MyListOfInspectors(listOfObjectsProperty);
    ///         root.Add(listOfInspectors);
    /// 
    ///         return root;
    ///     }
    /// }
    /// 
    /// // Creating a child class of ListOfInspectors allows for some customization:
    /// class MyListOfInspectors : ListOfInspectors
    /// {
    ///     public MyListOfInspectors(SerializedProperty arrayProp) : base(arrayProp) { }
    /// 
    ///     // Override this method to add elements to an inspector header before its label. 
    ///     protected override void AddPrelabelHeaderElements(VisualElement header, int itemIndex, SerializedObject serializedObject)
    ///     {
    ///         base.AddPrelabelHeaderElements(header, itemIndex, serializedObject);
    ///     }
    /// 
    ///     // Override this method to change how a label is added to an inspector header.
    ///     // By default, it adds an editable label that allows changing the object's name.
    ///     protected override void AddHeaderLabel(VisualElement header, int itemIndex, SerializedObject serializedObject)
    ///     {
    ///         base.AddHeaderLabel(header, itemIndex, serializedObject);
    ///     }
    /// 
    ///     // Override this method to add elements to an inspector header after its label.
    ///     protected override void AddPostlabelHeaderElements(VisualElement header, int itemIndex, SerializedObject serializedObject)
    ///     {
    ///         base.AddPostlabelHeaderElements(header, itemIndex, serializedObject);
    ///     }
    /// 
    ///     // Override this method to add context menu items for an inspector header.
    ///     protected override void AddItemsToContextMenu(GenericMenu menu, VisualElement header, int itemIndex, SerializedObject serializedObject)
    ///     {
    ///         base.AddItemsToContextMenu(menu, header, itemIndex, serializedObject);
    ///         menu.AddItem(new GUIContent("My Custom Menu Item"), true, () => { /*Do Something*/});
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// This class is really an internal tool that's probably not needed by most people, but I figured it might help somebody.
    /// It requires a whole lotta other code that's not related to UIToolkit to be useful. I use it with assets that have
    /// subassets inside (<see cref="AssetDatabase.AddObjectToAsset(Object, Object)"/>). Most worthwhile usages of this element
    /// require a careful management of assets that's outside the scope of this package. If you're just looking for a way to embed
    /// inspectors from other Objects in your UI, you're probably better off looking at <see cref="InspectorElement"/>.
    /// 
    /// <para>
    /// The inspectors emulate the behavior of Component lists in GameObjects, with some differences and extra features.
    /// They will detect the presence of a <see cref="TooltipAttribute"/> in an Object and show it when hovering the Help button.
    /// By default, the header's label is an <see cref="EditableLabel"/> that allows changing the Object's name. The list will
    /// try to detect when an Object's script is missing and show dedicated UI for it; to support this, make sure the base type
    /// of the backing Object array is constructable by Unity (i.e. not abstract and not generic).
    /// </para>
    /// </remarks>
    public class ListOfInspectors : ListControl
    {
        /// <summary> USS class name of elements of this type. </summary>
        public new static readonly string ussClassName = "editor-aid-list-of-inspectors";
        /// <summary> USS class name of the items in the list. </summary>
        public static readonly string inspectorItemUssClassName = "editor-aid-list-of-inspectors__inspector-item";
        /// <summary> USS class name of the inspector headers. </summary>
        public static readonly string itemHeaderUssClassName = "editor-aid-list-of-inspectors__item-header";
        /// <summary> USS class name of collapsed inspector headers. </summary>
        public static readonly string itemHeaderCollapsedUssClassName = "editor-aid-list-of-inspectors__item-header--collapsed";
        /// <summary> USS class name of inspector header foldouts. </summary>
        public static readonly string itemHeaderFoldoutUssClassName = "editor-aid-list-of-inspectors__item-header-foldout";
        /// <summary> USS class name of inspector header labels. </summary>
        public static readonly string itemHeaderLabelUssClassName = "editor-aid-list-of-inspectors__item-header-label";
        /// <summary> USS class name of inspector header buttons. </summary>
        public static readonly string itemHeaderButtonUssClassName = "editor-aid-list-of-inspectors__item-header-button";
        /// <summary> USS class name of the inspector header icon. </summary>
        public static readonly string itemHeaderIconUssClassName = "editor-aid-list-of-inspectors__item-header-icon";
        [Obsolete("There's no custom tooltip element for item headers anymore.")]
        public static readonly string itemHeaderTooltipUssClassName = "editor-aid-list-control__item-header-tooltip";

        private static readonly Action<GenericMenu, Rect, Object[], int> s_ShowContextMenu;

        private readonly SerializedProperty m_ArrayProp;
        private readonly VisualElement m_TrackersContainer = new VisualElement();
        // CONSIDER: It seems we could use the size tracker as a local variable.
        private readonly ValueTracker<int> m_SizeTracker = new ValueTracker<int>();

        static ListOfInspectors()
        {
            MethodInfo contextMenuMethod = typeof(GenericMenu).GetMethod("ObjectContextDropDown",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null,
                new Type[] { typeof(Rect), typeof(Object[]), typeof(int)}, null);

            if (contextMenuMethod != null)
                s_ShowContextMenu = (Action<GenericMenu, Rect, Object[], int>)Delegate.CreateDelegate(typeof(Action<GenericMenu, Rect, Object[], int>), contextMenuMethod, false);
        }

        /// <summary>
        /// ListOfInspectors constructor.
        /// </summary>
        /// <param name="arrayProp">A serialized property that represents an array of Unity Objects</param>
        public ListOfInspectors(SerializedProperty arrayProp) : base()
        {
            AddToClassList(ussClassName);
            styleSheets.Add(EditorAidResources.listOfInspectorsStyle);
            m_TrackersContainer.style.display = DisplayStyle.None;

            if (arrayProp == null || !arrayProp.isArray || arrayProp.propertyType == SerializedPropertyType.String)
            {
                Debug.LogError("arrayProp must be a valid SerializedProperty that points to an array or a list of object references");
                return;
            }

            m_ArrayProp = arrayProp;

            var sizeProp = m_ArrayProp.FindPropertyRelative("Array.size");
            m_SizeTracker.SetUp(sizeProp, OnSizeChange, sizeProp.intValue);
            m_TrackersContainer.Add(m_SizeTracker);

            Add(m_TrackersContainer);
            SetListSize(m_ArrayProp.arraySize);

            BindTrackers();
        }

        private void OnSizeChange(ChangeEvent<int> e)
        {
            int prevListSize = GetListSize();
            SetListSize(m_ArrayProp.arraySize);
            if (GetListSize() > prevListSize)
                BindTrackers();
        }

        private void BindTrackers()
        {
            m_TrackersContainer.Bind(m_ArrayProp.serializedObject);
        }

        /// <summary>
        /// Override this method in a child class to customize inspector headers as a whole.
        /// </summary>
        /// <param name="itemIndex">The index in the list</param>
        /// <param name="serializedObject">The inspected object</param>
        /// <param name="inspector">The inspector element under the header.</param>
        /// <returns>The created header.</returns>
        protected virtual VisualElement CreateHeader(int itemIndex, SerializedObject serializedObject, InspectorElement inspector)
        {
            var target = serializedObject.targetObject;
            var header = new Disabler(() => !serializedObject.IsEditable()) { viewDataKey = $"Header{target.GetInstanceID()}" };
            var headerContainer = header.contentContainer;
            headerContainer.AddToClassList(itemHeaderUssClassName);

            bool wasExpanded = InternalEditorUtility.GetIsInspectorExpanded(target);
            SetItemExpanded(wasExpanded, headerContainer, target, inspector);

            var foldout = AddHeaderFoldout(headerContainer, wasExpanded);
            foldout.RegisterValueChangedCallback(e => SetItemExpanded(e.newValue, headerContainer, target, inspector));

            header.AddManipulator(new DragAndClickManipulator
            {
                onStartDragging = () => StartDraggingItem(itemIndex),
                onClick = () => foldout.value = !foldout.value
            });
            header.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == 1)
                    ShowInspectorContextMenu(new Rect(e.mousePosition, default), headerContainer, itemIndex, serializedObject);
            });

            AddPrelabelHeaderElements(headerContainer, itemIndex, serializedObject);
            AddHeaderLabel(headerContainer, itemIndex, serializedObject);
            AddPostlabelHeaderElements(headerContainer, itemIndex, serializedObject);

            return header;
        }

        private void SetItemExpanded(bool expanded, VisualElement header, Object target, InspectorElement inspector)
        {
            inspector.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            header.EnableInClassList(itemHeaderCollapsedUssClassName, !expanded);
            InternalEditorUtility.SetIsInspectorExpanded(target, expanded);
        }

        private static Foldout AddHeaderFoldout(VisualElement header, bool expanded)
        {
            var foldout = new Foldout { pickingMode = PickingMode.Ignore, value = expanded };
            foldout.Query().Descendents<VisualElement>().ForEach(el => el.pickingMode = PickingMode.Ignore);
            foldout.AddToClassList(itemHeaderFoldoutUssClassName);
            header.Add(foldout);
            return foldout;
        }

        /// <summary>
        /// Override this method to customize the elements before the header's label.
        /// </summary>
        /// <param name="header">The header</param>
        /// <param name="itemIndex">The index in the list</param>
        /// <param name="serializedObject">The inspected object</param>
        protected virtual void AddPrelabelHeaderElements(VisualElement header, int itemIndex, SerializedObject serializedObject)
        {
            Image icon = new Image { image = AssetPreview.GetMiniThumbnail(serializedObject.targetObject) };
            icon.AddToClassList(itemHeaderIconUssClassName);
            header.Add(icon);
        }

        /// <summary>
        /// Override this method to customize the header's label.
        /// </summary>
        /// <param name="header">The header</param>
        /// <param name="itemIndex">The index in the list</param>
        /// <param name="serializedObject">The inspected object</param>
        protected virtual void AddHeaderLabel(VisualElement header, int itemIndex, SerializedObject serializedObject)
        {
            var label = new EditableLabel { bindingPath = "m_Name", isDelayed = true };
            label.AddToClassList(itemHeaderLabelUssClassName);
            label.editOnDoubleClick = false;
            label.emptyTextLabel = ObjectNames.NicifyVariableName(serializedObject.targetObject.GetType().Name);
            header.Add(label);
            header.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.altKey && e.button == 0)
                    label.BeginEditing();
            });
        }

        /// <summary>
        /// Override this method to customize the elements after the header's label.
        /// </summary>
        /// <param name="header">The header</param>
        /// <param name="itemIndex">The index in the list</param>
        /// <param name="serializedObject">The inspected object</param>
        protected virtual void AddPostlabelHeaderElements(VisualElement header, int itemIndex, SerializedObject serializedObject)
        {
            var target = serializedObject.targetObject;
            var targetType = target.GetType();
            // Check for attribute because Help.HasHelpForObject always returns true for most custom objects.
            bool hasHelp = Attribute.IsDefined(targetType, typeof(HelpURLAttribute));
            bool hasTooltip = Attribute.IsDefined(targetType, typeof(TooltipAttribute));

            var help = new Button();
            help.AddToClassList(itemHeaderButtonUssClassName);
            help.style.backgroundImage = EditorGUIUtility.IconContent("_Help").image as Texture2D;
            help.SetEnabled(hasHelp || hasTooltip);
            if (hasHelp)
            {
                help.tooltip = $"Open Help for {targetType.Name}.";
                help.clicked += () => Help.ShowHelpForObject(target);
            }
            if (hasTooltip)
            {
                var tooltipAttr = (TooltipAttribute)Attribute.GetCustomAttributes(targetType, typeof(TooltipAttribute))[0];
                help.tooltip = tooltipAttr.tooltip;
            }
            header.Add(help);

            if (new PresetType(target).IsValid() && (target.hideFlags & HideFlags.NotEditable) == 0)
            {
                var presets = new Button();
                presets.AddToClassList(itemHeaderButtonUssClassName);
                presets.style.backgroundImage = EditorGUIUtility.IconContent("Preset.Context").image as Texture2D;
                presets.clicked += () => ShowPresetSelector(serializedObject);
                header.Add(presets);
            }

            var settings = new Button();
            settings.AddToClassList(itemHeaderButtonUssClassName);
            settings.style.backgroundImage = EditorGUIUtility.IconContent("_Menu").image as Texture2D;
            settings.clicked += () => ShowInspectorContextMenu(settings.worldBound, header, itemIndex, serializedObject);
            header.Add(settings);
        }

        /// <summary>
        /// Override this method to add custom menu items to the header's context menu.
        /// </summary>
        /// <param name="menu">The context menu</param>
        /// <param name="header">The header</param>
        /// <param name="itemIndex">The index in the list</param>
        /// <param name="serializedObject">The inspected object</param>
        protected virtual void AddItemsToContextMenu(GenericMenu menu, VisualElement header, int itemIndex, SerializedObject serializedObject) { }

        private void ShowInspectorContextMenu(Rect position, VisualElement header, int itemIndex, SerializedObject serializedObject)
        {
            if (!serializedObject.IsEditable())
                return;

            var menu = new GenericMenu();
            var target = serializedObject.targetObject;

            var editableLabel = header?.Q<EditableLabel>(null, itemHeaderLabelUssClassName);
            if (editableLabel != null
                && editableLabel.style.display != DisplayStyle.None
                && editableLabel.style.visibility != Visibility.Hidden)
            {
#if UNITY_EDITOR_OSX
                var editNameLabel = new GUIContent("Edit Name (⌥ + Click)");
#else
                var editNameLabel = new GUIContent("Edit Name (Alt + Click)");
#endif
                menu.AddItem(editNameLabel, false, () => editableLabel.BeginEditing());
            }

            AddItemsToContextMenu(menu, header, itemIndex, serializedObject);

            position.position = GUIUtility.GUIToScreenPoint(position.position);
            s_ShowContextMenu?.Invoke(menu, position, serializedObject.targetObjects, 0);
        }

        private void ShowPresetSelector(SerializedObject serializedObject)
        {
            var target = serializedObject.targetObject;
            if (!new PresetType(target).IsValid() || (target.hideFlags & HideFlags.NotEditable) != 0)
                return;
            PresetSelector.ShowSelector(serializedObject.targetObjects, null, true);
        }

        protected override VisualElement CreateItemForIndex(int index)
        {
            var stopper = new BindingStopper();
            stopper.Add(new InspectorItem(this, index));
            return stopper;
        }

        protected override void OnReorderDragPerformed(int draggedIndex, int dropIndex)
        {
            ListControlUtils.ReorderSerializedArray(m_ArrayProp, draggedIndex, dropIndex);
        }

        protected override bool IsReorderable()
        {
            return m_ArrayProp.serializedObject.IsEditable();
        }

        private class InspectorItem : VisualElement
        {
            private readonly ListOfInspectors m_OwnerList;
            private readonly int m_Index;
            private readonly SerializedProperty m_BackingProperty;
            private readonly ValueTracker<Object> m_ObjectTracker = new ValueTracker<Object>();
            
            public InspectorItem(ListOfInspectors ownerList, int index)
            {
                AddToClassList(inspectorItemUssClassName);

                m_OwnerList = ownerList;
                m_Index = index;
                m_BackingProperty = ownerList.m_ArrayProp.GetArrayElementAtIndex(m_Index);

                if (m_BackingProperty.propertyType != SerializedPropertyType.ObjectReference)
                {
                    Debug.LogError("Property needs to be an object reference to create an inspector");
                    return;
                }

                m_ObjectTracker.SetUp(m_BackingProperty, e => AssignObject(), m_BackingProperty.objectReferenceValue);
                m_OwnerList.m_TrackersContainer.Add(m_ObjectTracker);

                AssignObject();

                RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            }

            private void OnAttachToPanel(AttachToPanelEvent e)
            {
                //We need to add it here in case OnDetachFromPanel happened because of panel interactions and not because the element was removed.
                m_OwnerList.m_TrackersContainer.Add(m_ObjectTracker);
            }

            private void OnDetachFromPanel(DetachFromPanelEvent e)
            {
                m_ObjectTracker.RemoveFromHierarchy();
            }

            private void AssignObject()
            {
                Object obj = m_BackingProperty.objectReferenceValue;

                Clear();

                if (!obj)
                {
                    if (m_BackingProperty.objectReferenceInstanceIDValue != 0)
                        AssignControlsForInvalidScript();
                    return;
                }

                var serializedObject = new SerializedObject(obj);
                var inspector = new InspectorElement();
                var header = m_OwnerList.CreateHeader(m_Index, serializedObject, inspector);

                if (header != null)
                    Add(header);
                Add(inspector);

                this.Bind(serializedObject);
            }

            private void AssignControlsForInvalidScript()
            {
                var header = new VisualElement { style = { height = 22 } };
                header.AddToClassList(itemHeaderUssClassName);
                var body = new VisualElement();
                body.style.SetPadding(3, 0, 3, 15);

                var foldout = AddHeaderFoldout(header, true);
                foldout.RegisterValueChangedCallback(e =>
                {
                    header.EnableInClassList(itemHeaderCollapsedUssClassName, !e.newValue);
                    body.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                });

                var icon = new Image { image = EditorGUIUtility.IconContent("Warning").image };
                icon.AddToClassList(itemHeaderIconUssClassName);
                header.Add(icon);

                var label = new Label("Object With Invalid Script");
                label.AddToClassList(itemHeaderLabelUssClassName);
                header.Add(label);

                header.AddManipulator(new DragAndClickManipulator
                {
                    onStartDragging = () => m_OwnerList.StartDraggingItem(m_Index),
                    onClick = () => foldout.value = !foldout.value
                });

                body.Add(new HelpBox(
                            "This object's script is invalid. Make sure it doesn't have errors, its" +
                            " class has the same name as its file, and it's the right type.", HelpBoxMessageType.Warning));

                body.Add(new Button(() => Selection.activeInstanceID = m_BackingProperty.objectReferenceInstanceIDValue)
                {
                    text = "Select Object With Invalid Script"
                });

                Add(header);
                Add(body);
            }
        }
    }
}