using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Agregar el espacio de nombres de HurricaneVR
using HurricaneVR.Framework.Core.Player;

namespace HexabodyVR.PlayerController
{
    public class DemoUIHexaManager : MonoBehaviour
    {
        [Header("Player")]
        public HexaBodyPlayer4 HexaBodyPlayer;
        public HVRPlayerController HurricanePlayer; // Referencia separada para HurricaneVR

        [Header("Inputs")]
        public TextMeshProUGUI SitStandText;
        public TextMeshProUGUI PauseText;
        public TextMeshProUGUI ForceGrabText;
        public TextMeshProUGUI LeftForceText;
        public TextMeshProUGUI RightForceText;
        public TextMeshProUGUI TurnRateText;
        public TextMeshProUGUI SnapRateText;

        public Slider TurnRateSlider;
        public Slider SnapRateSlider;

        public Toggle SmoothTurnToggle;
        public Toggle LineGrabTriggerToggle;

        [Header("Hands")]
        public HexaHandsBase LeftHand;
        public HexaHandsBase RightHand;

        private bool Paused;

        private void Awake()
        {
            if (!HexaBodyPlayer)
            {
                HexaBodyPlayer = FindObjectOfType<HexaBodyPlayer4>();
            }

            if (!HurricanePlayer)
            {
                HurricanePlayer = FindObjectOfType<HVRPlayerController>();
            }

            if (!LeftHand || !RightHand)
            {
                var hands = FindObjectsOfType<HexaHandsBase>();
                foreach (var hand in hands)
                {
                    if (hand.IsLeft)
                        LeftHand = hand;
                    else
                        RightHand = hand;
                }
            }

            if (!HexaBodyPlayer && !HurricanePlayer)
            {
                Debug.LogError("DemoUIHexaManager: Missing required HexabodyVR or HurricaneVR components.");
            }

            // Asignar valores iniciales a los elementos de UI
            if (TurnRateSlider) TurnRateSlider.onValueChanged.AddListener(OnTurnRateChanged);
            if (SnapRateSlider) SnapRateSlider.onValueChanged.AddListener(OnSnapRateChanged);
            if (SmoothTurnToggle) SmoothTurnToggle.onValueChanged.AddListener(OnSmoothTurnToggleChanged);
            if (LineGrabTriggerToggle) LineGrabTriggerToggle.onValueChanged.AddListener(OnLineGrabTriggerToggleChanged);
        }

        private void Start()
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            // Actualizar elementos de UI comunes
            if (SitStandText)
                SitStandText.text = "N/A"; // Placeholder para HexabodyVR

            if (PauseText)
                PauseText.text = Paused ? "Unpause" : "Pause";

            if (ForceGrabText)
                ForceGrabText.text = "N/A"; // Placeholder para HexabodyVR

            if (LeftForceText)
                LeftForceText.text = $"Left Force: {LeftHand.AverageStuckForce:F2}";

            if (RightForceText)
                RightForceText.text = $"Right Force: {RightHand.AverageStuckForce:F2}";

            if (HurricanePlayer)
            {
                if (TurnRateText && TurnRateSlider)
                {
                    TurnRateText.text = HurricanePlayer.SmoothTurnSpeed.ToString("F2");
                    TurnRateSlider.value = HurricanePlayer.SmoothTurnSpeed;
                }

                if (SnapRateText && SnapRateSlider)
                {
                    SnapRateText.text = HurricanePlayer.SnapAmount.ToString("F2");
                    SnapRateSlider.value = HurricanePlayer.SnapAmount;
                }

                if (SmoothTurnToggle)
                    SmoothTurnToggle.isOn = HurricanePlayer.RotationType == RotationType.Smooth;

                if (LineGrabTriggerToggle)
                {
                    // Eliminar referencias a HVRSettings y manejar el toggle de forma independiente
                    Debug.LogWarning("LineGrabTriggerToggle functionality is not implemented due to missing HVRSettings.");
                }
            }
        }

        public void TogglePause()
        {
            if (LeftHand && RightHand)
            {
                if (Paused)
                {
                    PauseText.text = "Pause";
                    Time.timeScale = 1f;
                }
                else
                {
                    PauseText.text = "Unpause";
                    Time.timeScale = .00000001f;
                }

                Paused = !Paused;
                UpdateUI();
            }
        }

        public void OnTurnRateChanged(float value)
        {
            if (HurricanePlayer)
            {
                HurricanePlayer.SmoothTurnSpeed = value;
                TurnRateText.text = value.ToString("F2");
            }
        }

        public void OnSnapRateChanged(float value)
        {
            if (HurricanePlayer)
            {
                HurricanePlayer.SnapAmount = value;
                SnapRateText.text = value.ToString("F2");
            }
        }

        public void OnSmoothTurnToggleChanged(bool value)
        {
            if (HurricanePlayer)
            {
                HurricanePlayer.RotationType = value ? RotationType.Smooth : RotationType.Snap;
            }
        }

        public void OnLineGrabTriggerToggleChanged(bool value)
        {
            // Eliminar referencias a HVRSettings y manejar el toggle de forma independiente
            Debug.LogWarning("OnLineGrabTriggerToggleChanged functionality is not implemented due to missing HVRSettings.");
        }
    }
}