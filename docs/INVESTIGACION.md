# Investigación y referencias

Consulta realizada el 3 de noviembre de 2026, antes de modificar el código. Se consultaron fuentes primarias y se descargó el código de los paquetes Unity en sus versiones reales. No se instalaron SDK recientes de Meta sobre una base de 2022 sin verificar compatibilidad.

## Spatial TV: qué se pudo inspeccionar

| Referencia solicitada | Acceso real | Uso en este trabajo |
|---|---|---|
| [Video principal](https://www.youtube.com/watch?v=sV4557R7hr0) | La página de reproducción no fue recuperable. El endpoint oficial oEmbed sí devolvió el título «Spatial TV on Meta Quest 3 TOTALLY Transforms My Room!», autor RealityRise y miniatura | Metadatos, sin afirmar haber visto la secuencia ni probado sus gestos |
| [Video adicional](https://www.youtube.com/watch?v=j-x-D_uLLBI) | Igual limitación. oEmbed devolvió «Spatial TV Takes GAMING To The NEXT LEVEL On Quest! Play Games With Ambient Lighting!», autor Getting Things Dun | Metadatos; no se atribuyen comportamientos concretos a fotogramas no inspeccionados |
| [Página oficial](https://spatial-tv.app/) | Sitio recuperable; promueve iluminación en tiempo real | Referencia de concepto visual, no especificación de implementación |
| [Ficha Meta](https://www.meta.com/experiences/spatial-tv/7087076334694024/) | Ficha y enlaces a imágenes oficiales accesibles | Referencia de producto; no equivale a una prueba de la app |

La propuesta toma la pantalla en MR, colocación cómoda, controles discretos, curvatura y ambiente configurable que pide el propietario. El glow implementado es una aproximación virtual alrededor de la pantalla. No se presenta como una reproducción completa del efecto de Spatial TV ni como iluminación reconstruida sobre las paredes de la habitación.

## SDK e integración escogida

Se revisaron la documentación de [Meta Interaction SDK](https://developers.meta.com/vr/documentation/unity/unity-isdk-interaction-sdk-overview/), sus [funciones](https://developers.meta.com/vr/documentation/unity/unity-isdk-features-overview/) y [escenas de ejemplo](https://developers.meta.com/vr/documentation/unity/unity-isdk-example-scenes/). Meta ofrece grab, escalado, poke, ray y compatibilidad con manos/controles; el SDK no estaba en este repositorio.

La base oficial fijada ya incorpora una alternativa compatible: [Unity XRI Hands Interaction Demo 2.5](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.5/manual/samples-hands-interaction-demo.html), XR Hands 1.3 y OpenXR 1.8.2. Se inspeccionaron los prefabs incluidos y sus scripts; el generador anterior descartaba ese rig y creaba una cámara mínima.

APIs comprobadas directamente en el código oficial descargado desde `packages.unity.com`, no inventadas:

| Paquete | API / recurso comprobado |
|---|---|
| XRI 2.5.1 | `XRGrabInteractable`, `InteractableSelectMode.Multiple`, listas serializadas de grab transformers, `XRGeneralGrabTransformer.allowTwoHandedScaling`, `allowTwoHandedRotation`, límites de escala y `XRInteractionManager.CancelInteractableSelection` |
| XRI 2.5.1 | `XRRayInteractor.enableUIInteraction`, `XRPokeInteractor`, `XRDirectInteractor`, `XRUIInputModule`, `TrackedDeviceGraphicRaycaster`, `InputActionManager` |
| XR Hands 1.3.0 | `XRHandSubsystem.leftHand/rightHand.isTracked`, `MetaAimHand.left/right`, `indexPressed`, `aimFlags`, `MetaAimFlags.SystemGesture/MenuPressed` |
| Meta OpenXR 0.2.1 | `ARCameraFeature`, `ARSessionFeature`, soporte de passthrough mediante `XR_FB_passthrough` y composición alpha |
| XR Management 4.4.0 | Configuración Android, `InitManagerOnStart` y ciclo de inicio; los flags internos de automaticLoading=false no se trataron erróneamente como prueba de que XR no arrancaba |

No se importaron clases ficticias de Meta ni se mezclaron rigs OVR con el rig OpenXR existente. Las APIs anteriores se identificaron en los archivos exactos de los paquetes, lo cual no sustituye compilarlas en Unity.

## Referencias de plataforma

- [StreamingAssets de Unity](https://docs.unity3d.com/2022.3/Documentation/Manual/StreamingAssets.html): carpeta especial directamente dentro de Assets; Android requiere leer su contenido empaquetado mediante un mecanismo adecuado.
- [Meta OpenXR camera/passthrough](https://docs.unity3d.com/Packages/com.unity.xr.meta-openxr@0.2/manual/features/camera.html).
- [Meta Aim Hand](https://docs.unity3d.com/Packages/com.unity.xr.hands@1.3/api/UnityEngine.XR.Hands.MetaAimHand.html).
- [USB Host Android](https://developer.android.com/develop/connectivity/usb/host) y [UsbManager](https://developer.android.com/reference/android/hardware/usb/UsbManager).
- [Lanzamiento oficial Meta Quest HDMI Link](https://www.meta.com/blog/quest/hdmi-link-launch/): capturadora UVC/UAC, USB 3 recomendado, objetivo 1080p60, restricciones HDCP y permisos.
- [Storage Access Framework](https://developer.android.com/training/data-storage/shared/documents-files): selección de documentos sin permisos generales de almacenamiento.

## Persistencia espacial

La base tiene una función Meta AR Anchor. Su presencia no prueba guardado durable, carga ni relocalización entre sesiones. El candidato guarda preferencias y recentra al iniciar. La persistencia espacial queda para una etapa con proveedor y API de anclas verificadas para la versión concreta, más pruebas de permisos, guardado, reinicio, recuperación y eliminación en Quest. No hay una función «ancla persistente» simulada con PlayerPrefs.
