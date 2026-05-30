using System;
using System.IO;
using UnityEngine;
using Journey.Data;

namespace Journey.Core
{
    /// <summary>
    /// Lee y escribe JourneySaveData como JSON en Application.persistentDataPath.
    /// Stateless: todos los métodos son estáticos.
    /// No conoce JourneyManager — solo serializa y deserializa datos.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "sintoniadirecta_save.json";

        private static string FilePath =>
            Path.Combine(Application.persistentDataPath, FileName); // 🟡 PERF — property, no campo, porque persistentDataPath no está disponible fuera de runtime

        // ── API pública ───────────────────────────────────────────

        /// <summary>Guarda el estado actual de la partida en disco.</summary>
        public static void Save(JourneySaveData data)
        {
            if (data == null)
            {
                Debug.LogError("[SaveSystem] data es null. No se guardó nada.");
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(FilePath, json);
                Debug.Log($"[SaveSystem] Guardado en: {FilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Error al guardar: {ex.Message}");
            }
        }

        /// <summary>
        /// Carga la partida guardada. Retorna null si no existe archivo.
        /// El caller decide qué hacer cuando no hay save (nueva partida).
        /// </summary>
        public static JourneySaveData Load()
        {
            if (!File.Exists(FilePath))
            {
                Debug.Log("[SaveSystem] No existe archivo de guardado. Partida nueva.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(FilePath);
                return JsonUtility.FromJson<JourneySaveData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Error al cargar: {ex.Message}. Se inicia partida nueva.");
                return null;
            }
        }

        /// <summary>Retorna true si existe un archivo de guardado.</summary>
        public static bool HasSave() => File.Exists(FilePath);

        /// <summary>Borra el archivo de guardado. Para "Nueva Partida".</summary>
        public static void Delete()
        {
            if (!File.Exists(FilePath)) return;

            try
            {
                File.Delete(FilePath);
                Debug.Log("[SaveSystem] Guardado borrado.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Error al borrar: {ex.Message}");
            }
        }
    }
}
