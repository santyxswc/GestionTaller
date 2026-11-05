# Taller Moreno

Programa de escritorio para el taller de carros **Taller Moreno**: venta de repuestos, inventario con fotos,
clientes, proveedores e historial de ventas. Está pensado para mecánicos que tocan poco el computador:
botones grandes, pocas pantallas y lo justo en cada una.

## Lo que hace

| Sección | Para qué sirve |
| --- | --- |
| 🛒 **Vender** (F1) | Tocar los repuestos (con foto) para agregarlos, elegir el cliente si se quiere, cómo paga y **Cobrar**. Con **Queda debiendo (fiado)** el cliente paga una parte o nada y el resto queda anotado. Las unidades se descuentan solas del inventario. |
| 📦 **Inventario** (F2) | Tarjetas con foto, unidades y precio. Buscar, filtrar por categoría, ver **solo lo que hay que pedir**, registrar **Llegó mercancía** y **Descargar Excel** en cualquier momento. |
| 👥 **Clientes** (F3) | Nombre, placa, vehículo, **kilometraje** y celular. Muestra **cuánto ha comprado, cuánto te debe y cuántas devoluciones tiene cada cliente**, el detalle de cada compra y **Registrar abono** cuando paga lo que debía. |
| 🚚 **Proveedores** (F4) | A quién se le compra, **cuánto se le ha comprado, cuánto se le ha pagado y cuánto se le debe**, cada entrada de mercancía, **Registrar pago** para ir pagando lo que se quedó debiendo y **Devolución o garantía** para lo que se le devuelve. |
| 🧾 **Historial de ventas** (F5) | Ventas de hoy, la semana, el mes o el año, con totales y el detalle de cada venta, desde donde se registra una **devolución o garantía**. |

### Conteo de unidades

Las unidades nunca se escriben a mano después de crear el repuesto: cambian solas y cada cambio queda registrado.

- **Venta** → resta (si hay 20 baterías y se vende 1, quedan 19). No deja vender más de lo que hay.
- **Llegó mercancía** → suma y actualiza el costo.
- **Anular venta** (administrador) → devuelve las unidades.
- **Ajustar unidades** (administrador) → para conteos físicos, daños o pérdidas, con el motivo.

Cada repuesto tiene un mínimo: cuando quedan esas unidades o menos sale marcado **por pedir**.

### Lo que se debe

- **A un proveedor:** en **Llegó mercancía**, marcar **Le quedé debiendo parte** y escribir cuánto se pagó.
  Lo que falta queda en la columna **Le debes** de Proveedores. Cada pago posterior se registra con
  **Registrar pago** (cuánto, cómo y una nota, por ejemplo el número del comprobante).
- **Un cliente:** en **Vender**, elegir el cliente, marcar **Queda debiendo (fiado)** y escribir cuánto abona
  ahora (puede ser 0). Lo que falta queda en la columna **Te debe** de Clientes y se va bajando con **Registrar abono**.
- No se puede pagar ni abonar más de lo que se debe. Si un pago o abono se anotó mal, el administrador lo borra
  tocando dos veces su fila en **Entradas y devoluciones** o **Compras y devoluciones**.
- Al anular una venta fiada, su deuda desaparece.
- Al actualizar desde una versión anterior, todo lo que ya estaba registrado queda como pagado completo.

### Devoluciones y garantías

- **De un cliente:** abrir la venta (desde **Historial** o desde **Compras y devoluciones** del cliente) y tocar
  **Devolución o garantía**. Se elige el repuesto, el motivo y qué se le dio: **otro igual** (sale del inventario),
  **la plata** o **una rebaja de lo que debe**. Si el repuesto devuelto está bueno, se marca y vuelve al inventario.
- **A un proveedor:** en Proveedores, **Devolución o garantía**. Si todavía no responde, queda **pendiente**;
  cuando responda, se toca dos veces en **Entradas y devoluciones** y se anota si lo cambió (entra al inventario),
  devolvió la plata o lo rebajó de la deuda. Si es un repuesto malo que devolvió un cliente, se desmarca
  **Sale de mi inventario**.
- Lo devuelto en plata o rebajado se resta de lo comprado y de lo que se debe. Una venta con devoluciones no se anula;
  si una devolución se anotó mal, el administrador la borra tocándola dos veces.

### Kilometraje

Al vender con cliente se anota el kilometraje del carro; el programa muestra el de la última vez y avisa si el nuevo
es menor. También se puede escribir en el formulario del cliente. Se ve en la tabla de clientes, en sus compras y en
el detalle de cada venta.

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
