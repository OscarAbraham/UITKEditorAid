using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// Element that shows a <see cref="SerializedProperty">SerializedProperty's</see> prefab override indicators,
    /// and the property's menu on context click. <c>UXML support</c>
    /// </summary>
    /// <remarks>
    /// Currently, Unity only adds these capabilities for fields that inherit from <see cref="BaseField{TValueType}"/> or <see cref="Foldout"/>.
    /// This element makes those features available everywhere. It can be used with any SerializedProperty, even those that have nested
    /// properties inside. To use it, assign a <see cref="bindingPath"/> to the property it represents, and call the
    /// <see cref="VisualElement.Add(VisualElement)"/> method to add the UI for that property.
    /// <para>
    /// It should also show other kinds of indicators, like the yellow ones from the localization package, but I haven't tested that.
    /// </para>
    /// </remarks>
    /// <example>
    /// The <see cref="prefabOverrideUssClassName"/> can be used to apply custom styles when theres a prefab override. For example,
    /// a custom style sheet could do something like this to make a particular label bold when there's an override:
    /// <code language="css"><![CDATA[
    /// .editor-aid-property-container--prefab-override .my-custom-label-class {
    ///     -unity-font-style: bold;
    /// }
    /// ]]></code>
    /// </example>
#if !REMOVE_UXML_FACTORIES && UNITY_2023_3_OR_NEWER
    [UxmlElement]
#endif
    public partial class PropertyContainer : VisualElement
    {
#if !REMOVE_UXML_FACTORIES && !UNITY_2023_3_OR_NEWER
        public new class UxmlFactory : UxmlFactory<PropertyContainer, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_PropertyPath = new UxmlStringAttributeDescription { name = "binding-path" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var propertyContainer = ve as PropertyContainer;
                propertyContainer.bindingPath = m_PropertyPath.GetValueFromBag(bag, cc);
            }
        }
#endif

        /// <summary> USS class name of elements of this type. </summary>
        public static readonly string ussClassName = "editor-aid-property-container";
        /// <summary> USS class name of elements of this type when their property is a prefab override. </summary>
        public static readonly string prefabOverrideUssClassName = ussClassName + "--prefab-override";
        /// <summary> USS class name for the content element. </summary>
        public static readonly string contentUssClassName = ussClassName + "__content";
        /// <summary> USS class name for an invisible element that makes Unity apply the relevant SerializedProperty features. </summary>
        public static readonly string propertyProxyUssClassName = ussClassName + "__property-proxy";

        private const long k_CheckPrefabOverrideInterval = 500;

        private readonly Foldout m_PropertyProxy;
        private readonly Toggle m_ProxyToggle;
        private readonly VisualElement m_ContentContainer;
        private bool m_HasPropertyOverride = false;

        /// <summary>
        /// An event that's triggered when a change in the property's override status is detected.
        /// Receives a <see cref="bool"/> that indicates whether the property is a prefab override.
        /// </summary>
        public event Action<bool> onPrefabOverrideChanged;

        /// <summary> The path to property represented by this element. </summary>
#if !REMOVE_UXML_FACTORIES && UNITY_2023_3_OR_NEWER
        [UxmlAttribute]
#endif
        public string bindingPath { get  => m_PropertyProxy.bindingPath; set => m_PropertyProxy.bindingPath = value; }

        [RemoveFromDocs]
        public override VisualElement contentContainer => m_ContentContainer;

        /// <summary> Constructor. </summary>
        public PropertyContainer() : this((string)null) { }

        /// <summary> Constructor. The Property parameter just sets the <see cref="bindingPath"/>; it still needs to be bound. </summary>
        /// <param name="property"> The property represented by this element. </param>
        public PropertyContainer(SerializedProperty property) : this(property?.propertyPath) { }

        /// <summary> Constructor. Receives a string that is assigned to <see cref="bindingPath"/>. </summary>
        /// <param name="propertyPath"> The path of the property represented by this element. </param>
        public PropertyContainer(string propertyPath)
        {
            AddToClassList(ussClassName);

            // We use a separate content container to be able to put a proxy element on top in case it's needed. 
            m_ContentContainer = new VisualElement();
            m_ContentContainer.AddToClassList(contentUssClassName);
            m_ContentContainer.pickingMode = PickingMode.Position;
            hierarchy.Add(m_ContentContainer);

            m_PropertyProxy = new Foldout { pickingMode = PickingMode.Ignore };
            m_PropertyProxy.AddToClassList(propertyProxyUssClassName);
            m_PropertyProxy.style.position = Position.Absolute;
            m_PropertyProxy.style.top = m_PropertyProxy.style.bottom = m_PropertyProxy.style.left = m_PropertyProxy.style.right = 0;
            m_PropertyProxy.style.opacity = 0;

            m_ProxyToggle = m_PropertyProxy.Q<Toggle>(null, Foldout.toggleUssClassName);
            m_ProxyToggle.focusable = false;
            m_ProxyToggle.pickingMode = PickingMode.Ignore;
            m_ProxyToggle.Query().ForEach(ve => ve.pickingMode = PickingMode.Ignore);
            m_ProxyToggle.style.position = Position.Absolute;
            m_ProxyToggle.style.top = m_ProxyToggle.style.bottom = m_ProxyToggle.style.left = 0;
            m_ProxyToggle.style.width = 0;
            m_ProxyToggle.style.marginBottom = m_ProxyToggle.style.marginTop = m_ProxyToggle.style.marginLeft = m_ProxyToggle.style.marginRight = 0;

            hierarchy.Add(m_PropertyProxy);
            bindingPath = propertyPath;

            RegisterCallback<PointerUpEvent>(OnPointerUp);

            schedule.Execute(CheckPrefabOverride).Every(k_CheckPrefabOverrideInterval);
        }

        private void OnPointerUp(PointerUpEvent e)
        {
            // Prevent getting stuck in an infinite loop with the fake events sent to the proxyToggle.
            if (e.target == m_ProxyToggle)
                return;

            // Unity's current implementation doesn't detect ctrl + click on macOS, but that doesn't stop us from supporting it.
            if (e.button == 1 || (Application.platform == RuntimePlatform.OSXEditor && e.button == 0 && e.ctrlKey))
            {
                if (!(e.target is VisualElement))
                    return;

                e.StopPropagation();

                // In 2021.3, the menu is displayed from the toggleElement's position, so we put it under the mouse.
                // We need to remove this code in newer versions because they display prefab blue bars next to the
                // Toggle instead of next to the whole Foldout.
#if !UNITY_2022_2_OR_NEWER
                m_ProxyToggle.style.height = 1;
                m_ProxyToggle.style.top = e.localPosition.y;
                // We don't set left because there's a bug where Unity doesn't handle it well when it's not 0.
#endif

                var fakeSystemEvent = new Event() { button = 1, mousePosition = e.position, type = EventType.MouseDown };
                using (var fakeDownEvent = PointerDownEvent.GetPooled(fakeSystemEvent))
                {
                    fakeDownEvent.target = m_ProxyToggle;
                    m_ProxyToggle.SendEvent(fakeDownEvent);
                }

                fakeSystemEvent.type = EventType.MouseUp;
                using (var fakeUpEvent = PointerUpEvent.GetPooled(fakeSystemEvent))
                {
                    fakeUpEvent.target = m_ProxyToggle;
                    m_ProxyToggle.SendEvent(fakeUpEvent);
                }
            }
        }

        private void CheckPrefabOverride()
        {
            bool proxyHasPropertyOverride = m_ProxyToggle.ClassListContains(BindingExtensions.prefabOverrideUssClassName);
            if (proxyHasPropertyOverride != m_HasPropertyOverride)
            {
                m_HasPropertyOverride = proxyHasPropertyOverride;
                EnableInClassList(prefabOverrideUssClassName, proxyHasPropertyOverride);
                onPrefabOverrideChanged?.Invoke(proxyHasPropertyOverride);
            }
        }
    }
}