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
    // This class could use some cleanup and refactoring. It's uses are few and very specific, though, so it doesn't seem to be worth it.
    /// <summary>
    /// A UIToolkit list similar to the component list in a GameObject's inspector. It's been tested with a SerializedProperty that represent's
    /// an array of <see cref="ScriptableObject"/>. It doesn't do too well with multitarget SerializedObjects.
    /// <para>It's recommended to add it with <see cref="UIToolkitExtensions.AddDelayed(VisualElement, VisualElement)"/>, specially
    /// when used inside inspectors, to avoid the child inspectors being bound to the root's SerializedObject.</para>
    /// </summary>
    /// <remarks>
    /// This class is really an internal tool that's probably not needed by most people, but I figured it might help somebody.
    /// It requires a whole lotta other code that's not related to UIToolkit to be useful. I use it with assets that have
    /// subassets inside (<see cref="AssetDatabase.AddObjectToAsset(Object, Object)"/>), and a specially configured 
    /// <see cref="UnityEditor.IMGUI.Controls.AdvancedDropdown"/> to add new subassets. It requires a careful managment of assets
    /// that's outside the scope of this library. Yet, if you know what I'm talking about, I bet this element can be useful to you.
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

        private static Action<GenericMenu, Rect, Object[], int> s_ShowContextMenu;

        private readonly SerializedProperty m_ArrayProp;
        private readonly VisualElement m_TrackersContainer = new VisualElement();
        //TODO It seems we could use the size tracker as a local variable.
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
                {
                    ShowInspectorContextMenu(new Rect(e.mousePosition, default), headerContainer, itemIndex, serializedObject);
                }
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

        private Foldout AddHeaderFoldout(VisualElement header, bool expanded)
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
            icon.AddToClassList(itemHeaderButtonUssClassName);
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

            var presets = new Button();
            presets.AddToClassList(itemHeaderButtonUssClassName);
            presets.style.backgroundImage = EditorGUIUtility.IconContent("Preset.Context").image as Texture2D;
            presets.clicked += () => ShowPresetSelector(serializedObject);
            header.Add(presets);

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

            // Check for attribute because Help.HasHelpForObject always returns true for most custom objects.
            if (Attribute.IsDefined(target.GetType(), typeof(HelpURLAttribute)))
            {
                menu.AddItem(new GUIContent($"Open Help for {target.GetType().Name}"), false, () => Help.ShowHelpForObject(target));
            }
            // If the object is not editable, we still can show its help page, but anything else is too risky.
            if (!serializedObject.IsEditable())
            {
                menu.DropDown(position);
                return;
            }

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
            return new InspectorItem(this, index);
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
            private readonly ValueTracker<Object> m_ObjectTracker = new ValueTracker<Object>();
            
            private SerializedObject m_SerializedObject;
            private InspectorElement m_Inspector;
            private VisualElement m_Header;

            public InspectorItem(ListOfInspectors ownerList, int index)
            {
                AddToClassList(inspectorItemUssClassName);

                m_OwnerList = ownerList;
                m_Index = index;
                SerializedProperty property = ownerList.m_ArrayProp.GetArrayElementAtIndex(m_Index);

                if (property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    Debug.LogError("Property needs to be an object reference to create an inspector");
                    return;
                }

                m_ObjectTracker.SetUp(property, e => AssignObject(e.newValue), property.objectReferenceValue);
                m_OwnerList.m_TrackersContainer.Add(m_ObjectTracker);

                AssignObject(property.objectReferenceValue);

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

            private void AssignObject(Object newObject)
            {
                //It seems, at least in 2019.4 with IMGUI, that just rebinding the inspector is not enough; we need to recreate it.
                m_Inspector?.RemoveFromHierarchy();
                m_Inspector = null;
                m_Header?.RemoveFromHierarchy();
                m_Header = null;

                // TODO Display a missing script notice or something?
                if (!newObject) return;

                m_SerializedObject = new SerializedObject(newObject);
                m_Inspector = new InspectorElement();
                m_Header = m_OwnerList.CreateHeader(m_Index, m_SerializedObject, m_Inspector);

                if (m_Header != null)
                    Add(m_Header);
                Add(m_Inspector);

                this.Bind(m_SerializedObject);
            }
        }
    }
}