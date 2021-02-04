using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// UITK equivalent to <see cref="EditorGUILayout.Space"/>. Use it to quickly add a space between elements along the parent's flex-direction.
    /// </summary>
    public class FixedSpace : VisualElement
    {
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
                space.style.flexBasis = m_Size.GetValueFromBag(bag, cc);
            }
        }

        private const float k_DefaultSize = 6;

        /// <summary> FixedSpace constructor. Uses a small default size. </summary>
        public FixedSpace() : this(k_DefaultSize) { }

        /// <summary> FixedSpace constructor. </summary>
        /// <param name="size">The size of the space. Sets the element's flex-basis style property.</param>
        public FixedSpace(float size)
        {
            pickingMode = PickingMode.Ignore;
            style.flexBasis = size;
            style.flexGrow = 0;
            style.flexShrink = 0;
        }
    }
}