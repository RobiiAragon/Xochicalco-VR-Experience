using System.Collections.Generic;
using UnityEngine;

public class VRPortalTraveller : PortalTraveller {

    public override void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot) {
        // Para VR, manejar XR Origin teleportation
        var xrOrigin = GetComponent<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            // Get the current XR Origin position
            Vector3 currentXROriginPos = xrOrigin.transform.position;
            
            // For VR teleportation, we want to place the XR Origin so that the camera 
            // (which represents the player's head) ends up at the calculated position
            // But we need to account for the camera's offset from the XR Origin
            Vector3 cameraOffset = xrOrigin.Camera.transform.position - xrOrigin.transform.position;
            
            // Calculate where to place the XR Origin so the camera ends up at 'pos'
            // Only subtract horizontal offset, keep the camera at a reasonable height
            Vector3 targetXROriginPos = pos - new Vector3(cameraOffset.x, 0, cameraOffset.z);
            
            // Ensure the XR Origin doesn't go below ground level
            // Assume ground is at Y = 0, adjust if your ground is at a different level
            if (targetXROriginPos.y < 0)
                targetXROriginPos.y = 0;
            
            // Use XR Origin's MoveCameraToWorldLocation for proper VR teleportation
            xrOrigin.MoveCameraToWorldLocation(targetXROriginPos);
            
            // Calculate the rotation difference and apply it
            Vector3 forward = rot * Vector3.forward;
            Vector3 up = Vector3.up;
            xrOrigin.MatchOriginUpCameraForward(up, forward);
            
            Debug.Log($"🚶‍♂️ XR Origin teleported from {currentXROriginPos} to {targetXROriginPos}");
        }
        else
        {
            // Fallback para objetos no-VR
            base.Teleport(fromPortal, toPortal, pos, rot);
        }
    }

    public override void EnterPortalThreshold() {
        // Para VR, usar la cámara como graphics object si no se especifica otro
        if (graphicsObject == null)
        {
            var xrOrigin = GetComponent<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                // Usar el objeto de la cámara como referencia visual
                graphicsObject = xrOrigin.Camera.gameObject;
            }
            else
            {
                // Usar el propio objeto como fallback
                graphicsObject = gameObject;
            }
        }
        
        base.EnterPortalThreshold();
    }
}