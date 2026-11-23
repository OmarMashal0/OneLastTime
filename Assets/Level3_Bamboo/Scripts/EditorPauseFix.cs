namespace Level3Bamboo
{
    // EDITOR-ONLY WORKAROUND. In a built game this does nothing.
    //
    // The project's shared editor tool (Assets/_Core/Scripts/Editor/McpLocalBridge.cs, not part of Level 3)
    // sets Time.timeScale back to 1 on every editor update while in Play Mode. That would undo the HUD pause
    // button when testing in the Unity Editor. While the game is paused, this class runs that tool's update
    // itself and then puts the pause back, so the pause button also works in the Editor.
    public static class EditorPauseFix
    {
#if UNITY_EDITOR
        private static UnityEditor.EditorApplication.CallbackFunction bridgeUpdate;
        private static bool keepPaused;
#endif

        public static void KeepPaused(bool paused)
        {
#if UNITY_EDITOR
            keepPaused = paused;
            if (paused)
                TakeOverBridgeUpdate();
            else
                GiveBackBridgeUpdate();
#endif
        }

#if UNITY_EDITOR
        private static void TakeOverBridgeUpdate()
        {
            if (bridgeUpdate != null)
                return;

            // Look up the shared tool's private OnEditorUpdate method by name.
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var bridge = assembly.GetType("OneLastTime.Editor.McpLocalBridge");
                var update = bridge?.GetMethod("OnEditorUpdate",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                if (update == null)
                    continue;

                bridgeUpdate = (UnityEditor.EditorApplication.CallbackFunction)System.Delegate.CreateDelegate(
                    typeof(UnityEditor.EditorApplication.CallbackFunction), update);
                UnityEditor.EditorApplication.update -= bridgeUpdate;
                UnityEditor.EditorApplication.update += RunBridgeThenStayPaused;
                UnityEditor.EditorApplication.delayCall += StayPaused;
                return;
            }
        }

        private static void GiveBackBridgeUpdate()
        {
            if (bridgeUpdate == null)
                return;
            UnityEditor.EditorApplication.delayCall -= StayPaused;
            UnityEditor.EditorApplication.update -= RunBridgeThenStayPaused;
            UnityEditor.EditorApplication.update += bridgeUpdate;
            bridgeUpdate = null;
        }

        private static void RunBridgeThenStayPaused()
        {
            try
            {
                bridgeUpdate?.Invoke();
            }
            finally
            {
                StayPaused();
            }
        }

        private static void StayPaused()
        {
            if (keepPaused && UnityEditor.EditorApplication.isPlaying)
                UnityEngine.Time.timeScale = 0f;
        }
#endif
    }
}
