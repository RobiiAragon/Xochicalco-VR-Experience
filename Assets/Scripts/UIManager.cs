using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
	// Asignar en el Inspector
	public Canvas welcomeCanvas;
	public Canvas farewellCanvas;

	public static UIManager Instance { get; private set; }

	void Awake()
	{
		if (Instance == null) Instance = this;
		else { Destroy(gameObject); return; }

		// Opcional: persistir entre escenas
		// DontDestroyOnLoad(gameObject);
	}

	void Start()
	{
		ShowWelcome();
	}

	public void ShowWelcome()
	{
		if (welcomeCanvas != null) welcomeCanvas.gameObject.SetActive(true);
		if (farewellCanvas != null) farewellCanvas.gameObject.SetActive(false);
	}

	public void ShowFarewell()
	{
		if (welcomeCanvas != null) welcomeCanvas.gameObject.SetActive(false);
		if (farewellCanvas != null) farewellCanvas.gameObject.SetActive(true);
	}

	// Conectar al botón "Reiniciar" desde el Inspector
	public void RestartTour()
	{
		// Recarga la escena actual para restablecer todo al estado inicial
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}
}
