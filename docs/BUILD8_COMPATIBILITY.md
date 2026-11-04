# Build #8: editor y compatibilidad para el siguiente intento

## Causa confirmada

El build #8 usó el commit `01329bdab231e528f46c0125c07c70e6b962cccb` de `repair/spatial-player-hands`. El registro identifica Unity **2022.3.62f3 (96770f904ca7)** y termina al ejecutar `SpatialBuild.PrepareScene`: nuestra validación exigía exactamente **2022.3.8f1**, el editor original del ejemplo, y abortó antes de revisar la escena o exportar Android.

El registro no contiene errores `CS`. Llegar a ejecutar este método demuestra que la versión anterior de los scripts se compiló/cargó para el editor. No demuestra que el shader GLES, Java, IL2CPP, la exportación Android o el comportamiento en Quest funcionen. La corrección actual no se ha ejecutado aún dentro de Unity.

## Corrección

- `Assets/Editor/SpatialBuildCompatibility.json` es la única política compartida por el pre-export y la preparación del proyecto.
- Se admiten expresamente `2022.3.8f1` y `2022.3.62f3`. El editor preferido es `2022.3.62f3`. No se permite cualquier parche, preview, versión menor ni Unity 6.
- Ambos scripts de ensamblado ajustan **solo** `Base/ProjectSettings/ProjectVersion.txt` al editor y changeset del target existente. Se mantiene el SHA del upstream. No se modifica la configuración de Unity Cloud, su licencia ni sus servicios.
- Se conservan las comprobaciones de dependencias del manifiesto fuente, URP, Android, OpenXR, manos, passthrough, Touch, media, escena, referencias, shaders y acciones de UI.
- La preparación rechaza metadatos de un editor inesperado. El preflight rechaza metadatos del upstream que no hayan sido alineados o un changeset incorrecto.
- No se cambia el manifiesto ni el lock de paquetes para forzar una actualización o degradación. El versionCode Android sigue siendo 8; el número de ejecución #9 de UBA es independiente.

## Compatibilidad real y paquetes resueltos

2022.3.62f3 pertenece a la misma línea Unity 2022.3 LTS. Sus [notas oficiales](https://unity.com/releases/editor/whats-new/2022.3.62f3) identifican el changeset `96770f904ca7`. [URP 14](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/requirements.html) admite la familia Unity 2022 y es un paquete core asociado al editor; no corresponde tratar cada parche URP como si siguiera siendo el del ejemplo original.

El log del build #8 muestra esta diferencia, que antes no estaba documentada:

| Paquete | Manifiesto fuente | Resuelto por Unity en #8 |
|---|---|---|
| URP | 14.0.8 | 14.0.12 |
| OpenXR | 1.8.2 | 1.14.3 |
| Unity OpenXR Meta | 0.2.1 | 1.0.4 |
| XR Hands | 1.3.0 | 1.4.0 |
| XRI | 2.5.1 | 2.6.4 |
| AR Foundation | 5.1.0-pre.10 | 5.2.0 |
| Input System | 1.7.0 | 1.14.0 |
| XR Management | 4.4.0 | 4.4.0 |

Se descargaron los paquetes oficiales resueltos de OpenXR, Meta OpenXR, Hands, XRI, AR Foundation e Input System y se inspeccionaron sus requisitos y fuentes. Meta OpenXR 1.0.4 declara Unity 2022.3.0f1 como mínimo; los restantes declaran 2021.3. Las APIs usadas por el proyecto siguen presentes, incluidas `ARCameraFeature`, `ARSessionFeature`, `MetaAimHand`, los interactores y el transformer de dos manos.

Se cotejaron los GUID de scripts usados por los assets con los paquetes nuevos. Se conservan los GUID de runtime de mano, OpenXR y Meta, además de los interactores del rig. Un script de demostración antiguo de XRI ya no aparece en el paquete nuevo, pero su copia importada con su `.meta` se conserva en los assets del upstream; no se elimina ni se reemplaza el rig importado de XRI 2.5.1.

La conclusión es **compatibilidad de requisitos, APIs y referencias revisada**, apoyada por la ejecución C# observada en #8. No se transforma esa evidencia en una afirmación de exportación Android o funcionamiento en hardware.

## Otros posibles bloqueos revisados

- La ruta `Assets/StreamingAssets/demo.mp4` y el MP4 H.264/AAC siguen correctos.
- Los features serializados de Android siguen habilitados y sus GUID corresponden a las clases del stack de Cloud.
- Se conservan el perfil Touch, el rig, el módulo UI y las acciones de input importadas.
- Las APIs de manos, grab y escalado utilizadas siguen existiendo en los paquetes resueltos.
- El plugin Android local usa APIs disponibles antes del minSdk 29 y evita dependencia de clases Unity en su compilación Java mediante reflexión. Su compilación Gradle y apertura del selector requieren todavía el build Android.
- No se encontraron errores C# adicionales en el log. Las invariantes posteriores a la versión y las variantes de shader para Android no se ejecutaron en #8, por lo que no se declaran aprobadas.

## Verificación sin una nueva ejecución de Unity Cloud

Se añadieron nueve pruebas de política/preparación de editor y dos sobre los metadatos ensamblados, además de las trece regresiones existentes. Cubren aceptación de 2022.3.62f3, alineación desde el upstream, changeset, idempotencia, rechazo de editores no revisados, ausencia de política y preservación de manifiesto/lock.

El workflow de PR ejecuta el análisis C#/Java, shell, contratos de fuentes, decodificación de media, ensamblado mediante el hook UBA y las pruebas del proyecto preparado. No lanza Unity ni genera una APK. Para el build #9 manual se debe utilizar el commit final de esta misma rama, manteniendo el target y el método pre-export existentes.
