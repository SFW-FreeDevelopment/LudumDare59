using UnityEngine;
using UnityEngine.UIElements;

namespace SignalScrubber.UI
{
    /// <summary>
    /// Rotary knob control. Drag vertically to change <see cref="value"/>
    /// in [0, 1]. 200 px of travel covers the full range.
    ///
    /// Emits <see cref="ChangeEvent{Single}"/> on value changes so UXML
    /// bindings and RegisterValueChangedCallback wire up naturally.
    /// </summary>
    [UxmlElement]
    public partial class KnobElement : VisualElement, INotifyValueChanged<float>
    {
        const float DragPixelsForFullRange = 200f;

        [UxmlAttribute] public float AngleRange { get; set; } = 270f;

        float _value;
        [UxmlAttribute]
        public float value
        {
            get => _value;
            set => SetValueWithoutNotify(Mathf.Clamp01(value));
        }

        readonly VisualElement _indicator;
        Vector2 _dragStartPos;
        float _dragStartValue;
        bool _dragging;

        public KnobElement()
        {
            AddToClassList("knob");
            focusable = true;
            pickingMode = PickingMode.Position;

            _indicator = new VisualElement { name = "indicator" };
            _indicator.AddToClassList("knob-indicator");
            _indicator.pickingMode = PickingMode.Ignore;
            Add(_indicator);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(_ => _dragging = false);

            UpdateIndicatorRotation();
        }

        public void SetValueWithoutNotify(float newValue)
        {
            newValue = Mathf.Clamp01(newValue);
            if (Mathf.Approximately(newValue, _value)) return;
            _value = newValue;
            UpdateIndicatorRotation();
        }

        void UpdateIndicatorRotation()
        {
            float halfRange = AngleRange * 0.5f;
            float angle = Mathf.Lerp(-halfRange, halfRange, _value);
            _indicator.style.rotate = new StyleRotate(new Rotate(new Angle(angle, AngleUnit.Degree)));
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;
            _dragging = true;
            _dragStartPos = evt.position;
            _dragStartValue = _value;
            this.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_dragging) return;
            float deltaY = evt.position.y - _dragStartPos.y;
            float newValue = Mathf.Clamp01(_dragStartValue - deltaY / DragPixelsForFullRange);
            if (Mathf.Approximately(newValue, _value)) return;

            float previous = _value;
            _value = newValue;
            UpdateIndicatorRotation();

            using (var change = ChangeEvent<float>.GetPooled(previous, _value))
            {
                change.target = this;
                SendEvent(change);
            }
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!_dragging) return;
            _dragging = false;
            this.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }
    }
}
