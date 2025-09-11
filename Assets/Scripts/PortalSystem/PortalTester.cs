using UnityEngine;

namespace Xochicalco.PortalSystem
{
    public class PortalTester : MonoBehaviour
    {
        [Header("Testing")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float lookSpeed = 2f;
        [SerializeField] private bool enableTesting = true;

        private CharacterController characterController;
        private Camera playerCamera;
        private float xRotation = 0f;

        private void Start()
        {
            if (!enableTesting) return;

            // Configurar cámara para testing
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindFirstObjectByType<Camera>();
            }

            // Agregar CharacterController si no existe
            characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
                characterController.height = 1.8f;
                characterController.radius = 0.5f;
                characterController.center = new Vector3(0, 0.9f, 0);
            }

            // Configurar cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Asignar esta cámara a todos los portales
            AssignCameraToPortals();

            Debug.Log("🎮 Portal Tester activado!");
            Debug.Log("📹 Usa WASD para moverte, Mouse para mirar");
            Debug.Log("🚪 Camina hacia los portales para teletransportarte");
            Debug.Log("ESC para liberar cursor");
        }

        private void Update()
        {
            if (!enableTesting) return;

            HandleMouseLook();
            HandleMovement();
            HandleCursor();
        }

        private void HandleMouseLook()
        {
            if (playerCamera == null) return;

            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleMovement()
        {
            if (characterController == null) return;

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 direction = transform.right * horizontal + transform.forward * vertical;
            Vector3 move = direction * moveSpeed;

            // Agregar gravedad
            move.y = -9.81f;

            characterController.Move(move * Time.deltaTime);
        }

        private void HandleCursor()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        private void AssignCameraToPortals()
        {
            PortalControllerURP[] portals = FindObjectsByType<PortalControllerURP>(FindObjectsSortMode.None);
            foreach (var portal in portals)
            {
                portal.SetPlayerCamera(playerCamera.transform);
            }

            Debug.Log($"✅ Cámara asignada a {portals.Length} portales");
        }

        private void OnDrawGizmosSelected()
        {
            // Mostrar el área del CharacterController
            if (characterController != null)
            {
                Gizmos.color = Color.green;
                Vector3 center = transform.position + characterController.center;
                float radius = characterController.radius;
                float height = characterController.height;
                
                // Dibujar cilindro manualmente ya que DrawWireCapsule no existe en todas las versiones
                Gizmos.DrawWireSphere(center + Vector3.up * (height/2 - radius), radius);
                Gizmos.DrawWireSphere(center + Vector3.down * (height/2 - radius), radius);
                
                // Líneas laterales
                Vector3 topCenter = center + Vector3.up * (height/2 - radius);
                Vector3 bottomCenter = center + Vector3.down * (height/2 - radius);
                
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * Mathf.PI * 0.5f;
                    Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                    Gizmos.DrawLine(topCenter + offset, bottomCenter + offset);
                }
            }
        }

        // Métodos públicos para debugging
        [ContextMenu("Find All Portals")]
        public void FindAllPortals()
        {
            PortalControllerURP[] portals = FindObjectsByType<PortalControllerURP>(FindObjectsSortMode.None);
            Debug.Log($"🔍 Encontrados {portals.Length} portales:");
            foreach (var portal in portals)
            {
                Debug.Log($"   - {portal.name} en posición {portal.transform.position}");
            }
        }

        [ContextMenu("Test Teleport")]
        public void TestTeleport()
        {
            PortalControllerURP[] portals = FindObjectsByType<PortalControllerURP>(FindObjectsSortMode.None);
            if (portals.Length >= 2)
            {
                transform.position = portals[0].transform.position + Vector3.forward * 2;
                Debug.Log("🚀 Posicionado frente al primer portal para prueba");
            }
        }
    }
}