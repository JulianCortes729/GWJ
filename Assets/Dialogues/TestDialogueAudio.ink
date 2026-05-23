-> start

=== start ===
# play_music theme_esquel

Operador: Buenas noches. Aquí Radio Esquel, en el aire.
Operador: Hoy la noche está tranquila por la cordillera.

Operador: ¿Escuchás eso?
# play_sound radio_static

Técnico: Interferencia. Pasa seguido con el viento del sur.
Técnico: Dale un golpe seco al transmisor, siempre funciona.
# play_sound radio_static


* [Ajustar la antena]
    # play_sound door_knock
    Operador: Listo. Señal limpia.

* [Ignorarlo]
    Operador: Total, ya estamos acostumbrados.
    # play_sound radio_static

- // <-- Gather: Ambas ramas convergen aquí automáticamente

Operador: Seguimos en el aire hasta las tres.
Operador: Y con esto, cerramos la transmisión de hoy.
# stop_music

Operador: Buenas noches, Esquel.
-> END
