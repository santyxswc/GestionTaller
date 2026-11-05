# Cambios

## [1.4.0] - 2026-11-04

### Agregado

- **Devoluciones y garantías de clientes:** en el detalle de una venta, **Devolución o garantía**: qué repuesto
  devolvió, por qué, y si se le cambió por otro, se le devolvió la plata o se le rebajó de lo que debe. Si el
  repuesto está bueno vuelve al inventario. Clientes muestra cuántas devoluciones tiene cada uno.
- **Devoluciones a proveedores:** botón **Devolución o garantía** en Proveedores. Si el proveedor todavía no
  responde, queda pendiente y luego se anota si lo cambió, devolvió la plata o lo rebajó de la deuda.
  La tabla muestra las devoluciones y las garantías sin respuesta.
- **Kilometraje del carro:** se anota al vender (con el último kilometraje a la vista) o en el formulario del
  cliente. Se ve en la tabla de clientes, en sus compras y en el detalle de cada venta.
- Columnas "Kilometraje" y "Devoluciones" en el Excel de clientes.

### Cambiado

- La base de datos pasa al esquema versión 4. Lo registrado antes no cambia.
- Lo devuelto en plata o rebajado se descuenta de lo comprado y, si fue en plata, también de lo pagado.
- Una venta con devoluciones ya no se puede anular.
- En la tabla de clientes la placa y el vehículo van juntos y la última compra queda en la ventana de compras.

### Corregido

- El detalle de una venta cortaba el título de la columna "Cant.".

## [1.3.0] - 2026-11-04

### Agregado

- **Lo que se les debe a los proveedores:** al registrar mercancía se puede marcar "Le quedé debiendo parte" y
  escribir cuánto se pagó. Proveedores muestra lo comprado, lo pagado y lo que se debe, con **Registrar pago**.
- **Lo que deben los clientes:** en Vender, "Queda debiendo (fiado)" con lo que abona ahora. Clientes muestra
  lo que debe cada uno, con **Registrar abono**.
- Las ventanas "Entradas y pagos" y "Compras y abonos" muestran los pagos junto a las entradas y compras;
  el administrador puede borrar un pago o abono anotado por error.
- Columna "Debe" en el Excel de clientes.

### Cambiado

- La base de datos pasa al esquema versión 3. Lo que ya estaba registrado queda como pagado completo y antes
  de actualizar se guarda una copia automática.
- Las tablas de Clientes y Proveedores ponen primero las cifras de dinero; el número de compras y el contacto
  del proveedor quedan en la ventana de detalle.

## [1.2.1] - 2026-11-03

### Corregido

- La foto de referencia ahora se elige primero por el nombre del repuesto y después por la categoría: una "Batería" guardada en "Eléctricos" mostraba un alternador.
- Si la foto propia de un repuesto se borró o está dañada, se muestra la de referencia en vez de un cuadro vacío. Ningún repuesto queda sin imagen.
- La tabla "Historial de unidades" del detalle cortaba los títulos de las columnas.

## [1.2.0] - 2026-11-03

### Agregado

- Copia de seguridad automática diaria en `copias\` (se guardan las últimas 14), con carpeta extra opcional (`Copias:CarpetaExtra`) para una USB o la nube.
- **Restaurar copia** desde el menú: revisa que la copia sea válida, guarda antes cómo estaba todo y no hay que cerrar el programa ni descomprimir a mano.
- Código de recuperación para el dueño y **¿Olvidaste tu contraseña?** en la entrada.
- Copia automática antes de que una versión nueva actualice la base de datos.

### Cambiado

- Si el programa ya está abierto, al abrirlo otra vez se trae al frente la ventana que hay en vez de abrir otra.
- Los datos de una versión más nueva del programa no se abren; se pide instalar la última versión.

### Corregido

- Restaurar descomprimiendo el `.zip` sobre la carpeta de datos podía dañar la base de datos si quedaban los archivos `-wal` y `-shm`.

## [1.1.0] - 2026-11-03

### Agregado

- Instalador para Windows (Inno Setup) con ícono propio y acceso directo en el escritorio.
- Workflow de GitHub que crea el instalador y lo publica en Releases al subir una etiqueta `v*`.

### Cambiado

- Los errores se explican con palabras sencillas en vez del mensaje técnico, y los inesperados quedan anotados en `errores.log`.
- Si un error no se atrapa, el programa lo muestra y sigue abierto en vez de cerrarse sin avisar.
- Si el programa no puede arrancar (por ejemplo, base de datos dañada), lo dice y muestra dónde quedó el detalle.
- Los repuestos sin foto muestran una foto real de su categoría (batería, llanta, aceite…) marcada como "Foto de referencia", en vez de un emoji. Créditos en `docs/CREDITOS-IMAGENES.md`.

## [1.0.0] - 2026-10-26

### Agregado

- Vender: repuestos en tarjetas con foto, carrito, cliente opcional, método de pago y cobro en un toque.
- Inventario con fotos, búsqueda, categorías, filtro "Solo por pedir", entradas de mercancía y descarga en Excel.
- Conteo automático de unidades: ventas, entradas, anulaciones y ajustes quedan registrados como movimientos.
- Clientes con total comprado, número de compras y detalle de cada compra; descarga en Excel.
- Proveedores con total comprado y detalle de cada entrada de mercancía.
- Historial de ventas por periodo con totales, ganancia y lo más vendido (administrador), anulación y Excel.
- Usuarios con roles Administrador y Mecánico; cuenta del dueño al primer uso.
- Copia de seguridad en `.zip` con la base de datos y las fotos.
- Pruebas de integración y script para correrlas en Docker.
