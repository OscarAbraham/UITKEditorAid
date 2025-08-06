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
using static UnityEngine.GraphicsBuffer;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// A list of Objects similar to the components list in a GameObject's inspector.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    /// This element is typically used with a list of subassets. Subasset systems require code that's outside the scope of this
    /// package. You may be better off using <see cref="InspectorElement"/> if you just want to embed inspectors from different Objects.
    /// </para>
    /// <para>
    /// The inspector's help button shows a custom tooltip if there's a <see cref="TooltipAttribute"/> in the Object's class.
    /// By default, the inspector's header has an <see cref="EditableLabel"/> for changing the Object's name. More customizations
    /// are available by creating a child class. Multitarget SerializedObjects and Editors aren't supported.
    /// </para>
    /// <para>
    /// Inspectors will try to show special UI when an Object's script is missing. To support this, Unity needs the base type
    /// of the Object array/list to be creatable (i.e. not abstract nor generic).
    /// </para>
    /// </remarks>
    /// 
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// class CustomEditorWithListOfInspectors : Editor
    /// {
    ///     public override VisualElement CreateInspectorGUI()
    ///     {
    ///         var root = new VisualElement();
    /// 
    ///         // The mylistOfObjects field should be a list or array of elements that
    ///         // derive from UnityEngine.Object. It's usually a list of ScriptableObjects
    ///         // embedded as subassets in the field's owner.
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
    ///     protected override void AddPrelabelHeaderElements(
    ///         VisualElement header,
    ///         int itemIndex,
    ///         SerializedObject serializedObject)
    ///     {
    ///         base.AddPrelabelHeaderElements(header, itemIndex, serializedObject);
    ///     }
    /// 
    ///     // Override this method to change how a label is added to an inspector header.
    ///     // The base implementation adds an editable label for changing the object's name.
    ///     protected override void AddHeaderLabel(
    ///         VisualElement header,
    ///         int itemIndex,
    ///         SerializedObject serializedObject)
    ///     {
    ///         base.AddHeaderLabel(header, itemIndex, serializedObject);
    ///     }
    /// 
    ///     // Override this method to add elements to an inspector header after its label.
    ///     protected override void AddPostlabelHeaderElements(
    ///         VisualElement header,
    ///         int itemIndex,
    ///         SerializedObject serializedObject)
    ///     {
    ///         base.AddPostlabelHeaderElements(header, itemIndex, serializedObject);
    ///     }
    /// 
    ///     // Override this method to add context menu items for an inspector header.
    ///     protected override void AddItemsToContextMenu(
    ///         GenericMenu menu,
    ///         VisualElement header,
    ///         int itemIndex,
    ///         SerializedObject serializedObject)
    ///     {
    ///         base.AddItemsToContextMenu(menu, header, itemIndex, serializedObject);
    ///         menu.AddItem(new GUIContent("My Menu Item"), true, () =>
    ///         {
    ///             // Do something.
    ///         });
    ///     }
    /// }
    /// ]]></code>
    /// </example>
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

        static ListOfInspectors()
        {
            MethodInfo contextMenuMethod = typeof(GenericMenu).GetMethod("ObjectContextDropDown",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null,
                new Type[] { typeof(Rect), typeof(Object[]), typeof(int)}, null);

            if (contextMenuMethod != null)
                s_ShowContextMenu = (Action<GenericMenu, Rect, Object[], int>)Delegate.CreateDelegate(typeof(Action<GenericMenu, Rect, Object[], int>), contextMenuMethod, false);
        }

        /// <summary>
        /// Constructor. It receives a <see cref="SerializedProperty"/> for an array or a list of Objects
        /// with a type derived from <see cref="Object">UnityEngine.Object</see>.
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
            m_TrackersContainer.Add(new ValuePropertyTracker<int>(sizeProp, OnSizeChange, sizeProp.intValue) { name = "Size Tracker" });

            Add(m_TrackersContainer);
            SetListSize(m_ArrayProp.arraySize);

            BindTrackers();
        }

        private void OnSizeChange(int prevValue, int newValue)
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
            var header = new VisualElement();
            header.AddToClassList(itemHeaderUssClassName);

            bool wasExpanded = InternalEditorUtility.GetIsInspectorExpanded(target);
            SetItemExpanded(wasExpanded, header, target, inspector);

            var foldout = AddHeaderFoldout(header, wasExpanded);
            foldout.RegisterValueChangedCallback(e => SetItemExpanded(e.newValue, header, target, inspector));

            header.AddManipulator(new DragAndClickManipulator
            {
                onStartDragging = () => StartDraggingItem(itemIndex),
                onClick = () => foldout.value = !foldout.value
            });
            header.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == 1)
                    ShowInspectorContextMenu(new Rect(e.mousePosition, default), header, itemIndex, serializedObject);
            });

            AddPrelabelHeaderElements(header, itemIndex, serializedObject);
            AddHeaderLabel(header, itemIndex, serializedObject);
            AddPostlabelHeaderElements(header, itemIndex, serializedObject);

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
            label.emptyTextLabel = GetDefaultHeaderLabelText(serializedObject);

            var targetType = serializedObject.targetObject.GetType();
            if (Attribute.IsDefined(targetType, typeof(TooltipAttribute)))
            {
                var tooltipAttr = (TooltipAttribute)Attribute.GetCustomAttributes(targetType, typeof(TooltipAttribute))[0];
                label.tooltip = tooltipAttr.tooltip;
            }

            header.Add(label);
            header.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.altKey && e.button == 0)
                    label.BeginEditing();
            });
        }

        /// <summary>
        /// Get the text that is shown in an inspector's header when the targetObject has no name.
        /// </summary>
        /// <param name="serializedObject">The inspected object.</param>
        /// <returns> The default header label text. </returns>
        protected virtual string GetDefaultHeaderLabelText(SerializedObject serializedObject)
        {
            return ObjectNames.NicifyVariableName(serializedObject.targetObject.GetType().Name);
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
            if (Attribute.IsDefined(targetType, typeof(HelpURLAttribute)))
            {
                var help = new Button();
                help.AddToClassList(itemHeaderButtonUssClassName);
                help.style.backgroundImage = EditorGUIUtility.IconContent("_Help").image as Texture2D;
                help.tooltip = $"Open Reference for {ObjectNames.NicifyVariableName(targetType.Name)}.";
                help.clicked += () => Help.ShowHelpForObject(target);
                header.Add(help);
            }

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

        [RemoveFromDocs]
        protected override VisualElement CreateItemForIndex(int index)
        {
            var stopper = new BindingStopper();
            stopper.Add(new InspectorItem(this, index));
            return stopper;
        }

        [RemoveFromDocs]
        protected override void OnReorderDragPerformed(int draggedIndex, int dropIndex)
        {
            ListControlUtils.ReorderSerializedArray(m_ArrayProp, draggedIndex, dropIndex);
        }

        private class InspectorItem : VisualElement
        {
            private const string k_InvalidScriptWarning =
                "This object's script is invalid. Make sure it doesn't have errors, its " +
                "class has the same name as its file, and it has the right type.";
            private const string k_NullEntryWarning =
                "This object is null. It may have been improperly deleted. " +
                "Check your Version Control to fix it if available, or click the button to delete this entry.";
            private readonly ListOfInspectors m_OwnerList;
            private readonly int m_Index;
            private readonly SerializedProperty m_BackingProperty;
            private readonly ValuePropertyTracker<Object> m_ObjectTracker = new ValuePropertyTracker<Object>();

            private Editor m_Editor;
            
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

                m_ObjectTracker.value = m_BackingProperty.objectReferenceValue;
                m_ObjectTracker.valueChanged = (prevValue, newValue) => LoadGUI();
                m_ObjectTracker.bindingPath = m_BackingProperty.propertyPath;
                m_ObjectTracker.name = $"object{index}-reference-tracker";
                m_OwnerList.m_TrackersContainer.Add(m_ObjectTracker);

                RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            }

            private void OnAttachToPanel(AttachToPanelEvent e)
            {
                m_OwnerList.m_TrackersContainer.Add(m_ObjectTracker);
                LoadGUI();
            }

            private void OnDetachFromPanel(DetachFromPanelEvent e)
            {
                m_ObjectTracker.RemoveFromHierarchy();
                if (m_Editor)
                    Object.DestroyImmediate(m_Editor);
            }

            private void LoadGUI()
            {
                Object obj = m_BackingProperty.objectReferenceValue;

                Clear();

                if (m_Editor)
                    Object.DestroyImmediate(m_Editor);
                if (obj)
                    m_Editor = Editor.CreateEditor(obj);

                if (!m_Editor)
                {
                    AssignControlsForNull();
                    return;
                }

                // As of Unity 6000.43, creating the InspectorElement with an Editor is necessary to make the
                // inspector UI bind to the Editor's SerializedObject. Without an Editor, the Element will manage its
                // own Editor, but it won't bind to that Editor's serializedObject.
                //
                // We do this because some custom Editors may contain code that expects their serializedObject to be
                // updated when data changes, which won't happen if that serializedObject is not bound. Also, if a
                // custom Editor binds some fields to its serializedObject directly, it'll be more performant because
                // the binding system won't need to keep updating two different serializedObjects for the same target.
                //
                // CONSIDER: Should we report this as a bug to Unity? Seems hard to fix without adding methods to
                // create Editors that use a preexisting SerializedObject.
                var inspector = new InspectorElement(m_Editor);

                // Without this class somewhere in the hierarchy between our Inspector and a main Inspector that may
                // contain this List, our aligned BaseFields use the main Inspector to compute label widths instead
                // of our Inspector. The main Inspector may have different layout than our own, which could make our
                // aligned Labels noticeably mismatch aligned Labels in other Inspectors with the same layout.
                // This can still create a small mismatch when the main Inspector has scrollbars, but it's a small
                // amount and the Labels' min-width stays the same. 
                inspector.AddToClassList("unity-inspector-main-container");

                var header = m_OwnerList.CreateHeader(m_Index, m_Editor.serializedObject, inspector);
                if (header != null)
                    Add(header);
                Add(inspector);

                header.Bind(m_Editor.serializedObject);
            }

            private void AssignControlsForNull()
            {
                int referenceId = m_BackingProperty.objectReferenceInstanceIDValue;
                // A missing object here should mean an invalid script assigned to that object,
                // assuming subassets in this list are always removed from it when deleted.
                bool invalidScript = referenceId != 0;

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

                var label = new Label(invalidScript ? "Object With Invalid Script" : "Null Object Entry");
                label.AddToClassList(itemHeaderLabelUssClassName);
                header.Add(label);

                header.AddManipulator(new DragAndClickManipulator
                {
                    onStartDragging = () => m_OwnerList.StartDraggingItem(m_Index),
                    onClick = () => foldout.value = !foldout.value
                });

                body.Add(new HelpBox(invalidScript ? k_InvalidScriptWarning : k_NullEntryWarning, HelpBoxMessageType.Warning));

                Button fixButton = new Button(() =>
                {
                    if (invalidScript)
                        Selection.activeInstanceID = m_BackingProperty.objectReferenceInstanceIDValue;
                    else
                    {
                        m_BackingProperty.DeleteCommand();
                        m_BackingProperty.serializedObject.ApplyModifiedProperties();
                    }
                }) { text = invalidScript ? "Select Object With Invalid Script" : "Delete Object Entry From List" };
                body.Add(fixButton);

                Add(header);
                Add(body);
            }
        }
    }
}