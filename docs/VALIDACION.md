# Validación, compilación e instalación

Estado del candidato 0.2.0 al 3 de noviembre de 2026. **No hay una APK nueva de estos cambios.** El build #7 anterior fue instalado y reportado defectuoso por el propietario. No se usa su éxito de compilación como prueba de este candidato.

## Evidencia separada por nivel

| Nivel | Resultado real |
|---|---|
| A — análisis de código | Auditados todos los archivos del repositorio original, escena generada, shaders, reproducción, controles, scripts UBA/Actions y documentación. Descargada la base oficial exacta y los fuentes oficiales de XRI 2.5.1, Hands 1.3.0, OpenXR 1.8.2, Meta OpenXR 0.2.1 y XR Management 4.4.0. Las APIs usadas se comprobaron allí |
| B — pruebas automatizadas disponibles | 24 pruebas de regresión Python aprobadas. Contratos de fuente y proyecto ensamblado aprobados. Sintaxis de 12 archivos C#/Java analizada. Shell válido. MP4 completo y audio decodificados sin error por ffmpeg. Script UBA ejecutado desde Base, con clon/LFS del upstream y comprobación de versiones, features y rutas |
| B — pruebas dentro del editor | **Pendientes.** `SpatialBuild.Validate` prepara la escena, comprueba componentes y shaders e invoca acciones de UI. Estas pruebas están escritas, pero no se ejecutaron sin Unity |
| C — compilación Android | **Sin APK.** El build #8 cargó y ejecutó el código C# con Unity 2022.3.62f3, pero la comprobación del editor original abortó antes de exportar. La corrección posterior y shaders GLES/Java/IL2CPP requieren el build #9 manual |
| D — Meta Quest 3 real | **Pendiente para todas las funciones de 0.2.0.** No se declara resuelto el rosa, funcionando el hand tracking o validado el passthrough hasta esta prueba |

El análisis de sintaxis detecta errores gramaticales de código; no comprueba tipos, referencias de ensamblados, APIs ni comportamiento. Los tests de configuración rechazan regresiones como media ausente/en ruta incorrecta, una variante sin estéreo, un shader para otro pipeline, features de mano deshabilitadas, ausencia del rig e incompatibilidad de la versión XRI.

La corrección del bloqueo de versión y la distinción entre paquetes del upstream y los resueltos en Cloud están en el [análisis del build #8](BUILD8_COMPATIBILITY.md). La política admite expresamente el editor original y el del target; no acepta cualquier versión de Unity.

## Datos del demo verificados

24.000 s, H.264, 1280×720, 30 fps, yuv420p; audio AAC mono de 48 kHz. Archivo de 9,317,199 bytes. La decodificación completa de audio y video pasó con `ffmpeg -v error -xerror ... -f null -`. Esto comprueba el archivo fuente, no MediaCodec de Quest.

## Puerta de validación de Unity

1. Ensamblar Base con `tools/prepare-project.sh` en un checkout limpio, o usar el hook UBA existente.
2. Abrir con Unity **2022.3.62f3** y módulos Android, resolver los paquetes fijados y dejar terminar la compilación C#.
3. Ejecutar `SpatialBuild.Validate` con target Android. Debe terminar sin excepción y producir el marcador `SPATIAL_VALIDATION_PASS`.
4. Revisar el log, especialmente errores de shader, paquetes, scripts o referencias. Si falla, corregir y repetir la validación antes de exportar una APK.
5. Solo después ejecutar `SpatialBuild.Build`, que repite las invariantes antes de la exportación Android.

El editor valida:

- Un OpenXR loader Android, arranque XR, Meta passthrough/session, Hand Tracking, Meta Aim y perfil Touch.
- URP asignado, shaders importados sin errores reportados, ARM64, IL2CPP y GLES3.
- Cámara con el driver de pose del rig oficial, ARSession, ARCameraManager y un único XR Origin.
- EventSystem, XRUIInputModule, XRInteractionManager e InputActionManager con assets.
- Ray, poke y direct interactor en cada mano; cuatro colliders de agarre y transformer de dos manos serializado.
- Referencias de aplicación y ausencia de scripts faltantes, incluidas instancias de prefab anidadas.
- Construcción de los controles reales e invocación de tamaño, curva, glow, oscurecimiento, bloqueo/desbloqueo y movimiento.
- Malla creada, relación plana 16:9 y curvatura efectiva.

Las comprobaciones de importador no sustituyen revisar todas las variantes GLES que compile el build Android. El archivo de escena se genera antes del build, sin depender de un HUD de instrucciones ni de un autoinstalador duplicado.

## Unity Build Automation: intervención mínima

Este chat no dispone de un conector Unity ni del contexto de navegador conectado necesario para reutilizar una sesión. La herramienta de navegador devolvió: `Browser access requires the originating browser context`. No se abrió otra autenticación ni se pidieron credenciales. Tampoco hay Unity Editor ni compilador C# instalado en el entorno local.

En el target existente que produjo el build #7, reutilizar la configuración y la licencia ya disponibles. Comprobar antes de una ejecución:

| Campo | Valor del código preparado |
|---|---|
| Rama candidata | `repair/spatial-player-hands` |
| Unity project subfolder | `Base` |
| Script prebuild | `../tools/prepare-project-uba.sh`, conservando la ruta aceptada por el target actual |
| Método Unity pre-export | `SpatialBuild.PrepareScene` |
| Editor existente | `2022.3.62f3` — sin cambiar Cloud |
| Plataforma | Android, ARM64/IL2CPP, APK |
| Escena | `Assets/SpatialHDMI/Scenes/SpatialHDMI_Main.unity` |
| Package / version | `com.spatialhdmi.demo`, 0.2.0, versionCode 8 |

No se afirma que el dashboard tenga ya seleccionada esta rama ni que se haya editado el target. Si UBA es el único editor disponible, su ejecución será también la primera compilación C#: el método pre-export debe detenerla antes de exportar si las invariantes fallan. No activar facturación ni crear targets adicionales. Una sola ejecución, una vez revisada la configuración y los checks, basta para obtener el primer resultado de compilación del candidato.

El workflow GameCI existente sigue siendo manual y conserva sus requisitos de licencia. No se añadieron ni cambiaron secretos; no es necesario migrar de UBA a GameCI para validar este trabajo.

## Inspeccionar el APK antes de instalar

- Exigir un artefacto no vacío, log sin errores y SHA de la rama correcta.
- Abrir su listado ZIP y confirmar `assets/demo.mp4`.
- Revisar el manifest fusionado: paquete esperado, ARM64, OpenXR/VR, hand tracking/passthrough generados por los paquetes y Activity del selector no exportada.
- Conservar la clave de firma del build #7 si se quiere actualizar encima. Una firma diferente puede impedir la actualización.

No existe un enlace de descarga de una APK 0.2.0 en esta entrega.

## Instalación cuando exista el artefacto

Descargar la APK real de la ejecución correcta de UBA y usar el mismo método de instalación que permitió instalar el build #7. Si se dispone de ADB autorizado y modo desarrollador, `adb install -r SpatialHDMI.apk` instala una actualización con paquete y firma compatibles. No hace falta desinstalar la versión anterior como primer paso.

Abrir Spatial HDMI en las fuentes desconocidas del visor. No iniciar pruebas USB todavía; el primer objetivo es aprobar demo, manos, pantalla y MR.

## Checklist de aceptación en Quest

| Prueba | Resultado exigido |
|---|---|
| Arranque | Se ve la habitación en passthrough, una superficie de video y los controles, sin pantalla rosa |
| Demo 30–60 s | Cambia la imagen, avanza el tiempo, suena audio y hay fotogramas reales. El loop de 24 s continúa |
| Reproducción | Pausa congela el video; Play lo reanuda; Restart vuelve al inicio; timeline cambia posición y volumen cambia audio |
| Manos izquierda/derecha | Se ven/funcionan los rayos; pinch selecciona botones; toque directo funciona al acercar el panel |
| Agarre | Una esquina mueve/rota sin un salto grande. Al soltar, la pantalla permanece fija al caminar o mover la cabeza |
| Dos manos | Dos esquinas permiten escalar y orientar; no se deforma la proporción; se mantienen límites al repetir el gesto |
| Bloqueo | Los agarres no mueven el objeto bloqueado. Desbloquear lo recupera |
| Pérdida de tracking | Ocultar una mano durante agarre detiene selección sin teletransporte. Recuperar la mano con pellizco sostenido no mueve hasta soltar y volver a seleccionar |
| Pantalla | Tamaño, distancia, curva/plano, posición, tilt/yaw/roll y recentrado tienen efecto visible |
| Sentado/recostado | Recenter coloca pantalla frente a la mirada, incluido techo; no exige orientación vertical de pie |
| Interfaz | Hide oculta el panel; Controls lo recupera; Quick Recenter rescata una pantalla fuera de vista; no se pulsa sobre el video por accidente |
| Touch secundario | Rayos/botones/agarrar siguen disponibles; A pausa, B recentra y X cambia panel |
| Ambiente | Color cambia con el video, intensidad/spread responden, sin parpadeos molestos; nivel cero elimina glow |
| Oscurecimiento | Cambia de forma gradual el entorno, conservando legibilidad del video y passthrough utilizable |
| Archivo local | Selector abre o muestra error claro si no está disponible. Importar MP4 propio reproduce imagen y audio sin deformar aspecto |
| Preferencias | Tras cerrar/abrir se conserva configuración; la colocación se recentra y no se confunde con ancla persistente |
| Interrupción | Quitar el visor/abrir sistema pausa y cancela el agarre; volver permite continuar |
| Rendimiento | Usar 10–15 min, comprobar fluidez del visor, temperatura, estabilidad de decode y posibles frames perdidos |

El diagnóstico se guarda en `Application.persistentDataPath/device-diagnostics.json`, normalmente `/sdcard/Android/data/com.spatialhdmi.demo/files/device-diagnostics.json`. Puede obtenerse con ADB cuando el sistema lo permita. El botón Sources → Save device diagnostics fuerza una captura. Los logs Android de Unity/VideoPlayer y una grabación breve de la prueba completan la evidencia.

**Criterio de cierre:** si cualquiera de imagen/video, UI, manipulación con manos o realidad mixta falla, el candidato sigue sin aceptarse aunque el APK se haya compilado correctamente.
