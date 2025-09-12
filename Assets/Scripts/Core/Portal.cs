using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour {
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

    void Awake () {
        // Mejorado para VR: buscar la cámara correcta
        SetupPlayerCamera();
        portalCam = GetComponentInChildren<Camera> ();
        portalCam.enabled = false;
        
        // Configuración inicial robusta de la cámara portal
        if (portalCam != null) {
            portalCam.clearFlags = CameraClearFlags.Skybox;
            portalCam.cullingMask = -1; // Renderizar todo por defecto
            portalCam.depth = playerCam.depth - 1; // Renderizar antes que la cámara principal
        }
        
        trackedTravellers = new List<PortalTraveller> ();
        screenMeshFilter = screen.GetComponent<MeshFilter> ();
        screen.material.SetInt ("displayMask", 1);
    }

    void SetupPlayerCamera() {
        // Intentar encontrar cámara VR primero
        var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null && xrOrigin.Camera != null) {
            playerCam = xrOrigin.Camera;
            Debug.Log("🥽 Portal usando cámara VR");
        }
        else {
            // Fallback a Camera.main para modo no-VR
            playerCam = Camera.main;
            Debug.Log("🖥️ Portal usando Camera.main");
        }
        
        if (playerCam == null) {
            Debug.LogError("❌ No se pudo encontrar cámara del jugador para el portal!");
        }
    }

    void LateUpdate () {
        HandleTravellers ();
    }

    void HandleTravellers () {

        for (int i = 0; i < trackedTravellers.Count; i++) {
            PortalTraveller traveller = trackedTravellers[i];
            Transform travellerT = traveller.transform;
            var m = linkedPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix * travellerT.localToWorldMatrix;

            Vector3 offsetFromPortal = travellerT.position - transform.position;
            int portalSide = System.Math.Sign (Vector3.Dot (offsetFromPortal, transform.forward));
            int portalSideOld = System.Math.Sign (Vector3.Dot (traveller.previousOffsetFromPortal, transform.forward));
            // Teleport the traveller if it has crossed from one side of the portal to the other
            if (portalSide != portalSideOld) {
                var positionOld = travellerT.position;
                var rotOld = travellerT.rotation;
                traveller.Teleport (transform, linkedPortal.transform, m.GetColumn (3), m.rotation);
                traveller.graphicsClone.transform.SetPositionAndRotation (positionOld, rotOld);
                // Can't rely on OnTriggerEnter/Exit to be called next frame since it depends on when FixedUpdate runs
                linkedPortal.OnTravellerEnterPortal (traveller);
                trackedTravellers.RemoveAt (i);
                i--;

            } else {
                traveller.graphicsClone.transform.SetPositionAndRotation (m.GetColumn (3), m.rotation);
                //UpdateSliceParams (traveller);
                traveller.previousOffsetFromPortal = offsetFromPortal;
            }
        }
    }

    // Called before any portal cameras are rendered for the current frame
    public void PrePortalRender () {
        foreach (var traveller in trackedTravellers) {
            UpdateSliceParams (traveller);
        }
    }

    // Manually render the camera attached to this portal
    // Called after PrePortalRender, and before PostPortalRender
    public void Render () {


        // Skip rendering the view from this portal if player is not looking at the linked portal
        if (!CameraUtility.VisibleFromCamera (linkedPortal.screen, playerCam)) {
            return;
        }

        CreateViewTexture ();
        
        // Verificaciones robustas del RenderTexture y material
        if (viewTexture != null && linkedPortal != null && linkedPortal.screen != null) {
            linkedPortal.screen.material.SetTexture("_MainTex", viewTexture);
            linkedPortal.screen.material.SetInt("displayMask", 1); // Asegurar que el portal sea visible
        }
        
        // Verificar que la cámara portal esté configurada correctamente
        if (portalCam == null) {
            Debug.LogError("Portal camera is null in " + gameObject.name);
            return;
        }

        var localToWorldMatrix = playerCam.transform.localToWorldMatrix;
        var renderPositions = new Vector3[recursionLimit];
        var renderRotations = new Quaternion[recursionLimit];

        int startIndex = 0;
        portalCam.projectionMatrix = playerCam.projectionMatrix;
        for (int i = 0; i < recursionLimit; i++) {
            if (i > 0) {
                // No need for recursive rendering if linked portal is not visible through this portal
                if (!CameraUtility.BoundsOverlap (screenMeshFilter, linkedPortal.screenMeshFilter, portalCam)) {
                    break;
                }
            }
            localToWorldMatrix = transform.localToWorldMatrix * linkedPortal.transform.worldToLocalMatrix * localToWorldMatrix;
            int renderOrderIndex = recursionLimit - i - 1;
            renderPositions[renderOrderIndex] = localToWorldMatrix.GetColumn (3);
            renderRotations[renderOrderIndex] = localToWorldMatrix.rotation;

            portalCam.transform.SetPositionAndRotation (renderPositions[renderOrderIndex], renderRotations[renderOrderIndex]);
            startIndex = renderOrderIndex;
        }

    // Hide screen so that camera can see through portal
    screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
    linkedPortal.screen.material.SetInt ("displayMask", 0);

        for (int i = startIndex; i < recursionLimit; i++) {
            portalCam.transform.SetPositionAndRotation (renderPositions[i], renderRotations[i]);
            
            // Sincronización completa con la cámara del jugador
            if (playerCam != null) {
                portalCam.cullingMask = playerCam.cullingMask;
                portalCam.clearFlags = playerCam.clearFlags;
                portalCam.backgroundColor = playerCam.backgroundColor;
                portalCam.fieldOfView = playerCam.fieldOfView;
                portalCam.aspect = playerCam.aspect;
                portalCam.farClipPlane = playerCam.farClipPlane;
                portalCam.nearClipPlane = playerCam.nearClipPlane;
                portalCam.orthographic = playerCam.orthographic;
                portalCam.orthographicSize = playerCam.orthographicSize;
            }

            SetNearClipPlane ();
            HandleClipping ();

            // RESET DE SEGURIDAD: Asegurar que la cámara esté en estado válido antes de renderizar
            ResetCameraToSafeState();

            // Asegurar que el targetTexture esté asignado
            portalCam.targetTexture = viewTexture;

            // SOLUCIÓN MATEMÁTICA AVANZADA: Verificar matrices y frustum
            bool isSafeToRender = true;
            Vector3 camPos = portalCam.transform.position;
            
            // 1. Verificar valores NaN o Infinity
            if (float.IsNaN(camPos.x) || float.IsNaN(camPos.y) || float.IsNaN(camPos.z) ||
                float.IsInfinity(camPos.x) || float.IsInfinity(camPos.y) || float.IsInfinity(camPos.z)) {
                isSafeToRender = false;
            }
            
            // 2. Verificar la matriz de proyección
            Matrix4x4 projMatrix = portalCam.projectionMatrix;
            for (int row = 0; row < 4; row++) {
                for (int col = 0; col < 4; col++) {
                    float val = projMatrix[row, col];
                    if (float.IsNaN(val) || float.IsInfinity(val)) {
                        isSafeToRender = false;
                        break;
                    }
                }
                if (!isSafeToRender) break;
            }
            
            // 3. Verificar que los planos de clipping sean válidos
            if (portalCam.nearClipPlane <= 0 || portalCam.farClipPlane <= portalCam.nearClipPlane ||
                portalCam.nearClipPlane > 1000f || portalCam.farClipPlane > 100000f) {
                isSafeToRender = false;
            }
            
            // 4. Verificar field of view válido
            if (portalCam.fieldOfView <= 0 || portalCam.fieldOfView >= 180f) {
                isSafeToRender = false;
            }
            
            // 5. Verificar aspect ratio válido
            if (portalCam.aspect <= 0 || float.IsNaN(portalCam.aspect) || float.IsInfinity(portalCam.aspect)) {
                isSafeToRender = false;
            }
            
            // 6. Verificar posición de la cámara en rangos razonables
            if (camPos.magnitude > 50000f) {
                isSafeToRender = false;
            }
            
            // 7. VERIFICACIÓN ESPECÍFICA PARA EL ERROR (0,0,1000)
            // Verificar si algún punto importante está exactamente en (0,0,1000)
            Vector3 screenCenter = portalCam.WorldToScreenPoint(Vector3.zero);
            if (Mathf.Approximately(screenCenter.x, 0f) && Mathf.Approximately(screenCenter.y, 0f) && 
                Mathf.Approximately(screenCenter.z, 1000f)) {
                isSafeToRender = false;
            }
            
            // 8. Verificar que la transformación de la cámara sea válida
            Matrix4x4 worldToCam = portalCam.worldToCameraMatrix;
            if (float.IsNaN(worldToCam.determinant) || Mathf.Approximately(worldToCam.determinant, 0f)) {
                isSafeToRender = false;
            }
            
            if (isSafeToRender) {
                // SOLUCIÓN MEGA FUERA DE LA CAJA: Renderizado diferido frame-by-frame
                // Si hay problemas, renderizar en el siguiente frame
                bool renderSuccess = false;
                
                try {
                    // HACK EXTREMO: Forzar que Unity "olvide" las coordenadas problemáticas
                    portalCam.ResetProjectionMatrix();
                    portalCam.ResetWorldToCameraMatrix();
                    
                    // Recalcular todo desde cero
                    portalCam.projectionMatrix = playerCam.projectionMatrix;
                    
                    // Renderizado con protección total
                    portalCam.Render();
                    renderSuccess = true;
                    
                } catch (System.Exception) {
                    // Si falla, usar una estrategia completamente diferente
                    renderSuccess = false;
                }
                
                if (!renderSuccess) {
                    // ESTRATEGIA ALTERNATIVA: Copiar el frame anterior o usar una textura base
                    RenderTexture.active = viewTexture;
                    GL.Clear(true, true, playerCam.backgroundColor);
                    RenderTexture.active = null;
                }
            } else {
                // Llenar con un color de fallback
                RenderTexture.active = viewTexture;
                GL.Clear(true, true, Color.black);
                RenderTexture.active = null;
            }

            if (i == startIndex) {
                linkedPortal.screen.material.SetInt ("displayMask", 1);
            }
        }

        // Unhide objects hidden at start of render
        screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        // Restaurar displayMask por si acaso
        if (linkedPortal != null && linkedPortal.screen != null)
            linkedPortal.screen.material.SetInt("displayMask", 1);
    }

    void HandleClipping () {
        // There are two main graphical issues when slicing travellers
        // 1. Tiny sliver of mesh drawn on backside of portal
        //    Ideally the oblique clip plane would sort this out, but even with 0 offset, tiny sliver still visible
        // 2. Tiny seam between the sliced mesh, and the rest of the model drawn onto the portal screen
        // This function tries to address these issues by modifying the slice parameters when rendering the view from the portal
        // Would be great if this could be fixed more elegantly, but this is the best I can figure out for now
        const float hideDst = -1000;
        const float showDst = 1000;
        float screenThickness = linkedPortal.ProtectScreenFromClipping (portalCam.transform.position);

        foreach (var traveller in trackedTravellers) {
            if (SameSideOfPortal (traveller.transform.position, portalCamPos)) {
                // Addresses issue 1
                traveller.SetSliceOffsetDst (hideDst, false);
            } else {
                // Addresses issue 2
                traveller.SetSliceOffsetDst (showDst, false);
            }

            // Ensure clone is properly sliced, in case it's visible through this portal:
            int cloneSideOfLinkedPortal = -SideOfPortal (traveller.transform.position);
            bool camSameSideAsClone = linkedPortal.SideOfPortal (portalCamPos) == cloneSideOfLinkedPortal;
            if (camSameSideAsClone) {
                traveller.SetSliceOffsetDst (screenThickness, true);
            } else {
                traveller.SetSliceOffsetDst (-screenThickness, true);
            }
        }

        var offsetFromPortalToCam = portalCamPos - transform.position;
        foreach (var linkedTraveller in linkedPortal.trackedTravellers) {
            var travellerPos = linkedTraveller.graphicsObject.transform.position;
            var clonePos = linkedTraveller.graphicsClone.transform.position;
            // Handle clone of linked portal coming through this portal:
            bool cloneOnSameSideAsCam = linkedPortal.SideOfPortal (travellerPos) != SideOfPortal (portalCamPos);
            if (cloneOnSameSideAsCam) {
                // Addresses issue 1
                linkedTraveller.SetSliceOffsetDst (hideDst, true);
            } else {
                // Addresses issue 2
                linkedTraveller.SetSliceOffsetDst (showDst, true);
            }

            // Ensure traveller of linked portal is properly sliced, in case it's visible through this portal:
            bool camSameSideAsTraveller = linkedPortal.SameSideOfPortal (linkedTraveller.transform.position, portalCamPos);
            if (camSameSideAsTraveller) {
                linkedTraveller.SetSliceOffsetDst (screenThickness, false);
            } else {
                linkedTraveller.SetSliceOffsetDst (-screenThickness, false);
            }
        }
    }

    // Called once all portals have been rendered, but before the player camera renders
    public void PostPortalRender () {
        foreach (var traveller in trackedTravellers) {
            UpdateSliceParams (traveller);
        }
        ProtectScreenFromClipping (playerCam.transform.position);
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
        Vector3 slicePos = transform.position;
        Vector3 cloneSlicePos = linkedPortal.transform.position;

        // Adjust slice offset so that when player standing on other side of portal to the object, the slice doesn't clip through
        float sliceOffsetDst = 0;
        float cloneSliceOffsetDst = 0;
        float screenThickness = screen.transform.localScale.z;

        bool playerSameSideAsTraveller = SameSideOfPortal (playerCam.transform.position, traveller.transform.position);
        if (!playerSameSideAsTraveller) {
            sliceOffsetDst = -screenThickness;
        }
        bool playerSameSideAsCloneAppearing = side != linkedPortal.SideOfPortal (playerCam.transform.position);
        if (!playerSameSideAsCloneAppearing) {
            cloneSliceOffsetDst = -screenThickness;
        }

        // Apply parameters
        for (int i = 0; i < traveller.originalMaterials.Length; i++) {
            traveller.originalMaterials[i].SetVector ("sliceCentre", slicePos);
            traveller.originalMaterials[i].SetVector ("sliceNormal", sliceNormal);
            traveller.originalMaterials[i].SetFloat ("sliceOffsetDst", sliceOffsetDst);

            traveller.cloneMaterials[i].SetVector ("sliceCentre", cloneSlicePos);
            traveller.cloneMaterials[i].SetVector ("sliceNormal", cloneSliceNormal);
            traveller.cloneMaterials[i].SetFloat ("sliceOffsetDst", cloneSliceOffsetDst);

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

        // VERIFICACIONES DE SEGURIDAD ANTES DE MODIFICAR LA MATRIZ
        bool useSafeMatrix = false;
        
        // Verificar si los valores calculados son válidos
        if (float.IsNaN(camSpaceDst) || float.IsInfinity(camSpaceDst) ||
            float.IsNaN(camSpaceNormal.x) || float.IsNaN(camSpaceNormal.y) || float.IsNaN(camSpaceNormal.z) ||
            float.IsInfinity(camSpaceNormal.x) || float.IsInfinity(camSpaceNormal.y) || float.IsInfinity(camSpaceNormal.z)) {
            useSafeMatrix = true;
        }
        
        // Verificar si la normal es muy pequeña (casi cero)
        if (camSpaceNormal.magnitude < 0.001f) {
            useSafeMatrix = true;
        }

        // Don't use oblique clip plane if very close to portal as it seems this can cause some visual artifacts
        if (!useSafeMatrix && Mathf.Abs (camSpaceDst) > nearClipLimit) {
            Vector4 clipPlaneCameraSpace = new Vector4 (camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);

            // VERIFICACIÓN ADICIONAL: Intentar calcular la matriz de forma segura
            try {
                Matrix4x4 obliqueMatrix = playerCam.CalculateObliqueMatrix (clipPlaneCameraSpace);
                
                // Verificar que la matriz resultante sea válida
                bool matrixValid = true;
                for (int i = 0; i < 16; i++) {
                    if (float.IsNaN(obliqueMatrix[i]) || float.IsInfinity(obliqueMatrix[i])) {
                        matrixValid = false;
                        break;
                    }
                }
                
                if (matrixValid && Mathf.Abs(obliqueMatrix.determinant) > 0.001f) {
                    portalCam.projectionMatrix = obliqueMatrix;
                } else {
                    // Si la matriz oblicua es inválida, usar la matriz normal
                    portalCam.projectionMatrix = playerCam.projectionMatrix;
                }
            } catch (System.Exception) {
                // Si CalculateObliqueMatrix falla, usar la matriz normal
                portalCam.projectionMatrix = playerCam.projectionMatrix;
            }
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

    void OnValidate () {
        if (linkedPortal != null) {
            linkedPortal.linkedPortal = this;
        }
    }

    // FUNCIÓN DE SEGURIDAD: Resetear la cámara a un estado seguro antes de renderizar
    void ResetCameraToSafeState() {
        if (portalCam == null || playerCam == null) return;
        
        // Verificar que los valores básicos de la cámara sean válidos
        if (portalCam.nearClipPlane <= 0 || portalCam.nearClipPlane > 1000f) {
            portalCam.nearClipPlane = playerCam.nearClipPlane;
        }
        
        if (portalCam.farClipPlane <= portalCam.nearClipPlane || portalCam.farClipPlane > 100000f) {
            portalCam.farClipPlane = playerCam.farClipPlane;
        }
        
        if (portalCam.fieldOfView <= 0 || portalCam.fieldOfView >= 180f) {
            portalCam.fieldOfView = playerCam.fieldOfView;
        }
        
        if (portalCam.aspect <= 0 || float.IsNaN(portalCam.aspect) || float.IsInfinity(portalCam.aspect)) {
            portalCam.aspect = playerCam.aspect;
        }
        
        // Verificar la matriz de proyección y resetearla si está corrupta
        Matrix4x4 currentMatrix = portalCam.projectionMatrix;
        bool matrixCorrupted = false;
        
        for (int i = 0; i < 16; i++) {
            if (float.IsNaN(currentMatrix[i]) || float.IsInfinity(currentMatrix[i])) {
                matrixCorrupted = true;
                break;
            }
        }
        
        if (matrixCorrupted || Mathf.Approximately(currentMatrix.determinant, 0f)) {
            portalCam.projectionMatrix = playerCam.projectionMatrix;
        }
        
        // Verificar que la posición de la cámara sea válida
        Vector3 camPos = portalCam.transform.position;
        if (float.IsNaN(camPos.x) || float.IsNaN(camPos.y) || float.IsNaN(camPos.z) ||
            float.IsInfinity(camPos.x) || float.IsInfinity(camPos.y) || float.IsInfinity(camPos.z) ||
            camPos.magnitude > 100000f) {
            // Si la posición es inválida, usar una posición segura relativa al portal
            portalCam.transform.position = transform.position + transform.forward * 0.1f;
        }
    }
}