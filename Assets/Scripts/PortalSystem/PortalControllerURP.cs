using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Xochicalco.PortalSystem
{
    public class PortalControllerURP : MonoBehaviour
    {
        [Header("Portal Configuration")]
        [SerializeField] private PortalControllerURP linkedPortal;
        [SerializeField] private Transform portalCamera;
        [SerializeField] private Transform playerCamera;
        [SerializeField] private RenderTexture portalTexture;
        [SerializeField] private Material portalMaterial;
        
        [Header("Portal Area")]
        [SerializeField] private Transform destinationPoint;
        [SerializeField] private BoxCollider portalCollider;
        
        [Header("Performance")]
        [SerializeField] private float maxRenderDistance = 50f;
        [SerializeField] private bool enablePortalRendering = true;
        [SerializeField] private int renderTextureSize = 512; // Tamaño más pequeño para mejor performance
        
        private Camera portalCam;
        private bool isRendering = false;

        private void Start()
        {
            SetupPortalCamera();
            SetupPortalMaterial();
            CreateRenderTexture();
        }

        private void SetupPortalCamera()
        {
            if (portalCamera == null)
            {
                GameObject camObj = new GameObject("PortalCamera");
                camObj.transform.SetParent(transform, false);
                portalCamera = camObj.transform;
            }

            portalCam = portalCamera.GetComponent<Camera>();
            if (portalCam == null)
            {
                portalCam = portalCamera.gameObject.AddComponent<Camera>();
            }

            // Configurar la cámara del portal para URP
            portalCam.enabled = true; // Habilitamos la cámara
            portalCam.depth = -10; // Renderizar antes que la cámara principal
            
            // Copiar configuraciones de la cámara principal
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                portalCam.fieldOfView = mainCam.fieldOfView;
                portalCam.nearClipPlane = mainCam.nearClipPlane;
                portalCam.farClipPlane = mainCam.farClipPlane;
                
                // Copiar configuración URP
                var mainCamData = mainCam.GetUniversalAdditionalCameraData();
                var portalCamData = portalCam.GetUniversalAdditionalCameraData();
                if (mainCamData != null && portalCamData != null)
                {
                    portalCamData.renderType = CameraRenderType.Base;
                    portalCamData.renderPostProcessing = false; // Desactivar post-processing para mejor performance
                    portalCamData.renderShadows = true;
                }
            }
        }

        private void CreateRenderTexture()
        {
            if (portalTexture != null)
            {
                portalTexture.Release();
            }

            portalTexture = new RenderTexture(renderTextureSize, renderTextureSize, 16, RenderTextureFormat.ARGB32);
            portalTexture.name = $"PortalTexture_{gameObject.name}";
            portalTexture.Create();

            if (portalCam != null)
            {
                portalCam.targetTexture = portalTexture;
            }
        }

        private void SetupPortalMaterial()
        {
            if (portalMaterial != null && portalTexture != null)
            {
                portalMaterial.mainTexture = portalTexture;
            }
        }

        private void LateUpdate()
        {
            if (linkedPortal != null && enablePortalRendering && !isRendering)
            {
                float distanceToPortal = Vector3.Distance(playerCamera.position, transform.position);
                if (distanceToPortal <= maxRenderDistance)
                {
                    UpdatePortalCamera();
                }
                else
                {
                    // Desactivar la cámara cuando esté muy lejos
                    if (portalCam != null && portalCam.enabled)
                    {
                        portalCam.enabled = false;
                    }
                }
            }
        }

        private void UpdatePortalCamera()
        {
            if (playerCamera == null || portalCam == null) return;

            // Activar la cámara si está desactivada
            if (!portalCam.enabled)
            {
                portalCam.enabled = true;
            }

            // Calcular la posición y rotación de la cámara del portal
            Vector3 playerOffsetFromPortal = playerCamera.position - transform.position;
            
            // Transformar el offset al espacio del portal de destino
            Vector3 portalCameraPosition = linkedPortal.transform.position + 
                linkedPortal.transform.TransformDirection(transform.InverseTransformDirection(playerOffsetFromPortal));

            portalCamera.position = portalCameraPosition;

            // Calcular rotación
            Quaternion angularDifferenceBetweenPortals = Quaternion.Inverse(transform.rotation) * linkedPortal.transform.rotation;
            Vector3 cameraDirection = playerCamera.rotation * Vector3.forward;
            Vector3 portalCameraDirection = angularDifferenceBetweenPortals * cameraDirection;
            
            portalCamera.rotation = Quaternion.LookRotation(portalCameraDirection, linkedPortal.transform.up);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // El jugador ha entrado en el área del portal
                Debug.Log($"Player entered portal: {gameObject.name}");
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player") && linkedPortal != null)
            {
                // Verificar si el jugador ha cruzado completamente el portal
                Vector3 portalToPlayer = other.transform.position - transform.position;
                if (Vector3.Dot(portalToPlayer, transform.forward) > 0.0f)
                {
                    TeleportPlayer(other.transform);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log($"Player exited portal: {gameObject.name}");
            }
        }

        private void TeleportPlayer(Transform player)
        {
            if (linkedPortal == null || linkedPortal.destinationPoint == null) return;

            Debug.Log($"Teleporting player from {gameObject.name} to {linkedPortal.gameObject.name}");

            // Desactivar temporalmente el collider para evitar teleportación múltiple
            linkedPortal.portalCollider.enabled = false;

            // Calcular nueva posición y rotación del jugador
            Vector3 relativePos = transform.InverseTransformPoint(player.position);
            relativePos = Vector3.Scale(relativePos, new Vector3(-1, 1, -1));
            player.position = linkedPortal.destinationPoint.TransformPoint(relativePos);

            // Calcular nueva rotación
            Vector3 relativeDir = transform.InverseTransformDirection(player.forward);
            relativeDir = Vector3.Scale(relativeDir, new Vector3(-1, 1, -1));
            player.rotation = Quaternion.LookRotation(linkedPortal.destinationPoint.TransformDirection(relativeDir));

            // Reactivar el collider después de un frame
            StartCoroutine(ReenablePortalCollider());
        }

        private System.Collections.IEnumerator ReenablePortalCollider()
        {
            yield return new WaitForEndOfFrame();
            if (linkedPortal != null && linkedPortal.portalCollider != null)
            {
                linkedPortal.portalCollider.enabled = true;
            }
        }

        public void SetLinkedPortal(PortalControllerURP portal)
        {
            linkedPortal = portal;
        }

        public void SetPlayerCamera(Transform camera)
        {
            playerCamera = camera;
        }

        public void SetPortalMaterial(Material material)
        {
            portalMaterial = material;
            SetupPortalMaterial();
        }

        private void OnValidate()
        {
            if (portalCollider == null)
                portalCollider = GetComponent<BoxCollider>();
                
            if (portalCollider != null)
                portalCollider.isTrigger = true;
        }

        private void OnDestroy()
        {
            if (portalTexture != null)
            {
                portalTexture.Release();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Mostrar el área del portal
            Gizmos.color = Color.cyan;
            if (portalCollider != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(portalCollider.center, portalCollider.size);
            }

            // Mostrar dirección del portal
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);

            // Mostrar punto de destino
            if (destinationPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(destinationPoint.position, 0.5f);
            }

            // Mostrar conexión con portal vinculado
            if (linkedPortal != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, linkedPortal.transform.position);
            }
        }
    }
}