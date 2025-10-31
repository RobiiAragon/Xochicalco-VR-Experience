using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class LowerFenceOnAllTargetsActivated : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Targets que deben activarse. Pueden ser cualquier GameObject con Collider (Is Trigger)")]
    public GameObject[] targets;

    public enum ActivationMode { All, Any }

    [Header("Activación")]
    [Tooltip("Modo: 'All' requiere que todos los targets se activen; 'Any' baja la cerca cuando cualquiera se active.")]
    public ActivationMode activationMode = ActivationMode.All;

    [Tooltip("Si está activado, solo se contará la colisión si el collider tiene la etiqueta indicada. Si está desactivado, cualquier collider activará el target.")]
    public bool useTag = false;

    [Tooltip("Etiqueta del objeto que debe tocar el target para activarlo (si Use Tag está activo).")]
    public string activatingColliderTag = "Arrow";

    [Header("Movimiento de la cerca")]
    [Tooltip("Cantidad a bajar en el eje Y (positivo = bajar)")]
    public float lowerAmount = 2f;

    [Tooltip("Duración en segundos del movimiento. Si es 0, la bajada es instantánea.")]
    public float lowerDuration = 1f;

    [Header("Eventos")]
    [Tooltip("Evento que se dispara cuando todos los targets están activados. Útil para conectar otras acciones en el inspector.")]
    public UnityEvent onAllTargetsActivated;

    // estado interno
    private bool[] activated;
    private Vector3 originalPosition;
    private bool isLowered = false;

    void Awake()
    {
        originalPosition = transform.position;

        if (targets == null || targets.Length == 0)
        {
            Debug.LogWarning("LowerFenceOnAllTargetsActivated: no se han asignado targets en el inspector.", this);
            activated = new bool[0];
            return;
        }

        activated = new bool[targets.Length];

        // Añadir (o configurar) el helper en cada target para escuchar OnTriggerEnter
        for (int i = 0; i < targets.Length; i++)
        {
            var t = targets[i];
            if (t == null)
            {
                Debug.LogWarning($"LowerFence: target en la posición {i} es null.");
                continue;
            }

            // comprobar si tiene collider
            var col = t.GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogWarning($"LowerFence: target '{t.name}' no tiene Collider. Debe tener un Collider (Is Trigger si quieres detección de trigger).", t);
            }
            else if (!col.isTrigger)
            {
                Debug.LogWarning($"LowerFence: el Collider de '{t.name}' no está marcado como Is Trigger. Aunque puede funcionar según tu configuración física, lo recomendado es marcarlo como Trigger.", t);
            }

            // Añadimos un componente TargetActivator si no existe
            var activator = t.GetComponent<TargetActivator>();
            if (activator == null)
            {
                activator = t.AddComponent<TargetActivator>();
            }
            activator.Initialize(this, i, activatingColliderTag, useTag);
        }
    }

    /// Método llamado por los TargetActivator cuando un target se activa.
    internal void NotifyTargetActivated(int index)
    {
        if (index < 0 || index >= activated.Length) return;
        if (activated[index]) return; // ya activado

        activated[index] = true;
        Debug.Log($"LowerFence: target activado [{index}] - {targets[index]?.name}");

        if (activationMode == ActivationMode.Any)
        {
            // bajar al primer target activado
            StartCoroutine(LowerFenceCoroutine());
            onAllTargetsActivated?.Invoke();
            return;
        }

        // comprobar si todos están activados (modo All)
        for (int i = 0; i < activated.Length; i++)
        {
            if (!activated[i]) return;
        }

        // si llegamos aquí, todos activados
        StartCoroutine(LowerFenceCoroutine());
        onAllTargetsActivated?.Invoke();
    }

    private IEnumerator LowerFenceCoroutine()
    {
        if (isLowered) yield break;
        isLowered = true;

        Vector3 targetPos = originalPosition + Vector3.down * Mathf.Abs(lowerAmount);

        if (lowerDuration <= 0f)
        {
            transform.position = targetPos;
            yield break;
        }

        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < lowerDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lowerDuration);
            // suavizado opcional (ease out)
            float ease = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(startPos, targetPos, ease);
            yield return null;
        }

        transform.position = targetPos;
    }

    /// Vuelve a poner todos los targets como no activados (útil para reiniciar el puzzle).
    public void ResetTargets()
    {
        for (int i = 0; i < activated.Length; i++) activated[i] = false;
        isLowered = false;
        transform.position = originalPosition;
        // opcional: notificar a los activators para que actualicen su visual
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;
            var activator = targets[i].GetComponent<TargetActivator>();
            if (activator != null) activator.ResetVisual();
        }
    }

    // Component auxiliar que se añade dinámicamente a cada target.
    private class TargetActivator : MonoBehaviour
    {
    private LowerFenceOnAllTargetsActivated owner;
    private int index;
    private string tagFilter;
    private bool requireTag = false;
    private bool activated = false;

        internal void Initialize(LowerFenceOnAllTargetsActivated owner, int index, string tagFilter, bool requireTag)
        {
            this.owner = owner;
            this.index = index;
            this.tagFilter = tagFilter;
            this.requireTag = requireTag;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (activated) return;

            if (requireTag)
            {
                if (!string.IsNullOrEmpty(tagFilter))
                {
                    if (!other.CompareTag(tagFilter)) return;
                }
                else
                {
                    // si requireTag está activo pero no hay tag configurada, no contamos
                    return;
                }
            }

            activated = true;
            // opción: cambiar color si tiene Renderer
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                try
                {
                    var mat = rend.material;
                    if (mat != null) mat.color = Color.green;
                }
                catch { }
            }

            owner?.NotifyTargetActivated(index);
        }

        internal void ResetVisual()
        {
            activated = false;
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                try
                {
                    var mat = rend.material;
                    if (mat != null) mat.color = Color.white;
                }
                catch { }
            }
        }
    }
}
