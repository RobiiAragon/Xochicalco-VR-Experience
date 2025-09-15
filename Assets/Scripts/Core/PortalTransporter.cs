using UnityEngine;
using System.Collections.Generic;

public class PortalTransporter : MonoBehaviour
{
    private Portal portal;
    private List<PortalableObject> objectsInPortal = new List<PortalableObject>();
    Plane _portalPlane;
    
    void Awake()
    {
        portal = GetComponent<Portal>();
        _portalPlane = new Plane(transform.forward, transform.position);
        
        // Ensure we have required components
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning("PortalTransporter requires a Collider component!");
        }
        
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        PortalableObject portalableObj = other.GetComponent<PortalableObject>();
        if (portalableObj != null && !objectsInPortal.Contains(portalableObj))
        {
            objectsInPortal.Add(portalableObj);
            portalableObj.OnEnterPortalCollider();
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        PortalableObject portalableObj = other.GetComponent<PortalableObject>();
        if (portalableObj != null && objectsInPortal.Contains(portalableObj))
        {
            objectsInPortal.Remove(portalableObj);
            portalableObj.OnExitPortalCollider();
        }
    }
    
    void Update()
    {
        if (portal == null || portal.linkedPortal == null) return;

        // Actualizar plano si el portal se mueve
        if (transform.hasChanged)
        {
            _portalPlane.SetNormalAndPosition(transform.forward, transform.position);
            transform.hasChanged = false;
        }

        for (int i = objectsInPortal.Count - 1; i >= 0; i--)
        {
            var obj = objectsInPortal[i];
            if (obj == null)
            {
                objectsInPortal.RemoveAt(i);
                continue;
            }

            if (!obj.portallingEnabled) continue;

            // Usa signo del lado del plano
            float sideNow = _portalPlane.GetDistanceToPoint(obj.transform.position);
            if (sideNow < 0f) // cruzó
            {
                TeleportObject(obj);
                objectsInPortal.RemoveAt(i);
            }
        }
    }
    
    void TeleportObject(PortalableObject obj)
    {
        if (portal.linkedPortal == null) return;
        
        // Get transform to teleport
        Transform targetTransform = obj.transformToPortal != null ? obj.transformToPortal : obj.transform;
        
        // Calculate new position and rotation
        Vector3 newPosition = portal.TransformPosition(targetTransform.position);
        Quaternion newRotation = portal.TransformRotation(targetTransform.rotation);
        
        // Fire pre-portal event
        obj.OnPrePortalEvent();
        
        // Perform teleportation
        targetTransform.position = newPosition;
        targetTransform.rotation = newRotation;
        
        // Fire post-portal event
        obj.OnPastPortalEvent();
        
        // Handle collider that might be blocking passage (optional)
        // if (portal.wallCollider != null)
        // {
        //     StartCoroutine(DisableColliderTemporarily(portal.wallCollider));
        // }
    }
    
    System.Collections.IEnumerator DisableColliderTemporarily(Collider col)
    {
        col.enabled = false;
        yield return new WaitForFixedUpdate();
        col.enabled = true;
    }
}