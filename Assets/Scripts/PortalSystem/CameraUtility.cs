using UnityEngine;

namespace Xochicalco.PortalSystem
{
    public static class CameraUtility
    {
        // Returns true if the object is visible from the camera
        public static bool VisibleFromCamera(Renderer renderer, Camera camera)
        {
            if (renderer == null || camera == null) return false;
            
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
        }

        // Returns true if the two renderers have overlapping bounds when viewed from the camera
        public static bool BoundsOverlap(MeshFilter boundsA, MeshFilter boundsB, Camera camera)
        {
            if (boundsA == null || boundsB == null || camera == null) return false;

            Bounds bounds1 = boundsA.mesh.bounds;
            Bounds bounds2 = boundsB.mesh.bounds;
            
            // Transform bounds to world space
            bounds1.center = boundsA.transform.TransformPoint(bounds1.center);
            bounds1.size = Vector3.Scale(bounds1.size, boundsA.transform.lossyScale);
            
            bounds2.center = boundsB.transform.TransformPoint(bounds2.center);
            bounds2.size = Vector3.Scale(bounds2.size, boundsB.transform.lossyScale);

            return bounds1.Intersects(bounds2);
        }
    }
}