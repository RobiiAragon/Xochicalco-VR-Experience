using UnityEngine;
using System.Collections.Generic;

namespace Xochicalco.PortalSystem
{
    public class PortalManager : MonoBehaviour
    {
        [Header("Portal Network")]
        [SerializeField] private List<PortalPair> portalPairs = new List<PortalPair>();
        
        [Header("Player Configuration")]
        [SerializeField] private Transform playerCamera;
        [SerializeField] private string playerTag = "Player";
        
        [Header("Environment Management")]
        [SerializeField] private List<EnvironmentZone> environmentZones = new List<EnvironmentZone>();
        [SerializeField] private float environmentCullingDistance = 100f;
        
        private Transform currentPlayerTransform;

        [System.Serializable]
        public class PortalPair
        {
            [Header("Portal Connection")]
            public PortalControllerURP portalA;
            public PortalControllerURP portalB;
            public string connectionName;
            
            [Header("Environment Info")]
            public string environmentAName;
            public string environmentBName;
            
            public void Initialize()
            {
                if (portalA != null && portalB != null)
                {
                    portalA.SetLinkedPortal(portalB);
                    portalB.SetLinkedPortal(portalA);
                }
            }
        }

        [System.Serializable]
        public class EnvironmentZone
        {
            public string zoneName;
            public GameObject environmentRoot;
            public Vector3 centerPosition;
            public float activationRadius = 50f;
            public bool isActive = true;
            
            [Header("Performance Settings")]
            public bool enableLOD = true;
            public LODGroup[] lodGroups;
        }

        private void Start()
        {
            InitializePortalSystem();
            FindPlayerCamera();
        }

        private void InitializePortalSystem()
        {
            // Configurar todos los pares de portales
            foreach (var portalPair in portalPairs)
            {
                portalPair.Initialize();
                
                if (playerCamera != null)
                {
                    portalPair.portalA?.SetPlayerCamera(playerCamera);
                    portalPair.portalB?.SetPlayerCamera(playerCamera);
                }
            }

            Debug.Log($"Portal System initialized with {portalPairs.Count} portal pairs");
        }

        private void FindPlayerCamera()
        {
            if (playerCamera == null)
            {
                // Buscar la cámara del jugador
                GameObject player = GameObject.FindGameObjectWithTag(playerTag);
                if (player != null)
                {
                    playerCamera = player.GetComponentInChildren<Camera>()?.transform;
                    currentPlayerTransform = player.transform;
                    
                    // Actualizar todas las referencias de cámara en los portales
                    foreach (var portalPair in portalPairs)
                    {
                        portalPair.portalA?.SetPlayerCamera(playerCamera);
                        portalPair.portalB?.SetPlayerCamera(playerCamera);
                    }
                }
            }
        }

        private void Update()
        {
            if (currentPlayerTransform != null)
            {
                ManageEnvironmentCulling();
            }
        }

        private void ManageEnvironmentCulling()
        {
            Vector3 playerPosition = currentPlayerTransform.position;

            foreach (var zone in environmentZones)
            {
                if (zone.environmentRoot == null) continue;

                float distanceToZone = Vector3.Distance(playerPosition, zone.centerPosition);
                bool shouldBeActive = distanceToZone <= environmentCullingDistance;

                // Solo cambiar el estado si es diferente al actual
                if (zone.isActive != shouldBeActive)
                {
                    zone.isActive = shouldBeActive;
                    zone.environmentRoot.SetActive(shouldBeActive);
                    
                    Debug.Log($"Environment '{zone.zoneName}' {(shouldBeActive ? "activated" : "deactivated")} - Distance: {distanceToZone:F2}");
                }

                // Gestión de LOD si está habilitada y la zona está activa
                if (zone.enableLOD && zone.isActive && zone.lodGroups != null)
                {
                    foreach (var lodGroup in zone.lodGroups)
                    {
                        if (lodGroup != null)
                        {
                            lodGroup.ForceLOD(CalculateLODLevel(distanceToZone, zone.activationRadius));
                        }
                    }
                }
            }
        }

        private int CalculateLODLevel(float distance, float maxDistance)
        {
            float normalizedDistance = distance / maxDistance;
            
            if (normalizedDistance < 0.3f) return 0; // LOD 0 - Máxima calidad
            if (normalizedDistance < 0.6f) return 1; // LOD 1 - Media calidad
            if (normalizedDistance < 0.9f) return 2; // LOD 2 - Baja calidad
            return 3; // LOD 3 - Mínima calidad
        }

        public void AddPortalPair(PortalControllerURP portalA, PortalControllerURP portalB, string connectionName = "")
        {
            PortalPair newPair = new PortalPair
            {
                portalA = portalA,
                portalB = portalB,
                connectionName = connectionName
            };
            
            portalPairs.Add(newPair);
            newPair.Initialize();
            
            if (playerCamera != null)
            {
                portalA?.SetPlayerCamera(playerCamera);
                portalB?.SetPlayerCamera(playerCamera);
            }
        }

        public void RemovePortalPair(PortalControllerURP portal)
        {
            for (int i = portalPairs.Count - 1; i >= 0; i--)
            {
                if (portalPairs[i].portalA == portal || portalPairs[i].portalB == portal)
                {
                    portalPairs.RemoveAt(i);
                    break;
                }
            }
        }

        public List<PortalPair> GetPortalPairs()
        {
            return new List<PortalPair>(portalPairs);
        }

        public void RegisterEnvironmentZone(string zoneName, GameObject environmentRoot, Vector3 centerPosition, float activationRadius = 50f)
        {
            EnvironmentZone newZone = new EnvironmentZone
            {
                zoneName = zoneName,
                environmentRoot = environmentRoot,
                centerPosition = centerPosition,
                activationRadius = activationRadius,
                isActive = true
            };
            
            environmentZones.Add(newZone);
        }

        // Método para debugging - mostrar información de portales
        [ContextMenu("Debug Portal Info")]
        public void DebugPortalInfo()
        {
            Debug.Log("=== PORTAL SYSTEM DEBUG ===");
            Debug.Log($"Total Portal Pairs: {portalPairs.Count}");
            Debug.Log($"Total Environment Zones: {environmentZones.Count}");
            Debug.Log($"Player Camera: {(playerCamera != null ? playerCamera.name : "Not Found")}");
            
            foreach (var pair in portalPairs)
            {
                Debug.Log($"Portal Pair: {pair.connectionName} | A: {pair.portalA?.name} | B: {pair.portalB?.name}");
            }
        }
    }
}