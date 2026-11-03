# Spatial HDMI — preparación de la primera APK

**Estado:** integración sin compilar. El ZIP no contiene una APK ni el proyecto Unity base. Contiene el video, componentes, generador de escena, script para obtener la base y workflow de compilación. La escena `SpatialHDMI_Main.unity` se crea y guarda automáticamente dentro del proyecto durante la compilación; no viene pre-generada en el ZIP. Nada se ha ejecutado con Unity ni en Quest.

## Preparar y compilar

1. Extrae el ZIP en un repositorio GitHub que contenga `.github/`, `Overlay/` y `tools/` en su raíz. El workflow descarga el proyecto oficial de Unity MR Example, etiqueta `v1.0.0-pre.1`, y comprueba que el commit empiece por `1130bc5`. El generador crea una escena nueva vacía; no carga ni compila `SampleScene.unity`.
2. Para Unity Personal, configura en **Settings → Secrets and variables → Actions** los secretos `UNITY_LICENSE` (contenido del archivo de licencia activada de Unity), `UNITY_EMAIL` y `UNITY_PASSWORD`, siguiendo https://game.ci/docs/github/activation/ . No los subas al repositorio ni los pegues en logs o chats. Esta ruta asume que GameCI siga admitiendo ese método; licencias Pro/servidor requieren adaptar el workflow. Cuenta Unity y activación válida son necesarias. Revisa también cuota y costos de GitHub Actions.
3. Ejecuta manualmente **Actions → Spatial HDMI Android → Run workflow**. Si tiene éxito, descarga `SpatialHDMI-APK`. El job verifica que la APK exista y no esté vacía. En fallo intenta subir `SpatialHDMI-diagnostics`; los logs de GitHub Actions también registran el proceso. Trata los diagnósticos como privados y revisa su contenido antes de compartirlos.
4. Alternativamente, en una computadora con Git, Git LFS y Python 3 ejecuta `bash tools/prepare-project.sh`. Instala el Unity Editor indicado en `Base/ProjectSettings/ProjectVersion.txt`, con Android Build Support, SDK, NDK y OpenJDK de Unity Hub. Abre Base y usa:

```bash
/path/to/Unity -batchmode -nographics -quit -projectPath "$PWD/Base" -buildTarget Android -executeMethod SpatialBuild.Build -logFile "$PWD/unity-build.log"
```

La salida prevista es `Base/build/Android/SpatialHDMI.apk`. Los ajustes XR, paquete y archivos de proyecto vienen de la revisión fijada del ejemplo oficial. El script imprime las versiones reales del Editor y las dependencias directas desde esa base; no fueron accesibles en este entorno, por lo que no se atribuyen números de versión sin evidencia. `packages-lock.json`, si lo incluye la base, fija las dependencias transitivas. No se necesita afirmar que Meta XR Core SDK esté instalado: esta ruta utiliza Unity OpenXR: Meta y AR Foundation del ejemplo.

## Qué hace el generador

`SpatialBuild.Build` comprueba los archivos básicos y que Android tenga un cargador OpenXR, establece Android ARM64/IL2CPP, crea un material y una escena con `XR Origin`, cámara, `ARSession`, `ARCameraManager`, fondo negro transparente, pantalla curva, reproductor Demo, controles y panel. Asigna las referencias por código. Reutiliza los scripts y el video de la entrega anterior. La escena conserva el material generado y se convierte en la única escena de compilación.

La presencia del cargador no verifica por sí sola que la función Meta passthrough, el perfil de mandos, los permisos y el compositor estén configurados: estos vienen de la base y deben comprobarse en Unity y el dispositivo. El shader incorpora macros de estéreo pero **no se ha compilado ni validado en URP o Quest**. No hay soporte HDMI ni iluminación avanzada. La ubicación guardada es relativa al origen de seguimiento, no un ancla persistente. Se elimina la función de inicialización de la entrega anterior para no crear objetos duplicados.

Controles propuestos: joystick izquierdo mueve; con grip izquierdo cambia tamaño/curvatura. Joystick derecho acerca/aleja; con grip derecho gira. A pausa/reanuda; B recentra; X muestra/oculta instrucciones. Estas asignaciones y el panel necesitan prueba en Quest. El video se copia al almacenamiento local en el primer arranque.

## Comprobaciones efectuadas

El ZIP y el video se verificaron con herramientas de archivo y ffprobe. `tools/validate.py` comprueba estructura y marcadores estáticos, y `bash -n` revisa la sintaxis del script de preparación. Esas comprobaciones no compilan C#, no evalúan la corrección del shader ni demuestran que XR, controles o reproducción funcionen. La ejecución de Unity puede revelar errores que haya que corregir.

## BLOQUEOS REALES

- Descargar el proyecto de Unity, dependencias y herramienta de compilación desde un entorno con acceso a Internet.
- Disponer de un editor Unity y soporte Android compatibles, o de GitHub Actions con la licencia y credenciales de Unity configuradas, y ejecutar la compilación.
- Resolver cualquier error que revele la primera compilación y probar la APK en un Quest 3 para verificar passthrough y controles.

## Fuentes

- https://github.com/Unity-Technologies/mr-example-meta-openxr/releases/tag/v1.0.0-pre.1
- https://docs.unity.cn/Packages/com.unity.xr.meta-openxr@1.0/manual/features/camera.html
- https://game.ci/docs/github/builder/
