# Cambios

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
