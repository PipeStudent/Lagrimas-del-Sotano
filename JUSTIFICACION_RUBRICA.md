# Justificación por criterio de la rúbrica

Proyecto: **Lágrimas del Sótano** (Unity 6, URP 2D, build Windows x64).

## 2.1.1 Selección de la plataforma

**Plataforma: PC con Windows (StandaloneWindows64)**, ventana 1280x720 redimensionable.

*Tipo de juego.* Es un shooter cenital de acción en tiempo real: se mueve con una mano (WASD) y se dispara con la otra (flechas) a la vez, mientras se esquivan proyectiles. Ese esquema de doble control necesita teclado.

*Público objetivo.* Jugadores casuales y fans de los "roguelite" tipo Isaac que juegan partidas de 5 a 10 minutos en computador. Los evaluadores abren el `.exe` sin instalar nada.

*Justificación técnica.*
- El teclado permite mover y apuntar a la vez con precisión; en una pantalla táctil requeriría dos joysticks virtuales que tapan la acción.
- Una sala completa (18x12 unidades) cabe en pantalla con cámara fija y el HUD queda legible en 1280x720 o más.
- Un `.exe` standalone es la distribución más estable: no depende de las limitaciones de WebGL (memoria, audio en streaming) ni de firmar/publicar en una tienda móvil.
- `Controls.cs` es una capa de entrada que funciona con el Input System nuevo y el clásico, evitando errores al abrir el proyecto en otra máquina.
- La interfaz usa IMGUI, que no requiere paquetes adicionales: se reduce el riesgo de errores de compatibilidad al desplegar.

*Despliegue.* `Tools > Sotano Shooter > 2 - Compilar build Windows` genera `Builds/Windows/LagrimasDelSotano.exe`, con la escena registrada en Build Settings, resolución por defecto 1280x720, ventana redimensionable y ejecución en segundo plano activada. La herramienta verifica que el módulo Windows Build Support esté instalado.

## 2.1.2 Motor de renderizado / pipeline

**Pipeline: Universal Render Pipeline (URP), plantilla Universal 2D.**

*Por qué URP.* El juego es 2D con sprites: URP 2D ofrece renderizado por sprites con orden de dibujo, cámara ortográfica y post-procesado a bajo costo. HDRP está pensado para 3D de alta gama y sería innecesario.

*Recursos visuales 2D.*
- 16 sprites de pixel art propios, importados con **32 píxeles por unidad**, filtro **Point** (bordes nítidos), sin compresión ni mipmaps y con transparencia alfa; la herramienta de editor aplica esa configuración automáticamente.
- Fondo de sala único (576x384 px = 18x12 unidades) con ladrillos, losetas y sombreado junto a las paredes.
- **Orden de dibujo** coherente (Sorting Order): fondo (-10), puertas (1), rocas y objetos (2), enemigos (3), jugador (4), lágrimas (5).
- **Cámara ortográfica** fija (tamaño 6,5) que muestra la sala completa.
- **Post-procesado** con un Volume global: viñeta para ambiente oscuro y bloom leve (se activa en la cámara; si el proyecto no lo soporta, se omite sin romper nada).
- Retroalimentación visual: parpadeo del jugador al ser golpeado, destello rojo de los enemigos al recibir daño, enemigos semitransparentes al aparecer y sacudida de cámara.
- La herramienta comprueba que el proyecto use URP antes de construir y avisa si no es así.

## 2.1.3 Aplicación de herramientas del motor

**Flujo completo: inicio -> juego -> fin.** `GameController` implementa los estados `Menu`, `Playing`, `Won` y `Lost`, y reinicia con un piso nuevo sin recargar la escena. El menú muestra la primera sala de fondo.

*Scripts (comentados, una responsabilidad por script).*

| Script | Responsabilidad |
|---|---|
| `GameController` | Estados, puntaje, récord (`PlayerPrefs`) |
| `Player` | Movimiento, disparo, vida, mejoras |
| `Enemy` | Vida, tres comportamientos (perseguidor, tirador, jefe), daño por contacto |
| `Tear` | Proyectil del jugador y de enemigos |
| `FloorManager` | Genera el piso, crea el contenido de cada sala, bloquea puertas |
| `Door`, `Pickup` | Puertas con estado abierto/cerrado; corazones y mejoras |
| `HudUI` | Menú, HUD, minimapa, barra del jefe, pantalla final |
| `SoundManager`, `CameraShake`, `Controls` | Audio, sacudida de cámara, entrada |

*Prefabs.* `Player`, `TearPlayer`, `TearEnemy`, `EnemyChaser`, `EnemyShooter`, `Boss`, `Rock` y `Pickup`. Los enemigos, las rocas y los objetos se instancian en tiempo de ejecución desde sus prefabs.

*Generación procedural.* Cada partida crea un piso distinto: caminata aleatoria en cuadrícula, jefe en la sala más lejana (recorrido en anchura), cuarto del tesoro en un callejón sin salida. El contenido de cada sala depende de una semilla, así que al volver a entrar se ve igual.

*Interfaz.* Menú con botón JUGAR, corazones (llenos, medios y vacíos), estadísticas (daño, cadencia, velocidad), puntaje, minimapa con colores por tipo de sala, barra de vida del jefe y pantalla final con estadísticas y botón de reinicio. La UI escala a cualquier resolución.

*Desacople.* Los scripts se comunican por métodos públicos concretos (`RegisterKill`, `OnEnemyKilled`, `TakeDamage`, `TryMove`) y las referencias se asignan en el editor, no por búsquedas por nombre.

## 2.1.4 Uso de física

Es un juego cenital, así que la **gravedad se desactiva a propósito** (`Physics2D.gravity = (0,0)`); la física se usa para colisiones, fuerzas y fricción.

- **Rigidbody2D** en jugador, enemigos y lágrimas; `freezeRotation` para que los sprites no giren; interpolación para movimiento suave y detección continua para que las lágrimas rápidas no atraviesen paredes.
- **Movimiento con velocidad y aceleración:** el jugador y los enemigos aceleran hacia la velocidad deseada (`MoveTowards`), lo que da sensación de peso; los golpes aplican velocidad o impulso (`AddForce` con `ForceMode2D.Impulse`), que decae por amortiguamiento lineal en los enemigos.
- **Colisiones:** colliders sólidos (círculo en personajes, caja en rocas y paredes) para el choque; colliders **trigger** para lágrimas, puertas y objetos.
- **Daño por colisión:** los enemigos hieren al jugador con `OnCollisionStay2D`; las lágrimas detectan impactos con `OnTriggerEnter2D` y distinguen jugador, enemigo y pared.
- **Masas diferentes:** el jefe pesa 8 veces más que un enemigo común, por lo que el jugador no puede empujarlo.
- **Materiales físicos 2D** (`PhysicsMaterial2D`):

| Material | Fricción | Rebote | Uso |
|---|---|---|---|
| PM2D_Player | 0 | 0 | Jugador (se desliza junto a paredes sin trabarse) |
| PM2D_Enemy | 0 | 0,25 | Enemigos (rebotan levemente al chocar) |
| PM2D_Wall | 0 | 0 | Paredes y rocas |

- **Rocas** como obstáculos sólidos que bloquean tanto al jugador como a las lágrimas.

## 2.1.5 Integración de sonido

- **BGM:** `bgm_loop.wav`, bucle oscuro de ~22 s (acordes graves, latido y campanitas) con carga en *streaming* y compresión Vorbis. Suena desde el menú y baja al 25 % en las pantallas de victoria o derrota para que se oiga el efecto final.
- **SFX (8, más de los 2 pedidos), cada uno disparado por un evento del juego:**

| Evento | Clip | Volumen relativo |
|---|---|---|
| Disparo (jugador y enemigos) | `sfx_shoot` | 0,45 |
| Lágrima impacta enemigo | `sfx_enemy_hit` | 0,70 |
| Enemigo muere | `sfx_enemy_die` | 0,85 |
| Jugador herido | `sfx_hurt` | 1,00 |
| Recoger objeto | `sfx_pickup` | 0,80 |
| Puerta (abrir sala / cruzar) | `sfx_door` | 0,70 |
| Victoria | `sfx_win` | 0,90 |
| Derrota | `sfx_lose` | 0,90 |

- **Mezcla equilibrada:** música a 0,30, efectos a 0,85 con volumen relativo por clip. El disparo es el más suave porque se repite mucho y el golpe al jugador es el más fuerte porque es lo más importante de oír.
- **Variación de tono** (±8 %) en los efectos repetitivos para que no suenen idénticos.
- **Control del jugador:** tecla M silencia o activa todo el audio.
- Los efectos se importan como `DecompressOnLoad` (ADPCM) para baja latencia.
- Todos los sonidos y sprites se generaron procedimentalmente para este proyecto, así que no tienen problemas de derechos de autor.
