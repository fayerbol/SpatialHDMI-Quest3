# Plan técnico de entrada HDMI por USB

## Estado exacto

**No hay captura HDMI funcional en este candidato.** `Inspect USB` únicamente enumera cuántos dispositivos USB ve Android. No solicita permiso, no identifica todos los formatos UVC, no abre endpoints y no decodifica video/audio. `ILiveVideoSource` es el contrato de extensión; todavía no tiene un proveedor de captura ni se usa para entregar fotogramas reales.

El demo y el importador local funcionan como rutas independientes en el código y no dependen de ese módulo. No se interpreta un adaptador USB-C→HDMI de salida como una capturadora de entrada.

## Cadena y frontera nativa

Fuente HDMI → entrada HDMI de una capturadora → salida USB UVC/UAC → puerto USB Host de Quest → permiso Android → sesión UVC → decoder → textura Unity → superficie espacial.

Con la arquitectura actual se prevé un plugin Android nativo y un puente JNI. Unity VideoPlayer no es un capturador UVC general. Que una cámara externa aparezca en Camera2 o WebCamTexture depende del HAL y de Horizon OS; no se da por disponible. La app Meta HDMI Link demuestra que el caso de uso es posible, pero no proporciona aquí un API para reutilizar su sesión o sus fotogramas dentro de Spatial HDMI.

| Módulo por desarrollar | Trabajo y criterio de salida |
|---|---|
| UsbHost/Discovery | Enumerar interfaces de clase video/audio, VID/PID, endpoints, descriptores y modos. Manejar attach/detach sin bloquear Unity |
| Permissions | Comprobar USB Host; solicitar CAMERA para UVC cuando corresponda al target Android; obtener `UsbManager.requestPermission`; resolver denegación y desconexión. RECORD_AUDIO para el camino de audio que lo requiera |
| UvcSession | Abrir únicamente el dispositivo autorizado, negociar probe/commit, formato/resolución/fps, iniciar/parar y liberar descriptor. Seleccionar una biblioteca auditada y compatible con ARM64/Horizon OS antes de integrarla |
| Decoder | MJPEG: decoder JPEG; YUY2: conversión YUV→RGB preferentemente GPU/NEON; H.264/H.265: MediaCodec solo si el dispositivo realmente expone ese formato y se comprueba el decoder |
| Unity texture bridge | Render event en el hilo de gráficos; textura externa GLES o buffers nativos con propiedad/ciclo de vida explícitos. Evitar enviar cada fotograma mediante UnitySendMessage o arrays C# |
| UAC audio | Negociar formato PCM y canalización de reproducción, medir sincronización A/V y clock drift. El video UVC no garantiza que la tarjeta tenga audio UAC |
| Source coordinator | Detener/reanudar fuentes, entregar textura y relación de aspecto al presenter, reiniciar ambiente y aislar errores USB del demo |
| Diagnostics | Registrar modo negociado real, frames/fps, frames perdidos, reconexiones, latencia y rendimiento; nunca afirmar capacidades por el nombre comercial de la tarjeta |

Android exige permiso USB antes de abrir un dispositivo. La documentación de UsbManager especifica CAMERA adicional para clientes dirigidos a Android P o posterior que acceden a dispositivos de clase video. La concesión USB dura hasta desconectar el dispositivo. Los detalles finales deben probarse con la versión de Horizon OS del Quest y la capturadora real.

## Formatos y ancho de banda

YUY2 usa 2 bytes por píxel: 1920×1080×60×2 = **248,832,000 bytes/s**, aproximadamente **1.99 Gbit/s antes del overhead**. Supera el máximo teórico de USB 2.0. MJPEG reduce el flujo con una compresión variable y añade trabajo de decodificación. Un modo HDMI de entrada 4K60 o un puerto marcado 4K60 puede corresponder a passthrough, no a captura USB 4K60.

Primera meta de ingeniería: enumerar modos y reproducir **720p30**, después 1080p30 y 1080p60 solo si la tarjeta, USB y decoder lo permiten. 4K60 no es un compromiso ni un criterio satisfecho. El enlace de USB, el hub, cables, UVC transport, formato, carga térmica y render deben medirse juntos.

## Latencia y alimentación

Medir fuente→capturadora→USB→decoder→textura→presentación. Usar colas acotadas y priorizar el fotograma más reciente; no acumular video atrasado. Comparar con la pantalla de origen mediante cámara rápida/reloj visual y revisar A/V, no solo el fps del render XR.

La carga USB-C simultánea requiere una capturadora/hub con datos en dirección correcta y Power Delivery compatible. Un conector PD solo para alimentar la tarjeta no demuestra que cargue el visor. Verificar negociación, estabilidad y reconexiones con y sin alimentación. No se puede validar esto con software sin la cadena física.

## Bloqueo para la siguiente etapa

Faltan el proveedor UVC/UAC nativo, la negociación y entrega de texturas/audio, un Quest accesible, la capturadora concreta, sus descriptores/modos y una fuente HDMI sin HDCP. El entorno de este trabajo no tiene hardware USB de Quest conectado. Ningún permiso de GitHub o compilación resuelve ese bloqueo físico.

La intervención mínima para validar el módulo futuro será conectar esa cadena y obtener el inventario y modos reales. Primero debe aprobarse el reproductor espacial de demo en el visor. No se omitirán mecanismos DRM/HDCP; una señal protegida puede quedar en negro y no implica un fallo del shader.

Fuentes: [Android USB Host](https://developer.android.com/develop/connectivity/usb/host), [UsbManager](https://developer.android.com/reference/android/hardware/usb/UsbManager), [Meta HDMI Link oficial](https://www.meta.com/blog/quest/hdmi-link-launch/).
