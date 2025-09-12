using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Portal : MonoBehaviour 
{
    [Header ("Main Settings")]
    public Portal linkedPortal;
    public MeshRenderer screen;
    public int recursionLimit = 5;
    
    [Header ("Advanced Settings")]
    public float nearClipOffset = 0.05f;
    public float nearClipLimit = 0.2f;

    // Private variables
    RenderTexture viewTexture;
    Camera portalCam;
    Camera playerCam;
    Material firstRecursionMat;
    List<PortalTraveller> trackedTravellers;
    MeshFilter screenMeshFilter;
    
    [HideInInspector]
    public PortalSurface portalSurface;

    void Awake () {
        playerCam = Camera.main;
        portalCam = GetComponentInChildren<Camera> ();
        trackedTravellers = new List<PortalTraveller> ();
        screenMeshFilter = screen.GetComponent<MeshFilter> ();
        
        // Get PortalSurface component
        portalSurface = GetComponent<PortalSurface>();
        if (portalSurface == null) {
            portalSurface = GetComponentInChildren<PortalSurface>();
        }
        
        if (portalCam != null) {
            portalCam.enabled = false;
        }
    }

    void LateUpdate () {
        HandleTravellers ();
    }

    void HandleTravellers () {
        for (int i = 0; i < trackedTravellers.Count; i++) {
            PortalTraveller traveller = trackedTravellers[i];
            Transform travellerT = traveller.transform;
            var m = transform.localToWorldMatrix * linkedPortal.transform.worldToLocalMatrix * travellerT.localToWorldMatrix;

            Vector3 offsetFromPortal = travellerT.position - transform.position;
            int portalSide = System.Math.Sign (Vector3.Dot (offsetFromPortal, transform.forward));
            int portalSideOld = System.Math.Sign (Vector3.Dot (traveller.previousOffsetFromPortal, transform.forward));
            
            // Teleport the traveller if it has crossed from one side of the portal to the other
            if (portalSide != portalSideOld) {
                var positionOld = travellerT.position;
                var rotOld = travellerT.rotation;
                traveller.Teleport (transform, linkedPortal.transform, m.GetColumn (3), m.rotation);
                traveller.graphicsClone.transform.SetPositionAndRotation (positionOld, rotOld);
                // Can't rely on OnTriggerEnter/Exit to be called next frame since it depends on when physics update runs
                linkedPortal.OnTravellerEnterPortal (traveller);
                trackedTravellers.RemoveAt (i);
                i--;
            } else {
                traveller.graphicsClone.transform.SetPositionAndRotation (m.GetColumn (3), m.rotation);
                UpdateSliceParams (traveller);
                traveller.previousOffsetFromPortal = offsetFromPortal;
            }
        }
    }

    // Called before any portal cameras are rendered for the current frame
    public void PrePortalRender () {
        playerCam.projectionMatrix = playerCam.CalculateObliqueMatrix (CameraUtility.GetCameraPlaneCameraSpace (playerCam, transform.position, transform.forward));
    }

    // Manually render the camera attached to this portal
    // Called after PrePortalRender, and before PostPortalRender
    public void Render () {
        // Skip rendering the view from this portal if player is not looking at the linked portal
        if (!CameraUtility.VisibleFromCamera (linkedPortal.screen, playerCam)) {
            return;
        }

        CreateViewTexture ();

        var localToWorldMatrix = playerCam.transform.localToWorldMatrix;
        var renderPositions = new Vector3[recursionLimit];
        var renderRotations = new Quaternion[recursionLimit];

        for (int i = 0; i < recursionLimit; i++) {
            if (i > 0) {
                // No need for recursive rendering in VR for now
                break;
            }

            localToWorldMatrix = transform.localToWorldMatrix * linkedPortal.transform.worldToLocalMatrix * localToWorldMatrix;
            int renderOrderIndex = recursionLimit - i - 1;
            renderPositions[renderOrderIndex] = localToWorldMatrix.GetColumn (3);
            renderRotations[renderOrderIndex] = localToWorldMatrix.rotation;

            portalCam.transform.SetPositionAndRotation (renderPositions[renderOrderIndex], renderRotations[renderOrderIndex]);
            
            for (int j = 0; j < trackedTravellers.Count; j++) {
                UpdateSliceParams (trackedTravellers[j]);
            }

            portalCam.Render ();

            if (i == 0) {
                SetNearClipPlane ();
            }
        }
    }

    void HandleClipping () {
        // Skip clipping for VR for now - can be added later
    }

    // Called once all portals have been rendered, but before the player camera renders
    public void PostPortalRender () {
        playerCam.projectionMatrix = playerCam.CalculateObliqueMatrix (CameraUtility.GetCameraPlaneCameraSpace (playerCam, transform.position, -transform.forward));
    }
    
    void CreateViewTexture () {
        if (viewTexture == null || viewTexture.width != Screen.width || viewTexture.height != Screen.height) {
            if (viewTexture != null) {
                viewTexture.Release ();
            }
            viewTexture = new RenderTexture (Screen.width, Screen.height, 0);
            // Render the view from the portal camera to the view texture
            portalCam.targetTexture = viewTexture;
            // Display the view texture on the screen of the linked portal
            linkedPortal.screen.material.SetTexture ("_MainTex", viewTexture);
        }
    }

    // Sets the thickness of the portal screen so as not to clip with camera near plane when player goes through
    float ProtectScreenFromClipping (Vector3 viewPoint) {
        float halfHeight = playerCam.nearClipPlane * Mathf.Tan (playerCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * playerCam.aspect;
        float dstToNearClipPlaneCorner = new Vector3 (halfWidth, halfHeight, playerCam.nearClipPlane).magnitude;
        float screenThickness = dstToNearClipPlaneCorner;

        Transform screenT = screen.transform;
        bool camFacingSameDirAsPortal = Vector3.Dot (transform.forward, transform.position - viewPoint) > 0;
        screenT.localScale = new Vector3 (screenT.localScale.x, screenT.localScale.y, screenThickness);
        screenT.localPosition = Vector3.forward * screenThickness * ((camFacingSameDirAsPortal) ? 0.5f : -0.5f);
        return screenThickness;
    }

    void UpdateSliceParams (PortalTraveller traveller) {
        // Calculate slice normal
        int side = SideOfPortal (traveller.transform.position);
        Vector3 sliceNormal = transform.forward * -side;
        Vector3 cloneSliceNormal = linkedPortal.transform.forward * side;

        // Calculate slice centre
        Vector3 slicePoint = transform.position;
        Vector3 cloneSlicePoint = linkedPortal.transform.position;

        // Adjust slice offset so that when player is exactly on portal threshold, the slice is exactly at the mesh edge
        // Set slice data
        for (int i = 0; i < traveller.originalMaterials.Length; i++) {
            traveller.originalMaterials[i].SetVector ("sliceNormal", sliceNormal);
            traveller.originalMaterials[i].SetVector ("sliceCentre", slicePoint);
            traveller.originalMaterials[i].SetFloat ("sliceOffsetDst", 0);

            traveller.cloneMaterials[i].SetVector ("sliceNormal", cloneSliceNormal);
            traveller.cloneMaterials[i].SetVector ("sliceCentre", cloneSlicePoint);
            traveller.cloneMaterials[i].SetFloat ("sliceOffsetDst", 0);
        }

        bool playerSameSideAsTraveller = SameSideOfPortal (playerCam.transform.position, traveller.transform.position);
        if (!playerSameSideAsTraveller) {
            traveller.SetSliceOffsetDst (0, false);
        }

        bool playerSameSideAsCloneAppearing = side != SideOfPortal (playerCam.transform.position);
        if (!playerSameSideAsCloneAppearing) {
            traveller.SetSliceOffsetDst (0, true);
        }
    }

    // Use custom projection matrix to align portal camera's near clip plane with the surface of the portal
    // Note that this affects precision of the depth buffer, which can cause issues with effects like screenspace AO
    void SetNearClipPlane () {
        // Learning resource:
        // http://www.terathon.com/lengyel/Lengyel-Oblique.pdf
        Transform clipPlane = transform;
        int dot = System.Math.Sign (Vector3.Dot (clipPlane.forward, transform.position - portalCam.transform.position));

        Vector3 camSpacePos = portalCam.worldToCameraMatrix.MultiplyPoint (clipPlane.position);
        Vector3 camSpaceNormal = portalCam.worldToCameraMatrix.MultiplyVector (clipPlane.forward) * dot;
        float camSpaceDst = -Vector3.Dot (camSpacePos, camSpaceNormal) + nearClipOffset;

        // Don't use oblique clip plane if very close to portal as it seems this can cause some visual artifacts
        if (Mathf.Abs (camSpaceDst) > nearClipLimit) {
            Vector4 clipPlaneCameraSpace = new Vector4 (camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);
            portalCam.projectionMatrix = playerCam.CalculateObliqueMatrix (clipPlaneCameraSpace);
        } else {
            portalCam.projectionMatrix = playerCam.projectionMatrix;
        }
    }

    void OnTravellerEnterPortal (PortalTraveller traveller) {
        if (!trackedTravellers.Contains (traveller)) {
            traveller.EnterPortalThreshold ();
            traveller.previousOffsetFromPortal = traveller.transform.position - transform.position;
            trackedTravellers.Add (traveller);
        }
    }

    void OnTriggerEnter (Collider other) {
        var traveller = other.GetComponent<PortalTraveller> ();
        if (traveller) {
            OnTravellerEnterPortal (traveller);
        }
    }

    void OnTriggerExit (Collider other) {
        var traveller = other.GetComponent<PortalTraveller> ();
        if (traveller && trackedTravellers.Contains (traveller)) {
            traveller.ExitPortalThreshold ();
            trackedTravellers.Remove (traveller);
        }
    }

    /*
     ** Some helper/convenience stuff:
     */

    int SideOfPortal (Vector3 pos) {
        return System.Math.Sign (Vector3.Dot (pos - transform.position, transform.forward));
    }

    bool SameSideOfPortal (Vector3 posA, Vector3 posB) {
        return SideOfPortal (posA) == SideOfPortal (posB);
    }

    Vector3 portalCamPos {
        get {
            return portalCam.transform.position;
        }
    }

    public bool IsVisible(Camera camera) {
        if (screen == null) return false;
        
        Renderer renderer = screen.GetComponent<Renderer>();
        if (renderer == null) return false;
        
        return CameraUtility.VisibleFromCamera(renderer, camera);
    }

    public Vector3 TransformPosition(Vector3 position) {
        if (linkedPortal == null) return position;
        
        Matrix4x4 matrix = transform.localToWorldMatrix * linkedPortal.transform.worldToLocalMatrix;
        return matrix.MultiplyPoint3x4(position);
    }

    public Quaternion TransformRotation(Quaternion rotation) {
        if (linkedPortal == null) return rotation;
        
        Matrix4x4 matrix = transform.localToWorldMatrix * linkedPortal.transform.worldToLocalMatrix;
        return matrix.rotation * rotation;
    }

    public bool IsPointInFrontOfPortal(Vector3 point) {
        Vector3 directionToPoint = point - transform.position;
        return Vector3.Dot(directionToPoint, transform.forward) > 0;
    }

    void OnValidate () {
        if (linkedPortal != null) {
            linkedPortal.linkedPortal = this;
        }
    }
}