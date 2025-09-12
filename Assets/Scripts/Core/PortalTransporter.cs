using UnityEngine;
using System.Collections.Generic;

public class PortalTransporter : MonoBehaviour
{
    private Portal portal;
    private List<PortalableObject> objectsInPortal = new List<PortalableObject>();
    
    void Awake()
    {
        portal = GetComponent<Portal>();
        
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
        
        // Check for teleportation
        for (int i = objectsInPortal.Count - 1; i >= 0; i--)
        {
            PortalableObject obj = objectsInPortal[i];
            if (obj == null)
            {
                objectsInPortal.RemoveAt(i);
                continue;
            }
            
            if (obj.portallingEnabled && ShouldTeleport(obj))
            {
                TeleportObject(obj);
                objectsInPortal.RemoveAt(i);
            }
        }
    }
    
    bool ShouldTeleport(PortalableObject obj)
    {
        // Check if object has crossed the portal plane
        Vector3 objPosition = obj.transform.position;
        
        // Get the position relative to portal
        Vector3 localPos = transform.InverseTransformPoint(objPosition);
        
        // If the object is behind the portal (negative Z), it should teleport
        return localPos.z < 0;
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