# STRONGBODY — sitio web del grupo de CrossFit al aire libre

Sitio web de **STRONGBODY**, grupo deportivo de CrossFit que entrena al aire libre, manejado por dos profesores que dictan las clases indistintamente. Automatiza lo que hoy se lleva a mano: inventario de herramientas, registro de clientes, control de pagos y objetivos. Tiene una parte pública (inicio, grupos, planes) y un panel de administración protegido por sesión. Trabajo académico de Ingeniería Informática.

> Este README es **bitácora viva del proyecto**. Cada iteración suma una sección al final con lo que se hizo, las decisiones tomadas, problemas encontrados y pendientes. Está pensado para que cualquiera (incluyendo Claude en sesiones futuras) pueda recuperar contexto rápido.

## Arquitectura general

```
Navegador
   │
   │  Blazor Web App · render mode InteractiveServer global
   ▼
Components/
   ├── Layout/         SitioLayout (público)  ·  PanelLayout (administración)
   ├── Pages/Publico/  4 vistas abiertas      ·  Pages/Panel/  8 vistas con sesión
   └── Shared/         Modal · TarjetaClase · TablaClientes
   │
   │  Las vistas NO deciden nada: piden al servicio y muestran la respuesta.
   ▼
Services/   ReservaService · ClienteService · InventarioService · SesionService
   │            (reglas de negocio: cupo máximo, unicidad de documento, …)
   ▼
DatosService  ← almacén en memoria, singleton, con datos de ejemplo precargados
   │
   ▼
Models/     Grupo · Horario · Reserva · Cliente · Plan · Suscripcion ·
            ControlMensual · SolicitudPlan · Herramienta · Usuario · Notificacion
```

Sin base de datos por ahora: `DatosService` es el almacén. La lógica vive en los servicios y no en las vistas, precisamente para poder migrar a EF Core más adelante sin tocar los componentes.

## Stack técnico

- **.NET 10** (`net10.0`) — plantilla Blazor Web App
- **Render mode:** `InteractiveServer` **global**, declarado en `Components/App.razor` sobre `<Routes>` y `<HeadOutlet>`. Sin esto los `@onclick` no responden.
- **Persistencia:** ninguna. Todo en memoria en un singleton (`DatosService`).
- **CSS:** propio, escrito a mano en `wwwroot/css/app.css`. Sin Bootstrap ni Tailwind.
- **Tipografía:** Archivo (Google Fonts), **versión variable** con ejes de anchura (62–125), grosor (400–900) y cursiva. El eje de anchura es lo que da los titulares condensados de las secciones oscuras, sin traer una segunda familia.
- **Sin librerías externas** más allá de lo que trae .NET (requisito académico).

## Cómo correrlo

```bash
cd AireLibre
dotnet run
```

Abre <http://localhost:5062>.

**Desde Visual Studio Community** (probado con la 18): abrir `AireLibre.sln` en la raíz y pulsar **F5**. Abre el navegador solo.

**Desde Visual Studio Code:** abrir la carpeta `c:\crossfit_ppi` (necesita la extensión *C# Dev Kit*) y pulsar **F5**, o usar `dotnet run` en la terminal integrada.

**Nota sobre el archivo de solución:** está en formato **clásico `.sln`** a propósito. `dotnet new sln` del SDK 10 genera el formato nuevo `.slnx`, que solo abren Visual Studio 17.13 y posteriores; el clásico abre en cualquier versión, incluida Visual Studio 2019. Para un trabajo que otra persona va a abrir en su máquina, eso importa más que usar el formato nuevo.

## Estructura del proyecto

Todo archivo tiene un comentario de cabecera que explica qué hace, cómo se usa y qué decisiones no obvias hay detrás. Para entender el proyecto no hace falta leer código: basta con las primeras líneas de cada archivo.

```
crossfit_ppi/
├── AireLibre.sln                    Solución, formato clásico (F5 en Visual Studio)
├── AireLibre.Api/                   API REST + Swagger (Iteración 9). Enlaza Models/ y Services/
├── README.md                        Esta bitácora
└── AireLibre/
    ├── Program.cs                   Arranque y registro de servicios
    │
    ├── Models/                      Los datos. Clases sin comportamiento.
    │   ├── Enumeraciones.cs         Las 6 listas cerradas + los textos para mostrarlas
    │   ├── Grupo.cs  Horario.cs     Grupos (mañana/noche) y sus horas con cupo
    │   ├── Reserva.cs               Un cupo separado. ClienteId es OPCIONAL
    │   ├── Cliente.cs               Ficha de una persona. Calcula el IMC
    │   ├── Plan.cs  Suscripcion.cs  Modalidad de pago y estado "al día"
    │   ├── ControlMensual.cs        Peso y avance, un registro por medición
    │   ├── SolicitudPlan.cs         Interesado que llega de /planes
    │   ├── Herramienta.cs           Un TIPO de implemento, no una unidad
    │   ├── Usuario.cs               Profesor con acceso al panel
    │   └── Notificacion.cs          Aviso para el profesor
    │
    ├── Services/                    La lógica. Aquí viven TODAS las reglas.
    │   ├── Resultado.cs             Lo que devuelve toda operación: Exito + Mensaje
    │   ├── DatosService.cs          El almacén (singleton). Hace de base de datos
    │   ├── ReservaService.cs        ★ El límite de 20 cupos, con candado
    │   ├── ClienteService.cs        Solicitudes, CRUD de clientes, control mensual, pagos
    │   ├── InventarioService.cs     CRUD de herramientas
    │   └── SesionService.cs         Autenticación (Scoped, uno por persona)
    │
    ├── Components/
    │   ├── App.razor                El documento HTML raíz
    │   ├── Routes.razor             Enrutador
    │   ├── _Imports.razor           Los @using que comparten todos los .razor
    │   │
    │   ├── Layout/
    │   │   ├── SitioLayout.razor    Público: cabecera, menú y pie
    │   │   └── PanelLayout.razor    Panel: barra lateral y sesión
    │   │
    │   ├── Shared/                  Componentes reutilizables
    │   │   ├── Modal.razor          Ventana emergente
    │   │   ├── TarjetaClase.razor   ★ La tarjeta con el medidor de 20 cupos
    │   │   └── TablaClientes.razor  La tabla de clientes con sus acciones
    │   │
    │   └── Pages/                   Las 12 vistas, separadas por quién las ve
    │       ├── Error.razor  NotFound.razor
    │       │
    │       ├── Publico/             Sin iniciar sesión
    │       │   ├── Inicio.razor     1 · /
    │       │   ├── Grupos.razor     2 · /grupos      ← aquí se separa el cupo
    │       │   ├── Planes.razor     3 · /planes
    │       │   └── Login.razor      4 · /login
    │       │
    │       └── Panel/               Requieren sesión
    │           ├── PaginaPanel.cs   ★ La clase base que protege las 8 de abajo
    │           ├── Resumen.razor        5 · /panel
    │           ├── Clases.razor         6 · /panel/clases
    │           ├── Clientes.razor       7 · /panel/clientes
    │           ├── DetalleCliente.razor 8 · /panel/clientes/{id}
    │           ├── Pagos.razor          9 · /panel/pagos
    │           ├── Inventario.razor    10 · /panel/inventario
    │           ├── Solicitudes.razor   11 · /panel/solicitudes
    │           └── Notificaciones.razor 12 · /panel/notificaciones
    │
    ├── wwwroot/css/app.css          Todo el diseño, en 8 bloques numerados
    └── wwwroot/img/                 Fotos del sitio (ver su LEEME.txt)
```

**La regla que ordena todo el proyecto:** las vistas **no deciden nada**. No validan, no comprueban duplicados, no calculan reglas. Le piden al servicio y muestran lo que responde. Todas las reglas de negocio están en `Services/`, en un solo lugar cada una. Por eso se puede cambiar el almacén por EF Core sin tocar un solo `.razor`.

## Decisiones de arquitectura clave

| Decisión | Razón |
|---|---|
| `net10.0` en vez de `net9.0` | La máquina tiene solo SDK 10 (runtimes 8 y 10, sin runtime 9). El código Blazor es idéntico para lo que pide el proyecto. Decisión de Camila Loaiza, 2026-09-06. Volver a 9 es cambiar una línea del `.csproj` + instalar el runtime. |
| `InteractiveServer` global, no por componente | Requisito explícito del enunciado y evita el error clásico de botones que no responden por olvidar el `@rendermode` en una página. |
| Lógica en `Services/`, no en las vistas | Permite migrar a EF Core después cambiando solo `DatosService` sin tocar los `.razor`. |
| Almacén en memoria como singleton | El estado debe ser compartido entre todos los usuarios y sobrevivir a la navegación. Un scoped se reiniciaría por circuito. |
| Sin backend real: ni base de datos, ni API, ni EF Core | Confirmado por Camila Loaiza el 2026-09-06 ("esto debe ser solamente front"). Todo se resuelve en memoria dentro del propio servidor de Blazor. |
| Ids con contador ascendente, no `Max(id) + 1` | `Max + 1` reutiliza el id del último registro borrado: un cliente nuevo heredaría las suscripciones y los controles mensuales del anterior. |
| Fechas de ejemplo relativas a `DateTime.Today` | Los contadores de "cupos separados hoy" y los avisos de "pagan mañana" quedan correctos sin importar el día en que se ejecute el proyecto. |
| Candado (`lock`) en las operaciones de leer-validar-escribir | El almacén es singleton y Blazor Server atiende varios usuarios a la vez. Sin candado, dos personas reservando en el mismo instante podrían pasarse del límite de 20. |
| Validación del cupo máximo en el servicio, no en la pantalla | Una validación solo en la UI se salta con dos pestañas abiertas. El servicio cuenta las reservas antes de insertar. |
| Autenticación propia sin ASP.NET Core Identity | Requisito del enunciado: dos usuarios quemados, sin base de datos, código explicable ante un profesor. |
| `SesionService` es Scoped, obligatoriamente | Como singleton habría **una sola sesión para toda la aplicación**: apenas un profesor entrara, cualquiera desde otro computador estaría dentro del panel. Es un agujero, no un detalle. |
| El control de sesión vive en la clase base `PaginaPanel` | Son 8 páginas protegidas. Copiada 8 veces, basta olvidarla en una para dejar el panel abierto por esa puerta. |
| Prerrenderizado ACTIVADO, con salida temprana en `PaginaPanel` | Desactivarlo en todo el sitio arreglaba el login pero costaba el primer pintado de las páginas públicas. `RendererInfo.IsInteractive` resuelve solo donde está el problema. |
| Un solo tipo `Resultado` para todos los servicios | Antes cada servicio tenía el suyo (`ResultadoReserva`, `ResultadoSolicitud`, `ResultadoSesion`) con los mismos dos campos: tres nombres para la misma idea. |
| `Pages/` dividido en `Publico/` y `Panel/` | Con las 12 vistas en una sola carpeta no se distinguía de un vistazo qué exige sesión y qué no. Las rutas salen del `@page`, así que mover los archivos no cambia ninguna dirección. |
| Los formularios de edición trabajan sobre una **copia** (`Copia()`) | Editando el objeto guardado, cada tecla cambiaría el dato en vivo y "Cancelar" no desharía nada. |
| Al eliminar un cliente, sus reservas **no** se borran | Esa persona sí ocupó el cupo ese día; borrarlas falsearía el historial. Se suelta el vínculo (`ClienteId = null`) y la reserva conserva el nombre como texto. |
| Los avisos de pago se **calculan**, los de cupo se **guardan** | Un aviso de pago guardado ayer diría hoy "vence mañana" con el plazo ya vencido. Un cupo separado, en cambio, es un hecho fijo. |
| CSS propio sin frameworks | Requisito del enunciado. Un solo archivo con variables de diseño en `:root`. |
| Clases de estado nombradas por significado, no por color | Al cambiar la paleta a rojo/blanco/negro, las clases `--verde` y `--naranja` quedaron pintando de rojo: el nombre mentía. Con `--ok`, `--alerta`, `--noche`, `--neutro`, cambiar la paleta es editar el bloque 1 de `app.css` y nada más. |

## Iteraciones planificadas

| # | Objetivo | Estado |
|---|---|---|
| 1 | Proyecto creado, CSS base funcionando, compila y corre | ✅ Completada |
| 2 | Modelos y `DatosService` con datos de ejemplo | ✅ Completada |
| 3 | Layout público + vistas 1 (`/`), 2 (`/grupos`) y 3 (`/planes`) | ✅ Completada |
| 4 | Login (`/login`) y layout del panel | ✅ Completada |
| 5 | Vistas del panel (vistas 5 a 12) + reorganización de carpetas | ✅ Completada |
| 6 | Rediseño del Inicio como ficha de sede de gimnasio | ✅ Completada |
| 7 | "¿Quiénes somos?" y secciones oscuras de impacto | ✅ Completada |
| 7.1 | Fotografías reales | ✅ Completada |
| 7.2 | Fotos de objetivos y saneamiento de archivos | ✅ Completada |
| 8 | Marca STRONGBODY y paleta rojo, blanco y negro | ✅ Completada |
| 9 | API REST con Swagger en un proyecto aparte (`AireLibre.Api`) | ✅ Completada |

## Bitácora de iteraciones

### Iteración 1 — Proyecto base y sistema de diseño

| | |
|---|---|
| **Fecha** | 2026-09-06 |
| **Estado** | ✅ Completada — compila y corre verificado |
| **Objetivo** | Dejar el esqueleto del proyecto en pie con el CSS propio funcionando, para que las fases siguientes solo agreguen modelos y vistas sin pelear con la infraestructura. |

#### Qué se hizo

- Proyecto creado con `dotnet new blazor -o AireLibre --interactivity Server --all-interactive --framework net10.0 --no-https`. El `--all-interactive` es lo que deja el render mode `InteractiveServer` global.
- **Limpieza de la plantilla:** se eliminó `wwwroot/lib/bootstrap/` completo, `Counter.razor`, `Weather.razor`, `NavMenu.razor(.css)` y `MainLayout.razor.css`. Verificado en el HTML servido: cero menciones a Bootstrap.
- [App.razor](AireLibre/Components/App.razor) — `lang="es"`, carga de Archivo desde Google Fonts con los cuatro pesos, y enlace a la hoja propia. El `@rendermode="InteractiveServer"` queda documentado ahí mismo con un comentario, porque es el punto donde se rompe todo si alguien lo quita.
- [wwwroot/css/app.css](AireLibre/wwwroot/css/app.css) — sistema de diseño completo (~600 líneas, comentado en español y dividido en 7 bloques numerados): variables de paleta, reinicio, tipografía, estructura del sitio público y del panel, componentes (tarjeta, botón, formulario, tabla, modal, distintivo, aviso, estado vacío), el medidor de cupos, y responsive + accesibilidad.
- [MainLayout.razor](AireLibre/Components/Layout/MainLayout.razor) reducido a layout de respaldo (solo para error y 404); los layouts reales llegan en las fases 3 y 4.
- [Error.razor](AireLibre/Components/Pages/Error.razor) y [NotFound.razor](AireLibre/Components/Pages/NotFound.razor) traducidos al español y pasados al estilo del sitio.
- [Home.razor](AireLibre/Components/Pages/Home.razor) — **provisional**: página de verificación que muestra paleta, tipografía, tarjetas mañana/noche, el medidor de cupos y un contador con botón. Se reemplaza por el Inicio real en la Iteración 3.
- [.gitignore](.gitignore) creado con el bloque mínimo del equipo TI + descartables de .NET (`bin/`, `obj/`, `.vs/`).

#### Decisiones tomadas (con el usuario)

- **`net10.0` en vez del `net9.0` que pedía el enunciado** — decisión de Camila Loaiza. La máquina tiene solo SDK 10 y no tiene runtime de .NET 9, así que targetear 9 exigía descargar e instalar el runtime. Ver la tabla de decisiones de arquitectura.
- **Verde = mañana, azul noche = noche como código semántico**, no como decoración: las clases se llaman `--manana` / `--noche` y significan lo mismo en las 12 vistas. Sale del propio dominio (el enunciado ya asigna esos colores a esos grupos).
- **Cifras tabulares** (`font-variant-numeric: tabular-nums`) en tablas, contadores y cupos, para que las columnas de números queden alineadas.
- **El medidor de cupos se trató como conteo literal, no como barra de progreso.** 20 marcas, una por cupo, contables con el dedo. Cuando el grupo se llena las marcas pasan a naranja de alerta.

#### Verificación end-to-end

- `dotnet build` → **Compilación correcta, 0 advertencias, 0 errores.**
- `dotnet run` → escucha en `http://localhost:5062`, `GET /` responde **HTTP 200**.
- `css/app.css` se sirve con **HTTP 200** (19.739 bytes) bajo su nombre con huella.
- HTML servido: `<html lang="es">`, `blazor.web.js` presente, **0** menciones de Bootstrap, las **40** marcas de cupo renderizadas (23 ocupadas, que es 7 + 16 según el ejemplo).
- ⏳ **Falta verificación visual del usuario**: que la tipografía Archivo se vea, y que el botón "Sumar uno" incremente el contador sin recargar (prueba real de que `InteractiveServer` responde).

#### ⏳ Diferido — NO perder

- ⏳ **Migración a EF Core** — el enunciado la anuncia como trabajo posterior. Por eso toda la lógica va en `Services/` y el almacén queda aislado en `DatosService`. No se hace ahora; se retoma cuando el usuario lo pida.
- ⏳ **Volver a `net9.0`** — solo si el profesor exige la versión 9 exacta. Requiere instalar el runtime de .NET 9 y cambiar `<TargetFramework>` en `AireLibre.csproj`. Nada más del código depende de la versión.
- ⏳ **Imágenes reales** — el enunciado pide bloques de color con degradado como marcador de posición (clase `.imagen-marcador`). Reemplazarlas por fotos reales queda por fuera de este trabajo.
- ⏳ **`git init` y primer commit** — la carpeta todavía no es un repositorio git. El `.gitignore` ya está listo para cuando el usuario lo inicialice. Los commits los hace el desarrollador, no Claude (regla de equipo).

#### Tests

Sin suite de pruebas automatizadas. El enunciado no las pide y no se agregaron librerías externas. La verificación es la de arriba: build limpio + comprobaciones sobre el HTML servido.

---

### Iteración 2 — Modelos del dominio y almacén en memoria

| | |
|---|---|
| **Fecha** | 2026-09-06 |
| **Estado** | ✅ Completada — compila y datos verificados sobre el HTML servido |
| **Objetivo** | Dejar modelado todo el vocabulario del negocio y cargado el almacén con datos de ejemplo realistas, para que las vistas de las fases siguientes solo tengan que leer y pintar. |

#### Qué se hizo

**11 modelos, uno por archivo**, todos con docstring que explica qué representan y por qué están así:

- [Grupo.cs](AireLibre/Models/Grupo.cs) · [Horario.cs](AireLibre/Models/Horario.cs) — separados a propósito: el grupo es lo que el cliente reconoce y lo que da el color; el cupo de 20 se cuenta por horario. `Horario.HoraTexto` arma el formato `8:30 am` a mano para no depender de la cultura del servidor.
- [Reserva.cs](AireLibre/Models/Reserva.cs) — `NombreCompleto` como texto y `ClienteId` **nullable**, porque se reserva sin registrarse. Lleva `FechaClase` además del horario: sin la fecha no se podría contar "cupos separados hoy".
- [Cliente.cs](AireLibre/Models/Cliente.cs) — con `Imc` calculado (no guardado) a partir de estatura y peso.
- [Plan.cs](AireLibre/Models/Plan.cs) · [Suscripcion.cs](AireLibre/Models/Suscripcion.cs) — `AlDia` es una marca manual del profesor, no el resultado de una transacción. `FechaProximoPago` es lo que dispara los avisos.
- [ControlMensual.cs](AireLibre/Models/ControlMensual.cs) — histórico de peso y avance. Documenta por qué no se sobrescribe `Cliente.PesoKg`: se perdería la evolución, que es justo el punto de la vista de detalle.
- [SolicitudPlan.cs](AireLibre/Models/SolicitudPlan.cs) · [Herramienta.cs](AireLibre/Models/Herramienta.cs) · [Usuario.cs](AireLibre/Models/Usuario.cs) · [Notificacion.cs](AireLibre/Models/Notificacion.cs).
- [Enumeraciones.cs](AireLibre/Models/Enumeraciones.cs) — las 6 enumeraciones juntas, más `TextosDominio`: métodos de extensión `.Texto()`, `.TextoCorto()`, `.ClaseCss()` y `.ClaseDistintivo()`. Resueltos con expresiones `switch` en vez de atributos y reflexión, para que no haya nada oculto que explicar.

**[DatosService.cs](AireLibre/Services/DatosService.cs)** — el almacén, registrado como singleton en [Program.cs](AireLibre/Program.cs):

- 11 listas (una por entidad), 7 contadores de id, 6 búsquedas por id y `DescribirHorario()` que devuelve `"Grupo de la noche · 7:00 pm"` para el panel y los avisos.
- Propiedad `Bloqueo` para las operaciones de leer-validar-escribir.
- Datos de ejemplo: 2 grupos, 3 horarios, 3 planes, 2 usuarios, 8 clientes, 8 suscripciones, 12 controles mensuales, **42 reservas de hoy**, 4 solicitudes, 6 herramientas y 3 notificaciones.

[Program.cs](AireLibre/Program.cs) reescrito con docstring que advierte las dos cosas que no se deben tocar: el par `AddInteractiveServerComponents()` / `AddInteractiveServerRenderMode()` y el `AddSingleton<DatosService>()`.

[Home.razor](AireLibre/Components/Pages/Home.razor) sigue siendo **provisional**: se convirtió en banco de pruebas del almacén (conteos, tabla de clientes, inventario, avisos y el medidor de cupos de los tres horarios). Se reemplaza por el Inicio real en la Iteración 3.

#### Decisiones tomadas (con el usuario)

- **"Solamente front"** — Camila Loaiza aclaró el alcance: se sigue con Blazor tal como pide el enunciado, sin base de datos, sin API y sin EF Core. Todo en memoria. La Fase 1 se conserva.
- **Ids con contador ascendente y no `Max(id) + 1`** — ver la tabla de decisiones de arquitectura. Es el tipo de detalle que un profesor puede preguntar.
- **Fechas de ejemplo siempre relativas a hoy**, nunca escritas a mano.
- **El horario de las 8:00 pm arranca con 19 de 20 cupos ocupados**, a propósito: permite probar el tope de 20 con una sola reserva en vez de veinte.
- **`Plan.Precio` es informativo** — se agregó para que la vista pública de planes tenga qué mostrar. El enunciado no lo pedía explícitamente; queda marcado por si se decide quitarlo. El sitio no procesa dinero.

#### Verificación end-to-end

`dotnet build` → **0 advertencias, 0 errores**. Con la aplicación corriendo, leyendo el HTML realmente servido en `/`:

| Comprobación | Esperado | Obtenido |
|---|---|---|
| Grupos / horarios / planes / usuarios | 2 / 3 / 3 / 2 | ✅ 2 / 3 / 3 / 2 |
| Clientes | 8 | ✅ 8 |
| Cupos separados hoy | 42 | ✅ 42 |
| Reservas enlazadas a un cliente registrado | 8 | ✅ 8 |
| Controles mensuales | 12 | ✅ 12 |
| Herramientas | 6 | ✅ 6 |
| Pagos pendientes / solicitudes sin atender / pagan mañana | 3 / 3 / 3 | ✅ 3 / 3 / 3 |
| Formato de hora | `8:30 am`, `7:00 pm`, `8:00 pm` | ✅ los tres |
| Cupos libres por horario | 13 / 4 / 1 | ✅ 13 / 4 / 1 |
| Documentos | 6 C.C. + 2 T.I. | ✅ 6 + 2 |
| Distintivos de inventario | 3 Bueno, 2 Regular, 1 Para reponer | ✅ 3 / 2 / 1 |
| `DescribirHorario()` en los avisos | "Grupo de la noche · 8:00 pm" | ✅ |
| IMC calculado | valores coherentes (20,9 – 27,9) | ✅ |

Nota: en el HTML crudo las tildes salen como entidades (`Definici&#xF3;n`). Es la codificación normal de Razor para caracteres no ASCII; el navegador las muestra bien. No es un error.

#### ⏳ Diferido — NO perder

- ⏳ **`Plan.Precio`** — agregado por iniciativa propia para que `/planes` tenga qué mostrar. Si el profesor objeta que el enunciado dice que no hay transacciones de dinero, se quita el campo y la columna; nada más depende de él.
- ⏳ **Los servicios de negocio** (`ReservaService`, `ClienteService`, `InventarioService`, `SesionService`) todavía no existen. Se crean en la fase donde los necesita su vista: `ReservaService` en la Iteración 3, `SesionService` en la 4, los otros dos en la 5. El enunciado pedía explícitamente que la Fase 2 fuera solo modelos y `DatosService`.
- ⏳ **`Home.razor` provisional** — hoy es banco de pruebas del almacén, no la página de inicio. Se reemplaza en la Iteración 3.

#### Tests

Sin suite automatizada (el enunciado no la pide y no se agregaron librerías). La verificación es la tabla de arriba, hecha contra el HTML realmente servido por la aplicación.

---

### Iteración 3 — Sitio público y separación de cupos

| | |
|---|---|
| **Fecha** | 2026-09-06 |
| **Estado** | ✅ Completada — 33/33 comprobaciones de negocio en verde |
| **Objetivo** | Dejar las tres vistas públicas funcionando y, sobre todo, que separar un cupo funcione de verdad con el límite de 20 validado en el servicio. |

#### Qué se hizo

**Servicios de negocio (2 nuevos):**

- [ReservaService.cs](AireLibre/Services/ReservaService.cs) — el único lugar autorizado para crear una reserva. `SepararCupo()` hace **contar → comparar → insertar** dentro de `lock (datos.Bloqueo)`, y genera la notificación para el profesor en el mismo candado para que no pueda quedar una reserva sin su aviso. El encabezado del archivo explica la carrera concreta que evita el candado.
- [ClienteService.cs](AireLibre/Services/ClienteService.cs) — por ahora solo `RegistrarSolicitud()`, que es lo que necesita `/planes`. El CRUD de clientes llega en la Iteración 5.

**Componentes compartidos (2 nuevos):**

- [Modal.razor](AireLibre/Components/Shared/Modal.razor) — ventana reutilizable. Se cierra con la X, con clic en el fondo y con Escape; recibe el foco al abrirse.
- [TarjetaClase.razor](AireLibre/Components/Shared/TarjetaClase.razor) — la tarjeta de un horario con el medidor de 20 marcas. No calcula nada del negocio: recibe los ocupados ya contados.

**Layout y vistas:**

- [SitioLayout.razor](AireLibre/Components/Layout/SitioLayout.razor) — cabecera con menú y pie con teléfono e íconos de Instagram, Facebook y WhatsApp (SVG en línea, sin librerías), los tres con `target="_blank" rel="noopener noreferrer"`. Datos de contacto agrupados como constantes al final del archivo.
- [Inicio.razor](AireLibre/Components/Pages/Inicio.razor) (`/`) — portada, bloques de imagen con degradado, misión y visión.
- [Grupos.razor](AireLibre/Components/Pages/Grupos.razor) (`/grupos`) — los tres horarios agrupados por jornada, con modal para separar cupo.
- [Planes.razor](AireLibre/Components/Pages/Planes.razor) (`/planes`) — los tres planes y el formulario de solicitud.
- [Routes.razor](AireLibre/Components/Routes.razor) — `DefaultLayout` pasa a `SitioLayout`.
- [Error.razor](AireLibre/Components/Pages/Error.razor) y [NotFound.razor](AireLibre/Components/Pages/NotFound.razor) adaptados al layout público.
- Eliminados: `Home.razor` (era provisional) y `MainLayout.razor` (ya no lo usa nadie).
- [_Imports.razor](AireLibre/Components/_Imports.razor) — se agregaron los `@using` de `Models`, `Services` y `Components.Shared`.
- **`AireLibre.sln`** creado en la raíz, para poder abrir el proyecto con F5 desde Visual Studio Community.

#### Decisiones tomadas (con el usuario)

- **"Solamente front"** — Camila Loaiza lo reiteró. Los dos servicios nuevos no son backend: son clases C# que corren en el mismo proceso de Blazor. Sigue sin haber base de datos, API ni EF Core.
- **Los cupos ocupados no se guardan en un campo**, se cuentan en cada render. Así el número nunca se desincroniza: si se borra una reserva, el cupo se libera solo.
- **La vista deshabilita el botón, pero no valida.** La regla de los 20 está solo en el servicio. Deshabilitar el botón es comodidad para la persona, no la defensa.
- **El teléfono y las URLs de redes son de ejemplo**, agrupados como constantes al final de `SitioLayout.razor` para que cambiarlos sea editar un solo bloque.

#### Problemas encontrados y solución

- **`RZ9996: Unrecognized child content inside component 'Modal'`.** Al usar el fragmento con nombre `<Pie>`, Razor exige que el cuerpo TAMBIÉN vaya en un fragmento con nombre. Se envolvió el cuerpo en `<ChildContent>`. Queda documentado dentro de [Modal.razor](AireLibre/Components/Shared/Modal.razor) para no repetirlo en las fases siguientes.
- **Enlace `tel:` roto.** Escrito como `href="tel:+57@TelefonoSinEspacios"`, Razor interpretó `57@TelefonoSinEspacios` como una dirección de correo y la dejó literal: el href salía con el nombre de la variable dentro. Se armó el href completo en una propiedad (`EnlaceTelefono`) y se dejó el comentario explicando el porqué. **Lo detectó la verificación del HTML servido, no el compilador** — compilaba sin una sola advertencia.

#### Verificación end-to-end

`dotnet build` → **0 advertencias, 0 errores**.

Rutas servidas: `/` → 200 · `/grupos` → 200 · `/planes` → 200 · ruta inexistente → 404.
Pie: `tel:+573001234567` correcto, 3 enlaces con `target="_blank" rel="noopener noreferrer"`, las 3 URLs de redes presentes.
Medidores: 13 / 4 / 1 cupos libres, con el singular correcto ("1 cupo libre").

**Reglas de negocio ejecutadas directamente contra los servicios** (proyecto de consola desechable en el scratchpad, con referencia al proyecto real; borrado después): **33 comprobaciones, 33 en verde.**

| Bloque | Qué se probó |
|---|---|
| Reserva normal | acepta y el contador sube |
| Validación del nombre | rechaza vacío, solo espacios y muy corto; recorta espacios sobrantes |
| Relación opcional con cliente | enlaza si el nombre coincide, deja `ClienteId` nulo si no |
| **Límite de 20** | acepta la persona 20, avisa "quedó completa", **rechaza la 21**, nunca pasa de 20 |
| **Concurrencia** | **40 hilos compitiendo por 20 cupos → exactamente 20 aceptados**, sin ids repetidos |
| Notificación automática | se genera, dice quién reservó y para cuál horario |
| Solicitudes de plan | rechaza nombre vacío, documento con letras, documento corto, teléfono corto, plan inexistente, documento duplicado y documento de alguien que ya es cliente |

⏳ **Falta la verificación visual del usuario**: abrir `/grupos`, separar un cupo en el horario de las 8:00 pm (que arranca en 19/20) y confirmar que al llenarse aparece el mensaje y el botón queda deshabilitado.

#### ⏳ Diferido — NO perder

- ⏳ **Datos de contacto reales** — teléfono `300 123 4567` y las URLs de Instagram, Facebook y WhatsApp son de ejemplo. Están juntos al final de [SitioLayout.razor](AireLibre/Components/Layout/SitioLayout.razor).
- ⏳ **Reservas solo para hoy** — no se pueden separar cupos para días futuros. El enunciado no lo pide; si se necesita, hay que agregar un selector de fecha en `/grupos` (el modelo ya guarda `FechaClase`, así que el cambio es de vista, no de datos).
- ⏳ **No se controla que la misma persona reserve dos veces** el mismo horario. El enunciado no lo pide (la regla de unicidad del documento es para clientes, y las reservas no piden documento). Si se quisiera, iría en `ReservaService.SepararCupo`.
- ⏳ **`ClienteService` está a medias** — solo tiene `RegistrarSolicitud()`. El CRUD de clientes, el control mensual y la conversión de solicitud en cliente se agregan en la Iteración 5.
- ⏳ **Faltan `InventarioService` y `SesionService`** — Iteraciones 4 y 5.

#### Tests

Sin suite automatizada permanente (el enunciado no la pide y no se agregaron librerías de testing). La verificación fue el proyecto de consola desechable descrito arriba: **33 comprobaciones sobre `ReservaService` y `ClienteService`, todas en verde**, incluida la prueba de concurrencia con 40 hilos.

---

### Iteración 4 — Inicio de sesión y layout del panel

| | |
|---|---|
| **Fecha** | 2026-09-06 |
| **Estado** | ✅ Completada — 20/20 comprobaciones de sesión en verde |
| **Objetivo** | Dejar el panel protegido y con su estructura montada, para que la fase siguiente solo tenga que agregar las 8 vistas adentro. |

#### Qué se hizo

- [SesionService.cs](AireLibre/Services/SesionService.cs) — autenticación contra los dos usuarios quemados. `IniciarSesion()`, `CerrarSesion()`, `HaySesion`, `UsuarioActual` (con `set` privado, para que ninguna vista pueda "autenticarse" asignándolo a mano). El usuario no distingue mayúsculas; la contraseña sí.
- [PaginaPanel.cs](AireLibre/Components/PaginaPanel.cs) — clase base de las páginas protegidas. Comprueba la sesión en `OnInitialized` y redirige a `/login`. Las páginas se protegen con `@inherits PaginaPanel`.
- [PanelLayout.razor](AireLibre/Components/Layout/PanelLayout.razor) — barra lateral con los 7 enlaces del panel, el nombre de quien tiene la sesión abierta y el botón de cerrar sesión. En móvil la barra lateral pasa a barra superior (ya estaba resuelto en `app.css`).
- [Login.razor](AireLibre/Components/Pages/Login.razor) (`/login`) — **vista 4**. Usa el layout público, porque se llega sin sesión. Si alguien que ya tiene sesión entra a `/login`, se le manda directo al panel. Muestra las credenciales de prueba en pantalla, cosa que en un sistema real no existiría.
- [Panel.razor](AireLibre/Components/Pages/Panel.razor) (`/panel`) — **provisional**: confirma que el candado y el layout funcionan. Los contadores reales llegan en la Iteración 5.
- [SitioLayout.razor](AireLibre/Components/Layout/SitioLayout.razor) — el menú público muestra "Panel" en vez de "Ingresar" cuando hay sesión abierta.
- [Program.cs](AireLibre/Program.cs) — `SesionService` registrado como Scoped, con el comentario de por qué no puede ser singleton.

#### Decisiones tomadas

- **`SesionService` Scoped, no singleton** — ver la tabla de decisiones de arquitectura. Verificado con una prueba: dos `SesionService` sobre el mismo almacén, iniciar sesión en uno **no** abre sesión en el otro.
- **El control de sesión en una clase base** en vez de copiado en cada página, para que no se pueda olvidar en ninguna. Queda documentado dentro del archivo que quien sobrescriba `OnInitialized` **debe** llamar a `base.OnInitialized()` primero.
- **Cerrar sesión lleva al inicio, no a `/login`** — cerrar sesión es irse, no volver a entrar.
- **El mensaje de error no dice si falló el usuario o la contraseña.** Decirlo le confirmaría a un desconocido cuáles usuarios existen.

#### Problemas encontrados y solución

- **`CS0103: El nombre 'InteractiveServerNoPrerender' no existe`.** Ese identificador no existe en `RenderMode`; el prerrenderizado se desactiva construyendo el modo: `new InteractiveServerRenderMode(prerender: false)`.

- **El login rebotaba: se entraba y el sitio devolvía a `/login`.** Causa raíz: con prerrenderizado, cada página se dibuja **dos veces y en dos ámbitos distintos** (el de la petición HTTP y el del circuito), y los servicios **Scoped no son la misma instancia** en los dos. En el primero `SesionService` estaba recién creado y vacío, así que la comprobación de `OnInitialized` no veía sesión y redirigía a alguien que sí estaba autenticado.

  **Primer intento (descartado):** desactivar el prerrenderizado en todo el sitio. Funcionaba, pero cobraba el precio en las páginas públicas: el HTML de la primera carga llegaba vacío.

  **Solución aplicada:** dejar el prerrenderizado activo y salir temprano de la comprobación mientras el componente no sea interactivo, con `RendererInfo.IsInteractive` en [PaginaPanel.cs](AireLibre/Components/PaginaPanel.cs). La comprobación real ocurre en la segunda pasada, que es la que tiene la sesión de verdad. Arregla solo donde está el problema y no le cobra nada al sitio público.

#### Verificación end-to-end

`dotnet build` → **0 advertencias, 0 errores**. Rutas: `/`, `/grupos`, `/planes`, `/login`, `/panel` → todas 200.

**`SesionService` ejecutado directamente** (proyecto de consola desechable en el scratchpad, borrado después): **20 comprobaciones, 20 en verde** — arranca sin sesión; rechaza usuario vacío, contraseña vacía, usuario inexistente, contraseña incorrecta y contraseña con mayúsculas cambiadas; el mensaje no revela si el usuario existe; acepta `profe` y `profesora` con `aire2026`; `CerrarSesion()` limpia todo; el usuario admite mayúsculas y espacios sobrantes; **y dos sesiones sobre el mismo almacén no se contagian**.

**`/panel` sin sesión, sobre el HTML realmente servido:** 0 apariciones de "Sesión de", 0 del botón "Cerrar sesión", 0 del nombre de usuario y 0 del contenido protegido. Solo se prerrenderiza el esqueleto del menú lateral, sin ningún dato.

**Las páginas públicas no se vieron afectadas:** `/grupos` sigue prerrenderizando sus 102 marcas de cupo y los tres contadores (13 / 4 / 1); `/` sigue trayendo misión, visión y los bloques de imagen.

⏳ **Falta la verificación visual del usuario**: entrar con `profe` / `aire2026`, comprobar que llega al panel y que el menú público cambia "Ingresar" por "Panel"; después cerrar sesión y confirmar que `/panel` vuelve a rebotar a `/login`.

#### ⏳ Diferido — NO perder

- ⏳ **La sesión no sobrevive a un F5.** Vive en memoria dentro del circuito: al refrescar o al escribir una dirección del panel directamente en la barra del navegador, se crea un circuito nuevo y hay que volver a entrar. Navegar por los enlaces del sitio sí la conserva. Arreglarlo exige sacar la sesión del circuito (cookie de autenticación o almacenamiento del navegador), que es justo la plomería que el enunciado pidió evitar. **Documentado en [SesionService.cs](AireLibre/Services/SesionService.cs).**
- ⏳ **Contraseñas en texto plano** y usuarios quemados en el código. Decisión del enunciado, inaceptable en un sistema real: ahí van con hash y sal. Documentado en `SesionService.cs` y en `Usuario.cs`.
- ⏳ **6 enlaces del menú lateral devuelven 404** (`/panel/clases`, `/clientes`, `/pagos`, `/inventario`, `/solicitudes`, `/notificaciones`). El menú es parte del layout, que es el entregable de esta fase; las páginas llegan en la Iteración 5.
- ⏳ **`Panel.razor` es provisional** — los contadores reales (cupos separados hoy, clientes registrados, pagos pendientes, solicitudes sin atender) se implementan en la Iteración 5.

#### Tests

Sin suite automatizada permanente. La verificación fue el proyecto de consola desechable descrito arriba: **20 comprobaciones sobre `SesionService`, todas en verde**, más las comprobaciones sobre el HTML servido.

---

### Iteración 5 — Las 8 vistas del panel y reorganización del proyecto

| | |
|---|---|
| **Fecha** | 2026-09-06 |
| **Estado** | ✅ Completada — 68/68 comprobaciones de negocio en verde, 12/12 rutas sirviendo |
| **Objetivo** | Cerrar las 12 vistas del enunciado y, de paso, dejar la estructura del proyecto entendible antes de que creciera a 24 archivos. |

#### Reorganización de la estructura (pedida por Camila Loaiza)

- **`Pages/` se dividió en `Publico/` y `Panel/`.** Con las 12 vistas en una sola carpeta no se distinguía de un vistazo qué exige sesión y qué no. Las rutas salen del `@page`, así que mover los archivos no cambió ninguna dirección.
- **`PaginaPanel.cs` se movió junto a las páginas que protege** (`Components/Pages/Panel/`), en vez de quedar suelta en la raíz de `Components/`.
- **`Panel.razor` se renombró a `Resumen.razor`.** Un componente llamado `Panel` dentro de una carpeta `Panel` era confuso; la ruta `/panel` no cambió.
- **Se unificaron los tipos de resultado.** `ResultadoReserva`, `ResultadoSolicitud` y `ResultadoSesion` tenían los mismos dos campos: eran tres nombres para la misma idea. Ahora hay un solo [Resultado.cs](AireLibre/Services/Resultado.cs) con los atajos `Ok()` y `Error()`. Las vistas no cambiaron: los campos se siguen llamando `Exito` y `Mensaje`.
- **Se añadió al README el mapa completo del proyecto**, con una línea por archivo.

#### Qué se hizo

**Servicios:**

- [ClienteService.cs](AireLibre/Services/ClienteService.cs) ampliado y organizado en 5 bloques numerados: solicitudes, clientes, pagos, avisos y validación compartida. Nuevos: `ConvertirSolicitudEnCliente`, `DescartarSolicitud`, `CrearCliente`, `ActualizarCliente`, `EliminarCliente`, `ControlesDe`, `AgregarControlMensual`, `EliminarControlMensual`, `MarcarPago`, `AvisosDePagoManana`.
- [InventarioService.cs](AireLibre/Services/InventarioService.cs) — CRUD de herramientas con nombre único.
- `Cliente.Copia()` y `Herramienta.Copia()` en los modelos, para que los formularios de edición no toquen el original.

**Las 8 vistas del panel:**

| # | Vista | Qué hace |
|---|---|---|
| 5 | [Resumen.razor](AireLibre/Components/Pages/Panel/Resumen.razor) | Los 4 contadores, cada uno enlazando a donde se resuelve, más la ocupación de las 3 clases |
| 6 | [Clases.razor](AireLibre/Components/Pages/Panel/Clases.razor) | Quién separó cupo, agrupado por horario, con el grupo y la hora explícitos |
| 7 | [Clientes.razor](AireLibre/Components/Pages/Panel/Clientes.razor) | CRUD completo con búsqueda por nombre o documento |
| 8 | [DetalleCliente.razor](AireLibre/Components/Pages/Panel/DetalleCliente.razor) | Ficha, control mensual, diferencia de peso mes a mes y gráfica de evolución |
| 9 | [Pagos.razor](AireLibre/Components/Pages/Panel/Pagos.razor) | Marcar al día / en mora. Lo pendiente va arriba |
| 10 | [Inventario.razor](AireLibre/Components/Pages/Panel/Inventario.razor) | CRUD completo. Lo que hay que reponer va arriba |
| 11 | [Solicitudes.razor](AireLibre/Components/Pages/Panel/Solicitudes.razor) | Convertir en cliente o descartar; las atendidas quedan como historial |
| 12 | [Notificaciones.razor](AireLibre/Components/Pages/Panel/Notificaciones.razor) | Cupos separados (guardados, se marcan leídos) y pagos de mañana (calculados) |

[TablaClientes.razor](AireLibre/Components/Shared/TablaClientes.razor) — el tercer componente compartido que pedía el enunciado.

#### Decisiones tomadas

- **La gráfica de evolución del peso está hecha con `div` y CSS**, sin librerías (requisito académico). La altura de cada barra compara el peso contra el rango propio del cliente, no contra una escala fija: así se ve la variación aunque sean tres kilos. El rango va de 25% a 100% para que la barra más baja no desaparezca.
- **Al crear un cliente se le abre la suscripción automáticamente**, marcada en mora. Sin suscripción no aparecería en la vista de pagos: quedaría invisible para el profesor.
- **Eliminar un cliente borra su suscripción y sus controles, pero NO sus reservas.** Esa persona sí ocupó el cupo ese día; borrarlas falsearía el historial de asistencia. Se suelta el vínculo (`ClienteId = null`) y la reserva conserva el nombre como texto.
- **La conversión de solicitud pide 3 datos más** (estatura, peso, objetivo), porque el formulario público solo recoge 4 y la ficha de cliente necesita 7.
- **Las solicitudes atendidas no se borran**, se marcan: queda el rastro de por dónde entró cada cliente.
- **Marcar una notificación como leída se hace directo sobre el almacén, sin pasar por un servicio.** Es cambiar una propiedad booleana de un objeto existente: no hay ninguna regla que validar, y meterlo en un servicio sería ceremonia sin contenido.
- **`Plan.DiasDuracion` se usa al crear la suscripción** para calcular la fecha de próximo pago (hoy + días del plan).

#### Problemas encontrados y solución

- **`CS0542: los nombres de los miembros no pueden coincidir con sus tipos envolventes`**, en `Clientes.razor` e `Inventario.razor`. El archivo genera una clase con el nombre del archivo (`Clientes`), y `@inject ClienteService Clientes` intentaba crear un miembro con ese mismo nombre. Se renombraron las inyecciones a `ServicioClientes` y `ServicioInventario`.

#### Verificación end-to-end

`dotnet build` → **0 advertencias, 0 errores**.

**Las 12 rutas del enunciado responden 200.** Además, `/panel/clientes/abc` devuelve **404**: la restricción `{Id:int}` de la ruta impide que llegue basura a la vista.

**Reglas de negocio ejecutadas directamente contra `ClienteService` e `InventarioService`** (proyecto de consola desechable en el scratchpad, borrado después): **68 comprobaciones, 68 en verde.**

| Bloque | Qué se probó |
|---|---|
| Crear cliente | se le asigna id, se le abre suscripción, nace en mora, próximo pago = hoy + 30 |
| Validaciones | nombre corto, documento con letras, **estatura en centímetros (172)**, peso absurdo, plan inexistente |
| **Documento único** | rechaza repetido al crear y al actualizar; permite guardarse a sí mismo sin cambiarlo |
| Actualizar | cambia los datos y **la suscripción sigue al nuevo plan** |
| **La copia** | modificar la copia NO cambia el cliente guardado |
| Control mensual | rangos de peso y de avance (0–100), cliente inexistente, orden del más reciente al más antiguo |
| **Eliminar cliente** | borra suscripción y controles; **no borra ninguna reserva**; deja `ClienteId` nulo y conserva el nombre |
| Pagos | marca y desmarca; rechaza cliente sin suscripción |
| Avisos de pago | uno por suscripción que vence mañana, tipo correcto, **Id = 0** (no se guardan), no ensucian el almacén |
| Convertir solicitud | conserva nombre, documento y plan; toma los datos nuevos; abre suscripción; **no se puede convertir dos veces**; si la conversión falla la solicitud **sigue pendiente** |
| Descartar | marca como descartada; no descarta una ya atendida |
| Inventario | conteos, **nombre repetido sin distinguir mayúsculas**, nombre corto, cantidad negativa, **acepta cantidad cero**, actualizar, guardarse a sí misma, eliminar |

**Ninguna vista del panel filtra datos sin sesión.** Se revisó el HTML servido de las 8 rutas buscando nombres de clientes, números de documento, teléfonos y nombres de herramientas: **0 apariciones en las 8**.

**Las páginas públicas siguen intactas:** `/grupos` con sus 102 marcas de cupo, `/` con misión y visión.

⏳ **Falta la verificación visual del usuario** — ver la lista de abajo.

#### ⏳ Diferido — NO perder

- ⏳ **No se pueden quitar reservas desde el panel.** La vista de clases es de solo lectura, que es lo que pedía el enunciado. Si alguien avisa que no va, hoy no hay forma de liberar el cupo. Iría en `ReservaService`.
- ⏳ **No hay renovación de la fecha de pago.** El enunciado solo pide marcar al día / en mora, y eso es lo que hay. Correr `FechaProximoPago` al pagar exigiría una acción nueva; `Plan.DiasDuracion` ya está listo para calcularla.
- ⏳ **La misma persona puede reservar dos veces** el mismo horario. El enunciado no lo pide (la regla de unicidad del documento es para clientes, y las reservas no piden documento).
- ⏳ **Sigue pendiente de lo anterior:** la sesión no sobrevive a un F5, las contraseñas van en texto plano, los datos de contacto del pie son de ejemplo, `Plan.Precio` se agregó por iniciativa propia, y falta el `git init` con el primer commit.

#### Tests

Sin suite automatizada permanente (el enunciado no la pide y no se agregaron librerías de testing). Total verificado a lo largo del proyecto con proyectos de consola desechables: **33 + 20 + 68 = 121 comprobaciones sobre los 5 servicios**, todas en verde, incluida la prueba de concurrencia con 40 hilos.

---

### Iteración 6 — Rediseño del Inicio como ficha de sede

| | |
|---|---|
| **Fecha** | 2026-09-06 |
| **Estado** | ✅ Completada — compila limpio y las 14 secciones renderizan |
| **Objetivo** | Camila Loaiza pidió que la vista de inicio se pareciera a la página de sede de un gimnasio (`athleticgym.com.co/sedes/itagui`), pero centrada en CrossFit. |

#### Qué se hizo

**Sobre la referencia — qué se pudo ver y qué no.** El sitio de referencia es una aplicación de Next.js: el HTML que entrega el servidor no trae el diseño, solo un bloque de datos, y las demás peticiones (CSS, fuentes, portada) devuelven **403**. Es decir, **no se pudo ver el diseño visual renderizado**: ni sus colores, ni su tipografía, ni sus proporciones.

Lo que sí se obtuvo fue el **payload de datos de la página**, que revela su estructura de contenido: imagen de portada (escritorio y móvil), descripción, lista de características, **horarios por tipo de día**, dirección con coordenadas, video, correo y llamado a la acción. O sea, el orden de las secciones.

**Decisión:** se copió la **estructura**, no la apariencia. Los colores y la tipografía siguen siendo los de la paleta del proyecto, que además el enunciado fija.

**[Inicio.razor](AireLibre/Components/Pages/Publico/Inicio.razor) reescrito** con esta secuencia:

1. **Portada a sangre** — título, descripción, ubicación y dos botones, sobre un fondo oscuro con degradado (marcador de la foto).
2. **Franja de datos duros** — 3 horarios · 20 cupos por clase · **cupos libres hoy** · 2 profesores · 0 meses de permanencia.
3. **Descripción + "Lo que encuentras"** — el párrafo de presentación y la lista de 10 características, equivalente al bloque de bullets de la referencia.
4. **Horarios con los cupos EN VIVO** — ver abajo.
5. **Imágenes** — tres bloques con degradado.
6. **Planes** — los tres, leídos del almacén.
7. **Dónde entrenamos** — dirección y marcador de mapa.
8. **Misión y visión** — se conservan, que son requisito del enunciado.
9. **Cierre** a sangre con el llamado a la acción.

**Lo que se hizo distinto a propósito:** en la referencia los horarios son una tabla fija de horas de apertura. Aquí cada horario muestra su **medidor de 20 marcas con los cupos contados en vivo**, así que la portada dice cuántos puestos quedan ahora mismo. Es información que un gimnasio con máquinas no puede dar y este grupo sí, y reutiliza el elemento visual característico del sitio en vez de inventar otro.

**[app.css](AireLibre/wwwroot/css/app.css)** — se insertó el bloque **6. PORTADA DE INICIO** y se renumeraron los dos siguientes (medidor de cupos pasó a 7, responsive a 8), índice del encabezado incluido. Clases nuevas: `.portada`, `.franja-datos`, `.dato-fuerte`, `.lista-beneficios`, `.horario-fila`, `.mapa-marcador`, `.cierre`. La portada lleva una trama de marcas verticales (`.portada::after`) que es el mismo gesto del medidor de cupos convertido en textura.

#### Decisiones tomadas

- **Se copió la estructura, no la apariencia** — no había alternativa: el diseño de la referencia no es visible desde el servidor. Además el enunciado fija la paleta.
- **El Inicio dejó de ser una página puramente informativa.** Ahora recibe `DatosService` y `ReservaService` para leer la ocupación. Sigue sin escribir nada.
- **La dirección quedó como constante** al final del archivo, igual que el teléfono y las redes en `SitioLayout.razor`, para que cambiarla sea editar una línea.
- **El bloque de CSS nuevo se insertó como sección 6 y se renumeraron las siguientes**, en vez de añadirlo al final. El encabezado del archivo advierte que el orden define la cascada; romperlo por comodidad habría dejado los estilos responsive antes de los que deben sobrescribir.

#### Verificación end-to-end

`dotnet build` → **0 advertencias, 0 errores**. `/` responde **200**.

Sobre el HTML servido: las **14 secciones** renderizan en orden (portada, la sede, lo que encuentras, horarios, imágenes, los 3 planes, dónde entrenamos, misión, visión, cierre, más contacto y redes del pie). Los datos duros salen **3 · 20 · 18 · 2 · 0**, y el 18 cuadra con la suma real de cupos libres (13 + 4 + 1). Los tres horarios se imprimen como `8:30 am`, `7:00 pm` y `8:00 pm`, con sus **102 marcas de cupo** y el singular correcto en "1 cupo libre".

`app.css` se sirve con **200** (26.559 bytes) y contiene las 7 familias de clases nuevas.

⏳ **Falta la verificación visual del usuario**: abrir `/` y decir si la portada va en la dirección correcta, ya que el diseño de la referencia no se pudo ver y esto es una interpretación de su estructura.

#### ⏳ Diferido — NO perder

- ⏳ **La dirección es de ejemplo** (`Parque Principal de Itagüí · Carrera 51 con Calle 51`). Está como constante al final de [Inicio.razor](AireLibre/Components/Pages/Publico/Inicio.razor).
- ⏳ **El mapa es un marcador de posición.** Un mapa real exigiría incrustar un servicio externo (Google Maps u OpenStreetMap), que va contra el "sin librerías externas" del enunciado. La referencia sí trae coordenadas; si se quisiera, se resolvería con un `<iframe>`.
- ⏳ **La referencia tiene un video de YouTube en la portada.** No se incluyó, por la misma razón.
- ⏳ **Las fotos siguen siendo bloques con degradado.** La portada está preparada para recibir una foto real cambiando el `background-image` de `.portada`.

---

### Iteración 7 — "¿Quiénes somos?" y las secciones oscuras de impacto

| | |
|---|---|
| **Fecha** | 2026-09-06 |
| **Estado** | ✅ Completada — compila limpio, las 3 secciones nuevas renderizan |
| **Objetivo** | Camila Loaiza pasó dos capturas de un sitio de gimnasio y pidió que el diseño se viera así (no las imágenes, el diseño), más una sección de "¿Quiénes somos?" en el inicio. |

#### El hallazgo que hizo posible el diseño

Las capturas muestran titulares **condensados, pesados y en mayúsculas**, que es lo que le da el aire deportivo. La tentación era traer otra tipografía estrecha, pero el enunciado fija Archivo.

Resultó que **Archivo es una fuente variable con eje de anchura** (`wdth`, de 62 a 125). Cambiando la petición a Google Fonts por la versión variable, se consigue la condensada pesada **con la misma familia**:

```
family=Archivo:ital,wdth,wght@0,62..125,400..900;1,62..125,400..900
```

Comprobado: Google Fonts responde `font-stretch: 62% 125%`, pesos `400 900` y las dos inclinaciones. Los titulares usan `font-stretch: 62%`. El sitio sigue con una sola tipografía y aun así tiene dos voces.

#### Qué se hizo

**[app.css](AireLibre/wwwroot/css/app.css)** — el bloque 6 pasó a llamarse **"6. INICIO: PORTADA Y SECCIONES DE IMPACTO"** y se le sumó el lenguaje visual de las capturas:

- `.seccion--oscura` (+ variante `--oscura-calida`) — fondo casi negro con degradado.
- `.eyebrow--grande` — antetítulo centrado con espaciado de 0.34em. Lleva `text-indent` porque el espaciado se aplica también al último carácter y sin eso el texto queda descentrado hacia la izquierda.
- `.titular-gigante` — el cartel: hasta 7rem, peso 900, `font-stretch: 62%`, mayúsculas. Variante `--acento` en naranja, como el "PLANES" rojo de la referencia.
- `.ficha` — tarjeta vertical 2:3 con la foto en duotono y el texto encima. El velo `::before` de abajo hacia arriba es lo que garantiza que el texto se lea sobre cualquier foto real que se ponga después. Cuatro duotonos (`--a` a `--d`) para que las fichas de una fila no se vean idénticas.
- `.ficha-linea1` / `.ficha-linea2` — el remate en dos líneas de la referencia: la primera normal, la segunda **cursiva y más grande**.
- `.ficha--plan` — la misma pieza con precio, texto y botón.
- Variable nueva `--negro-verde: #071A0E`.

**[Inicio.razor](AireLibre/Components/Pages/Publico/Inicio.razor)** — tres secciones nuevas:

1. **¿Quiénes somos?** (fondo oscuro) — el texto que pasó Camila, reescrito: se cambió la enumeración genérica ("mejorar tu salud, ganar resistencia, superar tus límites") por lo que este grupo sí hace y se puede comprobar — el profesor gradúa la carga persona por persona, el grupo es de máximo veinte, se aprenden tu nombre en la primera semana, corrigen en el momento. Se conservó el cierre "aquí sudamos, nos reímos y crecemos juntos", que es lo mejor del original. Al lado va la lista de "Lo que encuentras".
2. **CON NOSOTROS / CONSEGUIRÁS** — cuatro fichas verticales.
3. **CONOCE NUESTROS / PLANES** — tres fichas con precio, descripción y botón "Únete ahora".

#### Decisiones tomadas

- **Las fichas de "Conseguirás" salen de la enumeración `Objetivo`, no escritas a mano.** El texto grande de cada una es `Objetivo.Texto()`, el mismo que muestran el formulario de clientes y el panel. Si algún día se agrega un objetivo al dominio, aparece solo en la portada: no se puede desfasar lo que anuncia el sitio de lo que ofrece el sistema. La frase de arriba de cada ficha (el "gancho") sí es texto, en un arreglo paralelo.
- **Se copió la composición, no los colores.** La referencia es vinotinto y rojo; aquí se usa la paleta que fija el enunciado. Cambiarla sería editar las siete variables del bloque 1 de `app.css` — está a un paso si se decide.
- **Los botones conservan el radio de 4px.** Los de la referencia son notoriamente más redondeados, pero el enunciado dice "de 3 a 5 px, nada de esquinas muy redondeadas".
- **Las fotos siguen siendo degradados.** La clase `.ficha` ya está preparada: se le añade `url(...)` al `background-image` dejando el degradado encima, y el texto se sigue leyendo.

#### Verificación end-to-end

`dotnet build` → **0 advertencias, 0 errores**. `/` responde **200**.

La fuente variable: la petición a Google Fonts devuelve **200** y sirve `font-stretch: 62% 125%`, `font-weight: 400 900` y los estilos normal y cursiva. El `<link>` del HTML lleva los tres ejes.

Sobre el HTML servido: los tres pares antetítulo + titular salen en orden (Quiénes somos / Entrenamos bajo el cielo · Con nosotros / Conseguirás · Conoce nuestros / Planes). Las **cuatro fichas de objetivos** se generan desde el enum: Ganar masa–Volumen, Bajar grasa–Definición, Marcar figura–Tonificar, Energía diaria–Salud y vitalidad. Las **tres fichas de planes** (Diario, Quincenal, Mensual) con sus 3 botones "Únete ahora".

`app.css` se sirve con **200** (33.441 bytes) y contiene las 9 familias de clases nuevas y las 5 declaraciones de `font-stretch`.

⏳ **Falta la verificación visual del usuario.**

#### ⏳ Diferido — NO perder

- ⏳ **La paleta sigue siendo la del enunciado.** Si se quiere el negro y rojo de la referencia, son siete variables en `:root` (bloque 1 de `app.css`). Ojo: el verde y el azul no son decorativos, codifican mañana y noche en las 12 vistas; habría que decidir qué dos colores toman ese papel.
- ⏳ **Las secciones oscuras solo están en el inicio.** `/planes` sigue con el estilo claro anterior, así que las dos pantallas de planes no se ven iguales. Las clases nuevas (`.seccion--oscura`, `.titular-gigante`, `.ficha`) son reutilizables: llevarlas a `/planes` y `/grupos` es aplicarlas.
- ⏳ **Los "ganchos" de las fichas de objetivos** ("Ganar masa", "Bajar grasa"…) están en un arreglo dentro de `Inicio.razor`, emparejado por posición con el enum. Si se agrega un objetivo hay que agregar su gancho, o el arreglo se queda corto.

---

### Iteración 7.1 — Fotografías reales

| | |
|---|---|
| **Fecha** | 2026-09-06 |
| **Estado** | 🚧 Código listo · **faltan los archivos de imagen** |
| **Objetivo** | Camila Loaiza pasó tres fotografías por el chat para incorporarlas al sitio. |

#### Qué se hizo

Las fotos llegaron pegadas en la conversación, no como archivos en disco, así que **no se pudieron guardar desde aquí**. Lo que sí se hizo fue dejar todo el código apuntando a ellas, de modo que basta con dejar los archivos en su carpeta:

- **`wwwroot/img/`** creada, con [LEEME.txt](AireLibre/wwwroot/img/LEEME.txt) que documenta los tres nombres exactos y las recomendaciones de tamaño y peso.
- **[app.css](AireLibre/wwwroot/css/app.css)** — la portada y dos fichas nuevas referencian las fotos por `url("../img/...")`.
- **[Inicio.razor](AireLibre/Components/Pages/Publico/Inicio.razor)** — la sección "Así se ve una clase" pasó de tres bloques de degradado a **dos fichas con fotografía** (4:3) y su rótulo.

| Archivo | Dónde se usa | Foto |
|---|---|---|
| `portada-grupo.jpg` | Portada, a todo el ancho | El grupo entrenando afuera (la de la llanta) |
| `balon-medicinal.jpg` | "Así se ve una clase" | Trabajo con balón medicinal |
| `peso-muerto.jpg` | "Así se ve una clase" | Levantamiento con barra |

#### Decisiones tomadas

- **La foto del grupo al aire libre va en la portada.** Es la única de las tres tomada afuera y con varias personas, que es exactamente de lo que trata el proyecto. Las otras dos son de interior y funcionan mejor como apoyo.
- **El velo oscuro va ENCIMA de la foto, no debajo.** En CSS la primera capa de `background-image` queda arriba, así que los degradados se declaran primero y la `url()` de último. No es decoración: es lo que garantiza que el texto blanco se lea sobre cualquier fotografía, sin importar qué tan clara sea.
- **Si el archivo falta, no se rompe nada.** El navegador ignora la capa que no puede cargar y se ve el degradado, que es el marcador de posición de siempre. Por eso el sitio funciona igual antes y después de poner las fotos.

#### Verificación end-to-end

`dotnet build` → **0 advertencias, 0 errores**. `/` responde **200**.

Se comprobó que la carpeta se sirve: se puso un archivo de prueba en `wwwroot/img/` y respondió **200** en `/img/`, o sea que las rutas `url("../img/...")` del CSS resuelven bien. Las dos fichas con foto y sus rótulos renderizan.

Los tres archivos de imagen responden **404**, que es lo esperado mientras no estén, **y la página se ve correctamente igual**.

#### ⏳ Diferido — NO perder

- ⏳ **Guardar los tres archivos** en `AireLibre/wwwroot/img/` con los nombres exactos de la tabla de arriba. Es lo único que falta de esta iteración.
- ⏳ **Faltan fotos para las fichas de "Conseguirás" (4) y de "Planes" (3).** Siguen con degradados. Con siete fotos verticales más, esas dos secciones quedarían idénticas a la referencia; el CSS solo necesitaría una variante por foto, igual que las dos que ya existen.

---

### Iteración 7.2 — Fotos de los objetivos y saneamiento de los archivos

| | |
|---|---|
| **Fecha** | 2026-09-06 |
| **Estado** | ✅ Completada · ⚠️ un archivo llegó dañado, ver abajo |
| **Objetivo** | Camila Loaiza guardó las siete fotos en `wwwroot/img/` y pidió poner las cuatro de objetivos en las fichas de "Con nosotros conseguirás". |

#### Qué se hizo

- **Las cuatro fotos de objetivos quedaron cableadas** a sus fichas, con clases `.ficha--foto-volumen`, `--foto-definicion`, `--foto-tonificar` y `--foto-salud`.
- **El nombre del archivo vive solo en el CSS.** El Razor aplica una clase y nada más, así que cambiar una foto es reemplazar el archivo: no se toca ningún `.razor`.
- **Se unificó el tinte** en la variable `--tinte-foto`, que se aplica igual a las seis fotos. Es lo que hace que fotos de origen distinto se vean como un conjunto y no como un collage.
- **Guardia contra desfase:** si algún día se agrega un objetivo a la enumeración y se olvida agregarle su gancho o su foto, `Metas` lanza una excepción con un mensaje que dice exactamente qué falta, en vez de reventar con un índice fuera de rango.
- [LEEME.txt](AireLibre/wwwroot/img/LEEME.txt) reescrito: tabla de las siete fotos con dónde se usa cada una, y las cuatro reglas para reemplazarlas.

#### Problemas encontrados en los archivos y solución

Al revisar los archivos aparecieron tres cosas. Se detectaron leyendo la **firma real** de cada archivo (los primeros bytes), no la extensión.

| Archivo | Problema | Qué se hizo |
|---|---|---|
| `bajar-grasa.jpg` | **No era una imagen.** Empezaba por `<!DOCTYPE html>`: era una página HTML de `nutrides.com` guardada con extensión `.jpg`. Se guardó la página en vez de la foto. | Camila Loaiza la volvió a descargar el mismo día. La nueva llegó como PNG de 2097 KB, y se le aplicó la misma conversión que a las otras dos: **2097 KB → 146 KB**. |
| `volumen.jpg` (2123 KB) y `salud-vitalidad.jpg` (2009 KB) | Eran **PNG** con extensión `.jpg`, y pesaban 2 MB cada una. Entre las dos, 4 MB de descarga solo en la portada. | Convertidas a **JPEG real**, redimensionadas a 1400 px y comprimidas al 82%. **4132 KB → 275 KB**, sin pérdida visible. Los originales quedaron respaldados en el scratchpad de la sesión. |
| `balon-medicinal.jpg` | Es un **WEBP** con extensión `.jpg`. | Se dejó como está: pesa 107 KB y los navegadores lo muestran bien porque detectan el formato por contenido, no por extensión. Solo queda anotado. |

#### Verificación end-to-end

`dotnet build` → **0 advertencias, 0 errores**. `/` responde **200**.

Las **cuatro fichas** salen con su clase de foto correcta y en el orden de la enumeración: `foto-volumen`, `foto-definicion`, `foto-tonificar`, `foto-salud`. Las **siete imágenes** responden **200** desde `/img/`.

Peso total de las fotos: de **4,6 MB** a **717 KB**.

#### ⏳ Diferido — NO perder

- ~~Volver a descargar `bajar-grasa.jpg`~~ → **resuelto el 2026-09-06**, ver la tabla de problemas de arriba. Las cuatro fichas de objetivos ya tienen su foto.
- ⏳ **`tonificada.jpg` mide 236 × 354 px.** Se ve, pero las fichas miden alrededor de 400 px de alto en pantalla grande, así que va a salir borrosa. Conviene una versión de al menos 600 px de ancho.
- ⏳ **`volumen.jpg` y `salud-vitalidad.jpg` son apaisadas** (1400 × 933) y las fichas son verticales 2:3, así que el navegador les recorta los lados. Funcionan, pero unas fotos verticales encuadrarían mejor.
- ⏳ **Faltan las tres fotos de los planes.** Las fichas de "Conoce nuestros planes" siguen con degradados. Con tres fotos verticales más, esa sección queda idéntica a la referencia.

---

### Iteración 8 — Cambio de marca a STRONGBODY y paleta rojo, blanco y negro

| | |
|---|---|
| **Fecha** | 2026-09-06 |
| **Estado** | ✅ Completada — compila limpio, las 12 rutas responden 200 |
| **Objetivo** | Camila Loaiza definió cuatro cambios: el gimnasio se llama **STRONGBODY**, la paleta pasa a rojo/blanco/negro, hay que poner el logo, y quitar las líneas verticales de la portada. |

#### 1. Paleta nueva: rojo, blanco y negro

Es la del logo (llanta negra, texto blanco, pesas rusas rojas).

| Antes | Ahora | Papel |
|---|---|---|
| `--verde-oscuro` #10331C | `--negro` #101013 | fondos oscuros y texto principal |
| — | `--negro-profundo` #000000 | secciones de impacto |
| `--azul-noche` #1D3557 | `--grafito` #33333A | superficies oscuras · grupo de la noche |
| `--verde` #2F7A3D y `--naranja` #E2571F | `--rojo` #D8232A | el acento: botones, alertas y grupo de la mañana |
| `--naranja-oscuro` | `--rojo-oscuro` #A5151B | hover del botón y grupo lleno |
| `--crema` #F4F6EF | `--hueso` #F4F4F5 | fondo de página |
| `--gris` #5C6659 | `--gris` #6E6E73 | texto secundario |

Solo hay tres colores de verdad; el gris y el grafito son negro rebajado, para tener jerarquía sin meter una cuarta familia.

#### El problema que destapó el cambio, y la lección

La sustitución de variables fue mecánica, pero **el verde y el naranja se colapsaron en el mismo rojo**, y con ellos dos distinciones que sí importaban:

- El aviso de éxito y el de error quedaron idénticos: no se distinguía "salió bien" de "salió mal".
- Los distintivos "Al día" y "En mora" quedaron los dos rojos.

La causa de fondo: **las clases se llamaban por su color** (`.distintivo--verde`, `.distintivo--naranja`), así que al cambiar la paleta quedaron mintiendo — una clase llamada "verde" pintando de rojo.

Se corrigió renombrando por **significado**:

| Antes | Ahora | Se usa en |
|---|---|---|
| `.distintivo--verde` | `.distintivo--ok` | Al día · Bueno · Cliente · Convertida |
| `.distintivo--naranja` | `.distintivo--alerta` | En mora · Para reponer · Vencido · Nueva · Completa |
| `.distintivo--azul` | `.distintivo--noche` | grupo de la noche |
| `.distintivo--gris` | `.distintivo--neutro` | estados sin carga |

Cada uso se reasignó **por la etiqueta que acompaña al distintivo**, no por sustitución ciega. También se ajustaron a mano: el aviso de éxito (ahora negro, el de error sigue rojo), el hover de los enlaces (`--rojo-oscuro`, porque el rojo normal era el color de reposo y no se notaba el cambio), el hover de fila de tabla (gris en vez de un tinte rojo por fila) y el color de "grupo lleno" (`--rojo-oscuro`, porque el rojo normal ya significa "grupo de la mañana" y se confundirían).

**La lección queda escrita en el CSS y en `Enumeraciones.cs`:** nombrar las clases por significado y no por color. Con nombres semánticos, cambiar la paleta es editar el bloque 1 de `app.css` y nada más.

#### 2. Marca: Aire Libre → STRONGBODY

Cambiados todos los textos visibles: títulos de las 12 páginas, pie, y las menciones en prosa.

**El nombre del proyecto .NET sigue siendo `AireLibre`** (carpeta, `.csproj`, `.sln` y espacios de nombres). Renombrarlo toca el `namespace` de los 20 archivos `.cs`, los `@using` y la solución: es mecánico pero es otra tarea. Ver los diferidos.

#### 3. Logo

[logo.png](AireLibre/wwwroot/img/logo.png) reemplaza al nombre escrito en las dos cabeceras (pública y del panel). Llega con `alt="STRONGBODY"`, así que para un lector de pantalla el enlace sigue diciendo a dónde va.

Venía a **6000 × 4000 px y 4484 KB**. Se redujo a 900 × 600 conservando la transparencia: **4484 KB → 370 KB**. La clase `.marca-logo` le fija **solo el alto** (46 px) y deja el ancho en automático, para que nunca se deforme sea cual sea la imagen que se ponga.

#### 4. Líneas verticales de la portada

Eliminadas. Eran una trama (`.portada::after`) que reproducía el gesto del medidor de cupos como textura. Sobre un degradado funcionaba; **sobre una fotografía real ensuciaba la imagen**. Se quitó la regla y también su ajuste responsive, que quedó huérfano. En el CSS queda una nota diciendo qué había ahí y por qué se fue.

#### Corrección adicional

En la captura que envió Camila se veía **un recuadro alrededor del título de la portada**. No era un error de estilo: el enrutador le da el foco al `<h1>` en cada navegación (`FocusOnNavigate`) para que un lector de pantalla anuncie la página nueva, y eso disparaba el indicador de foco. Se quitó **solo en el `<h1>`**: es foco puesto por código, no por teclado, así que nadie necesita verlo. Todo lo demás conserva su indicador.

#### Verificación end-to-end

`dotnet build` → **0 advertencias, 0 errores**. **Las 12 rutas responden 200.**

Sobre el CSS y el HTML realmente servidos: las **8 variables** de la paleta nueva están en su sitio; **cero restos** de los colores viejos (`#10331C`, `#2F7A3D`, `#E2571F`, `#1D3557`, `#F4F6EF`) en todo el archivo; los **4 distintivos semánticos** definidos; **cero** apariciones de las clases viejas en `Models/` y `Components/`; el logo se sirve con **200** y aparece en la cabecera pública y en la del panel; la regla `.portada::after` **ya no existe**; y no queda ninguna mención de "Aire Libre" en las vistas.

#### ⏳ Diferido — NO perder

- ⏳ **Renombrar el proyecto .NET de `AireLibre` a `StrongBody`.** Hoy la carpeta y los espacios de nombres dicen AireLibre y el sitio dice STRONGBODY. Es mecánico (namespace de 20 archivos `.cs`, `.csproj`, `.sln`, carpeta) pero conviene hacerlo de una sola vez y verificando que compile.
- ⏳ **El nombre se escribió "STRONGBODY" (junto)**, siguiendo cómo lo escribió Camila. El logo dice "STRONG BODY" en dos palabras. Confirmar cuál es la forma correcta.
- ⏳ **Las URLs de redes siguen diciendo `airelibre.crossfit`** y el teléfono es de ejemplo. Están juntos al final de `SitioLayout.razor`.
- ⏳ **El tagline del logo es "1.0 · TU DESAFÍO"** y no se está usando en ninguna parte del sitio. Podría ir en la portada.
- ⏳ **`tonificada.jpg` sigue siendo de 236 px** y faltan las tres fotos de los planes.

---

### Iteración 9 — API REST con Swagger en un proyecto aparte

| | |
|---|---|
| **Fecha** | 2026-10-03 |
| **Estado** | ✅ Completada — compila limpio, 31 endpoints en Swagger, reglas verificadas por HTTP |
| **Objetivo** | Tener el Swagger **aparte**: un proyecto nuevo, desde cero, que exponga el sistema como API REST documentada, sin tocar el sitio Blazor. |

#### Qué se hizo

- **Proyecto nuevo [`AireLibre.Api`](AireLibre.Api/)** (ASP.NET Core Web API, `net10.0`), agregado a `AireLibre.sln`. El proyecto Blazor **no se modificó**.
- **No copia código: lo enlaza.** El `.csproj` incluye `..\AireLibre\Models\**\*.cs` y `..\AireLibre\Services\**\*.cs` como archivos enlazados (en Visual Studio aparecen bajo `Compartido/`). Las reglas (cupo de 20 con candado, documento único, estatura en metros, etc.) siguen existiendo **una sola vez**.
- **Los controladores no deciden nada**, igual que las vistas: traducen HTTP ↔ servicio. `ControladorBase.Responder()` convierte el `Resultado` de cualquier servicio en **200** (éxito) o **400** (con el mismo mensaje que ve la persona en el sitio). Los ids inexistentes dan **404**.
- **Contratos (DTOs) en [`Contratos.cs`](AireLibre.Api/Contratos/Contratos.cs).** `Usuario` nunca sale por la API (trae la contraseña); las peticiones no dejan inventar el `Id` ni las fechas de registro.
- **Swagger** con Swashbuckle 6.5.0: descripciones y ejemplos salen de los comentarios `///`. Los enums viajan como texto (`"Cedula"`, `"Manana"`).
- `launchSettings.json` con perfiles **https** (`https://localhost:7062/swagger`) y **http** (`http://localhost:5063/swagger`). La raíz `/` redirige a Swagger.

#### Endpoints (31)

| Grupo | Endpoints |
|---|---|
| 1. Grupos y horarios | `GET /api/grupos` · `GET /api/horarios` · `GET /api/horarios/{id}` (con ocupados y disponibles; `?fecha=` opcional) |
| 2. Reservas | `GET /api/reservas` · `POST /api/reservas` (separar cupo) |
| 3. Planes y solicitudes | `GET /api/planes` · `GET /api/planes/{id}` · `GET/POST /api/solicitudes` · `POST /api/solicitudes/{id}/convertir` · `POST /api/solicitudes/{id}/descartar` |
| 4. Clientes | CRUD en `/api/clientes` · `GET/POST /api/clientes/{id}/controles` · `DELETE /api/clientes/controles/{controlId}` |
| 5. Pagos | `GET /api/pagos` · `PUT /api/clientes/{id}/pago` |
| 6. Inventario | CRUD en `/api/herramientas` · `GET /api/herramientas/resumen` |
| 7. Panel | `GET /api/resumen` · `GET /api/notificaciones` · `PUT /api/notificaciones/{id}/leida` · `POST /api/sesion/login` |

#### Cómo correrla

- **Visual Studio:** clic derecho en `AireLibre.Api` → *Establecer como proyecto de inicio* → perfil **https** → F5. Para levantar el sitio y la API a la vez: clic derecho en la solución → *Configurar proyectos de inicio* → *Varios proyectos de inicio* → ambos en *Iniciar*.
- **Terminal:** `cd AireLibre.Api` y `dotnet run --launch-profile https`.

#### Decisiones tomadas

- **Se revisa la decisión "sin API"** de la Iteración 1: la API vive en un proyecto **separado**, así que el sitio sigue siendo "solamente front" tal como se acordó.
- **Swashbuckle es una librería externa.** El requisito de "sin librerías" aplica al sitio Blazor, que sigue sin ninguna. Swagger no se puede generar sin un paquete.
- **El login solo valida credenciales** (`200` con el usuario o `401`). La API no guarda sesión entre peticiones; proteger los endpoints exigiría tokens JWT.

#### Verificación end-to-end

`dotnet build` de `AireLibre.Api` → **0 advertencias, 0 errores**. Swagger responde **200** y lista los **31 endpoints** en 7 grupos; ningún esquema expone `Usuario`.

Por HTTP contra la API corriendo: el horario de las 8:00 pm acepta el cupo 20 y **rechaza el 21 con 400**; nombre corto → 400; crear cliente → **201** con id e IMC; documento repetido → 400; estatura `172` → 400 ("va en metros"); cliente inexistente → 404; control mensual y pago → 200; solicitud → convertir → **convertir otra vez da 400**; herramienta con nombre repetido en mayúsculas → 400; login correcto → 200 sin contraseña, contraseña con mayúsculas cambiadas → **401**; `http://` redirige a `https://`.

#### ⏳ Diferido — NO perder

- ⏳ **La API y el sitio no comparten datos.** Cada uno corre en su proceso con su propio `DatosService` en memoria: lo que se crea desde Swagger no aparece en el sitio. Compartirlos exige la base de datos (migración a EF Core, ya diferida desde la Iteración 1).
- ⏳ **Los endpoints del panel no están protegidos.** Cualquiera con la URL puede leer clientes y pagos. Para un entorno real: JWT + `[Authorize]`.

