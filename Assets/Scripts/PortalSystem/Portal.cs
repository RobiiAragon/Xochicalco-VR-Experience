using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Xochicalco.PortalSystem
{
    public class Portal : MonoBehaviour
    {
        [Header("Main Settings")]
        public Portal linkedPortal;
        public MeshRenderer screen;
        public int recursionLimit = 5;

        [Header("Advanced Settings")]
        public float nearClipOffset = 0.05f;
        public float nearClipLimit = 0.2f;

        // VR Support
        [Header("VR Settings")]
        public Transform player;

        // Private variables
        private RenderTexture viewTexture;
        private Camera portalCam;
        private Camera playerCam;
        private List<PortalTraveller> trackedTravellers;
        private MeshFilter screenMeshFilter;
        
        // Teleportation debounce
        private float lastTeleportTime = 0f;
        private const float teleportCooldown = 1.0f;

        void Awake()
        {
            SetupPlayerCamera();
            SetupPortalCamera();
            trackedTravellers = new List<PortalTraveller>();
            screenMeshFilter = screen.GetComponent<MeshFilter>();
            
            // Set up material for portal screen
            if (screen.material != null)
            {
                screen.material.SetInt("_DisplayMask", 1);
            }
        }

        void SetupPlayerCamera()
        {
            // Try to find VR camera first
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                player = xrOrigin.transform;
                playerCam = xrOrigin.Camera;
                
                // Add PortalTraveller to XR Origin if it doesn't have one
                PortalTraveller traveller = xrOrigin.GetComponent<PortalTraveller>();
                if (traveller == null)
                {
                    traveller = xrOrigin.gameObject.AddComponent<PortalTraveller>();
                    Debug.Log("🎭 Added PortalTraveller to XR Origin");
                }
                
                // Add collider to XR Origin if it doesn't have one
                Collider col = xrOrigin.GetComponent<Collider>();
                if (col == null)
                {
                    CapsuleCollider capsule = xrOrigin.gameObject.AddComponent<CapsuleCollider>();
                    capsule.isTrigger = true;
                    capsule.height = 1.8f;
                    capsule.radius = 0.3f;
                    capsule.center = new Vector3(0, 0.9f, 0);
                    Debug.Log("🔷 Added trigger collider to XR Origin");
                }
                
                Debug.Log("🥽 VR camera detected for portal system!");
            }
            else
            {
                // Fallback to main camera
                playerCam = Camera.main;
                if (playerCam != null)
                    player = playerCam.transform;
                Debug.Log("🖥️ Desktop camera detected for portal system!");
            }
        }

        void SetupPortalCamera()
        {
            // Create portal camera if it doesn't exist
            portalCam = GetComponentInChildren<Camera>();
            if (portalCam == null)
            {
                GameObject camObj = new GameObject("Portal Camera");
                camObj.transform.SetParent(transform);
                portalCam = camObj.AddComponent<Camera>();
                
                // Set up URP camera data
                var cameraData = camObj.AddComponent<UniversalAdditionalCameraData>();
                cameraData.renderType = CameraRenderType.Base;
                cameraData.renderShadows = true;
            }
            
            portalCam.enabled = false;
        }

        void LateUpdate()
        {
            HandleTravellers();
        }

        void HandleTravellers()
        {
            for (int i = 0; i < trackedTravellers.Count; i++)
            {
                PortalTraveller traveller = trackedTravellers[i];
                Transform travellerT = traveller.transform;
                var m = linkedPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix * travellerT.localToWorldMatrix;

                Vector3 offsetFromPortal = travellerT.position - transform.position;
                int portalSide = System.Math.Sign(Vector3.Dot(offsetFromPortal, transform.forward));
                int portalSideOld = System.Math.Sign(Vector3.Dot(traveller.previousOffsetFromPortal, transform.forward));
                
                // Teleport the traveller if it has crossed from one side of the portal to the other
                if (portalSide != portalSideOld)
                {
                    // Check teleportation cooldown
                    if (Time.time - lastTeleportTime < teleportCooldown)
                    {
                        Debug.Log($"⏱️ Teleportation on cooldown for {traveller.name}");
                        continue;
                    }

                    var positionOld = travellerT.position;
                    var rotOld = travellerT.rotation;
                    traveller.Teleport(transform, linkedPortal.transform, m.GetColumn(3), m.rotation);
                    
                    lastTeleportTime = Time.time; // Set cooldown
                    
                    if (traveller.graphicsClone != null)
                        traveller.graphicsClone.transform.SetPositionAndRotation(positionOld, rotOld);
                    
                    // Handle portal entry
                    linkedPortal.OnTravellerEnterPortal(traveller);
                    trackedTravellers.RemoveAt(i);
                    i--;
                    
                    Debug.Log($"🌀 {traveller.name} teleported from {name} to {linkedPortal.name}");
                }
                else
                {
                    if (traveller.graphicsClone != null)
                        traveller.graphicsClone.transform.SetPositionAndRotation(m.GetColumn(3), m.rotation);
                    
                    UpdateSliceParams(traveller);
                    traveller.previousOffsetFromPortal = offsetFromPortal;
                }
            }
        }

        // Called before any portal cameras are rendered for the current frame
        public void PrePortalRender()
        {
            foreach (var traveller in trackedTravellers)
            {
                UpdateSliceParams(traveller);
            }
        }

        // Manually render the camera attached to this portal
        public void Render()
        {
            if (linkedPortal == null || playerCam == null) return;

            // Skip rendering if player is not looking at the linked portal
            if (!CameraUtility.VisibleFromCamera(linkedPortal.screen, playerCam))
            {
                return;
            }

            CreateViewTexture();

            var localToWorldMatrix = playerCam.transform.localToWorldMatrix;
            var renderPositions = new Vector3[recursionLimit];
            var renderRotations = new Quaternion[recursionLimit];

            int startIndex = 0;
            portalCam.projectionMatrix = playerCam.projectionMatrix;
            
            for (int i = 0; i < recursionLimit; i++)
            {
                if (i > 0)
                {
                    // No need for recursive rendering if linked portal is not visible through this portal
                    if (!CameraUtility.BoundsOverlap(screenMeshFilter, linkedPortal.screenMeshFilter, portalCam))
                    {
                        break;
                    }
                }
                
                localToWorldMatrix = transform.localToWorldMatrix * linkedPortal.transform.worldToLocalMatrix * localToWorldMatrix;
                int renderOrderIndex = recursionLimit - i - 1;
                renderPositions[renderOrderIndex] = localToWorldMatrix.GetColumn(3);
                renderRotations[renderOrderIndex] = localToWorldMatrix.rotation;

                portalCam.transform.SetPositionAndRotation(renderPositions[renderOrderIndex], renderRotations[renderOrderIndex]);
                startIndex = renderOrderIndex;
            }

            // Hide screen so that camera can see through portal
            screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            if (linkedPortal.screen.material != null)
                linkedPortal.screen.material.SetInt("_DisplayMask", 0);

            for (int i = startIndex; i < recursionLimit; i++)
            {
                portalCam.transform.SetPositionAndRotation(renderPositions[i], renderRotations[i]);
                SetNearClipPlane();
                portalCam.Render();

                if (i == startIndex)
                {
                    if (linkedPortal.screen.material != null)
                        linkedPortal.screen.material.SetInt("_DisplayMask", 1);
                }
            }

            // Unhide objects hidden at start of render
            screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        // Called once all portals have been rendered, but before the player camera renders
        public void PostPortalRender()
        {
            foreach (var traveller in trackedTravellers)
            {
                UpdateSliceParams(traveller);
            }
            ProtectScreenFromClipping(playerCam.transform.position);
        }

        void CreateViewTexture()
        {
            if (viewTexture == null || viewTexture.width != Screen.width || viewTexture.height != Screen.height)
            {
                if (viewTexture != null)
                {
                    viewTexture.Release();
                }
                viewTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.DefaultHDR);
                viewTexture.Create();
                
                // Render the view from the portal camera to the view texture
                portalCam.targetTexture = viewTexture;
                
                // Display the view texture on the screen of the linked portal
                if (linkedPortal.screen.material != null)
                {
                    linkedPortal.screen.material.SetTexture("_MainTex", viewTexture);
                    linkedPortal.screen.material.SetInt("_DisplayMask", 1);
                    Debug.Log($"🖼️ Set portal texture for {linkedPortal.name}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ No material found on {linkedPortal.name} screen!");
                }
            }
        }

        // Sets the thickness of the portal screen so as not to clip with camera near plane when player goes through
        float ProtectScreenFromClipping(Vector3 viewPoint)
        {
            float halfHeight = playerCam.nearClipPlane * Mathf.Tan(playerCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfWidth = halfHeight * playerCam.aspect;
            float dstToNearClipPlaneCorner = new Vector3(halfWidth, halfHeight, playerCam.nearClipPlane).magnitude;
            float screenThickness = dstToNearClipPlaneCorner;

            Transform screenT = screen.transform;
            bool camFacingSameDirAsPortal = Vector3.Dot(transform.forward, transform.position - viewPoint) > 0;
            screenT.localScale = new Vector3(screenT.localScale.x, screenT.localScale.y, screenThickness);
            screenT.localPosition = Vector3.forward * screenThickness * ((camFacingSameDirAsPortal) ? 0.5f : -0.5f);
            return screenThickness;
        }

        void UpdateSliceParams(PortalTraveller traveller)
        {
            // Calculate slice normal
            int side = SideOfPortal(traveller.transform.position);
            Vector3 sliceNormal = transform.forward * -side;
            Vector3 cloneSliceNormal = linkedPortal.transform.forward * side;

            // Calculate slice centre
            Vector3 slicePos = transform.position;
            Vector3 cloneSlicePos = linkedPortal.transform.position;

            // Adjust slice offset so that when player standing on other side of portal to the object, the slice doesn't clip through
            float sliceOffsetDst = 0;
            float cloneSliceOffsetDst = 0;
            float screenThickness = screen.transform.localScale.z;

            bool playerSameSideAsTraveller = SameSideOfPortal(playerCam.transform.position, traveller.transform.position);
            if (!playerSameSideAsTraveller)
            {
                sliceOffsetDst = -screenThickness;
            }
            bool playerSameSideAsCloneAppearing = side != linkedPortal.SideOfPortal(playerCam.transform.position);
            if (!playerSameSideAsCloneAppearing)
            {
                cloneSliceOffsetDst = -screenThickness;
            }

            // Apply parameters to materials that support slicing
            if (traveller.originalMaterials != null)
            {
                for (int i = 0; i < traveller.originalMaterials.Length; i++)
                {
                    var mat = traveller.originalMaterials[i];
                    if (mat.HasProperty("_SliceCentre"))
                    {
                        mat.SetVector("_SliceCentre", slicePos);
                        mat.SetVector("_SliceNormal", sliceNormal);
                        mat.SetFloat("_SliceOffsetDst", sliceOffsetDst);
                    }
                }
            }

            if (traveller.cloneMaterials != null)
            {
                for (int i = 0; i < traveller.cloneMaterials.Length; i++)
                {
                    var mat = traveller.cloneMaterials[i];
                    if (mat.HasProperty("_SliceCentre"))
                    {
                        mat.SetVector("_SliceCentre", cloneSlicePos);
                        mat.SetVector("_SliceNormal", cloneSliceNormal);
                        mat.SetFloat("_SliceOffsetDst", cloneSliceOffsetDst);
                    }
                }
            }
        }

        // Use custom projection matrix to align portal camera's near clip plane with the surface of the portal
        void SetNearClipPlane()
        {
            Transform clipPlane = transform;
            int dot = System.Math.Sign(Vector3.Dot(clipPlane.forward, transform.position - portalCam.transform.position));

            Vector3 camSpacePos = portalCam.worldToCameraMatrix.MultiplyPoint(clipPlane.position);
            Vector3 camSpaceNormal = portalCam.worldToCameraMatrix.MultiplyVector(clipPlane.forward) * dot;
            float camSpaceDst = -Vector3.Dot(camSpacePos, camSpaceNormal) + nearClipOffset;

            // Don't use oblique clip plane if very close to portal as it seems this can cause some visual artifacts
            if (Mathf.Abs(camSpaceDst) > nearClipLimit)
            {
                Vector4 clipPlaneCameraSpace = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);
                // Update projection based on new clip plane
                portalCam.projectionMatrix = playerCam.CalculateObliqueMatrix(clipPlaneCameraSpace);
            }
            else
            {
                portalCam.projectionMatrix = playerCam.projectionMatrix;
            }
        }

        void OnTravellerEnterPortal(PortalTraveller traveller)
        {
            if (!trackedTravellers.Contains(traveller))
            {
                traveller.EnterPortalThreshold();
                traveller.previousOffsetFromPortal = traveller.transform.position - transform.position;
                trackedTravellers.Add(traveller);
                
                Debug.Log($"🚪 {traveller.name} entered portal {name}");
            }
        }

        void OnTriggerEnter(Collider other)
        {
            var traveller = other.GetComponent<PortalTraveller>();
            if (traveller)
            {
                OnTravellerEnterPortal(traveller);
            }
        }

        void OnTriggerExit(Collider other)
        {
            var traveller = other.GetComponent<PortalTraveller>();
            if (traveller && trackedTravellers.Contains(traveller))
            {
                traveller.ExitPortalThreshold();
                trackedTravellers.Remove(traveller);
            }
        }

        // Helper methods
        int SideOfPortal(Vector3 pos)
        {
            return System.Math.Sign(Vector3.Dot(pos - transform.position, transform.forward));
        }

        bool SameSideOfPortal(Vector3 posA, Vector3 posB)
        {
            return SideOfPortal(posA) == SideOfPortal(posB);
        }

        Vector3 portalCamPos
        {
            get { return portalCam.transform.position; }
        }

        void OnValidate()
        {
            if (linkedPortal != null)
            {
                linkedPortal.linkedPortal = this;
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position, new Vector3(2, 3, 0.1f));

            if (linkedPortal != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, linkedPortal.transform.position);
            }
        }

        void OnDestroy()
        {
            if (viewTexture != null)
            {
                viewTexture.Release();
                DestroyImmediate(viewTexture);
            }
        }
    }
}