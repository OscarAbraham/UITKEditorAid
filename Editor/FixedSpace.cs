using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// Element that adds a fixed space along its parent's <see cref="IStyle.flexDirection"/>. <c>UXML support</c>
    /// </summary>
    /// <remarks>
    /// This element is analogous to IMGUI's <see cref="UnityEngine.GUILayout.Space"/>.
    /// </remarks>
#if !REMOVE_UXML_FACTORIES && UNITY_2023_3_OR_NEWER
    [UxmlElement]
#endif
    public partial class FixedSpace : VisualElement
    {
#if !REMOVE_UXML_FACTORIES && !UNITY_2023_3_OR_NEWER
        public new class UxmlFactory : UxmlFactory<FixedSpace, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlFloatAttributeDescription m_Size = new UxmlFloatAttributeDescription { name = "size", defaultValue = k_DefaultSize };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var space = ve as FixedSpace;
                space.size = m_Size.GetValueFromBag(bag, cc);
            }
        }
#endif

        private const float k_DefaultSize = 6;

        /// <summary>
        /// The space's size along the parent's <see cref="IStyle.flexDirection"/>.
        /// It works as a shorthand for this element's <see cref="IStyle.flexBasis"/> value.
        /// </summary>
#if !REMOVE_UXML_FACTORIES && UNITY_2023_3_OR_NEWER
        [UxmlAttribute]
#endif
        public float size { get => style.flexBasis.value.value; set => style.flexBasis = value; }

        /// <summary> Constructor. Uses a small default size. </summary>
        public FixedSpace() : this(k_DefaultSize) { }

        /// <summary> Constructor. Receives the size of the space. </summary>
        /// <param name="size">The size of the space. Sets the element's flex-basis style property.</param>
        public FixedSpace(float size)
        {
            pickingMode = PickingMode.Ignore;
            this.size = size;
            style.flexGrow = 0;
            style.flexShrink = 0;
        }
    }
}