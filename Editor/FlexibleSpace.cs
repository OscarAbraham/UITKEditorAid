using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// An element for quickly filling the remaining space along its parent's <see cref="IStyle.flexDirection"/>. <mark>UXML support</mark>
    /// </summary>
    /// <remarks>
    /// This element is analogous to IMGUI's <see cref="GUILayout.FlexibleSpace"/>.
    /// Its <see cref="IStyle.flexBasis"/> is 0 and its <see cref="IStyle.flexGrow"/> is 1.
    /// </remarks>
    public class FlexibleSpace : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<FlexibleSpace, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }

        public FlexibleSpace()
        {
            pickingMode = PickingMode.Ignore;
            style.flexBasis = 0;
            style.flexGrow = 1;
        }
    }
}