using System;
using UnityEngine.UIElements;
using UnityEditor;
using Object = UnityEngine.Object;

namespace ArteHacker.UITKEditorAid
{
    /// <summary> Event sent by <see cref="ManagedReferenceField"/> when the field's type changes. </summary>
    public class ReferenceTypeChangeEvent : EventBase<ReferenceTypeChangeEvent>
    {
        /// <summary>The previous reference Type.</summary>
        public Type previousType { get; protected set; }

        /// <summary>The new reference Type.</summary>
        public Type newType { get; protected set; }

        [RemoveFromDocs]
        protected override void Init()
        {
            base.Init();
            previousType = default;
            newType = default;
        }

        /// <summary>
        /// It's the same as <see cref="EventBase{T}.GetPooled()"/>, but it initializes the event with parameters.
        /// </summary>
        /// <param name="previousType">The previous reference Type.</param>
        /// <param name="newType">The new reference Type.</param>
        public static ReferenceTypeChangeEvent GetPooled(Type previousType, Type newType)
        {
            ReferenceTypeChangeEvent e = GetPooled();
            e.previousType = previousType;
            e.newType = newType;
            return e;
        }
    }
}