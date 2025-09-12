using UnityEngine;
using UnityEngine.Events;

public class PortalableObject : MonoBehaviour
{
    [Header("Portal Settings")]
    public Transform transformToPortal;
    public bool portallingEnabled = true;
    
    [Header("Master Settings")]
    public bool isMasterPortalableObject = false;
    public Transform clonePositionToClosestPortal;
    
    [Header("Events")]
    public UnityEvent onEnterPortalCollider;
    public UnityEvent onPrePortalEvent;
    public UnityEvent onPastPortalEvent;
    public UnityEvent onExitPortalCollider;
    
    private static PortalableObject masterObject;
    
    void Awake()
    {
        if (isMasterPortalableObject)
        {
            masterObject = this;
        }
        
        // Ensure we have a collider
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"PortalableObject on {gameObject.name} requires a Collider component!");
        }
    }
    
    public void OnEnterPortalCollider()
    {
        onEnterPortalCollider?.Invoke();
    }
    
    public void OnPrePortalEvent()
    {
        onPrePortalEvent?.Invoke();
        
        // If this is not the master but should portal along with master
        if (!isMasterPortalableObject && masterObject != null)
        {
            PortalAlongWithMaster();
        }
    }
    
    public void OnPastPortalEvent()
    {
        onPastPortalEvent?.Invoke();
    }
    
    public void OnExitPortalCollider()
    {
        onExitPortalCollider?.Invoke();
    }
    
    void PortalAlongWithMaster()
    {
        // Find all objects that should portal along with master
        PortalAlongWithMasterPortalableObject[] objectsToPortal = 
            FindObjectsByType<PortalAlongWithMasterPortalableObject>(FindObjectsSortMode.None);
        
        foreach (var obj in objectsToPortal)
        {
            if (obj.enabled && obj.gameObject.activeInHierarchy)
            {
                // Calculate relative position and rotation to master
                Vector3 relativePos = obj.transform.position - masterObject.transform.position;
                Quaternion relativeRot = Quaternion.Inverse(masterObject.transform.rotation) * obj.transform.rotation;
                
                // Apply to new master position
                obj.transform.position = masterObject.transform.position + masterObject.transform.rotation * relativePos;
                obj.transform.rotation = masterObject.transform.rotation * relativeRot;
            }
        }
    }
    
    void Update()
    {
        // Handle clone position to closest portal
        if (clonePositionToClosestPortal != null)
        {
            Portal closestPortal = FindClosestPortal();
            if (closestPortal != null && closestPortal.linkedPortal != null)
            {
                Vector3 targetPosition = closestPortal.linkedPortal.transform.position;
                clonePositionToClosestPortal.position = targetPosition;
            }
        }
    }
    
    Portal FindClosestPortal()
    {
        Portal[] portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
        Portal closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (Portal portal in portals)
        {
            float distance = Vector3.Distance(transform.position, portal.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = portal;
            }
        }
        
        return closest;
    }
}