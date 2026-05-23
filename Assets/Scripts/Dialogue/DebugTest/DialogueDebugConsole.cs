using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Dialogue.Core;
using Dialogue.Data;
using Ink.Runtime;

namespace Dialogue.DebugTest
{
    /// <summary>
    /// Herramienta de testing temporal — NO incluir en build de producción.
    /// Loguea en pantalla el estado de todos los sistemas del P3.
    /// </summary>
    public class DialogueDebugConsole : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private DialogueRunner    _runner;
        [SerializeField] private DialogueStorageSO _storage;

        [Header("Config")]
        [SerializeField] private int   _maxLogLines   = 20;
        [SerializeField] private float _fontSize      = 14f;
        [SerializeField] private float _panelWidth    = 500f;
        [SerializeField] private float _panelHeight   = 400f;

        private readonly List<string>  _log           = new();
        private readonly StringBuilder _sb            = new();  // 🟢 sin GC en runtime
        private GUIStyle               _boxStyle;
        private GUIStyle               _textStyle;
        private bool                   _stylesReady;

        // ── Lifecycle ────────────────────────────────────────────

        private void OnEnable()
        {
            if (_runner == null)
            {
                UnityEngine.Debug.LogError(
                    "[DialogueDebugConsole] _runner no asignado.");
                return;
            }

            _runner.OnLineReady     += HandleLineReady;
            _runner.OnChoicesReady  += HandleChoicesReady;
            _runner.OnDialogueEnded += HandleDialogueEnded;
            _runner.OnTagsReady     += HandleTagsReady;

            AddLog("── Debug Console activa ──");
        }

        private void OnDisable()
        {
            if (_runner == null) return;

            _runner.OnLineReady     -= HandleLineReady;
            _runner.OnChoicesReady  -= HandleChoicesReady;
            _runner.OnDialogueEnded -= HandleDialogueEnded;
            _runner.OnTagsReady     -= HandleTagsReady;
        }

        // ── Handlers ─────────────────────────────────────────────

        private void HandleLineReady(DialogueLine line)
        {
            _sb.Clear();
            _sb.Append("[LÍNEA] ");

            if (!string.IsNullOrEmpty(line.Speaker))
            {
                _sb.Append(line.Speaker);
                _sb.Append(": ");
            }

            _sb.Append(line.Text);
            AddLog(_sb.ToString());
            LogStorageSnapshot();
        }

        private void HandleChoicesReady(List<Choice> choices)
        {
            AddLog($"[OPCIONES] {choices.Count} disponibles:");
            for (int i = 0; i < choices.Count; i++)
                AddLog($"  [{i}] {choices[i].text}");
        }

        private void HandleDialogueEnded()
        {
            AddLog("── Diálogo terminado ──");
            LogStorageSnapshot();
        }

        private void HandleTagsReady(List<string> tags)
        {
            if (tags == null || tags.Count == 0) return;

            foreach (string tag in tags)
                AddLog($"[TAG] → {tag}");
        }

        // ── Storage snapshot ─────────────────────────────────────

        private void LogStorageSnapshot()
        {
#if UNITY_EDITOR
            if (_storage == null) return;

            bool hasAny = false;

            foreach (var kvp in _storage.DebugBools)
            {
                if (!hasAny) { AddLog("[STORAGE]"); hasAny = true; }
                AddLog($"  bool  {kvp.Key} = {kvp.Value}");
            }

            foreach (var kvp in _storage.DebugFloats)
            {
                if (!hasAny) { AddLog("[STORAGE]"); hasAny = true; }
                AddLog($"  float {kvp.Key} = {kvp.Value}");
            }

            foreach (var kvp in _storage.DebugStrings)
            {
                if (!hasAny) { AddLog("[STORAGE]"); hasAny = true; }
                AddLog($"  str   {kvp.Key} = {kvp.Value}");
            }
#endif
        }

        // ── GUI ──────────────────────────────────────────────────

        private void OnGUI()
        {
            EnsureStyles();

            float x = Screen.width - _panelWidth - 10f;
            float y = 10f;

            GUI.Box(new Rect(x, y, _panelWidth, _panelHeight), string.Empty, _boxStyle);

            float lineHeight = _fontSize + 10f;
            float padding    = 8f;
            float textY      = y + padding;

            int start = Mathf.Max(0, _log.Count - _maxLogLines);

            for (int i = start; i < _log.Count; i++)
            {
                GUI.Label(
                    new Rect(x + padding, textY, _panelWidth - padding * 2, lineHeight),
                    _log[i],
                    _textStyle);
                textY += lineHeight;
            }
        }

        // ── Helpers ──────────────────────────────────────────────

        private void AddLog(string message)
        {
            _log.Add(message);
            if (_log.Count > _maxLogLines * 2)
                _log.RemoveRange(0, _maxLogLines); // 🟡 PERF — trim ocasional, fuera de hot path

            UnityEngine.Debug.Log(message);
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.85f)) }
            };

            _textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = (int)_fontSize,
                normal    = { textColor = Color.green },
                wordWrap  = false
            };

            _stylesReady = true;
        }

        private static Texture2D MakeTex(int width, int height, Color color)
        {
            Color[]    pixels  = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
