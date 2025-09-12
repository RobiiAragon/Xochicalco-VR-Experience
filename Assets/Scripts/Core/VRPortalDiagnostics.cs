using UnityEngine;
using UnityEditor;

public class VRPortalDiagnostics : MonoBehaviour
{
    [MenuItem("Tools/XR Portal Setup/Complete VR Portal Diagnosis")]
    public static void CompleteVRPortalDiagnosis()
    {
        Debug.Log("🔍 === DIAGNÓSTICO COMPLETO DEL SISTEMA DE PORTALES VR ===");
        
        // 1. Verificar XR Origin
        var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("❌ PROBLEMA CRÍTICO: No se encontró XR Origin en la escena!");
            Debug.LogError("   Solución: Agrega un XR Origin desde XR > XR Origin (XR Rig)");
            return;
        }
        
        Debug.Log($"✅ XR Origin encontrado: {xrOrigin.name}");
        
        // 2. Verificar cámara VR
        Camera vrCamera = xrOrigin.Camera;
        if (vrCamera == null)
        {
            Debug.LogError("❌ PROBLEMA: XR Origin no tiene cámara asociada!");
        }
        else
        {
            Debug.Log($"✅ Cámara VR encontrada: {vrCamera.name}");
            
            // Verificar XRMainCamera
            var xrMainCamera = vrCamera.GetComponent<XRMainCamera>();
            if (xrMainCamera == null)
            {
                Debug.LogWarning("⚠️ FALTA: XRMainCamera no está en la cámara VR");
                Debug.LogWarning("   Solución: Ejecuta 'Configure XR Origin for Portals'");
            }
            else
            {
                Debug.Log("✅ XRMainCamera configurado correctamente");
            }
        }
        
        // 3. Verificar PortalTraveller en XR Origin
        var vrTraveller = xrOrigin.GetComponent<VRPortalTraveller>();
        var basicTraveller = xrOrigin.GetComponent<PortalTraveller>();
        
        if (vrTraveller == null && basicTraveller == null)
        {
            Debug.LogError("❌ PROBLEMA: XR Origin no tiene PortalTraveller!");
            Debug.LogError("   Solución: Ejecuta 'Configure XR Origin for Portals'");
        }
        else if (basicTraveller != null && vrTraveller == null)
        {
            Debug.LogWarning("⚠️ MEJORA DISPONIBLE: Usando PortalTraveller básico en lugar de VRPortalTraveller");
            Debug.LogWarning("   Solución: Ejecuta 'Configure XR Origin for Portals'");
        }
        else if (vrTraveller != null)
        {
            Debug.Log("✅ VRPortalTraveller configurado correctamente");
        }
        
        // 4. Verificar colliders y rigidbody
        var collider = xrOrigin.GetComponent<Collider>();
        if (collider == null || !collider.isTrigger)
        {
            Debug.LogWarning("⚠️ PROBLEMA: XR Origin necesita un collider trigger");
            Debug.LogWarning("   Solución: Ejecuta 'Configure XR Origin for Portals'");
        }
        else
        {
            Debug.Log("✅ Collider trigger configurado");
        }
        
        var rigidbody = xrOrigin.GetComponent<Rigidbody>();
        if (rigidbody == null || !rigidbody.isKinematic)
        {
            Debug.LogWarning("⚠️ PROBLEMA: XR Origin necesita un Rigidbody kinematic");
            Debug.LogWarning("   Solución: Ejecuta 'Configure XR Origin for Portals'");
        }
        else
        {
            Debug.Log("✅ Rigidbody kinematic configurado");
        }
        
        // 5. Verificar portales en la escena
        Portal[] portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
        Debug.Log($"📊 Portales encontrados: {portals.Length}");
        
        if (portals.Length == 0)
        {
            Debug.LogWarning("⚠️ No hay portales en la escena");
            Debug.LogWarning("   Solución: Ejecuta 'Create VR Portal Pair' o agrega portales manualmente");
        }
        else
        {
            int problemPortals = 0;
            foreach (var portal in portals)
            {
                bool hasProblems = false;
                
                // Verificar que tenga pantalla
                if (portal.screen == null)
                {
                    Debug.LogError($"❌ Portal {portal.name}: No tiene pantalla asignada!");
                    hasProblems = true;
                }
                
                // Verificar que esté enlazado
                if (portal.linkedPortal == null)
                {
                    Debug.LogWarning($"⚠️ Portal {portal.name}: No está enlazado a otro portal");
                    hasProblems = true;
                }
                
                // Verificar que tenga cámara
                Camera portalCam = portal.GetComponentInChildren<Camera>();
                if (portalCam == null)
                {
                    Debug.LogError($"❌ Portal {portal.name}: No tiene cámara!");
                    hasProblems = true;
                }
                
                if (hasProblems)
                {
                    problemPortals++;
                }
                else
                {
                    Debug.Log($"✅ Portal {portal.name}: Configurado correctamente");
                }
            }
            
            if (problemPortals > 0)
            {
                Debug.LogWarning($"⚠️ {problemPortals} portales tienen problemas");
                Debug.LogWarning("   Solución: Ejecuta 'Fix Portal Camera Issues' o 'Fix Portal Materials'");
            }
        }
        
        // 6. Verificar manos VR
        Debug.Log("👋 Verificando manos VR...");
        Transform[] allTransforms = xrOrigin.GetComponentsInChildren<Transform>();
        int handsFound = 0;
        int handsConfigured = 0;
        
        foreach (Transform child in allTransforms)
        {
            if (child.name.ToLower().Contains("hand") && 
                (child.name.ToLower().Contains("left") || child.name.ToLower().Contains("right")))
            {
                handsFound++;
                
                var handTraveller = child.GetComponent<PortalTraveller>();
                var handCollider = child.GetComponent<Collider>();
                
                if (handTraveller != null && handCollider != null)
                {
                    handsConfigured++;
                }
            }
        }
        
        Debug.Log($"👋 Manos encontradas: {handsFound}, configuradas: {handsConfigured}");
        
        if (handsFound > 0 && handsConfigured < handsFound)
        {
            Debug.LogWarning("⚠️ Algunas manos VR no están configuradas para portales");
            Debug.LogWarning("   Solución: Ejecuta 'Setup VR Hands for Portals'");
        }
        
        // 7. Resumen final
        Debug.Log("🎯 === RESUMEN DEL DIAGNÓSTICO ===");
        
        bool allGood = true;
        
        if (xrOrigin == null) allGood = false;
        if (vrCamera == null) allGood = false;
        if (vrTraveller == null && basicTraveller == null) allGood = false;
        if (portals.Length == 0) allGood = false;
        
        if (allGood)
        {
            Debug.Log("🎉 ¡SISTEMA DE PORTALES VR LISTO!");
            Debug.Log("   Todos los componentes están configurados correctamente.");
            Debug.Log("   ¡Puedes probar los portales en VR!");
        }
        else
        {
            Debug.LogWarning("⚠️ El sistema necesita configuración adicional");
            Debug.LogWarning("   Ejecuta los comandos sugeridos arriba para completar la configuración");
        }
        
        // 8. Acciones recomendadas
        Debug.Log("🛠️ === ACCIONES DISPONIBLES ===");
        Debug.Log("   • Tools > XR Portal Setup > Configure XR Origin for Portals");
        Debug.Log("   • Tools > XR Portal Setup > Setup VR Hands for Portals");
        Debug.Log("   • Tools > Portal System > Create VR Portal Pair");
        Debug.Log("   • Tools > XR Portal Setup > Fix Portal Camera Issues");
        Debug.Log("   • Tools > Portal System > Fix Portal Materials");
    }
}