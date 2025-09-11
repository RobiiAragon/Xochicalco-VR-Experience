using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Xochicalco.PortalSystem
{
    public class PortalSetupHelper : MonoBehaviour
    {
        [Header("Portal Configuration")]
        [SerializeField] private Vector3 portalAPosition = new Vector3(0, 1, 0);
        [SerializeField] private Vector3 portalBPosition = new Vector3(10, 1, 0);
        [SerializeField] private Vector3 portalSize = new Vector3(2, 3, 0.1f);
        
        [Header("Test Environment")]
        [SerializeField] private bool createTestEnvironments = true;
        [SerializeField] private Color environmentAColor = Color.red;
        [SerializeField] private Color environmentBColor = Color.blue;

        [ContextMenu("Create Portal System")]
        public void CreatePortalSystem()
        {
            CreateRenderTextures();
            CreatePortalMaterials();
            CreatePortalObjects();
            
            if (createTestEnvironments)
            {
                CreateTestEnvironments();
            }
            
            SetupPortalManager();
            
            Debug.Log("✅ Portal System created successfully!");
            Debug.Log("📍 Portal A at: " + portalAPosition);
            Debug.Log("📍 Portal B at: " + portalBPosition);
            Debug.Log("🎮 Test with the Player in VR or use WASD + Mouse in Scene view");
        }

        private void CreateRenderTextures()
        {
            // Crear RenderTexture para Portal A
            RenderTexture renderTextureA = new RenderTexture(1024, 1024, 16);
            renderTextureA.name = "PortalA_RenderTexture";
#if UNITY_EDITOR
            AssetDatabase.CreateAsset(renderTextureA, "Assets/RenderTextures/PortalA_RenderTexture.asset");
#endif

            // Crear RenderTexture para Portal B
            RenderTexture renderTextureB = new RenderTexture(1024, 1024, 16);
            renderTextureB.name = "PortalB_RenderTexture";
#if UNITY_EDITOR
            AssetDatabase.CreateAsset(renderTextureB, "Assets/RenderTextures/PortalB_RenderTexture.asset");
#endif

            Debug.Log("✅ RenderTextures created");
        }

        private void CreatePortalMaterials()
        {
            // Buscar el shader del portal
            Shader portalShader = Shader.Find("Xochicalco/Portal");
            if (portalShader == null)
            {
                Debug.LogWarning("⚠️ Portal shader not found! Using Standard shader instead.");
                portalShader = Shader.Find("Universal Render Pipeline/Lit");
            }

            // Material para Portal A
            Material materialA = new Material(portalShader);
            materialA.name = "PortalA_Material";
            if (portalShader.name.Contains("Portal"))
            {
                materialA.SetColor("_PortalColor", new Color(0.2f, 0.6f, 1.0f, 0.8f));
                materialA.SetFloat("_Brightness", 1.2f);
                materialA.SetFloat("_EdgeGlow", 0.5f);
            }

#if UNITY_EDITOR
            AssetDatabase.CreateAsset(materialA, "Assets/Materials/Portal/PortalA_Material.mat");
#endif

            // Material para Portal B
            Material materialB = new Material(portalShader);
            materialB.name = "PortalB_Material";
            if (portalShader.name.Contains("Portal"))
            {
                materialB.SetColor("_PortalColor", new Color(1.0f, 0.4f, 0.2f, 0.8f));
                materialB.SetFloat("_Brightness", 1.2f);
                materialB.SetFloat("_EdgeGlow", 0.5f);
            }

#if UNITY_EDITOR
            AssetDatabase.CreateAsset(materialB, "Assets/Materials/Portal/PortalB_Material.mat");
#endif

            Debug.Log("✅ Portal materials created");
        }

        private void CreatePortalObjects()
        {
            // Portal A
            GameObject portalA = CreateSinglePortal("PortalA", portalAPosition, "PortalA_Material", "PortalA_RenderTexture");
            
            // Portal B
            GameObject portalB = CreateSinglePortal("PortalB", portalBPosition, "PortalB_Material", "PortalB_RenderTexture");
            
            // Rotar Portal B para que mire hacia Portal A
            portalB.transform.LookAt(portalA.transform.position);
            portalB.transform.Rotate(0, 180, 0);
            
            // Rotar Portal A para que mire hacia Portal B
            portalA.transform.LookAt(portalB.transform.position);
            portalA.transform.Rotate(0, 180, 0);

            Debug.Log("✅ Portal objects created");
        }

        private GameObject CreateSinglePortal(string name, Vector3 position, string materialName, string renderTextureName)
        {
            // GameObject principal del portal
            GameObject portal = new GameObject(name);
            portal.transform.position = position;

            // Agregar PortalController
            PortalControllerURP controller = portal.AddComponent<PortalControllerURP>();

            // Crear el frame del portal (geometría)
            GameObject frame = CreatePortalFrame(name + "_Frame");
            frame.transform.SetParent(portal.transform, false);

            // Crear la superficie del portal
            GameObject surface = CreatePortalSurface(name + "_Surface", materialName, renderTextureName);
            surface.transform.SetParent(portal.transform, false);

            // Crear la cámara del portal
            GameObject portalCamera = new GameObject(name + "_Camera");
            portalCamera.transform.SetParent(portal.transform, false);
            Camera cam = portalCamera.AddComponent<Camera>();
            
            // Configurar cámara
            cam.enabled = false;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;

            // Cargar y asignar RenderTexture
#if UNITY_EDITOR
            RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>($"Assets/RenderTextures/{renderTextureName}.asset");
            if (rt != null)
            {
                cam.targetTexture = rt;
                
                // Asignar también al material
                Material mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/Portal/{materialName}.mat");
                if (mat != null)
                {
                    mat.mainTexture = rt;
                }
            }
#endif

            // Crear collider para detección
            BoxCollider collider = portal.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = portalSize;

            // Crear punto de destino
            GameObject destinationPoint = new GameObject(name + "_Destination");
            destinationPoint.transform.SetParent(portal.transform, false);
            destinationPoint.transform.localPosition = new Vector3(0, 0, -1);

            // Configurar el PortalController
            SetPortalControllerReferences(controller, portalCamera.transform, destinationPoint.transform, collider);

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
                
                // Material metálico
                Renderer renderer = framePart.GetComponent<Renderer>();
                renderer.material.color = new Color(0.3f, 0.3f, 0.3f);
            }
            
            return frame;
        }

        private GameObject CreatePortalSurface(string name, string materialName, string renderTextureName)
        {
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = name;
            surface.transform.localScale = new Vector3(portalSize.x, portalSize.y, 1);
            
            // Remover collider del quad
            DestroyImmediate(surface.GetComponent<Collider>());
            
            // Asignar material
#if UNITY_EDITOR
            Material mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/Portal/{materialName}.mat");
            if (mat != null)
            {
                surface.GetComponent<Renderer>().material = mat;
            }
#endif
            
            return surface;
        }

        private void CreateTestEnvironments()
        {
            // Ambiente A (alrededor de Portal A)
            CreateTestEnvironment("Environment_A", portalAPosition + Vector3.back * 5, environmentAColor, "A");
            
            // Ambiente B (alrededor de Portal B)
            CreateTestEnvironment("Environment_B", portalBPosition + Vector3.back * 5, environmentBColor, "B");
            
            Debug.Log("✅ Test environments created");
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
                testObject.GetComponent<Renderer>().material.color = color;
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
                GameObject managerObj = new GameObject("PortalManager");
                manager = managerObj.AddComponent<PortalManager>();
            }

            // Buscar los portales creados
            PortalControllerURP portalA = GameObject.Find("PortalA")?.GetComponent<PortalControllerURP>();
            PortalControllerURP portalB = GameObject.Find("PortalB")?.GetComponent<PortalControllerURP>();

            if (portalA != null && portalB != null)
            {
                manager.AddPortalPair(portalA, portalB, "Test Portal Connection");
                
                // Configurar referencias cruzadas
                portalA.SetLinkedPortal(portalB);
                portalB.SetLinkedPortal(portalA);
                
                // Buscar cámara del jugador
                Camera playerCam = Camera.main;
                if (playerCam != null)
                {
                    portalA.SetPlayerCamera(playerCam.transform);
                    portalB.SetPlayerCamera(playerCam.transform);
                }
            }

            Debug.Log("✅ PortalManager configured");
        }

#if UNITY_EDITOR
        [MenuItem("Xochicalco/Setup Portal System")]
        static void SetupPortalSystemMenuItem()
        {
            GameObject helper = new GameObject("PortalSetupHelper");
            PortalSetupHelper setup = helper.AddComponent<PortalSetupHelper>();
            setup.CreatePortalSystem();
            DestroyImmediate(helper);
        }
#endif
    }
}