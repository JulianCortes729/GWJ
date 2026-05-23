// ─────────────────────────────────────────────────────────
// TEST P3 — ConsequenceSystem completo
// Cubre: tags, variables, opciones, flujo
// ─────────────────────────────────────────────────────────

VAR radio_encendida = false
VAR veces_intentado = 0

// ── Bloque 1: tag simple al iniciar ──────────────────────
Operador: Entrando al estudio... # set_object_visibility antena true
Operador: Voy a probar la radio. # set_object_state radio apagada

// ── Bloque 2: opciones que modifican variables ───────────
-> menu_radio

=== menu_radio ===
Operador: ¿Qué hago con la radio?

* [Encender la radio]
    ~ radio_encendida = true
    ~ veces_intentado = veces_intentado + 1
    Operador: La enciendo... # set_object_state radio encendida
    Operador: ¡Funciona! # play_animation operador sorpresa
    -> resultado_encendido

* [Ignorar la radio]
    ~ veces_intentado = veces_intentado + 1
    Operador: Mejor lo dejo para después. # set_object_visibility radio false
    -> resultado_ignorado

* [Revisar la antena]
    ~ veces_intentado = veces_intentado + 1
    Operador: Voy a revisar afuera. # play_sound pasos_exterior
    -> menu_radio

=== resultado_encendido ===
Operador: La radio está encendida. Intentos: {veces_intentado}
Operador: Probando el sonido. # play_sound musica_radio
-> fin

=== resultado_ignorado ===
Operador: Dejé la radio apagada. Intentos: {veces_intentado}
-> fin

=== fin ===
Operador: Fin del test P3. Variables sincronizadas correctamente.
-> END