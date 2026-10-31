# Estado del proyecto

Última actualización: 2026-10-31.

## Hecho

- **Core, Infrastructure y Desktop completos**: compilan sin errores ni advertencias y el formato está limpio
  (`dotnet format TallerMoreno.slnx --verify-no-changes`).
- **23 pruebas de integración pasan** (`dotnet run --project tests/TallerMoreno.Tests`).
- Todas las pantallas escritas:
  - **Secciones:** Login (con primer uso), Vender, Inventario, Clientes, Proveedores, Historial de ventas y Usuarios.
  - **Ventanas:** detalle de repuesto, formulario de repuesto con foto, ajustar unidades, llegó mercancía,
    detalle de venta o entrada, lista de compras o entradas, formularios de cliente y proveedor, nuevo usuario
    y cambiar o restablecer contraseña.
- **Verificado en la app real:**
  - Primer uso y creación de la cuenta del dueño.
  - Vender, Inventario e Historial con datos de prueba.
  - El conteo de unidades: ventas restan y entradas suman.
- README, CHANGELOG y `scripts/pruebas-docker.ps1`.
- Manejo de errores: mensajes claros, `errores.log` en la carpeta de datos y aviso si no puede arrancar.
- Fotos reales de referencia por categoría para los repuestos sin foto (créditos en `docs/CREDITOS-IMAGENES.md`).
- Código subido a GitHub: <https://github.com/santyxswc/GestionTaller> (rama `main`).

## Pendiente

1. **Probar a mano con clics** lo que solo cubren las pruebas automáticas:
   - Crear un repuesto con foto (también foto de celular, para la reducción y la orientación EXIF).
   - Cobrar desde Vender: agregar al carrito, usar +/−, elegir cliente, "Nuevo cliente" y el aviso al cobrar.
   - Llegó mercancía con proveedor nuevo.
   - Ajustar unidades y eliminar repuesto.
   - Descargar Excel de inventario, clientes y ventas, y abrirlo.
   - Copia de seguridad `.zip` y restaurarla.
   - Anular venta y ver que las unidades vuelven.
   - Entrar como Mecánico y comprobar que no ve costos, ganancias ni las acciones de administrador.
2. **Correr las pruebas en Docker** con Docker Desktop abierto:
   `powershell -ExecutionPolicy Bypass -File scripts/pruebas-docker.ps1`.
3. Opcional: publicar el ejecutable para el computador del taller
   (`dotnet publish src/TallerMoreno.Desktop -c Release -r win-x64 --self-contained -o publicar`).

## Problemas conocidos

- **Chip "Luces":** una vez apareció seleccionado en Inventario sin tocarlo. No se pudo repetir.
  Si vuelve a pasar, revisar la búsqueda por teclado del `ListBox.chips` (selecciona al escribir letras).
- **`dotnet test`:** en este equipo falla con "Se recibió un nuevo protocolo de enlace…". Es un error del
  comando, no de las pruebas. Usar `dotnet run --project tests/TallerMoreno.Tests`.
  En GitHub Actions (Linux) se deja `dotnet test`.
- **Campo de placa:** el `TextBox` de Avalonia 11.3 no tiene `CharacterCasing`. La placa la pasa a
  mayúsculas `ClienteService` al guardar.

## Para probar con datos

Para que la app use otra carpeta de datos, crea `appsettings.json` junto al ejecutable
(`src/TallerMoreno.Desktop/bin/Debug/net10.0/`):

```json
{ "Datos": { "Carpeta": "C:/ruta/a/datos-de-prueba" } }
```

Ese archivo está en `.gitignore`. Sin él, la app usa `%LOCALAPPDATA%\TallerMoreno`.
