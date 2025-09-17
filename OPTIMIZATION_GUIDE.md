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

---

**Recuerda**: Optimizar es un proceso iterativo. Siempre mide antes y después de cada cambio.
