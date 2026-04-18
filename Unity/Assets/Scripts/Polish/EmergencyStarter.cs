using SignalScrubber.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SignalScrubber.Polish
{
    /// <summary>
    /// Minimal-dependency input poller that starts the game on Space,
    /// Enter, or left-click. Lives on its own GameObject; uses
    /// FindFirstObjectByType to locate the SignalManager at runtime so
    /// it works even if nothing else in the scene is wired correctly.
    ///
    /// Logs loudly on Awake so we can verify compile + activation just
    /// by looking at the console.
    /// </summary>
    public sealed class EmergencyStarter : MonoBehaviour
    {
        SignalManager _manager;
        bool _started;

        void Awake()
        {
            Debug.Log("[EmergencyStarter] Awake — waiting for Space / Enter / Click.");
        }

        void Start()
        {
            _manager = FindFirstObjectByType<SignalManager>();
            Debug.Log("[EmergencyStarter] Start — SignalManager "
                      + (_manager != null ? "FOUND" : "NOT FOUND"));
        }

        void Update()
        {
            if (_started) return;
            if (!PollInput()) return;

            _started = true;
            if (_manager != null)
            {
                Debug.Log("[EmergencyStarter] Input detected — calling SignalManager.Begin()");
                _manager.Begin();
            }
            else
            {
                Debug.LogError("[EmergencyStarter] Input detected but SignalManager is missing.");
            }
        }

        static bool PollInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame) return true;
                if (kb.enterKey.wasPressedThisFrame) return true;
            }
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space))  return true;
            if (Input.GetKeyDown(KeyCode.Return)) return true;
            if (Input.GetMouseButtonDown(0))      return true;
#endif
            return false;
        }
    }
}
