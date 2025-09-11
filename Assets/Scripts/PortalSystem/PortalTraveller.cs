using System.Collections.Generic;
using UnityEngine;

namespace Xochicalco.PortalSystem
{
    public class PortalTraveller : MonoBehaviour
    {
        public GameObject graphicsObject;
        public GameObject graphicsClone { get; set; }
        public Vector3 previousOffsetFromPortal { get; set; }

        public Material[] originalMaterials { get; set; }
        public Material[] cloneMaterials { get; set; }

        void Start()
        {
            // If no graphics object assigned, use this object
            if (graphicsObject == null)
                graphicsObject = gameObject;
        }

        public virtual void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
        {
            // For VR player, handle XR Origin teleportation
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
                
                Debug.Log($"🥽 VR Teleported XR Origin to {targetXROriginPos} (Target camera pos: {pos}, Camera offset: {cameraOffset})");
            }
            else
            {
                // Standard teleportation for non-VR objects
                transform.position = pos;
                transform.rotation = rot;
                Debug.Log($"🚶 Teleported {name} to {pos}");
            }
        }

        // Called when first touches portal
        public virtual void EnterPortalThreshold()
        {
            // For XR Origin, we don't want to clone the entire VR rig
            var xrOrigin = GetComponent<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                Debug.Log("🥽 XR Origin entered portal threshold - skipping clone creation");
                return; // Don't create clones for VR player
            }

            if (graphicsClone == null)
            {
                graphicsClone = Instantiate(graphicsObject);
                graphicsClone.transform.parent = graphicsObject.transform.parent;
                graphicsClone.transform.localScale = graphicsObject.transform.localScale;
                
                // Remove problematic components from clone
                RemoveProblematicComponents(graphicsClone);
                
                originalMaterials = GetMaterials(graphicsObject);
                cloneMaterials = GetMaterials(graphicsClone);
                
                Debug.Log($"🎭 Created clone for {name}");
            }
            else
            {
                graphicsClone.SetActive(true);
            }
        }

        // Called once no longer touching portal (excluding when teleporting)
        public virtual void ExitPortalThreshold()
        {
            if (graphicsClone != null)
                graphicsClone.SetActive(false);
            
            // Disable slicing
            if (originalMaterials != null)
            {
                for (int i = 0; i < originalMaterials.Length; i++)
                {
                    if (originalMaterials[i].HasProperty("_SliceNormal"))
                        originalMaterials[i].SetVector("_SliceNormal", Vector3.zero);
                }
            }
        }

        public void SetSliceOffsetDst(float dst, bool clone)
        {
            Material[] materials = clone ? cloneMaterials : originalMaterials;
            if (materials != null)
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i].HasProperty("_SliceOffsetDst"))
                        materials[i].SetFloat("_SliceOffsetDst", dst);
                }
            }
        }

        Material[] GetMaterials(GameObject g)
        {
            var renderers = g.GetComponentsInChildren<MeshRenderer>();
            var matList = new List<Material>();
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    matList.Add(mat);
                }
            }
            return matList.ToArray();
        }

        void RemoveProblematicComponents(GameObject clone)
        {
            // Remove all colliders to prevent physics conflicts
            Collider[] colliders = clone.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
                DestroyImmediate(col);

            // Remove all rigidbodies
            Rigidbody[] rigidbodies = clone.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rigidbodies)
                DestroyImmediate(rb);

            // Remove all AudioListeners to prevent multiple listener warnings
            AudioListener[] audioListeners = clone.GetComponentsInChildren<AudioListener>();
            foreach (var listener in audioListeners)
                DestroyImmediate(listener);

            // Remove all MonoBehaviour scripts
            MonoBehaviour[] scripts = clone.GetComponentsInChildren<MonoBehaviour>();
            foreach (var script in scripts)
            {
                // Don't remove renderers or other essential components
                if (!(script is Renderer) && !(script is MeshFilter))
                    DestroyImmediate(script);
            }

            // Remove XR components if any
            var xrComponents = clone.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>();
            foreach (var xr in xrComponents)
                DestroyImmediate(xr);

            Debug.Log($"🧹 Cleaned up clone: {clone.name}");
        }

        void OnDestroy()
        {
            if (graphicsClone != null)
                DestroyImmediate(graphicsClone);
        }
    }
}