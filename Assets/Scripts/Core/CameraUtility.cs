using UnityEngine;

public static class CameraUtility 
{
    static readonly Vector3[] cubeCornerOffsets = {
        new Vector3 (1, 1, 1),
        new Vector3 (-1, 1, 1),
        new Vector3 (-1, -1, 1),
        new Vector3 (-1, -1, -1),
        new Vector3 (-1, 1, -1),
        new Vector3 (1, -1, -1),
        new Vector3 (1, 1, -1),
        new Vector3 (1, -1, 1),
    };

    // Get plane in camera space from a given world position and normal
    public static Vector4 GetCameraPlaneCameraSpace(Camera cam, Vector3 pos, Vector3 normal) {
        Vector3 camSpacePos = cam.worldToCameraMatrix.MultiplyPoint(pos);
        Vector3 camSpaceNormal = cam.worldToCameraMatrix.MultiplyVector(normal).normalized;
        float camSpaceDst = -Vector3.Dot(camSpacePos, camSpaceNormal);
        return new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);
    }

    // Check if a renderer is visible from a camera
    public static bool VisibleFromCamera(Renderer renderer, Camera camera) {
        if (renderer == null || camera == null) return false;
        
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds);
    }

    // Check if a mesh renderer is visible from a camera (convenience method)
    public static bool VisibleFromCamera(MeshRenderer meshRenderer, Camera camera) {
        return VisibleFromCamera((Renderer)meshRenderer, camera);
    }

    // Get view distance between camera and renderer
    public static float GetDistanceToCamera(Renderer renderer, Camera camera) {
        if (renderer == null || camera == null) return float.MaxValue;
        return Vector3.Distance(camera.transform.position, renderer.bounds.center);
    }

    // Calculate camera space frustum corners
    public static Vector3[] GetFrustumCorners(Camera camera) {
        Vector3[] corners = new Vector3[4];
        
        float halfFOV = (camera.fieldOfView * 0.5f) * Mathf.Deg2Rad;
        float aspect = camera.aspect;
        float distance = camera.nearClipPlane;
        
        float height = distance * Mathf.Tan(halfFOV);
        float width = height * aspect;
        
        // Bottom left, top left, top right, bottom right
        corners[0] = new Vector3(-width, -height, distance);
        corners[1] = new Vector3(-width, height, distance);
        corners[2] = new Vector3(width, height, distance);
        corners[3] = new Vector3(width, -height, distance);
        
        return corners;
    }

    public static bool BoundsOverlap(MeshFilter nearObject, MeshFilter farObject, Camera camera) {
        var near = GetScreenRectFromBounds(nearObject, camera);
        var far = GetScreenRectFromBounds(farObject, camera);

        // ensure far object is indeed further away than near object
        if (far.zMax > near.zMin) {
            // Doesn't overlap on x axis
            if (far.xMax < near.xMin || far.xMin > near.xMax) {
                return false;
            }
            // Doesn't overlap on y axis
            if (far.yMax < near.yMin || far.yMin > near.yMax) {
                return false;
            }
            // Overlaps
            return true;
        }
        return false;
    }

    // With thanks to http://www.turiyaware.com/a-solution-to-unitys-camera-worldtoscreenpoint-causing-ui-elements-to-display-when-object-is-behind-the-camera/
    public static MinMax3D GetScreenRectFromBounds(MeshFilter renderer, Camera mainCamera) {
        MinMax3D minMax = new MinMax3D(float.MaxValue, float.MinValue);

        Vector3[] screenBoundsExtents = new Vector3[8];
        var localBounds = renderer.sharedMesh.bounds;
        bool anyPointIsInFrontOfCamera = false;

        for (int i = 0; i < 8; i++) {
            Vector3 localSpaceCorner = localBounds.center + Vector3.Scale(localBounds.extents, cubeCornerOffsets[i]);
            Vector3 worldSpaceCorner = renderer.transform.TransformPoint(localSpaceCorner);
            Vector3 viewportSpaceCorner = mainCamera.WorldToViewportPoint(worldSpaceCorner);

            if (viewportSpaceCorner.z > 0) {
                anyPointIsInFrontOfCamera = true;
            } else {
                // If point is behind camera, it gets flipped to the opposite side
                // So clamp to opposite edge to correct for this
                viewportSpaceCorner.x = (viewportSpaceCorner.x <= 0.5f) ? 1 : 0;
                viewportSpaceCorner.y = (viewportSpaceCorner.y <= 0.5f) ? 1 : 0;
            }

            // Update bounds with new corner point
            minMax.AddPoint(viewportSpaceCorner);
        }

        // All points are behind camera so just return empty bounds
        if (!anyPointIsInFrontOfCamera) {
            return new MinMax3D();
        }

        return minMax;
    }

    public struct MinMax3D {
        public float xMin;
        public float xMax;
        public float yMin;
        public float yMax;
        public float zMin;
        public float zMax;

        public MinMax3D(float min, float max) {
            this.xMin = min;
            this.xMax = max;
            this.yMin = min;
            this.yMax = max;
            this.zMin = min;
            this.zMax = max;
        }

        public void AddPoint(Vector3 point) {
            xMin = Mathf.Min(xMin, point.x);
            xMax = Mathf.Max(xMax, point.x);
            yMin = Mathf.Min(yMin, point.y);
            yMax = Mathf.Max(yMax, point.y);
            zMin = Mathf.Min(zMin, point.z);
            zMax = Mathf.Max(zMax, point.z);
        }
    }
}