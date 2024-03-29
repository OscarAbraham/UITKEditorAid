﻿using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// Element that takes any leftover space along its parent's <see cref="IStyle.flexDirection"/>. <c>UXML support</c>
    /// </summary>
    /// <remarks>
    /// This element is analogous to IMGUI's <see cref="GUILayout.FlexibleSpace"/>.
    /// Its <see cref="IStyle.flexBasis"/> is 0 and its <see cref="IStyle.flexGrow"/> is 1.
    /// </remarks>
    public class FlexibleSpace : VisualElement
    {
#if !REMOVE_UXML_FACTORIES
        public new class UxmlFactory : UxmlFactory<FlexibleSpace, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
#endif

        public FlexibleSpace()
        {
            pickingMode = PickingMode.Ignore;
            style.flexBasis = 0;
            style.flexGrow = 1;
        }
    }
}