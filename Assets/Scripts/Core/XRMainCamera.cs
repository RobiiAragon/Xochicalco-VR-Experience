using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XRMainCamera : MonoBehaviour {

    Portal[] portals;

    void Awake() {
        // Buscar todos los portales al inicio
        portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
    }

    void OnPreCull() {
        // Para VR, renderizar todos los portales en el orden correcto
        if (portals == null || portals.Length == 0) {
            // Re-buscar portales si no hay ninguno (por si se agregaron después)
            portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
        }

        // Seguir el patrón del MainCamera original pero con protección para VR
        for (int i = 0; i < portals.Length; i++) {
            if (portals[i] != null) {
                try {
                    portals[i].PrePortalRender();
                } catch (System.Exception e) {
                    Debug.LogWarning($"Portal PreRender failed safely in VR: {e.Message}");
                }
            }
        }

        for (int i = 0; i < portals.Length; i++) {
            if (portals[i] != null) {
                try {
                    portals[i].Render();
                } catch (System.Exception e) {
                    Debug.LogWarning($"Portal render failed safely in VR: {e.Message}");
                }
            }
        }

        for (int i = 0; i < portals.Length; i++) {
            if (portals[i] != null) {
                try {
                    portals[i].PostPortalRender();
                } catch (System.Exception e) {
                    Debug.LogWarning($"Portal PostRender failed safely in VR: {e.Message}");
                }
            }
        }
    }
}