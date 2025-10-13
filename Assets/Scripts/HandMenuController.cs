using UnityEngine;
using HurricaneVR.Framework.Shared; // Para HVRGlobalInputs
using HurricaneVR.Framework.ControllerInput; // Para HVRGlobalInputs

public class HandMenuController : MonoBehaviour
{
    public GameObject Menu; // Asignar el menú en el inspector
    public Transform LeftControllerTransform; // Asignar el transform del controlador izquierdo en el inspector
    public Transform PlayerHead; // Asignar el transform de la cámara del jugador (cabeza) en el inspector

    private Vector3 _defaultPosition; // Posición inicial del menú
    private Quaternion _defaultRotation; // Rotación inicial del menú
    private bool _menuInHand; // Indica si el menú está en la mano del jugador

    void Start()
    {
        // Guardar la posición y rotación inicial del menú
        _defaultPosition = Menu.transform.position;
        _defaultRotation = Menu.transform.rotation;
    }

    void Update()
    {
        // Detectar si el botón "Menu" fue presionado
        if (HVRGlobalInputs.Instance.LeftMenuButtonState.JustActivated)
        {
            if (_menuInHand)
            {
                // Regresar el menú a su posición y rotación iniciales
                ResetMenuToDefault();
            }
            else
            {
                // Mover el menú a la mano del jugador
                MoveMenuToHand();
            }

            // Alternar el estado del menú
            _menuInHand = !_menuInHand;
        }

        if (_menuInHand && LeftControllerTransform != null && PlayerHead != null)
        {
            // Actualizar la posición del menú para que siga la mano
            Menu.transform.position = LeftControllerTransform.position;

            // Hacer que el menú mire hacia la cabeza del jugador con un ajuste de 90 grados en el eje Y
            Vector3 directionToHead = PlayerHead.position - Menu.transform.position;
            Quaternion lookRotation = Quaternion.LookRotation(directionToHead, Vector3.up);
            Menu.transform.rotation = lookRotation * Quaternion.Euler(0, -90, 30);
        }
    }

    private void ResetMenuToDefault()
    {
        // Restablecer la posición y rotación del menú a su estado inicial
        Menu.transform.position = _defaultPosition;
        Menu.transform.rotation = _defaultRotation;
    }

    private void MoveMenuToHand()
    {
        // Mover el menú a la posición de la mano del jugador
        Menu.transform.position = LeftControllerTransform.position;

        // Asegurarse de que el menú esté activo
        Menu.SetActive(true);
    }
}