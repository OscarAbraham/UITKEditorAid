using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace ArteHacker.UITKEditorAid
{
    /// <summary> Event sent by <see cref="ManagedReferenceField"/> when the full type name changes. </summary>
    public class ReferenceTypeChangeEvent : EventBase<ReferenceTypeChangeEvent>
    {
        /// <summary>The previous value of <see cref="SerializedProperty.managedReferenceFullTypename"/></summary>
        public string previousTypeName { get; protected set; }

        /// <summary>The new value of <see cref="SerializedProperty.managedReferenceFullTypename"/></summary>
        public string newTypeName { get; protected set; }

        protected override void Init()
        {
            base.Init();
            previousTypeName = default;
            newTypeName = default;
        }

        /// <summary>
        /// It's the same as <see cref="EventBase{T}.GetPooled()"/>, but it initializes the event with parameters.
        /// </summary>
        /// <param name="previousTypeName">The previous value of <see cref="SerializedProperty.managedReferenceFullTypename"/></param>
        /// <param name="newTypeName">The new value of <see cref="SerializedProperty.managedReferenceFullTypename"/></param>
        public static ReferenceTypeChangeEvent GetPooled(string previousTypeName, string newTypeName)
        {
            ReferenceTypeChangeEvent e = GetPooled();
            e.previousTypeName = previousTypeName;
            e.newTypeName = newTypeName;
            return e;
        }
    }
}