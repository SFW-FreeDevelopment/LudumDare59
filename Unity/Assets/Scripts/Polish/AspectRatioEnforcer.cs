using UnityEngine;

namespace SignalScrubber.Polish
{
    /// <summary>
    /// Locks the camera to a fixed aspect ratio (default 16:9) by adjusting
    /// <see cref="Camera.rect"/> every frame. On monitors whose aspect does
    /// not match, the unused strip at the top+bottom (or left+right) stays
    /// black — Unity clears the full screen before the camera renders its
    /// viewport, so letterboxing is free.
    ///
    /// Keeps the game playing at the same apparent size and layout regardless
    /// of the player's monitor resolution, as long as it is at least as tall
    /// as the reference height.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public sealed class AspectRatioEnforcer : MonoBehaviour
    {
        [SerializeField] float targetWidth  = 16f;
        [SerializeField] float targetHeight = 9f;

        Camera _cam;

        void OnEnable()  => _cam = GetComponent<Camera>();
        void OnValidate() => _cam = GetComponent<Camera>();

        void LateUpdate()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (_cam == null) return;

            float target = targetWidth / targetHeight;
            float window = (float)Screen.width / Screen.height;
            float scaleHeight = window / target;

            Rect r;
            if (scaleHeight < 1f)
            {
                // Screen is narrower than target — letterbox top + bottom.
                r = new Rect(0f, (1f - scaleHeight) * 0.5f, 1f, scaleHeight);
            }
            else
            {
                // Screen is wider than target — pillarbox left + right.
                float scaleWidth = 1f / scaleHeight;
                r = new Rect((1f - scaleWidth) * 0.5f, 0f, scaleWidth, 1f);
            }

            if (_cam.rect != r) _cam.rect = r;
        }
    }
}
