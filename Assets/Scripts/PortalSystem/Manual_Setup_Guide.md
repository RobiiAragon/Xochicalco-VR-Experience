# Guía de Implementación Manual - Sistema de Portales

## Paso a Paso: Configuración Manual

### 1. Crear RenderTextures
1. **Clic derecho en Project** → Create → Render Texture
2. **Nombrar**: `PortalA_RenderTexture`
3. **Configurar**:
   - Size: 1024 x 1024
   - Depth Buffer: 16 bit
   - Anti-aliasing: None
4. **Repetir** para `PortalB_RenderTexture`

### 2. Crear Materiales
1. **Clic derecho en Project** → Create → Material
2. **Nombrar**: `PortalA_Material`
3. **Asignar Shader**: Xochicalco/Portal
4. **Configurar propiedades**:
   - Main Tex: PortalA_RenderTexture
   - Portal Color: Azul (0.2, 0.6, 1.0, 0.8)
   - Brightness: 1.2
   - Edge Glow: 0.5
5. **Repetir** para Portal B con color naranja

### 3. Crear GameObject Portal
1. **GameObject vacío** → Nombrar "PortalA"
2. **Agregar componente**: PortalController
3. **Crear geometría**:
   - Child object → Quad (para superficie portal)
   - Asignar PortalA_Material al Quad
   - Scale del Quad: (2, 3, 1)
4. **Crear marco decorativo** (opcional):
   - Varios cubos como children
   - Posicionarlos como marco alrededor del Quad

### 4. Configurar Cámara del Portal
1. **Child object de PortalA** → Nombrar "PortalCamera"
2. **Agregar componente**: Camera
3. **Configurar cámara**:
   - Enabled: ❌ (false)
   - Target Texture: PortalA_RenderTexture
   - Near Clipping: 0.1
   - Far Clipping: 1000

### 5. Configurar Collider
1. **En PortalA agregar**: BoxCollider
2. **Configurar**:
   - Is Trigger: ✅ (true)
   - Size: (2, 3, 0.5)

### 6. Crear Punto de Destino
1. **Child object de PortalA** → GameObject vacío
2. **Nombrar**: "DestinationPoint"
3. **Posición local**: (0, 0, -1)

### 7. Repetir para Portal B
- Mismos pasos pero con materiales/textures de Portal B
- Posicionar a ~10 unidades de distancia

### 8. Configurar PortalController
En cada PortalController asignar:
- **Portal Camera**: La cámara child
- **Player Camera**: La cámara principal del jugador
- **Linked Portal**: El otro portal
- **Destination Point**: El punto de destino
- **Portal Collider**: El BoxCollider

### 9. Crear PortalManager
1. **GameObject vacío** → "PortalManager"
2. **Agregar componente**: PortalManager
3. **Configurar Portal Pairs**:
   - Portal A: Referencia a PortalA
   - Portal B: Referencia a PortalB
   - Connection Name: "Test Connection"

## Testing y Debugging

### Verificar Funcionamiento
1. **Renderizado**: ¿Se ve el otro ambiente en el portal?
2. **Teleportación**: ¿El jugador se teletransporta al cruzar?
3. **Rotación**: ¿La orientación es correcta después del teleport?

### Problemas Comunes
- **Portal negro**: Verificar RenderTexture asignado
- **No teleporta**: Verificar que player tenga tag "Player"
- **Rotación incorrecta**: Verificar orientación de los portales

### Debug Tools
- Usar `PortalManager.DebugPortalInfo()` en inspector
- Activar Gizmos en Scene view
- Verificar Console para logs de debug

## Optimización
- Ajustar `maxRenderDistance` según necesidades
- Usar LOD en objetos complejos
- Configurar culling de ambientes lejanos