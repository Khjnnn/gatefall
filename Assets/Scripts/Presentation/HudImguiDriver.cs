using System;
using UnityEngine;

namespace Gatefall
{
    // Fallback HUD renderer. It exists only while GatefallGame uses the previous IMGUI path, so the default
    // canvas path pays no per-frame OnGUI cost. MobileHud.Draw() uses no GUILayout, hence no layout pass.
    [DisallowMultipleComponent]
    public sealed class HudImguiDriver : MonoBehaviour
    {
        public Action Draw;
        private void Awake() { useGUILayout = false; }
        private void OnGUI() { if (Draw != null) Draw(); }
    }
}
