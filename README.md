# Xochicalco VR Project

Un proyecto de realidad virtual desarrollado en Unity, inspirado en la universidad Xochicalco.

## 🎯 Características

- **Experiencia VR inmersiva** usando XR Interaction Toolkit
- **Shaders personalizados** con efectos de portal optimizados
- **Interacciones con las manos** usando XR Hands
- **Arquitectura modular** para fácil mantenimiento

## 🛠️ Requisitos del Sistema

### Software
- Unity 2021.3 LTS o superior
- XR Interaction Toolkit 3.2.1+
- XR Hands 1.6.1+
- Universal Render Pipeline (URP)

### Hardware
- Dispositivos VR compatibles (Meta Quest, HTC Vive, etc.)
- PC con especificaciones mínimas para VR

## 🚀 Instalación

1. **Clonar el repositorio**
   ```bash
   git clone [repository-url]
   cd Xochicalco
   ```

2. **Abrir en Unity**
   - Abrir Unity Hub
   - Seleccionar "Add project from disk"
   - Navegar a la carpeta del proyecto

3. **Configurar XR**
   - El proyecto debería auto-configurar las dependencias
   - Verificar en Project Validation (Window > XR > Project Validation)

## 📁 Estructura del Proyecto

```
Assets/
├── Scenes/                 # Escenas principales
├── Scripts/               # Scripts de C#
├── Shaders/              # Shaders personalizados
│   └── Portal.shader     # Shader optimizado para portales
├── Materials/            # Materiales del proyecto
├── Prefabs/              # Prefabs reutilizables
├── Textures/             # Texturas y assets visuales
└── VRTemplateAssets/     # Assets del template VR
```

## 🎨 Shaders Personalizados

### Portal Shader
- **Ubicación**: `Assets/Shaders/Portal.shader`
- **Características**:
  - Efectos de distorsión con ruido fractal
  - Efectos Fresnel para realismo
  - Animaciones de pulso configurables
  - Optimizado para VR (instancing, stereo rendering)

## ⚙️ Optimizaciones Implementadas

### Rendimiento
- **GPU Instancing** habilitado en shaders
- **Cache de validación** para Package Manager
- **Timeout management** en requests de red
- **Stereo rendering** optimizado

### Calidad Visual
- **Ruido fractal** multi-octava
- **Interpolación mejorada** en funciones de ruido
- **Efectos Fresnel** para mayor realismo
- **Blending optimizado** para transparencias

## 🔧 Configuración de Desarrollo

### Editor Layout
El proyecto incluye un layout personalizado (`TutorialLayout.wlt`) optimizado para desarrollo VR:
- Panel de Tutorial visible
- Jerarquía y Project optimizados
- Inspector configurado para componentes VR

### Project Validation
Sistema automático que verifica:
- Instalación de paquetes requeridos
- Versiones correctas de dependencias
- Importación de samples necesarios

## 🎮 Uso
- Abra la escena principal en Assets/Scenes.
- Asegúrese de tener los paquetes recomendados instalados (Input System, XR Management, etc.).
- Consulte OPTIMIZATION_GUIDE.md para consejos de render/performace.

## 🐛 Troubleshooting

### Problemas Comunes

**Error: XR Hands no encontrado**
- Solución: Ejecutar Project Validation (Window > XR > Project Validation)

**Shader no compila**
- Verificar que URP esté configurado
- Revisar compatibilidad con versión de Unity

**Performance issues en VR**
- Reducir calidad de shaders
- Verificar configuración de render pipeline

## 🐛 Problema: Artefacto solo en Ojo Izquierdo al Cruzar Portales

Cuando el portal se ve bien en el ojo derecho pero en el izquierdo aparece recorte / desplazamiento:

Causas típicas:
1. Se usa cámara auxiliar y solo se actualiza con Camera.StereoscopicEye.Right.
2. Se fuerza camera.projectionMatrix en lugar de usar GetStereoProjectionMatrix / GetStereoViewMatrix por ojo.
3. RenderTexture compartido sin habilitar VR instancing (dimension = 2D en vez de Tex2DArray).
4. Falta de ajustar el clip plane / oblique matrix por ojo.
5. Pass personalizado ejecutado después de que URP ya resolvió el target (en modo compatibility) y lee solo el color del último ojo.

Checklist rápido:
- Asegura XRGraphics.stereoRenderingMode == SinglePassInstanced (o MultiPass soportado correctamente).
- En el código del portal, para cada ojo:
  var view = cam.GetStereoViewMatrix(eye);
  var proj = cam.GetStereoProjectionMatrix(eye);
  var vp = proj * view;
- Si usas RenderTexture manual: rt.vrUsage = VRTextureUsage.TwoEyes; (o usa XRSystem target de URP).
- Evita Graphics.SetRenderTarget manual entre ojos; usa CommandBuffer y deja que URP gestione.
- Si reconstruyes matrices oblicuas:
  Matrix4x4.AdjustProjectionMatrix(proj, clipPlane) por ojo, no reutilices la del derecho.
- Verifica que no caches transformaciones del frame previo del ojo derecho para el izquierdo.

Cómo depurar rápido:
1. Activa Frame Debugger y filtra hasta tu pass de portal: ¿se ejecuta 2 veces (Left/Right)? Debe hacerlo.
2. Log temporal:
   Debug.Log($"Portal Eye {eye} VP hash {vp.GetHashCode()}");
   Si ambos hashes son iguales algo está mal (deben diferir ligeramente en X).
3. Activa modo Wireframe para ver si el quad del portal se desplaza solo en un ojo (parallax incorrecto).

Si el problema persiste antes de migrar a RenderGraph:
- Forzar m_EnableRenderGraph = 1 puede hacer más evidente el error (cada eye pass separado).
- Revisa scripts faltantes (regla de validación ahora lista cada objeto con componentes Missing antes de limpiar).

## 📈 Roadmap

- [ ] Implementar audio espacial
- [ ] Agregar más interacciones con objetos
- [ ] Optimizar para dispositivos móviles VR

## 🤝 Contribución

1. Fork del proyecto
2. Crear branch para feature (`git checkout -b feature/nueva-funcionalidad`)
3. Commit de cambios (`git commit -am 'Agregar nueva funcionalidad'`)
4. Push al branch (`git push origin feature/nueva-funcionalidad`)
5. Crear Pull Request

## 📄 Licencia

Este proyecto está bajo la Licencia MIT. Ver `LICENSE` para más detalles.

## 📞 Contacto

- **Desarrollador**: Jesus Roberto Aragon Lopez
- **Año**: 2025
- **Inspiración**: Universidad Tecnologica de Tijuana / Xochicalco Universidad, México

---

*Desarrollado con ❤️ para preservar y compartir el patrimonio cultural mexicano en realidad virtual.*
