using UnityEngine;

public class MuseumTrigger : MonoBehaviour
{
	// Si true solo responde a objetos con el tag especificado (configurable en Inspector)
	[SerializeField, Tooltip("Si true solo responde a objetos con el tag especificado")]
	private bool onlyTag = true;

	[SerializeField, Tooltip("Tag requerido (ej. 'Hurricane')")]
	private string requiredTag = "Hurricane";

	// Si true, solo dispara una vez
	public bool oneTime = true;

	bool triggered = false;

	void OnTriggerEnter(Collider other)
	{
		if (oneTime && triggered) return;
		if (onlyTag && !other.CompareTag(requiredTag)) return;

		if (UIManager.Instance != null)
		{
			UIManager.Instance.ShowFarewell();
			triggered = true;
		}
		else
		{
			Debug.LogWarning("UIManager instance not found in scene.");
		}
	}
}
