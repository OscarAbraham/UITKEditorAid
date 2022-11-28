using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace ArteHacker.UITKEditorAid
{
    // TODO: Find a practical way to make child texts bold when overriden, and make it optional. 
    /// <summary>
    /// An Element that represents a <see cref="SerializedProperty"/>. It shows the property's menu on context click,
    /// and it displays the relevant color indicators when the property is overriden or other similar statuses.
    /// </summary>
    public class PropertyContainer : VisualElement, IBindable
    {
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

        /// <summary> USS class name of elements of this type. </summary>
        public static readonly string ussClassName = "editor-aid-property-container";
        /// <summary> USS class name for the content element. </summary>
        public static readonly string contentUssClassName = ussClassName + "__content";

        private readonly Foldout m_PropertyProxy;
        private readonly Toggle m_ProxyToggle;
        private readonly VisualElement m_ContentContainer;

        public IBinding binding { get; set; }

        /// <summary> The path to property represented by this element. </summary>
        public string bindingPath { get  => m_PropertyProxy.bindingPath; set => m_PropertyProxy.bindingPath = value; }

        public override VisualElement contentContainer => m_ContentContainer;

        /// <summary> PropertyContainer constructor. </summary>
        public PropertyContainer() : this((string)null) { }

        /// <summary> PropertyContainer constructor. </summary>
        /// <param name="property"> The property represented by this element. </param>
        public PropertyContainer(SerializedProperty property) : this(property.propertyPath) { }

        /// <summary> PropertyContainer constructor. </summary>
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
            m_PropertyProxy.style.position = Position.Absolute;
            m_PropertyProxy.style.top = m_PropertyProxy.style.bottom = m_PropertyProxy.style.left = m_PropertyProxy.style.right = 0;
            m_PropertyProxy.style.opacity = 0;

            m_ProxyToggle = m_PropertyProxy.Q<Toggle>(null, Foldout.toggleUssClassName);
            m_ProxyToggle.pickingMode = PickingMode.Ignore;
            m_ProxyToggle.Query().ForEach(ve => ve.pickingMode = PickingMode.Ignore);
            m_ProxyToggle.style.position = Position.Absolute;
            m_ProxyToggle.style.top = m_ProxyToggle.style.left = 0;
            m_ProxyToggle.style.height = m_ProxyToggle.style.width = 0;
            m_ProxyToggle.style.marginBottom = m_ProxyToggle.style.marginTop = m_ProxyToggle.style.marginLeft = m_ProxyToggle.style.marginRight = 0;

            hierarchy.Add(m_PropertyProxy);
            bindingPath = propertyPath;

            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        private void OnMouseUp(MouseUpEvent e)
        {
            // Prevent getting stuck in an infinite loop with the fake events sent to the proxyToggle.
            if (e.target == m_ProxyToggle)
                return;

            // Unity's current implementation doesn't detect ctrl + click on macOS, but that doesn't stop us from supporting it.
            if (e.button == 1 || (Application.platform == RuntimePlatform.OSXEditor && e.button == 0 && e.ctrlKey))
            {
                if (!(e.target is VisualElement elementTarget))
                    return;

                e.PreventDefault();
                e.StopPropagation();

                // The menu is displayed from the toggleElement's position, so we need to make it cover the target.
                var localTargetRect = new Rect(0, 0, elementTarget.layout.width, elementTarget.layout.height);
                var toggleRect = elementTarget.ChangeCoordinatesTo(this, localTargetRect);
                m_ProxyToggle.style.width = 1;
                m_ProxyToggle.style.height = toggleRect.height;
                m_ProxyToggle.style.top = toggleRect.y;
                // We don't set left to toggleRect.x because there's a bug where Unity doesn't handle it well when it's not 0.

                // Currently, we don't really need the fakeDownEvent, but we send it to make this more future proof.
                using (var fakeDownEvent = MouseDownEvent.GetPooled(e.mousePosition, 1, 1, Vector2.zero))
                {
                    fakeDownEvent.target = m_ProxyToggle;
                    m_ProxyToggle.SendEvent(fakeDownEvent);
                }
                using (var fakeUpEvent = MouseUpEvent.GetPooled(e.mousePosition, 1, 1, Vector2.zero))
                {
                    fakeUpEvent.target = m_ProxyToggle;
                    m_ProxyToggle.SendEvent(fakeUpEvent);
                }
            }
        }
    }
}