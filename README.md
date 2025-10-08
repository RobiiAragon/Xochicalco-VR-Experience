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
- Jerarquía y Project optimizados
- Inspector configurado para componentes VR

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

*Desarrollado con ❤️ para preservar y compartir el patrimonio cultural mexicano.*
