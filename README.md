# Taller Moreno

Programa de escritorio para el taller de carros **Taller Moreno**: venta de repuestos, inventario con fotos,
clientes, proveedores e historial de ventas. Está pensado para mecánicos que tocan poco el computador:
botones grandes, pocas pantallas y lo justo en cada una.

## Lo que hace

| Sección | Para qué sirve |
| --- | --- |
| 🛒 **Vender** (F1) | Tocar los repuestos (con foto) para agregarlos, elegir el cliente si se quiere, cómo paga y **Cobrar**. Las unidades se descuentan solas del inventario. |
| 📦 **Inventario** (F2) | Tarjetas con foto, unidades y precio. Buscar, filtrar por categoría, ver **solo lo que hay que pedir**, registrar **Llegó mercancía** y **Descargar Excel** en cualquier momento. |
| 👥 **Clientes** (F3) | Nombre, placa, vehículo y celular. Muestra **cuánto ha comprado cada cliente** y el detalle de cada compra. |
| 🚚 **Proveedores** (F4) | A quién se le compra, cuánto se le ha comprado y cada entrada de mercancía. |
| 🧾 **Historial de ventas** (F5) | Ventas de hoy, la semana, el mes o el año, con totales y el detalle de cada venta. |

### Conteo de unidades

Las unidades nunca se escriben a mano después de crear el repuesto: cambian solas y cada cambio queda registrado.

- **Venta** → resta (si hay 20 baterías y se vende 1, quedan 19). No deja vender más de lo que hay.
- **Llegó mercancía** → suma y actualiza el costo.
- **Anular venta** (administrador) → devuelve las unidades.
- **Ajustar unidades** (administrador) → para conteos físicos, daños o pérdidas, con el motivo.

Cada repuesto tiene un mínimo: cuando quedan esas unidades o menos sale marcado **por pedir**.

### Usuarios

- **Administrador** (el dueño): ve costos y ganancias, crea repuestos, anula ventas, ajusta unidades,
  descarga ventas en Excel, maneja usuarios y hace copias de seguridad.
- **Mecánico**: vende, consulta el inventario, recibe mercancía y registra clientes y proveedores.

La primera vez que se abre el programa pide crear la cuenta del dueño.

## Cómo correrlo

Requiere el [SDK de .NET 10](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/TallerMoreno.Desktop
```

Para generar un ejecutable que se pueda copiar al computador del taller:

```bash
dotnet publish src/TallerMoreno.Desktop -c Release -r win-x64 --self-contained -o publicar
```

## Dónde quedan los datos

Todo queda en `%LOCALAPPDATA%\TallerMoreno`:

- `taller.db`: base de datos SQLite (repuestos, ventas, clientes, proveedores, usuarios).
- `fotos\`: fotos de los repuestos, reducidas automáticamente.
- `copias\`: copias de seguridad automáticas.
- `errores.log`: lo que salió mal y el detalle técnico, para revisarlo si alguien avisa de un problema.

Para usar otra carpeta de datos, copia `appsettings.example.json` como `appsettings.json` junto al
ejecutable y escribe la ruta en `Datos:Carpeta`. No uses una carpeta sincronizada (OneDrive, Dropbox)
para los datos: sincronizar la base de datos abierta puede dañarla. Para eso está `Copias:CarpetaExtra`.

### Copias de seguridad

- **Automáticas:** una al día, al abrir el programa (y cada hora revisa si ya se hizo, por si el computador
  queda prendido). Se guardan las últimas 14 en `copias\`. Si en `Copias:CarpetaExtra` se escribe otra
  carpeta (una USB o una carpeta de OneDrive), la copia también se deja allí. Así, si se daña el disco,
  no se pierde todo.
- **A mano:** en el menú, **Copia de seguridad** (administrador) guarda un `.zip` donde se elija.
- **Antes de actualizar:** si una versión nueva cambia la base de datos, primero se guarda
  `copias\antes-de-actualizar-….zip`.

Para restaurar, en el menú, **Restaurar copia** (administrador): se elige el `.zip`, el programa guarda primero
cómo está todo en `copias\antes-de-restaurar-….zip`, restaura y pide entrar de nuevo. No hace falta cerrar el
programa ni descomprimir nada a mano.

### Si el dueño olvida la contraseña

Al crear la cuenta del dueño el programa muestra un **código de recuperación** para anotarlo en papel. En la
entrada, **¿Olvidaste tu contraseña?** pide ese código y una contraseña nueva. Cada código sirve una vez: al
usarlo se muestra uno nuevo. Desde el menú, **Código de recuperación** crea otro si se perdió el anterior.
A un mecánico que olvide su contraseña se la pone nueva el dueño, desde **Usuarios**.

## Proyecto

```
src/
  TallerMoreno.Core            Modelos, reglas de negocio y sesión
  TallerMoreno.Infrastructure  SQLite (Dapper), Excel (ClosedXML), fotos y copias de seguridad
  TallerMoreno.Desktop         Interfaz en Avalonia (MVVM)
tests/
  TallerMoreno.Tests           Pruebas de integración contra una base SQLite temporal
```

## Pruebas

```bash
dotnet run --project tests/TallerMoreno.Tests
```

Con Docker Desktop abierto se puede correr lo mismo que hace GitHub Actions (formato, compilación y pruebas)
en Linux, sin gastar minutos de Actions:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/pruebas-docker.ps1
```

## Licencia

[MIT](LICENSE)
