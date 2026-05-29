using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction.Core
{
    /// <summary>
    /// Canal de estado del verbo activo.
    /// Actúa como intermediario desacoplado entre VerbHUD,
    /// CursorController e InteractionSystem.
    /// Ninguno de los tres se conoce entre sí.
    /// </summary>
    [CreateAssetMenu(fileName = "VerbSelector", menuName = "Esquel/Interaction/VerbSelector")]
    public sealed class VerbSelectorSO : ScriptableObject
    {
        // ── Estado ──────────────────────────────────────────────
        [field: SerializeField]
        public VerbType ActiveVerb { get; private set; } = VerbType.Look; // ⚠️ SOLID: no exponer setter público — cambios solo vía SetVerb()

        // ── Eventos ─────────────────────────────────────────────
        public event Action<VerbType> OnVerbChanged;   // subscribers: CursorController, VerbHUD, InteractionSystem

        // ── API pública ─────────────────────────────────────────
        public void SetVerb(VerbType verb)
        {
            if (ActiveVerb == verb) return;   // 🟢 no dispara evento si no cambió nada
            ActiveVerb = verb;
            OnVerbChanged?.Invoke(ActiveVerb);
        }

        /// <summary>
        /// Llamar desde OnEnable del primer sistema que lo use
        /// para asegurar estado limpio al entrar a escena.
        /// </summary>
        public void ResetToDefault() => SetVerb(VerbType.Look);

        // ── Editor safety ────────────────────────────────────────
        private void OnEnable()
        {
            // Los SO persisten en memoria entre Play Mode y Edit Mode en el Editor.
            // Reseteamos para que no arranque con estado sucio de la sesión anterior.
#if UNITY_EDITOR
            ActiveVerb = VerbType.Look;
#endif
        }
    }
}
