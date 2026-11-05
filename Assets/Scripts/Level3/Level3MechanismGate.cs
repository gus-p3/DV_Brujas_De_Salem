using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Level3
{
    /// <summary>
    /// Portón de rastra o compuerta mecánica en el Nivel 3.
    /// Se abre automáticamente al activarse los interruptores rúnicos (<see cref="Level3RuneSwitch"/>) requeridos.
    /// </summary>
    public class Level3MechanismGate : MonoBehaviour
    {
        [Header("Requisitos de Apertura")]
        [SerializeField] private string gateId = "mech_gate_1";
        [SerializeField] private string gateDisplayName = "Compuerta del Acueducto";
        [SerializeField] private List<Level3RuneSwitch> requiredSwitches = new List<Level3RuneSwitch>();
        [SerializeField] private bool openWhenAnySwitchActive = false;

        [Header("Animación de Apertura")]
        [SerializeField] private Transform slidingBarrier;
        [SerializeField] private BoxCollider2D solidBlocker;
        [SerializeField] private float slideDistance = 3.2f;
        [SerializeField] private float slideDuration = 0.8f;
        [SerializeField] private Vector3 slideDirection = Vector3.up;

        [Header("Sonido y HUD")]
        [SerializeField] private string openMessage = "¡Compuerta mecánica abierta!";

        private bool isOpen = false;
        private Vector3 initialBarrierPos;

        public bool IsOpen => isOpen;
        public string GateId => gateId;

        public event Action<Level3MechanismGate> OnGateOpened;

        private void Awake()
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) gameObject.layer = groundLayer;

            if (solidBlocker == null) solidBlocker = GetComponent<BoxCollider2D>();
            if (slidingBarrier == null) slidingBarrier = transform;
            initialBarrierPos = slidingBarrier.position;
        }

        public void Configure(string id, string displayName, Transform barrier, BoxCollider2D blocker, float distance = 3.2f)
        {
            gateId = id;
            gateDisplayName = displayName;
            slidingBarrier = barrier;
            solidBlocker = blocker;
            slideDistance = distance;
            initialBarrierPos = slidingBarrier != null ? slidingBarrier.position : transform.position;
        }

        public void AddRequiredSwitch(Level3RuneSwitch sw)
        {
            if (sw != null && !requiredSwitches.Contains(sw))
            {
                requiredSwitches.Add(sw);
            }
        }

        public void NotifySwitchChanged(Level3RuneSwitch sw)
        {
            CheckConditionsAndToggle();
        }

        private void CheckConditionsAndToggle()
        {
            if (isOpen) return;

            bool canOpen;
            if (openWhenAnySwitchActive)
            {
                canOpen = requiredSwitches.Exists(s => s != null && s.IsActivated);
            }
            else
            {
                canOpen = requiredSwitches.Count > 0 && requiredSwitches.TrueForAll(s => s != null && s.IsActivated);
            }

            if (canOpen)
            {
                OpenGate();
            }
        }

        public void OpenGate()
        {
            if (isOpen) return;
            isOpen = true;
            StopAllCoroutines();
            StartCoroutine(SlideOpenRoutine());

            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null && !string.IsNullOrEmpty(openMessage))
            {
                hud.ShowMessage(openMessage, 2.5f);
            }

            OnGateOpened?.Invoke(this);
        }

        private IEnumerator SlideOpenRoutine()
        {
            Vector3 startPos = slidingBarrier.position;
            Vector3 targetPos = initialBarrierPos + slideDirection.normalized * slideDistance;
            float elapsed = 0f;

            if (solidBlocker != null)
            {
                solidBlocker.enabled = false;
            }

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
                slidingBarrier.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            slidingBarrier.position = targetPos;
        }
    }
}
