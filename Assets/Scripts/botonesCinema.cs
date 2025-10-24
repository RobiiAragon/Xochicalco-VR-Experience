using UnityEngine;
using System.Collections.Generic;

public class botonesCinema : MonoBehaviour
{
	[Header("Opciones")]
	[Tooltip("Si true, los hijos se ocultarán al iniciar.")]
	public bool hideOnStart = true;

	[Space]
	[Tooltip("Si true, solo reaccionará a colliders con la tag especificada.")]
	public bool useTagFilter = true;
	[Tooltip("Tag que activará el trigger (por defecto Hurricane).")]
	public string targetTag = "Hurricane";

	[Space]
	[Tooltip("Retraso (s) antes de ocultar los hijos cuando no quedan colliders válidos dentro del trigger.")]
	public float exitDelay = 0.12f;
	[Tooltip("Mostrar mensajes de depuración en consola.")]
	public bool debug = false;

	// Seguimiento de colliders que actualmente deberían mantener visibles los hijos
	private HashSet<Collider> validColliders = new HashSet<Collider>();
	private float hideTimer = 0f;
	private bool childrenVisibleState = false;

	// Collider del trigger (para comprobaciones de área)
	private Collider triggerCollider;

	void Start()
	{
		triggerCollider = GetComponent<Collider>();
		if (triggerCollider == null)
		{
			Debug.LogWarning($"[{name}] No se encontró Collider en el objeto. El script necesita un Collider marcado como Trigger.");
		}
		else
		{
			if (!triggerCollider.isTrigger)
			{
				Debug.LogWarning($"[{name}] El Collider no está marcado como 'Is Trigger'. Lo voy a activar automáticamente.");
				triggerCollider.isTrigger = true;
			}
		}

		if (hideOnStart)
		{
			SetChildrenActive(false);
			childrenVisibleState = false;
		}
		else
		{
			childrenVisibleState = true;
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (ShouldReact(other))
		{
			validColliders.Add(other);
			EnsureVisible();
			if (debug) Debug.Log($"Enter added: {other.name} (count={validColliders.Count})");
		}
	}

	void OnTriggerStay(Collider other)
	{
		// Reafirmar la validez en caso de que OnTriggerExit se haya llamado por un movimiento momentáneo
		if (ShouldReact(other))
		{
			if (!validColliders.Contains(other))
			{
				validColliders.Add(other);
				EnsureVisible();
				if (debug) Debug.Log($"Stay re-added: {other.name} (count={validColliders.Count})");
			}
			// resetear temporizador si aún hay colliders
			hideTimer = 0f;
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (validColliders.Remove(other))
		{
			if (debug) Debug.Log($"Exit removed: {other.name} (count={validColliders.Count})");
			// si no quedan colliders en la lista, comprobar si aún hay colliders con la tag dentro del trigger
			if (validColliders.Count == 0)
			{
				if (AnyTaggedCollidersInside())
				{
					// aún hay alguno tocando: mantener visible
					if (debug) Debug.Log("Aún hay colliders con la tag dentro del trigger: mantener visible.");
					EnsureVisible();
				}
				else
				{
					// iniciar contador para ocultar con retraso
					hideTimer = exitDelay;
				}
			}
		}
	}

	void Update()
	{
		// Manejar temporizador para ocultar con pequeño delay y evitar flicker
		if (hideTimer > 0f)
		{
			hideTimer -= Time.deltaTime;
			if (hideTimer <= 0f && validColliders.Count == 0)
			{
				EnsureHidden();
			}
		}
	}

	// Forzar visibilidad inmediata
	void EnsureVisible()
	{
		if (!childrenVisibleState)
		{
			SetChildrenActive(true);
			childrenVisibleState = true;
		}
		// reset any hide timer
		hideTimer = 0f;
	}

	// Forzar ocultar inmediata
	void EnsureHidden()
	{
		if (childrenVisibleState)
		{
			SetChildrenActive(false);
			childrenVisibleState = false;
		}
	}

	// Comprueba si hay colliders con la tag target dentro del bounds del trigger
	bool AnyTaggedCollidersInside()
	{
		if (triggerCollider == null) return false;
		if (!useTagFilter) return false;

		Bounds b = triggerCollider.bounds;
		Collider[] hits = Physics.OverlapBox(b.center, b.extents, transform.rotation);
		foreach (var c in hits)
		{
			if (c == triggerCollider) continue;
			// usar la misma comprobación jerárquica para la tag
			if (HasTagInHierarchy(c.transform, targetTag))
			{
				// Ignorar colliders que sean hijos del propio trigger (si corresponde)
				if (c.transform.IsChildOf(transform)) continue;
				return true;
			}
		}
		return false;
	}

	// Comprueba hacia arriba por la jerarquía si algún transform tiene la tag indicada
	bool HasTagInHierarchy(Transform t, string tagToCheck)
	{
		Transform current = t;
		while (current != null)
		{
			if (current.CompareTag(tagToCheck)) return true;
			if (current.parent == null) break;
			current = current.parent;
		}
		return false;
	}

	bool ShouldReact(Collider other)
	{
		// Si se usa filtro por tag, comprobamos no solo el objeto del collider,
		// sino también sus padres/raíz (útil en rigs VR donde la tag está en el root).
		if (useTagFilter)
		{
			if (HasTagInHierarchy(other.transform, targetTag)) return true;

			// También comprobar la raíz por si la jerarquía es distinta
			if (other.transform.root != null && other.transform.root != other.transform)
			{
				if (HasTagInHierarchy(other.transform.root, targetTag)) return true;
			}

			return false;
		}

		// Si no se usa filtro por tag, reaccionar a cualquier collider.
		return true;
	}

	void SetChildrenActive(bool active)
	{
		foreach (Transform child in transform)
		{
			// Activa/desactiva los hijos directos.
			child.gameObject.SetActive(active);
		}
	}
}