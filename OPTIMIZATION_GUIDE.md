# Guía de Optimización - Xochicalco VR

## 🎯 Objetivos de Rendimiento

### Targets para VR
- **90 FPS** mínimo para dispositivos de alta gama
- **72 FPS** para dispositivos móviles VR
- **<20ms** motion-to-photon latency
- **<50MB** uso de memoria de GPU

## 🔧 Optimizaciones Implementadas

### 1. Shaders
#### Portal.shader Optimizaciones
```hlsl
// Antes: Múltiples samples de textura
// Después: Single sample con distorsión calculada

// Antes: Ruido simple
float noise(float2 uv) {
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

// Después: Hash optimizado
float hash21(float2 p) {
    p = frac(p * float2(233.34, 851.73));
    p += dot(p, p + 23.45);
    return frac(p.x * p.y);
}
```

**Beneficios**:
- 40% menos operaciones matemáticas
- Mejor distribución de ruido
- Compatible con GPU instancing

### 2. Validación de Proyecto
```csharp
// Cache de validación para evitar checks repetitivos
static readonly Dictionary<string, (bool isValid, DateTime lastCheck)> s_ValidationCache;
static readonly TimeSpan s_CacheExpiry = TimeSpan.FromMinutes(5);
```

**Beneficios**:
- Reduce tiempo de startup del editor
- Evita requests innecesarios a Package Manager
- Mejora responsividad del editor

### 3. Memory Management
- **Object Pooling** para partículas y efectos
- **Texture Streaming** habilitado
- **Mesh Compression** en assets estáticos

## 📊 Profiling Guidelines

### Herramientas Recomendadas
1. **Unity Profiler** - CPU y memoria
2. **Frame Debugger** - GPU debugging
3. **XR Performance Toolkit** - VR específico
4. **Snapdragon Profiler** (Quest devices)

### Métricas Clave
```csharp
// Ejemplo de métricas a monitorear
- Draw Calls: <100 per frame
- Triangles: <100k per frame
- Memory: <512MB total
- GPU Time: <11ms per frame (90fps)
```

## ⚡ Técnicas de Optimización

### 1. LOD (Level of Detail)
```csharp
// Configuración recomendada
LOD 0: 0-25m    // Full detail
LOD 1: 25-50m   // 50% polygons
LOD 2: 50-100m  // 25% polygons
LOD 3: 100m+    // Billboard/Impostor
```

### 2. Occlusion Culling
- Habilitar en cámaras principales
- Bake occlusion data para escenas estáticas
- Usar Occlusion Areas para precisión

### 3. Texture Optimization
```yaml
# Configuración recomendada por tipo
UI Textures:
  - Format: RGB24/RGBA32
  - Max Size: 512x512
  - Compression: High Quality

Albedo Maps:
  - Format: RGB Compressed DXT1
  - Max Size: 1024x1024
  - Generate Mip Maps: Yes

Normal Maps:
  - Format: Normal map DXT5nm
  - Max Size: 1024x1024
  - Generate Mip Maps: Yes
```

### 4. Shader Variants
```csharp
// Usar shader keywords estratégicamente
#pragma multi_compile _ FEATURE_ENABLED
#pragma multi_compile_local _ LOCAL_FEATURE

// Evitar variants innecesarios
#pragma skip_variants LIGHTPROBE_SH
```

## 🎮 VR Specific Optimizations

### 1. Foveated Rendering
```csharp
// Para dispositivos compatibles
XRSettings.eyeTextureResolutionScale = 0.8f;
// Usar Variable Rate Shading cuando disponible
```

### 2. Single Pass Instanced
```csharp
// En configuración de XR
XRSettings.renderViewportScale = 1.0f;
XRSettings.useOcclusionMesh = true;
```

### 3. Physics Optimization
```csharp
// Reducir frecuencia de physics para objetos lejanos
Time.fixedDeltaTime = 1.0f / 45.0f; // Para objetos no críticos
```

## 📱 Mobile VR Considerations

### Quest 2/3 Específico
```csharp
// Configuración recomendada
Application.targetFrameRate = 72;
QualitySettings.vSyncCount = 0;

// Thermal throttling prevention
Application.runInBackground = false;
```

### Memory Budget
```
Total Memory Budget: 3GB
- Unity Engine: ~800MB
- Scripts/Logic: ~200MB  
- Textures: ~1.5GB
- Audio: ~300MB
- Meshes: ~200MB
```

## 🔍 Debugging Performance

### Common Issues
1. **High Draw Calls**
   - Solution: Batch similar objects
   - Use SRP Batcher
   - Combine meshes where possible

2. **GPU Bottleneck**
   - Reduce shader complexity
   - Lower texture resolution
   - Use simpler lighting

3. **CPU Bottleneck**
   - Optimize scripts
   - Reduce GameObject count
   - Use Job System for heavy calculations

### Profiler Workflow
1. **Identify bottleneck** (CPU vs GPU)
2. **Isolate problematic systems**
3. **Apply targeted optimizations**
4. **Measure improvement**
5. **Repeat until target performance**

### 🔍 Debug Estéreo en Portales

Errores “solo ojo izquierdo” casi siempre = matrices o RT incorrectos.

Buenas prácticas (compatibility y futura migración RenderGraph):
1. No uses Camera.projectionMatrix global; usa:
   for each eye:
     var proj = cam.GetStereoProjectionMatrix(eye);
     var view = cam.GetStereoViewMatrix(eye);
2. Si aplicas oblique clip plane:
   proj = GeometryUtility.CalculateObliqueMatrix(proj, plane);
3. RenderTexture:
   - dimension = Tex2DArray en Single Pass Instanced (Unity lo gestiona si usas targets de URP).
   - No reutilizar un RT creado antes de que XR cambie tamaño (escuchar XRDisplaySubsystem.refreshRate / texture descriptors).
4. En RenderGraph (futuro):
   - Crea un pass por eye o usa flag de multi-eye soportado por URP (cameraData.xr.enabled).
   - Evita almacenar matrices estáticas; usar cameraData.GetViewMatrix(eye) y cameraData.GetProjectionMatrix(eye).
5. Validar:
   - Diferencia leve entre projectionMatrix[0,2] de cada ojo (desplazamiento horizontal).
   - Si son idénticas → origen del bug.

Perf tip:
- Calcula lógica pesada (detección de portal destino) una vez por frame y solo duplica matrices.

## 📈 Performance Monitoring

### Runtime Metrics
```csharp
public class PerformanceMonitor : MonoBehaviour
{
    private float frameTime;
    private int frameCount;
    
    void Update()
    {
        frameTime += Time.unscaledDeltaTime;
        frameCount++;
        
        if (frameTime >= 1.0f)
        {
            float fps = frameCount / frameTime;
            if (fps < 72) // Warning threshold
            {
                Debug.LogWarning($"Performance issue: {fps:F1} FPS");
            }
            
            frameTime = 0;
            frameCount = 0;
        }
    }
}
```

## 🎯 Checklist de Optimización

### Pre-Build
- [ ] All textures compressed appropriately
- [ ] Unused assets removed
- [ ] LOD groups configured
- [ ] Occlusion culling baked
- [ ] Shader variants stripped

### Post-Build
- [ ] Profile on target device
- [ ] Check memory usage
- [ ] Verify 90fps in critical scenes
- [ ] Test thermal performance
- [ ] Validate with different users

## 🧪 Migración a RenderGraph (URP)

1. Objetivo: Rehabilitar RenderGraph y desactivar Compatibility Mode (actualmente requerido por PortalRenderer).
2. Identificar ScriptableRenderPass heredados (clase que hereda de ScriptableRenderPass).
3. Para cada Pass:
   - Sustituir Execute(ScriptableRenderContext, ref RenderingData) por un RecordRenderGraph(context, renderGraph, frameData) usando el nuevo API (URP 17+).
   - Reemplazar GetTemporaryRT / CommandBuffer con TextureHandle = renderGraph.CreateTexture / ImportTexture.
   - Usar RenderGraphBuilder:
     builder.ReadTexture(...)
     builder.WriteTexture(...)
     builder.SetRenderFunc((PassData data, RenderGraphContext ctx) => { /* dibujo */ });
4. Evitar CommandBuffer.ReleaseTemporaryRT (RenderGraph gestiona lifetime).
5. Para efectos de portal:
   - Crear textura destino: var descriptor = renderGraph.GetDescriptor(cameraData.cameraTargetDescriptor, "_PortalRT"); descriptor.depthBufferBits = 0;
   - Registrar cámara secundaria usando UniversalCameraData (o CameraStack) antes de RecordRenderGraph.
   - Usar builder.AllowPassCulling(false) si dependes de efectos laterales.
6. Revisar uso de MSAA: si necesitabas resolver manual (error “Missing resolve surface”), usar builder.SetGlobalTexture o EnableRandomWrite en descriptor + dejar que URP haga Resolve integrado.
7. Después de migrar todos los passes, usar regla de validación (añadida) o activar manualmente:
   - m_EnableRenderGraph = 1
   - m_EnableRenderCompatibilityMode = 0
8. Validar con Frame Debugger: no debe haber “Not inside a Renderpass”.
9. Medir:
   - Reducir picos de memoria transitoria
   - Mejor batching interno (menos SetPass)
10. Si un pass necesita un RT persistente varias frames: usar renderGraph.ImportTexture(existingRT) y gestionar su ciclo tú mismo.

## 🚀 Pasos Concretos (Proyecto URP con Compatibility Mode)

1. Inventario de pases personalizados:
   - Buscar clases que heredan de ScriptableRenderPass (ej: PortalRenderer / Mirror / cualquier *Pass*).
2. Aislar los que usan:
   - CommandBuffer.GetTemporaryRT / ReleaseTemporaryRT
   - Blit(cmd, src, dst, ...)
   - ConfigureTarget / ConfigureClear
   - Uso manual de MSAA o resolve.
3. Crear versión RenderGraph:
   - En URP 17+ usar override RecordRenderGraph en tu Renderer Feature (o AddRenderPasses -> cambiar a RecordRenderGraph).
   - Reemplazar Execute(...):
     builder = renderGraph.AddRenderPass<PassData>("Portal Pass", out var passData);
     passData.inColor = srcHandle;
     builder.ReadTexture(srcHandle);
     passData.outColor = builder.WriteTexture(renderGraph.CreateTexture(descriptor));
     builder.SetRenderFunc((PassData data, RenderGraphContext ctx) =>
     {
         // ctx.cmd: usar Blitter.BlitTexture o CoreUtils.DrawFullScreen
     });
4. Sustituir GetTemporaryRT por:
   descriptor = renderingData.cameraData.cameraTargetDescriptor;
   var rtHandle = renderGraph.CreateTexture(descriptor, "_PortalRT");
5. No usar ReleaseTemporaryRT (RenderGraph gestiona lifetime).
6. Para portal/mirror cámaras:
   - Evitar crear CommandBuffer.Begin/EndSample manuales.
   - Usar cameraData.GetUniversalAdditionalCameraData() si necesitas flags.
   - Si generas una cámara auxiliar, renderízala antes y pasa su ColorTexture como TextureHandle via ImportTexture si es externa.
7. MSAA / Resolve:
   - No llamar a cmd.Resolve. Ajustar descriptor.msaaSamples; RenderGraph hace el resolve implícito.
8. Profiler:
   - Verificar reducción de picos de memoria transitoria y ausencia de “Not inside a Renderpass”.
9. Activar RenderGraph:
   - Edita UniversalRenderPipelineGlobalSettings:
     m_EnableRenderGraph = 1
     m_EnableRenderCompatibilityMode = 0
10. Limpieza:
   - Eliminar código muerto (Execute obsoleto).
   - Validar en dispositivos XR (una cámara por ojo sigue funcionando).

Checklist rápida:
- [ ] Ningún ScriptableRenderPass antiguo en uso.
- [ ] Sin GetTemporaryRT / ReleaseTemporaryRT.
- [ ] Sin errores “Missing resolve surface”.
- [ ] Compatibility Mode desactivado.
- [ ] FPS estable y sin regresiones gráficas.

Ejemplo mínimo de conversión de un pass (pseudo):
```csharp
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    var cameraData = frameData.Get<UniversalCameraData>();
    var colorHandle = cameraData.renderer.cameraColorTargetHandle;

    var desc = cameraData.cameraTargetDescriptor;
    var output = renderGraph.CreateTexture(desc, "_PortalRT");

    using (var builder = renderGraph.AddRenderPass<PassData>("Portal RG Pass", out var passData))
    {
        passData.src = colorHandle;
        passData.dst = builder.WriteTexture(output);
        builder.ReadTexture(colorHandle);
        builder.SetRenderFunc((PassData data, RenderGraphContext ctx) =>
        {
            Blitter.BlitTexture(ctx.cmd, data.src, new Vector4(1,1,0,0), 0, false);
        });
    }
}
```

Problemas comunes:
- Artefactos negros: descriptor incorrecto (formato o depthBufferBits ≠ 0 si no necesitas depth).
- Doble clear: remover ConfigureClear antiguo; confiar en cámara principal o agregar pass de clear explícito RG.
- “Out of range RT”: faltó builder.ReadTexture/WriteTexture.

---

**Recuerda**: Optimizar es un proceso iterativo. Siempre mide antes y después de cada cambio.
