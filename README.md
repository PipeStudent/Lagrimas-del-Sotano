# Lágrimas del Sótano

Juego 2D de acción cenital (estilo *The Binding of Isaac*, pero simplificado) hecho en Unity 6 con URP. Recorres un piso de 7 salas conectadas, limpias cada sala para abrir las puertas, encuentras una mejora en el cuarto del tesoro y derrotas al jefe final.

## Instalación (5 minutos)

> **Importante:** crea un proyecto NUEVO. El proyecto anterior no tenía URP ni Unity UI instalados, y este necesita URP.

1. En **Unity Hub** crea un proyecto con Unity 6 (6000.x) y la plantilla **Universal 2D**. Si no la ves, pulsa *Download template*.
2. Cierra Unity, copia la carpeta **`Assets`** de este paquete dentro de la carpeta del proyecto (acepta combinar) y vuelve a abrirlo. Espera a que compile; la consola no debe mostrar errores rojos.
3. Menú **Tools > Sotano Shooter > 1 - Construir escena y prefabs**. Importa los sprites y audios con la configuración correcta, crea materiales físicos, prefabs y la escena `Assets/Scenes/Game.unity`.
4. Pulsa **Play**.
5. Menú **Tools > Sotano Shooter > 2 - Compilar build Windows**. Genera `Builds/Windows/LagrimasDelSotano.exe` (en Windows necesitas el módulo *Windows Build Support* de Unity Hub).

La opción **3** hace los pasos 3 y 5 seguidos.

No hace falta instalar ningún paquete extra: la interfaz usa IMGUI (`OnGUI`), que viene con Unity.

## Controles

| Acción | Tecla |
|---|---|
| Moverse | WASD |
| Disparar (4 direcciones) | Flechas |
| Silenciar | M |
| Empezar / reiniciar | Botón, Enter o R |

## Cómo se juega

- Cada partida genera un piso nuevo de 7 salas en una cuadrícula. El minimapa (arriba a la derecha) muestra las salas visitadas y las vecinas.
- Al entrar a una sala con enemigos, las puertas se cierran. Al derrotarlos todos se abren y puede aparecer un corazón o una mejora.
- **Enemigos:** rojo (te persigue), ojo morado (deambula y dispara) y el jefe (persigue lento y lanza anillos de lágrimas; se enfurece con menos de la mitad de vida).
- **Cuarto del tesoro** (amarillo en el minimapa): una mejora gratis de daño, cadencia o velocidad.
- Tienes 3 corazones (6 puntos de vida). Tras un golpe eres invulnerable 1 segundo.
- Ganas al derrotar al jefe. El puntaje suma kills, bono por jefe y bono por tiempo; el récord se guarda.

## Estructura

```
Assets/
  Sprites/   16 sprites pixel-art (jugador, enemigos, jefe, lágrimas, roca, corazones, ítems, puertas, fondo)
  Audio/     bgm_loop.wav + 8 efectos (disparo, golpe, muerte enemigo, daño, recoger, puerta, victoria, derrota)
  Scripts/   Controls, GameController, Player, Enemy, Tear, Pickup, Door, FloorManager, HudUI, SoundManager, CameraShake
  Editor/    SotanoBuilder (arma escena, prefabs y build)
```

## Si algo falla

- **"Este proyecto no usa URP"**: el proyecto no se creó con la plantilla *Universal 2D* (o *Universal 3D*).
- **Errores de compilación por `linearVelocity` / `linearDamping`**: son nombres de Unity 6; en Unity 2022 o anterior se llaman `velocity` y `drag`.
- **Los sprites se ven borrosos**: ejecuta de nuevo el paso 1; ahí se configura el filtro Point.
- **No hay sonido**: revisa que no esté silenciado con M y que los `.wav` estén en `Assets/Audio` antes del paso 1.
