# Cambios

## [Sin publicar]

### Cambiado

- Los errores se explican con palabras sencillas en vez del mensaje técnico, y los inesperados quedan anotados en `errores.log`.
- Si un error no se atrapa, el programa lo muestra y sigue abierto en vez de cerrarse sin avisar.
- Si el programa no puede arrancar (por ejemplo, base de datos dañada), lo dice y muestra dónde quedó el detalle.

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
