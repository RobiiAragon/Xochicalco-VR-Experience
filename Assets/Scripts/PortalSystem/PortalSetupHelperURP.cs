using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Xochicalco.PortalSystem
{
    public class PortalSetupHelperURP : MonoBehaviour
    {
        [Header("Portal Configuration")]
        [SerializeField] private Vector3 portalAPosition = new Vector3(0, 1, 0);
        [SerializeField] private Vector3 portalBPosition = new Vector3(10, 1, 0);
        [SerializeField] private Vector3 portalSize = new Vector3(2, 3, 0.1f);
        
        [Header("Test Environment")]
        [SerializeField] private bool createTestEnvironments = true;
        [SerializeField] private Color environmentAColor = Color.red;
        [SerializeField] private Color environmentBColor = Color.blue;

        [ContextMenu("Create URP Portal System")]
        public void CreatePortalSystem()
        {
            Debug.Log("🚀 Creating URP-compatible Portal System...");
            
            CreatePortalMaterials();
            CreatePortalObjects();
            
            if (createTestEnvironments)
            {
                CreateTestEnvironments();
            }
            
            SetupPortalManager();
            SetupXROrigin(); // Nuevo: configurar XR Origin automáticamente
            
            Debug.Log("✅ URP Portal System created successfully!");
            Debug.Log("📍 Portal A at: " + portalAPosition);
            Debug.Log("📍 Portal B at: " + portalBPosition);
            Debug.Log("🎮 Test with VR headset or use Scene view");
            Debug.Log("🚶‍♂️ Walk through portals to teleport!");
        }

        private void CreatePortalMaterials()
        {
            // Buscar el shader del portal
            Shader portalShader = Shader.Find("Xochicalco/Portal");
            if (portalShader == null)
            {
                Debug.LogWarning("⚠️ Portal shader not found! Using Unlit/Texture shader instead.");
                portalShader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            // Material para Portal A
            Material materialA = new Material(portalShader);
            materialA.name = "PortalA_Material_URP";
            if (portalShader.name.Contains("Portal"))
            {
                materialA.SetColor("_PortalColor", new Color(0.2f, 0.6f, 1.0f, 0.8f));
                materialA.SetFloat("_Brightness", 1.2f);
                materialA.SetFloat("_EdgeGlow", 0.5f);
            }

#if UNITY_EDITOR
            string pathA = "Assets/Materials/Portal/PortalA_Material_URP.mat";
            AssetDatabase.CreateAsset(materialA, pathA);
#endif

            // Material para Portal B
            Material materialB = new Material(portalShader);
            materialB.name = "PortalB_Material_URP";
            if (portalShader.name.Contains("Portal"))
            {
                materialB.SetColor("_PortalColor", new Color(1.0f, 0.4f, 0.2f, 0.8f));
                materialB.SetFloat("_Brightness", 1.2f);
                materialB.SetFloat("_EdgeGlow", 0.5f);
            }

#if UNITY_EDITOR
            string pathB = "Assets/Materials/Portal/PortalB_Material_URP.mat";
            AssetDatabase.CreateAsset(materialB, pathB);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif

            Debug.Log("✅ URP Portal materials created");
        }

        private void CreatePortalObjects()
        {
            // Portal A
            GameObject portalA = CreateSinglePortal("PortalA_URP", portalAPosition, "PortalA_Material_URP");
            
            // Portal B
            GameObject portalB = CreateSinglePortal("PortalB_URP", portalBPosition, "PortalB_Material_URP");
            
            // Rotar Portal B para que mire hacia Portal A
            portalB.transform.LookAt(portalA.transform.position);
            portalB.transform.Rotate(0, 180, 0);
            
            // Rotar Portal A para que mire hacia Portal B
            portalA.transform.LookAt(portalB.transform.position);
            portalA.transform.Rotate(0, 180, 0);

            // Configurar referencias cruzadas
            PortalControllerURP controllerA = portalA.GetComponent<PortalControllerURP>();
            PortalControllerURP controllerB = portalB.GetComponent<PortalControllerURP>();
            
            controllerA.SetLinkedPortal(controllerB);
            controllerB.SetLinkedPortal(controllerA);

            Debug.Log("✅ URP Portal objects created and linked");
        }

        private GameObject CreateSinglePortal(string name, Vector3 position, string materialName)
        {
            // GameObject principal del portal
            GameObject portal = new GameObject(name);
            portal.transform.position = position;
            portal.tag = "Portal"; // Agregar tag para identificación

            // Agregar PortalControllerURP
            PortalControllerURP controller = portal.AddComponent<PortalControllerURP>();

            // Crear el frame del portal (geometría)
            GameObject frame = CreatePortalFrame(name + "_Frame");
            frame.transform.SetParent(portal.transform, false);

            // Crear la superficie del portal
            GameObject surface = CreatePortalSurface(name + "_Surface", materialName);
            surface.transform.SetParent(portal.transform, false);

            // Crear la cámara del portal
            GameObject portalCamera = new GameObject(name + "_Camera");
            portalCamera.transform.SetParent(portal.transform, false);

            // Crear collider para detección
            BoxCollider collider = portal.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = portalSize;

            // Crear punto de destino
            GameObject destinationPoint = new GameObject(name + "_Destination");
            destinationPoint.transform.SetParent(portal.transform, false);
            destinationPoint.transform.localPosition = new Vector3(0, 0, -1);

            // Configurar el PortalControllerURP usando reflexión
            SetPortalControllerReferences(controller, portalCamera.transform, destinationPoint.transform, collider);

            // Asignar material
#if UNITY_EDITOR
            Material mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/Portal/{materialName}.mat");
            if (mat != null)
            {
                controller.SetPortalMaterial(mat);
            }
#endif

            return portal;
        }

        private GameObject CreatePortalFrame(string name)
        {
            GameObject frame = new GameObject(name);
            
            // Crear un frame básico con cubos
            for (int i = 0; i < 4; i++)
            {
                GameObject framePart = GameObject.CreatePrimitive(PrimitiveType.Cube);
                framePart.transform.SetParent(frame.transform, false);
                framePart.name = "FramePart_" + i;
                
                // Configurar posición y escala para formar un marco
                switch (i)
                {
                    case 0: // Top
                        framePart.transform.localPosition = new Vector3(0, portalSize.y/2 + 0.1f, 0);
                        framePart.transform.localScale = new Vector3(portalSize.x + 0.2f, 0.2f, 0.2f);
                        break;
                    case 1: // Bottom
                        framePart.transform.localPosition = new Vector3(0, -portalSize.y/2 - 0.1f, 0);
                        framePart.transform.localScale = new Vector3(portalSize.x + 0.2f, 0.2f, 0.2f);
                        break;
                    case 2: // Left
                        framePart.transform.localPosition = new Vector3(-portalSize.x/2 - 0.1f, 0, 0);
                        framePart.transform.localScale = new Vector3(0.2f, portalSize.y, 0.2f);
                        break;
                    case 3: // Right
                        framePart.transform.localPosition = new Vector3(portalSize.x/2 + 0.1f, 0, 0);
                        framePart.transform.localScale = new Vector3(0.2f, portalSize.y, 0.2f);
                        break;
                }
                
                // Material metálico usando URP
                Renderer renderer = framePart.GetComponent<Renderer>();
                Material frameMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                frameMat.color = new Color(0.3f, 0.3f, 0.3f);
                frameMat.SetFloat("_Metallic", 0.8f);
                frameMat.SetFloat("_Smoothness", 0.9f);
                renderer.material = frameMat;
            }
            
            return frame;
        }

        private GameObject CreatePortalSurface(string name, string materialName)
        {
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = name;
            surface.transform.localScale = new Vector3(portalSize.x, portalSize.y, 1);
            
            // Remover collider del quad
            DestroyImmediate(surface.GetComponent<Collider>());
            
            // El material se asignará después en el PortalController
            
            return surface;
        }

        private void CreateTestEnvironments()
        {
            // Ambiente A (alrededor de Portal A)
            CreateTestEnvironment("Environment_A_URP", portalAPosition + Vector3.back * 5, environmentAColor, "A");
            
            // Ambiente B (alrededor de Portal B)
            CreateTestEnvironment("Environment_B_URP", portalBPosition + Vector3.back * 5, environmentBColor, "B");
            
            Debug.Log("✅ URP Test environments created");
        }

        private void CreateTestEnvironment(string name, Vector3 center, Color color, string label)
        {
            GameObject environment = new GameObject(name);
            environment.transform.position = center;
            
            // Crear algunos objetos de prueba
            for (int i = 0; i < 5; i++)
            {
                GameObject testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                testObject.transform.SetParent(environment.transform, false);
                testObject.transform.localPosition = new Vector3(
                    Random.Range(-3f, 3f),
                    Random.Range(0f, 2f),
                    Random.Range(-3f, 3f)
                );
                testObject.transform.localScale = Vector3.one * Random.Range(0.5f, 1.5f);
                
                // Usar material URP
                Renderer renderer = testObject.GetComponent<Renderer>();
                Material urpMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                urpMat.color = color;
                renderer.material = urpMat;
                
                testObject.name = $"TestCube_{label}_{i}";
            }
            
            // Crear un texto 3D para identificar el ambiente
            GameObject textObj = new GameObject($"Text_{label}");
            textObj.transform.SetParent(environment.transform, false);
            textObj.transform.localPosition = Vector3.up * 3;
            
            TextMesh textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = $"ENVIRONMENT {label}";
            textMesh.fontSize = 100;
            textMesh.color = color;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textObj.transform.localScale = Vector3.one * 0.1f;
        }

        private void SetPortalControllerReferences(PortalControllerURP controller, Transform portalCamera, Transform destinationPoint, BoxCollider portalCollider)
        {
            // Usar reflexión para asignar campos privados
            var portalCameraField = typeof(PortalControllerURP).GetField("portalCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var destinationPointField = typeof(PortalControllerURP).GetField("destinationPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var portalColliderField = typeof(PortalControllerURP).GetField("portalCollider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            portalCameraField?.SetValue(controller, portalCamera);
            destinationPointField?.SetValue(controller, destinationPoint);
            portalColliderField?.SetValue(controller, portalCollider);
        }

        private void SetupPortalManager()
        {
            // Buscar o crear PortalManager
            PortalManager manager = FindFirstObjectByType<PortalManager>();
            if (manager == null)
            {
                GameObject managerObj = new GameObject("PortalManager_URP");
                manager = managerObj.AddComponent<PortalManager>();
            }

            // Buscar los portales creados
            PortalControllerURP portalA = GameObject.Find("PortalA_URP")?.GetComponent<PortalControllerURP>();
            PortalControllerURP portalB = GameObject.Find("PortalB_URP")?.GetComponent<PortalControllerURP>();

            if (portalA != null && portalB != null)
            {
                // Buscar cámara del jugador
                Camera playerCam = Camera.main;
                if (playerCam == null)
                {
                    playerCam = FindFirstObjectByType<Camera>();
                }
                
                if (playerCam != null)
                {
                    portalA.SetPlayerCamera(playerCam.transform);
                    portalB.SetPlayerCamera(playerCam.transform);
                }
            }

            Debug.Log("✅ URP PortalManager configured");
        }

        private void SetupXROrigin()
        {
            // Buscar XR Origin por nombre (más compatible)
            GameObject xrOriginObj = GameObject.Find("XR Origin");
            if (xrOriginObj == null)
            {
                // Buscar objetos que contengan "XR Origin" en el nombre
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("XR Origin") || obj.name.Contains("XROrigin"))
                    {
                        xrOriginObj = obj;
                        break;
                    }
                }
            }

            if (xrOriginObj == null)
            {
                Debug.LogWarning("⚠️ No XR Origin found. Portal system will work but may need manual camera setup.");
                return;
            }

            // Configurar XR Origin para portales usando método genérico
            ConfigureXROriginForPortals(xrOriginObj);

            Debug.Log("✅ XR Origin configured for portals");
        }

        public void ConfigureXROriginForPortals(GameObject xrOrigin)
        {
            Debug.Log("🔍 Setting up XR Origin for Portal System...");

            // 1. Configurar tag del XR Origin
            if (!xrOrigin.CompareTag("Player"))
            {
                xrOrigin.tag = "Player";
                Debug.Log($"✅ XR Origin tagged as 'Player'");
            }

            // 2. Asegurar que tiene Rigidbody para detectar colliders
            Rigidbody rb = xrOrigin.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = xrOrigin.AddComponent<Rigidbody>();
                rb.isKinematic = true; // Kinematic para que no interfiera con XR tracking
                Debug.Log("✅ Added kinematic Rigidbody to XR Origin");
            }

            // 3. Agregar collider si no tiene
            Collider col = xrOrigin.GetComponent<Collider>();
            if (col == null)
            {
                CapsuleCollider capsule = xrOrigin.AddComponent<CapsuleCollider>();
                capsule.isTrigger = true; // Trigger para no bloquear movimiento
                capsule.height = 1.8f;
                capsule.radius = 0.3f;
                capsule.center = new Vector3(0, 0.9f, 0);
                Debug.Log("✅ Added trigger CapsuleCollider to XR Origin");
            }

            // 4. Buscar cámara en XR Origin
            Camera xrCamera = xrOrigin.GetComponentInChildren<Camera>();
            if (xrCamera != null)
            {
                ConfigurePortalCamerasGeneric(xrCamera);
                Debug.Log($"✅ Found and configured XR camera: {xrCamera.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ No camera found in XR Origin children");
            }

            Debug.Log("🎯 XR Origin Portal Setup Complete!");
        }

        private void ConfigurePortalCamerasGeneric(Camera xrCamera)
        {
            // Configurar portales URP
            PortalControllerURP[] urpPortals = FindObjectsByType<PortalControllerURP>(FindObjectsSortMode.None);
            foreach (var portal in urpPortals)
            {
                portal.SetPlayerCamera(xrCamera.transform);
            }

            // Configurar portales clásicos si existen (para compatibilidad)
            PortalControllerURP[] classicPortals = FindObjectsByType<PortalControllerURP>(FindObjectsSortMode.None);
            foreach (var portal in classicPortals)
            {
                portal.SetPlayerCamera(xrCamera.transform);
            }

            Debug.Log($"✅ Configured {urpPortals.Length + classicPortals.Length} portals with XR camera");
        }

#if UNITY_EDITOR
        [MenuItem("Xochicalco/Setup URP Portal System")]
        static void SetupPortalSystemMenuItem()
        {
            GameObject helper = new GameObject("PortalSetupHelperURP");
            PortalSetupHelperURP setup = helper.AddComponent<PortalSetupHelperURP>();
            setup.CreatePortalSystem();
            DestroyImmediate(helper);
        }

        [MenuItem("Xochicalco/Fix XR Origin for Portals")]
        static void FixXROriginMenuItem()
        {
            // Buscar XR Origin por nombre
            GameObject xrOriginObj = GameObject.Find("XR Origin");
            if (xrOriginObj == null)
            {
                // Buscar objetos que contengan "XR Origin" en el nombre
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("XR Origin") || obj.name.Contains("XROrigin"))
                    {
                        xrOriginObj = obj;
                        break;
                    }
                }
            }

            if (xrOriginObj == null)
            {
                Debug.LogError("❌ No XR Origin found in scene!");
                return;
            }

            // Crear helper temporal para usar su método
            GameObject helper = new GameObject("TempHelper");
            PortalSetupHelperURP setup = helper.AddComponent<PortalSetupHelperURP>();
            setup.ConfigureXROriginForPortals(xrOriginObj);
            DestroyImmediate(helper);
        }
#endif
    }
}