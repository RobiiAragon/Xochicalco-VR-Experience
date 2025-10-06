using UnityEngine;
using System;
using System.Reflection;
using HurricaneVR.Framework.Components;
using HurricaneVR.Framework.Core.Stabbing; // <-- usado solo si el framework está presente

// No es necesario el 'RequireComponent' ya que puede ser MeshFilter o SkinnedMeshRenderer
public class StretchableMesh : MonoBehaviour
{
    // --- Configuración en el Inspector ---
    [Tooltip("Qué tan fuerte es el efecto de estiramiento.")]
    public float stretchIntensity = 0.5f;

    [Tooltip("El radio de influencia alrededor del punto de agarre.")]
    public float radiusOfInfluence = 0.3f;

    [Tooltip("La velocidad a la que la malla vuelve a su forma original.")]
    public float returnSpeed = 5f;

    // --- Referencias y Variables Internas ---
    private MeshFilter meshFilter;
    private SkinnedMeshRenderer skinned;
    private Mesh deformedMesh;

    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;

    // Estado del agarre
    private bool isBeingStretched = false;
    private Transform grabberTransform; // La 'mano' que está estirando

    // Reflection-based trigger getter (returns 0..1)
    private Func<float> _triggerGetter;
    private const float TriggerThreshold = 0.5f;

    void Start()
    {
        // Obtenemos el MeshFilter si existe
        meshFilter = GetComponent<MeshFilter>();
        skinned = GetComponent<SkinnedMeshRenderer>();

        // Si no hay MeshFilter pero sí SkinnedMeshRenderer, bakeamos la malla
        if (meshFilter == null && skinned != null)
        {
            deformedMesh = new Mesh();
            skinned.BakeMesh(deformedMesh);
        }
        else if (meshFilter != null)
        {
            // Acceder a .mesh hace que Unity cree una instancia única para este objeto
            deformedMesh = meshFilter.mesh;
        }
        else
        {
            Debug.LogWarning($"[{nameof(StretchableMesh)}] No MeshFilter ni SkinnedMeshRenderer en {name}.");
            deformedMesh = null;
        }

        if (deformedMesh != null)
        {
            originalVertices = deformedMesh.vertices;
            deformedVertices = new Vector3[originalVertices.Length];
            originalVertices.CopyTo(deformedVertices, 0);
        }

        // --- NUEVO: suscribir automáticamente si hay un HVRGrabbable en este objeto o en padres ---
        var grabbable = GetComponentInParent<HVRGrabbable>();
        if (grabbable)
        {
            grabbable.Grabbed.AddListener(OnGrabbedFromHVR);
            grabbable.Released.AddListener(OnReleasedFromHVR);
        }
    }

    void Update()
    {
        // Si estamos agarrados por HVR y tenemos getter de trigger, activar estiramiento según valor
        if (grabberTransform != null && _triggerGetter != null)
        {
            var triggerValue = _triggerGetter.Invoke();
            if (triggerValue > TriggerThreshold)
            {
                // si aún no estamos en modo estirar, activarlo
                if (!isBeingStretched)
                    isBeingStretched = true;
            }
            else
            {
                // si el gatillo se suelta, paramos el estiramiento
                if (isBeingStretched)
                    isBeingStretched = false;
            }
        }

        if (isBeingStretched && grabberTransform != null)
        {
            ApplyDeformation();
        }
        else
        {
            ReturnToOriginalShape();
        }
    }
    
    // El resto del código es idéntico al anterior...

    public void StartStretch(Transform grabber)
    {
        Debug.Log("--- EMPIEZA EL ESTIRAMIENTO ---");
        isBeingStretched = true;
        grabberTransform = grabber;
    }

    // Nuevo: StartStretch sin parámetros (útil para enlazar eventos que no dan el grabber)
    public void StartStretch()
    {
        Debug.Log("--- EMPIEZA EL ESTIRAMIENTO (sin transform) ---");
        isBeingStretched = true;
        // grabberTransform se mantiene si ya está seteado por SetGrabber o StartStretch(Component)
    }

    // Nuevo: StartStretch que acepta cualquier Component (p. ej. HVRHandGrabber desde el evento del framework)
    public void StartStretch(Component grabberComponent)
    {
        if (grabberComponent != null)
        {
            grabberTransform = grabberComponent.transform;
        }
        StartStretch(grabberTransform);
    }

    // Nuevo: Setear el grabber sin activar el estiramiento (útil para enlazar solo el grabber desde eventos)
    public void SetGrabber(Component grabberComponent)
    {
        if (grabberComponent != null)
        {
            grabberTransform = grabberComponent.transform;
        }
    }

    public void StopStretch()
    {
        Debug.Log("--- TERMINA EL ESTIRAMIENTO ---");
        isBeingStretched = false;
        grabberTransform = null;
    }

    // Nuevo: StopStretch que acepta componente (para enlazar directamente al evento que pasa el grabber)
    public void StopStretch(Component grabberComponent)
    {
        // ignoramos el parámetro, solo detenemos el estiramiento
        StopStretch();
    }

    private void ApplyDeformation()
    {
        if (deformedMesh == null || deformedVertices == null || originalVertices == null || grabberTransform == null) return;

        Vector3 grabberWorldPosition = grabberTransform.position;

        for (int i = 0; i < deformedVertices.Length; i++)
        {
            // posición original del vértice en world space
            Vector3 originalWorld = transform.TransformPoint(originalVertices[i]);

            float distance = Vector3.Distance(originalWorld, grabberWorldPosition);

            if (distance < radiusOfInfluence)
            {
                float influence = 1 - (distance / radiusOfInfluence);

                // desplazamiento en world space hacia la mano
                Vector3 displacementWorld = (grabberWorldPosition - originalWorld) * stretchIntensity * influence;
                Vector3 newVertexWorld = originalWorld + displacementWorld;

                // convertimos de vuelta a local space para asignar al array de vertices
                deformedVertices[i] = transform.InverseTransformPoint(newVertexWorld);
            }
            else
            {
                // fuera de influencia mantenemos original (asegura que no queden ceros)
                deformedVertices[i] = originalVertices[i];
            }
        }

        deformedMesh.vertices = deformedVertices;
        deformedMesh.RecalculateNormals();
        deformedMesh.RecalculateBounds();

        if (skinned != null && meshFilter == null)
        {
            skinned.sharedMesh = deformedMesh;
        }
    }

    private void ReturnToOriginalShape()
    {
        bool changed = false;
        for (int i = 0; i < deformedVertices.Length; i++)
        {
            if (Vector3.SqrMagnitude(deformedVertices[i] - originalVertices[i]) > 0.0001f)
            {
                changed = true;
                deformedVertices[i] = Vector3.Lerp(deformedVertices[i], originalVertices[i], Time.deltaTime * returnSpeed);
            }
        }
        
        if (changed)
        {
            deformedMesh.vertices = deformedVertices;
            deformedMesh.RecalculateNormals();
        }
    }

    // Nuevo: aplicar deformación puntual en coordenadas del mundo (útil para eventos del framework HVR)
    public void ApplyStretchAtWorldPoint(Vector3 worldPoint, Vector3 direction, float intensityScale = 1f)
    {
        if (deformedMesh == null || deformedVertices == null || originalVertices == null) return;

        Vector3 grabberWorldPosition = worldPoint;

        for (int i = 0; i < deformedVertices.Length; i++)
        {
            Vector3 originalWorld = transform.TransformPoint(originalVertices[i]);

            float distance = Vector3.Distance(originalWorld, grabberWorldPosition);

            if (distance < radiusOfInfluence)
            {
                float influence = 1 - (distance / radiusOfInfluence);
                Vector3 displacementWorld = (grabberWorldPosition - originalWorld) * (stretchIntensity * intensityScale) * influence;
                Vector3 newVertexWorld = originalWorld + displacementWorld;
                deformedVertices[i] = transform.InverseTransformPoint(newVertexWorld);
            }
            else
            {
                deformedVertices[i] = originalVertices[i];
            }
        }

        deformedMesh.vertices = deformedVertices;
        deformedMesh.RecalculateNormals();
        deformedMesh.RecalculateBounds();

        if (skinned != null && meshFilter == null)
        {
            skinned.sharedMesh = deformedMesh;
        }
    }

    // Nuevo: método de conveniencia para conectar directamente con HurricaneVR Stab events
    public void OnStabbed(StabArgs stabArgs)
    {
        // StabArgs es un tipo por valor; no se puede comparar con null.
        // Llamamos directamente usando sus datos.
        ApplyStretchAtWorldPoint(stabArgs.Point, stabArgs.Normal, 1f);
    }

    // Nuevo: manejadores conectados a HVRGrabbable
    private void OnGrabbedFromHVR(HVRGrabberBase grabber, HVRGrabbable g)
    {
        if (grabber == null) return;

        grabberTransform = grabber.transform;
        // Intentamos crear un getter del gatillo usando reflexión sobre el grabber o sus componentes
        _triggerGetter = CreateTriggerGetter(grabber) ?? CreateTriggerGetter(grabber.gameObject);
        // si no conseguimos getter, como fallback dejamos isBeingStretched=true (opcional)
        // isBeingStretched = _triggerGetter == null ? true : isBeingStretched;
    }

    private void OnReleasedFromHVR(HVRGrabberBase grabber, HVRGrabbable g)
    {
        // limpiar estado
        isBeingStretched = false;
        grabberTransform = null;
        _triggerGetter = null;
    }

    // Crea un getter de float desde un objeto (componentes) buscando propiedades/fields relacionados con "Trigger"
    private Func<float> CreateTriggerGetter(UnityEngine.Object source)
    {
        if (source == null) return null;

        // 1) Si source es un componente/objeto con propiedad float Trigger o TriggerValue
        var type = source.GetType();
        var getter = TryCreateGetterFromTypeInstance(source, type);
        if (getter != null) return getter;

        // 2) Si source es GameObject, revisar todos sus componentes
        if (source is GameObject go)
        {
            var comps = go.GetComponents<Component>();
            foreach (var comp in comps)
            {
                if (comp == null) continue;
                var g = TryCreateGetterFromTypeInstance(comp, comp.GetType());
                if (g != null) return g;
            }
        }

        // 3) Si source es Component, revisar children components también (por si la entrada está en un subcomponente)
        if (source is Component c)
        {
            foreach (var comp in c.GetComponentsInChildren<Component>())
            {
                if (comp == null) continue;
                var g = TryCreateGetterFromTypeInstance(comp, comp.GetType());
                if (g != null) return g;
            }
        }

        return null;
    }

    // Intenta construir un Func<float> a partir de un objeto (propiedades/fields con nombres comunes)
    private Func<float> TryCreateGetterFromTypeInstance(object instance, Type type)
    {
        if (instance == null || type == null) return null;

        // nombres a buscar (se pueden ampliar)
        string[] names = { "Trigger", "TriggerValue", "TriggerState", "TriggerPressed", "TriggerPressure" };

        // buscar propiedades primero
        foreach (var name in names)
        {
            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                // float property
                if (prop.PropertyType == typeof(float))
                {
                    return () => (float)prop.GetValue(instance);
                }

                // HVRButtonState-like struct: buscar campo/property 'Value' si el tipo name contiene "HVRButtonState"
                if (prop.PropertyType.Name.Contains("HVRButtonState"))
                {
                    return () =>
                    {
                        var state = prop.GetValue(instance);
                        if (state == null) return 0f;
                        var valueProp = state.GetType().GetField("Value") ?? (MemberInfo)state.GetType().GetProperty("Value");
                        if (valueProp is FieldInfo fi)
                            return (float)fi.GetValue(state);
                        if (valueProp is PropertyInfo pi)
                            return (float)pi.GetValue(state);
                        return 0f;
                    };
                }
            }

            // buscar campos
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                if (field.FieldType == typeof(float))
                {
                    return () => (float)field.GetValue(instance);
                }

                if (field.FieldType.Name.Contains("HVRButtonState"))
                {
                    return () =>
                    {
                        var state = field.GetValue(instance);
                        if (state == null) return 0f;
                        var valueField = state.GetType().GetField("Value") ?? (MemberInfo)state.GetType().GetProperty("Value");
                        if (valueField is FieldInfo fi)
                            return (float)fi.GetValue(state);
                        if (valueField is PropertyInfo pi)
                            return (float)pi.GetValue(state);
                        return 0f;
                    };
                }
            }
        }

        // si no encontramos, devolver null
        return null;
    }
}