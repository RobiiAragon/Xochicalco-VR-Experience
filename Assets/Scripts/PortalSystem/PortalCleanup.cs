using UnityEngine;
using UnityEditor;

namespace Xochicalco.PortalSystem
{
    public class PortalCleanup : MonoBehaviour
    {
        [MenuItem("Tools/Portal System/Emergency Cleanup")]
        public static void EmergencyCleanup()
        {
            Debug.Log("🚨 Starting emergency portal cleanup...");

            // 1. Destroy all clones
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int cleanupsCount = 0;

            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("(Clone)") && 
                    (obj.name.Contains("XR Origin") || obj.name.Contains("Hands")))
                {
                    Debug.Log($"🗑️ Destroying problematic clone: {obj.name}");
                    DestroyImmediate(obj);
                    cleanupsCount++;
                }
            }

            // 2. Clean up multiple Audio Listeners
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners.Length > 1)
            {
                Debug.Log($"🔊 Found {listeners.Length} Audio Listeners, keeping only the main one");
                
                // Keep the first one that's on the main camera or XR Origin
                AudioListener mainListener = null;
                foreach (var listener in listeners)
                {
                    if (listener.name.Contains("Camera") || listener.name.Contains("XR Origin"))
                    {
                        mainListener = listener;
                        break;
                    }
                }

                // Remove all others
                foreach (var listener in listeners)
                {
                    if (listener != mainListener)
                    {
                        Debug.Log($"🗑️ Removing extra Audio Listener from: {listener.name}");
                        DestroyImmediate(listener);
                    }
                }
            }

            // 3. Reset all portal travellers
            PortalTraveller[] travellers = FindObjectsByType<PortalTraveller>(FindObjectsSortMode.None);
            foreach (var traveller in travellers)
            {
                if (traveller.graphicsClone != null)
                {
                    Debug.Log($"🧹 Cleaning clone from: {traveller.name}");
                    DestroyImmediate(traveller.graphicsClone);
                    traveller.graphicsClone = null;
                }
            }

            // 4. Reset portal tracking
            Portal[] portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
            foreach (var portal in portals)
            {
                // This will clear the tracked travellers list on next frame
                Debug.Log($"🔄 Reset portal: {portal.name}");
            }

            Debug.Log($"✅ Emergency cleanup complete! Removed {cleanupsCount} problematic objects");
            Debug.Log("🎮 You can now try the portals again safely");
        }

        [MenuItem("Tools/Portal System/Fix Audio Listeners")]
        public static void FixAudioListeners()
        {
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            
            if (listeners.Length <= 1)
            {
                Debug.Log("✅ Audio Listeners are fine");
                return;
            }

            Debug.Log($"🔊 Found {listeners.Length} Audio Listeners, fixing...");

            // Find the main camera listener
            AudioListener mainListener = null;
            foreach (var listener in listeners)
            {
                if (listener.GetComponent<Camera>() != null)
                {
                    mainListener = listener;
                    break;
                }
            }

            // If no camera listener found, keep the first one
            if (mainListener == null)
                mainListener = listeners[0];

            // Remove all others
            int removed = 0;
            foreach (var listener in listeners)
            {
                if (listener != mainListener)
                {
                    Debug.Log($"🗑️ Removing Audio Listener from: {listener.name}");
                    DestroyImmediate(listener);
                    removed++;
                }
            }

            Debug.Log($"✅ Fixed! Removed {removed} extra Audio Listeners");
        }

        [MenuItem("Tools/Portal System/Debug Scene State")]
        public static void DebugSceneState()
        {
            Debug.Log("=== PORTAL SCENE DEBUG ===");

            // XR Origins
            var xrOrigins = FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None);
            Debug.Log($"XR Origins: {xrOrigins.Length}");
            foreach (var origin in xrOrigins)
            {
                Debug.Log($"  - {origin.name} at {origin.transform.position}");
            }

            // Portals
            Portal[] portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
            Debug.Log($"Portals: {portals.Length}");
            foreach (var portal in portals)
            {
                Debug.Log($"  - {portal.name} at {portal.transform.position}");
            }

            // Portal Travellers
            PortalTraveller[] travellers = FindObjectsByType<PortalTraveller>(FindObjectsSortMode.None);
            Debug.Log($"Portal Travellers: {travellers.Length}");
            foreach (var traveller in travellers)
            {
                bool hasClone = traveller.graphicsClone != null;
                Debug.Log($"  - {traveller.name} (Clone: {hasClone})");
            }

            // Audio Listeners
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            Debug.Log($"Audio Listeners: {listeners.Length}");
            foreach (var listener in listeners)
            {
                Debug.Log($"  - {listener.name}");
            }

            // Clones
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int cloneCount = 0;
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("(Clone)"))
                {
                    cloneCount++;
                }
            }
            Debug.Log($"Total Clone Objects: {cloneCount}");
        }
    }
}