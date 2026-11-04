# Diagnóstico del build #7

Auditoría del código de `main` en `77b523383d0f261eefa1a57845e79fcaa1ce5c19`, 3 de noviembre de 2026. El propietario informa que instaló el build #7 y vio una pantalla rosa estática, sin interacción de manos ni interfaz utilizable. No se dispuso del APK/log de ese build para vincular cada fallo a un mensaje del dispositivo o verificar su SHA exacto.

| Hallazgo comprobado en el código | Consecuencia | Cambio |
|---|---|---|
| MP4 en `Assets/SpatialHDMI/StreamingAssets`, mientras el reproductor buscaba `Application.streamingAssetsPath + /demo.mp4` | El archivo no estaba en la ruta de empaquetado usada por el reproductor; la preparación podía fallar | Mover a `Assets/StreamingAssets/demo.mp4` y comprobar el archivo ensamblado |
| Base URP 14.0.8 y shader de pantalla escrito con `CGPROGRAM`/`UnityCG.cginc`, sin ruta URP explícita | Riesgo de shader/variante incompatible. No prueba por sí mismo la causa exacta del rosa | Shader URP HLSL con estéreo y comprobación de errores del importador |
| No existían interactores de mano ni rig de interacción en la escena generada | Activar una función OpenXR no creaba interacción | Instanciar el rig oficial completo de XRI, incluidas sus acciones |
| El único HUD era `TextMesh` con instrucciones; no tenía botones/slider funcionales | El usuario no podía manejar la app con una interfaz espacial | UI world-space con raycaster XR y acciones reales |
| El componente `RoomLight` no se creaba ni se conectaba a geometría en la escena | No se producía el efecto ambiental anunciado por el nombre del script | Muestreo de video y cuatro halos virtuales creados por el nuevo sistema |
| No se comprobaban errores de shader, rig, manos, acciones de UI o referencias completas | Compilar podía producir una escena incompleta | Invariantes ejecutables en `SpatialBuild.PrepareScene` / `Validate` |
| La posición se guardaba como coordenadas del origen XR | No equivalía a un ancla persistente; podía reaparecer en un lugar incómodo | Guardar preferencias y recentrar al obtener tracking; anclas pendientes |
| README afirmaba que no había APK previa | Documentación desactualizada frente al reporte del propietario | Diferenciar build #7 reportado y candidato 0.2.0 todavía no compilado |

## Alcance de la conclusión sobre la pantalla rosa

Se corrigieron el contrato de empaquetado y la ruta de rendering. **No se afirma que el rosa haya desaparecido en Quest.** Un shader antiguo puede funcionar en determinados contextos URP; sin el log y las variantes compiladas no corresponde atribuirle con certeza el fallo de ese APK. La ausencia del MP4 explica un fallo de carga, pero tampoco demuestra por sí sola una superficie magenta.

La siguiente validación debe verificar `ShaderUtil.ShaderHasError`, compilar Android, confirmar que el APK contiene `assets/demo.mp4`, revisar los errores de OpenGL/VideoPlayer y comprobar que aumentan los fotogramas recibidos. El nuevo diagnóstico local registra shader soportado, pipeline, cargador XR, tiempo de reproducción y fotogramas. Esa evidencia ayudará a distinguir un fallo del decoder de uno del renderizador.

## Qué no se hizo

No se ocultó la pantalla, no se sustituyó por una imagen y no se presentó una captura HDMI ficticia. El mismo MP4 real H.264/AAC de 24 s se conserva en su ubicación correcta.
