// Assets/Scripts/Interaction/Core/InteractionSystem.cs
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Interaction.Handlers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Interaction.Core
{
    /// <summary>
    /// Coordina: detección de objetos bajo el cursor (raycast condicional),
    /// verbo activo (via VerbSelectorSO) y despacho de handlers.
    /// No conoce DialogueRunner ni ConsequenceSystem directamente.
    /// Se comunica con ellos vía eventos.
    /// </summary>
    public sealed class InteractionSystem : MonoBehaviour
    {
        // ── Config ───────────────────────────────────────────────
        [SerializeField] private VerbSelectorSO _verbSelector;
        [SerializeField] private Camera          _camera;
        [SerializeField] private LayerMask       _interactableLayer;

        [Tooltip("Distancia mínima de movimiento del cursor (px) para re-ejecutar el raycast.")]
        [SerializeField] private float _cursorMovementThreshold = 2f;

        // ── Eventos públicos ─────────────────────────────────────
        // DialogueRunner escucha OnKnotRequested.                   // 🔵 NET — en multijugador futuro, este evento cruza la red
        //public event Action<string>   OnKnotRequested;
        public event Action<string[]> OnConsequencesRequested;
        public event Action<string>   OnHoverChanged;               // CursorController y VerbHUD escuchan esto para el tooltip

        /// <summary>
        /// Disparado al hacer click derecho sobre un objeto interactuable.
        /// VerbHUD escucha este evento para abrir el menú radial.
        /// </summary>
        public event Action<Vector2, InteractableObject> OnRadialRequested;

        // ── Estado interno ───────────────────────────────────────
        private readonly RaycastHit2D[] _raycastBuffer = new RaycastHit2D[8]; // 🟢 POOL — array cacheado, NonAlloc, zero GC
        private readonly List<IInteractionHandler> _handlers = new(4); // handlers registrados, ordenados por prioridad (Ink primero) capacidad 4: Ink + Consequence + SceneTransition + reserva

        private InteractableObject _currentHovered;
        private Vector2            _lastCursorPosition;
        private bool               _isInteracting;
        private bool               _radialIsOpen;
        private int _lastRightClickFrame = -1;


        private CancellationTokenSource _cts;

        // ── Unity Lifecycle ──────────────────────────────────────
        private void Awake()
        {
            ValidateDependencies();
            RegisterHandlers();
        }

        private void OnEnable()
        {
            _verbSelector.ResetToDefault();
        }

        private void Update()
        {
            if (_isInteracting) return;     // 🟡 PERF — bloquea detección mientras se procesa una interacción
            UpdateHover();
        }

        private void OnDisable()
        {
            CancelCurrentInteraction();
        }

        private void OnDestroy()
        {
            CancelCurrentInteraction();
        }

        // ── Setup ────────────────────────────────────────────────
        private void ValidateDependencies()
        {
            if (_verbSelector == null)
                Debug.LogError("[InteractionSystem] VerbSelectorSO no asignado.", this);

            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                    Debug.LogError("[InteractionSystem] No se encontró cámara.", this);
            }
        }

        private void RegisterHandlers()
        {
            /// Orden de prioridad (de mayor a menor):
            //   [0] InkInteractionHandler         — se inserta desde DialogueSystemBootstrap.Start()
            //   [1] ConsequenceInteractionHandler  — registrado aquí en Awake()
            //   [2] SceneTransitionHandler         — se inserta desde SceneTransitionBootstrap.Start()
            //
            // InkHandler y SceneTransitionHandler se insertan después porque sus dependencias
            // (DialogueRunner y SceneTransitionManager) pueden no estar listas en Awake.
            _handlers.Add(new ConsequenceInteractionHandler(tags => OnConsequencesRequested?.Invoke(tags)));
        }

        // ── Hover (raycast condicional) ──────────────────────────
        private void UpdateHover()
        {
            var cursorPosition = Mouse.current.position.ReadValue();

            // Raycast condicional: solo si el cursor se movió lo suficiente
            var delta = (cursorPosition - _lastCursorPosition).magnitude;
            if (delta < _cursorMovementThreshold) return;           // 🟢 zero costo cuando el cursor está quieto
            _lastCursorPosition = cursorPosition;

            var worldPosition = _camera.ScreenToWorldPoint(cursorPosition);
            var hitCount = Physics2D.RaycastNonAlloc(            // 🟢 NonAlloc — sin allocations
                worldPosition,
                Vector2.zero,
                _raycastBuffer,
                0f,
                _interactableLayer
            );

            var detected = hitCount > 0
                ? _raycastBuffer[0].collider?.GetComponent<InteractableObject>()
                : null;

            if (detected == _currentHovered) return;              // sin cambio, sin evento

            _currentHovered = detected;
            OnHoverChanged?.Invoke(_currentHovered?.DisplayName ?? string.Empty);
            Debug.Log($"[Hover] {(_currentHovered != null ? _currentHovered.DisplayName : "ninguno")}");

        }


        /// <summary>Asignar al evento "RightClick" en PlayerInput.</summary>
        public void OnRightClickPerformed()
        {
            Debug.Log($"[RightClick RAW] frame={Time.frameCount} radialOpen={_radialIsOpen} lastFrame={_lastRightClickFrame}");

            if (Time.frameCount == _lastRightClickFrame) return; // evita múltiples disparos en el mismo frame
            if (_isInteracting)          return;
            if (_currentHovered == null) return;
            if (_radialIsOpen)           return;

            _lastRightClickFrame = Time.frameCount;

            var screenPos = Mouse.current.position.ReadValue();
            Debug.Log($"[RightClick] Abrir radial para: {(_currentHovered != null ? _currentHovered.DisplayName : "ninguno")} pos={screenPos}");
            OnRadialRequested?.Invoke(screenPos, _currentHovered);
        }

        // ── Despacho de handlers ─────────────────────────────────
        private async UniTask InteractAsync(InteractableObject target, VerbType verb)
        {
            Debug.Log($"[Interact] Iniciando '{verb}' sobre '{target.DisplayName}'");

            _isInteracting = true;
            _cts = new CancellationTokenSource();

            try
            {
                var handled = false;

                foreach (var handler in _handlers)               // O(n) — n = 2, constante
                {
                    if (!handler.CanHandle(target, verb)) continue;

                    await handler.HandleAsync(target, verb, _cts.Token);
                    Debug.Log($"[Interact] Handler {handler.GetType().Name} procesó la interacción");
                    handled = true;
                    break;                                        // el primer handler que puede, maneja
                }

                if (!handled)
                    Debug.Log($"[InteractionSystem] Ningún handler para '{target.DisplayName}' + {verb}.");
            }
            catch (OperationCanceledException)
            {
                // Cancelación limpia — no es un error
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isInteracting = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        // ── Cancelación ──────────────────────────────────────────
        private void CancelCurrentInteraction()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _isInteracting = false;
        }

        /// <summary>
        /// Llamado por VerbHUD al seleccionar un verbo en el radial.
        /// Ejecuta la interacción sobre el objeto actualmente bajo el cursor.
        /// </summary>
        public void ExecuteInteraction(InteractableObject target, VerbType verb)
        {
            Debug.Log($"[InteractionSystem] ExecuteInteraction verb={verb} target={target?.DisplayName ?? "ninguno"}");
            if (_isInteracting) return;
            if (target == null) return;
            if (verb == VerbType.None) return;
            InteractAsync(target, verb).Forget();
        }

        /// <summary>
        /// Llamado por DialogueSystemBootstrap en Start().
        /// Inserta InkInteractionHandler al FRENTE de la lista (Ink primero).
        /// </summary>
        public void RegisterInkHandler(
            Action<string>                   onKnotRequested,
            Func<CancellationToken, UniTask> waitForDialogueEnd)
        {
            _handlers.Insert(0, new InkInteractionHandler(onKnotRequested, waitForDialogueEnd));
        }

        /// <summary>
        /// Llamado por SceneTransitionBootstrap en Start().
        /// Agrega SceneTransitionInteractionHandler al FINAL de la lista
        /// (menor prioridad: solo aplica si no hay Ink ni consecuencias).
        /// </summary>
        public void RegisterSceneTransitionHandler(Action<string, string> onTransitionRequested)
        {
            _handlers.Add(new SceneTransitionInteractionHandler(onTransitionRequested));
        }



         // Métodos públicos para que VerbHUD notifique el estado
        public void NotifyRadialOpened()  => _radialIsOpen = true;
        public void NotifyRadialClosed()  => _radialIsOpen = false;

    }
}