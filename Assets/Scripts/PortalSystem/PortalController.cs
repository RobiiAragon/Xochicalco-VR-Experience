using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Xochicalco.PortalSystem
{
    public class PortalController : MonoBehaviour
    {
        [Header("Portal Configuration")]
        [SerializeField] private PortalController linkedPortal;
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
        
        private Camera portalCam;
        private bool hasRendered = false;

        private void Start()
        {
            SetupPortalCamera();
            SetupPortalMaterial();
        }

        private void SetupPortalCamera()
        {
            portalCam = portalCamera.GetComponent<Camera>();
            if (portalCam == null)
            {
                portalCam = portalCamera.gameObject.AddComponent<Camera>();
            }

            // Configurar la cámara del portal
            portalCam.enabled = false;
            portalCam.targetTexture = portalTexture;
            
            // Copiar configuraciones de la cámara principal
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                portalCam.fieldOfView = mainCam.fieldOfView;
                portalCam.nearClipPlane = mainCam.nearClipPlane;
                portalCam.farClipPlane = mainCam.farClipPlane;
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
            if (linkedPortal != null && enablePortalRendering)
            {
                UpdatePortalCamera();
                RenderPortalCamera();
            }
        }

        private void UpdatePortalCamera()
        {
            if (playerCamera == null) return;

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

        private void RenderPortalCamera()
        {
            // Solo renderizar si estamos lo suficientemente cerca
            float distanceToPortal = Vector3.Distance(playerCamera.position, transform.position);
            if (distanceToPortal > maxRenderDistance) return;

            // Evitar renderizado recursivo infinito
            if (hasRendered) return;

            hasRendered = true;
            linkedPortal.hasRendered = true;

            portalCam.Render();

            hasRendered = false;
            linkedPortal.hasRendered = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // El jugador ha entrado en el área del portal
                // Aquí podríamos agregar efectos de sonido o visuales
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
                // El jugador ha salido del área del portal
                // Aquí podríamos limpiar efectos temporales
            }
        }

        private void TeleportPlayer(Transform player)
        {
            if (linkedPortal == null || linkedPortal.destinationPoint == null) return;

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

        public void SetLinkedPortal(PortalController portal)
        {
            linkedPortal = portal;
        }

        public void SetPlayerCamera(Transform camera)
        {
            playerCamera = camera;
        }

        private void OnValidate()
        {
            if (portalCollider == null)
                portalCollider = GetComponent<BoxCollider>();
                
            if (portalCollider != null)
                portalCollider.isTrigger = true;
        }
    }
}