using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// A UIToolkit element that's equivalent to <see cref="GUILayout.FlexibleSpace"/>.
    /// Use it to quickly fill the remaining space along the parent's flex-direction.  Supports UXML. 
    /// </summary>
    /// <remarks>Its USS flex-basis property is 0 and its flex-grow property is 1.</remarks>
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

        /// <summary> FlexibleSpace constructor. </summary>
        public FlexibleSpace()
        {
            pickingMode = PickingMode.Ignore;
            style.flexBasis = 0;
            style.flexGrow = 1;
        }
    }
}