# Portal System Documentation

## Descripción General
Este sistema de portales está inspirado en el juego Portal de Valve, permitiendo crear conexiones instantáneas entre diferentes áreas del mundo con renderizado en tiempo real que permite "ver a través" del portal.

## Características Principales

### ✅ Renderizado en Tiempo Real
- **Vista previa**: Puedes ver el área de destino antes de cruzar el portal
- **Perspectiva correcta**: La vista se ajusta según tu posición y ángulo
- **Anti-recursión**: Previene renderizado infinito entre portales opuestos

### ✅ Teleportación Suave
- **Preservación de momentum**: La velocidad se mantiene al cruzar
- **Rotación calculada**: La orientación se ajusta correctamente
- **Prevención de glitches**: Sistema anti-múltiple teleportación

### ✅ Gestión de Performance
- **Culling por distancia**: Los ambientes lejanos se desactivan automáticamente
- **LOD automático**: Nivel de detalle basado en distancia
- **Renderizado condicional**: Solo renderiza portales cercanos

## Arquitectura Recomendada: Una Escena Grande

### Ventajas de esta Aproximación:
1. **Renderizado Real**: El efecto "ver a través" funciona perfectamente
2. **Sin Tiempos de Carga**: Transiciones instantáneas
3. **Física Continua**: El momentum se preserva naturalmente
4. **Simplicidad**: Implementación más directa

### Organización Espacial:
```
Mundo Principal
├── Área Central (0, 0, 0)
├── Ambiente Azteca (1000, 0, 0)
├── Ambiente Moderno (-1000, 0, 0)
├── Ambiente Subterráneo (0, -500, 0)
└── Ambiente Celestial (0, 500, 0)
```

## Componentes del Sistema

### 1. PortalController
- **Función**: Controla un portal individual
- **Características**:
  - Renderiza la vista del portal conectado
  - Maneja la teleportación del jugador
  - Gestiona la cámara de renderizado

### 2. PortalManager
- **Función**: Gestiona toda la red de portales
- **Características**:
  - Configura conexiones entre portales
  - Gestiona el culling de ambientes
  - Optimiza el rendimiento

### 3. Portal Shader
- **Función**: Renderiza el efecto visual del portal
- **Características**:
  - Distorsión de ruido animada
  - Efecto de brillo en los bordes
  - Soporte para URP

## Configuración Paso a Paso

### Paso 1: Crear un Portal
1. Crear un GameObject vacío
2. Agregar el componente `PortalController`
3. Crear una geometría de portal (plano o marco)
4. Aplicar el material con el shader Portal
5. Configurar el BoxCollider como trigger

### Paso 2: Configurar la Cámara del Portal
1. Crear un hijo del portal llamado "PortalCamera"
2. Agregar componente Camera
3. Crear un RenderTexture para la salida
4. Asignar el RenderTexture al material del portal

### Paso 3: Conectar Portales
1. Crear al menos dos portales
2. En el PortalManager, agregar un PortalPair
3. Asignar Portal A y Portal B
4. Configurar los puntos de destino

### Paso 4: Configurar Ambientes
1. Crear GameObjects padre para cada ambiente
2. Registrar cada ambiente en EnvironmentZone
3. Configurar distancias de activación
4. Configurar LODGroups si es necesario

## Optimización de Performance

### Culling de Ambientes
```csharp
// Los ambientes se activan/desactivan automáticamente
// basado en la distancia al jugador
environmentCullingDistance = 100f; // Metros
```

### LOD Automático
- **LOD 0**: < 30% de distancia máxima (máxima calidad)
- **LOD 1**: 30-60% de distancia (media calidad)
- **LOD 2**: 60-90% de distancia (baja calidad)
- **LOD 3**: > 90% de distancia (mínima calidad)

### Renderizado de Portales
- Solo renderiza portales dentro de `maxRenderDistance`
- Previene recursión infinita
- Se ejecuta en LateUpdate para máxima precisión

## Consideraciones para VR

### Compatibilidad con XR Toolkit
- El sistema es compatible con el XR Interaction Toolkit
- Funciona con teleportación VR existente
- Mantiene el tracking de la cabeza correctamente

### Performance VR
- Target: 90 FPS para VR
- Usar LOD agresivo para objetos distantes
- Limitar número de portales activos simultáneamente

## Casos de Uso en Xochicalco

### Experiencia Educativa
1. **Portal Temporal**: Del presente al pasado azteca
2. **Portal Escalar**: Del macro al micro (arquitectura)
3. **Portal Temático**: Entre diferentes áreas de conocimiento
4. **Portal Interactivo**: Activado por puzzles o interacciones

### Narrativa Inmersiva
- Transiciones contextuales entre épocas
- Revelación gradual de información
- Experiencias comparativas (antes/después)

## Troubleshooting

### Problemas Comunes
1. **Portal no renderiza**: Verificar RenderTexture y Material
2. **Teleportación múltiple**: Configurar correctamente los colliders
3. **Performance baja**: Ajustar distancias de culling y LOD
4. **Rotación incorrecta**: Verificar orientación de los portales

### Debug Tips
- Usar `DebugPortalInfo()` en PortalManager
- Visualizar gizmos de colliders en Scene View
- Monitorear performance con Profiler
- Verificar que la cámara del jugador esté asignada

## Extensiones Futuras

### Características Avanzadas
- **Portales de diferente escala**: Agrandar/reducir al cruzar
- **Portales temporales**: Efectos de tiempo
- **Portales condicionales**: Activación por eventos
- **Audio espacial**: Sonido que cruza portales
- **Partículas**: Efectos que atraviesan portales

### Integración con Gameplay
- Sistema de llaves/desbloqueo
- Portales como mecánica de puzzle
- Indicadores de destino
- Efectos de sonido ambiente
