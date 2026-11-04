# Arquitectura del candidato 0.2.0

| Responsabilidad | Implementación |
|---|---|
| Ensamblar proyecto | Scripts shell con base oficial fijada por SHA completo |
| Crear/validar escena y exportar | `SpatialBuild` en Editor; una única escena de aplicación |
| Superficie y UV | `SpatialScreen`: malla cilíndrica, 16:9, curvatura 0–1 |
| Rendering | `Screen.shader`: URP, estéreo, alpha opaco, letterbox/pillarbox y corrección vertical |
| Decodificación y reproducción | `DemoVideo`: VideoPlayer → RenderTexture, audio 2D, preparación, primer fotograma, controles y errores |
| Selección de fuente | `VideoSources`: Demo, documento local y diagnóstico USB |
| Importar documento Android | `MediaPickerActivity`: SAF y copia por stream a almacenamiento privado; sin permisos amplios de archivos |
| Entrada de manos y Touch | Prefab oficial `XR Interaction Hands Setup`, XR Hands + Meta Aim + XRI 2.5.1 |
| Agarrar/mover/rotar/escalar | XRGrabInteractable + XRGeneralGrabTransformer; cuatro colliders dedicados |
| Recuperación de tracking | `HandInteractionGuard`: cancelar selección, bloquear gestos del sistema y exigir soltar antes de recuperar interacción |
| Colocación y ajustes | `ScreenController`: bloqueo, recentrado, movimiento, distancia, inclinación, giro y límites absolutos de escala |
| Interfaz | `SpatialUI`, uGUI world-space, XRUIInputModule del rig y TrackedDeviceGraphicRaycaster |
| Sliders | `SliderGesture`: evita que la sincronización visual sobrescriba un arrastre activo |
| Ambiente | `EnvironmentEffects`: downsample 16×8, readback asíncrono a 10 Hz (fallback de 4 Hz) y suavizado temporal de 250 ms |
| Preferencias | `SettingsStore`: tamaño, curva, volumen, orientación y ambiente; sin simular anclas |
| Evidencia del dispositivo | `RuntimeDiagnostics`: JSON local con estado XR, pipeline, decoder, fotogramas y manos |
| Contrato para captura futura | `ILiveVideoSource`: textura, estado y ciclo de captura. No hay proveedor UVC real todavía |

## Interacción

La superficie de video no tiene un collider de agarre. El usuario apunta y pellizca una de las cuatro esquinas cian o la toma directamente. Una mano mueve y rota; dos seleccionan el mismo interactable para rotar y escalar. No hay lanzamiento al soltar, gravedad ni seguimiento permanente de la cabeza. El bloqueo deshabilita el agarre. La escala uniforme se limita a un ancho de 0.8–5 m, también después de agarres sucesivos.

El rig oficial aporta manos visuales, rayos, poke, selección y transición entre manos y Touch. La protección de tracking cancela el agarre si se pierde una mano seleccionadora, y exige una liberación de pellizco antes de permitir una nueva selección. Los gestos reservados del sistema no deben mover la pantalla. La comodidad y estabilidad de estas transiciones requieren validación en hardware.

El panel principal queda debajo del contenido, con pestañas Playback, Screen, Atmosphere y Sources. Sus botones miden aproximadamente 6–9 cm de alto al tamaño normal del panel. El acceso pequeño frente al usuario permite mostrar controles y recentrar aunque la pantalla haya quedado fuera de la vista. El gesto de menú de Meta Aim también conmuta el panel. Touch conserva A para play/pause, B para recentrar y X para el menú.

## Realidad mixta y efectos

`ARSession` y `ARCameraManager` utilizan las funciones Meta OpenXR ya habilitadas en la base. La cámara del rig oficial mantiene su TrackedPoseDriver; se desactivan locomoción, HDR y postprocesado para conservar una composición simple con alpha.

El oscurecimiento usa el alpha negro del fondo de cámara sobre el passthrough. Su resultado depende de la composición en Quest y queda pendiente de prueba. La pantalla escribe alpha 1 para conservar contraste. El glow son cuatro superficies virtuales detrás de los bordes, cuyos colores se muestrean del video; no se mapean paredes ni se modifica la iluminación física. Si el dispositivo no admite AsyncGPUReadback, se usa un readback pequeño de 16×8 a 4 Hz; su coste de sincronización GPU debe medirse en Quest y el usuario puede desactivar el efecto. Los halos permanecen transparentes hasta recibir una muestra válida; un fallo de muestreo no bloquea el video.

## Fuentes y persistencia

El MP4 se copia desde la ruta real de StreamingAssets a un archivo privado versionado y seekable. El reproductor espera preparación y un fotograma real, con límites de tiempo y mensajes de error. Los videos locales se importan mediante un documento `content://` sin inventar una ruta de archivo equivalente. Se recomienda iniciar con MP4 H.264/AAC sin DRM; no hay garantía universal de formatos o de que Horizon OS exponga el selector de documentos.

No se restaura una pose como si fuera un ancla. La pantalla se coloca frente a la orientación completa de la cabeza al disponer de tracking, lo que permite recentrar hacia el techo al estar recostado. La persistencia espacial necesita un proveedor de anclas y prueba de guardado, carga y relocalización entre sesiones.
