using System.Collections.Generic;
using UnityEngine;

namespace Dialogue.Data
{
    // Fuente de verdad del estado del diálogo para el resto del juego.
    // Ningún sistema externo conoce a Ink — solo conoce este SO.
    // 📌 GDD — Aquí se reflejan las consecuencias de las elecciones
    // del jugador para que el mundo del juego pueda reaccionar.

    [CreateAssetMenu(
        fileName = "DialogueStorage",
        menuName = "Dialogue/Dialogue Storage")]
    public sealed class DialogueStorageSO : ScriptableObject
    {
        // ─── Estado sincronizado desde Ink ───────────────────────────

        // 🟡 PERF — Diccionarios O(1) promedio. Suficiente para la
        // cantidad de variables de un Point & Click.
        private readonly Dictionary<string, bool>   _bools   = new();
        private readonly Dictionary<string, float>  _floats  = new();
        private readonly Dictionary<string, string> _strings = new();

        // ─── API de escritura (solo DialogueVariableStorage escribe) ─

        public void SetBool(string key, bool value)     => _bools[key]   = value;
        public void SetFloat(string key, float value)   => _floats[key]  = value;
        public void SetString(string key, string value) => _strings[key] = value;

        // ─── API de lectura (cualquier sistema del juego puede leer) ─

        public bool TryGetBool(string key, out bool value)
            => _bools.TryGetValue(key, out value);

        public bool TryGetFloat(string key, out float value)
            => _floats.TryGetValue(key, out value);

        public bool TryGetString(string key, out string value)
            => _strings.TryGetValue(key, out value);

        // Helpers para lectura con valor por defecto.
        // Útiles para sistemas del juego que leen flags simples.
        public bool   GetBool(string key,   bool   defaultValue = false) =>
            _bools.TryGetValue(key,   out bool   v) ? v : defaultValue;

        public float  GetFloat(string key,  float  defaultValue = 0f)    =>
            _floats.TryGetValue(key,  out float  v) ? v : defaultValue;

        public string GetString(string key, string defaultValue = "")    =>
            _strings.TryGetValue(key, out string v) ? v : defaultValue;

        // ─── Reset ────────────────────────────────────────────────────

        // Llamar al iniciar partida nueva.
        // Crítico: los SO retienen cambios entre Play sessions en Editor.
        public void ResetAll()
        {
            ClearLocalVariables(_bools);
            ClearLocalVariables(_floats);
            ClearLocalVariables(_strings);
        }


        private void ClearLocalVariables<T>(Dictionary<string, T> dict)
        {
            // 🔴 GC ALLOC — Instanciamos una lista para guardar las keys.
            // 🟡 PERF — Es totalmente aceptable porque ocurre SOLO UNA VEZ 
            // al cambiar de día durante una carga de nivel (fuera del hot path).
            List<string> keysToRemove = new List<string>();

            foreach (var key in dict.Keys)
            {
                if (!key.StartsWith("GLOBAL_"))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                dict.Remove(key);
            }
        }

        // ─── Debug ────────────────────────────────────────────────────
#if UNITY_EDITOR
        public IReadOnlyDictionary<string, bool>   DebugBools   => _bools;
        public IReadOnlyDictionary<string, float>  DebugFloats  => _floats;
        public IReadOnlyDictionary<string, string> DebugStrings => _strings;
#endif
    }
}
