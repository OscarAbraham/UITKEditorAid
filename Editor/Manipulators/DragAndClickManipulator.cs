using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace ArteHacker.UITKEditorAid.Manipulators
{
    /// <summary>
    /// Manipulator that executes an <see cref="onStartDragging"/> callback when the mouse is dragged,
    /// and an <see cref="onClick"/> callback when the mouse is only clicked.
    /// </summary>
    /// <remarks> This is useful for draggable foldouts and toggles, like the headers of Components in the inspector,
    /// that only process the click on MouseUp when the mouse is not dragged. </remarks>
    public class DragAndClickManipulator : MouseManipulator
    {
        private bool m_Active = false;
        private Vector2 m_OriginalPosition;

        /// <summary>
        /// Delegate called when mouse begins dragged. It usually should start a drag operation using the <see cref="DragAndDrop"/> API.
        /// </summary>
        public System.Action onStartDragging { get; set; }
        /// <summary> Delegate called when mouse is released and it hasn't been dragged.</summary>
        public System.Action onClick { get; set; }
        /// <summary> The distance the mouse has to move before <see cref="onStartDragging"/> is called. </summary>
        public float dragDistanceThreshold { get; set; } = 5;

        public DragAndClickManipulator()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        /// <param name="onStartDragging">Sets <see cref="onStartDragging"/></param>
        /// <param name="onProcessClick">Sets <see cref="onClick"/></param>
        public DragAndClickManipulator(System.Action onStartDragging, System.Action onProcessClick) : this()
        {
            this.onStartDragging = onStartDragging;
            this.onClick = onProcessClick;
        }

        [RemoveFromDocs]
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
        }

        [RemoveFromDocs]
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
        }

        private void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(e))
                return;

            m_OriginalPosition = e.mousePosition;
            m_Active = true;
            target.CaptureMouse();
            e.StopImmediatePropagation();
        }

        private void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            if(Vector2.Distance(m_OriginalPosition, e.mousePosition) >=dragDistanceThreshold)
            {
                onStartDragging?.Invoke();
                m_Active = false;
                target.ReleaseMouse();
            }

            e.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !CanStopManipulation(e))
                return;

            onClick?.Invoke();
            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();
        }

        private void OnMouseCaptureOut(MouseCaptureOutEvent e)
        {
            m_Active = false;
        }
    }
}