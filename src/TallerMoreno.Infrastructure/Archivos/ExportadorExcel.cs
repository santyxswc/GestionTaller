/**
 * @file ExportadorExcel.cs
 * @brief Listados en Excel con ClosedXML.
 * @author Santiago Caicedo
 */
using ClosedXML.Excel;
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Core.Models;

namespace TallerMoreno.Infrastructure.Archivos
{
    /**
     * @brief Escribe el inventario, las ventas y los clientes en archivos .xlsx.
     *
     * Cada hoja tiene un título con la fecha, encabezados fijos con filtro, formato de pesos
     * y una fila de totales, para que se pueda imprimir o enviar tal cual.
     */
    public sealed class ExportadorExcel : IExportadorExcel
    {
        /** Formato de pesos sin decimales. */
        private const string FormatoPesos = "\"$\" #,##0";
        /** Fila de los encabezados (la 1 es el título y la 2 la fecha). */
        private const int FilaEncabezado = 4;

        /**
         * @brief Exporta el inventario con stock, precios y valor en bodega.
         * @param repuestos Repuestos a exportar
         * @param ruta Archivo .xlsx de destino
         *
         * Los repuestos con poco stock quedan resaltados en rojo claro.
         */
        public void ExportarInventario(IEnumerable<Repuesto> repuestos, string ruta)
        {
            var lista = repuestos.ToList();
            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Inventario");
            string[] columnas = { "Código", "Repuesto", "Categoría", "Marca", "Ubicación", "Proveedor", "Stock", "Stock mínimo", "Estado", "Precio compra", "Precio venta", "Valor en bodega" };
            double[] anchos = { 14, 34, 18, 16, 14, 22, 9, 13, 13, 15, 15, 17 };
            Encabezado(hoja, "Inventario · Taller Moreno", columnas, anchos);

            int fila = FilaEncabezado + 1;
            foreach (var r in lista)
            {
                hoja.Cell(fila, 1).Value = r.Codigo;
                hoja.Cell(fila, 2).Value = r.Nombre;
                hoja.Cell(fila, 3).Value = r.Categoria;
                hoja.Cell(fila, 4).Value = r.Marca;
                hoja.Cell(fila, 5).Value = r.Ubicacion;
                hoja.Cell(fila, 6).Value = r.ProveedorNombre;
                hoja.Cell(fila, 7).Value = r.Stock;
                hoja.Cell(fila, 8).Value = r.StockMinimo;
                hoja.Cell(fila, 9).Value = r.EstadoStock;
                hoja.Cell(fila, 10).Value = r.PrecioCompra;
                hoja.Cell(fila, 11).Value = r.PrecioVenta;
                hoja.Cell(fila, 12).FormulaA1 = $"G{fila}*J{fila}";
                if (r.StockBajo)
                    hoja.Range(fila, 1, fila, columnas.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#FDECEA");
                fila++;
            }

            int ultima = fila - 1;
            hoja.Cell(fila, 2).Value = $"{lista.Count} repuestos · {lista.Count(r => r.StockBajo)} por pedir";
            Total(hoja, fila, 7, ultima, formatoPesos: false);
            Total(hoja, fila, 12, ultima);
            hoja.Range(FilaEncabezado + 1, 10, fila, 12).Style.NumberFormat.Format = FormatoPesos;
            Cerrar(hoja, fila, columnas.Length);
            libro.SaveAs(ruta);
        }

        /**
         * @brief Exporta un listado de ventas.
         * @param ventas Ventas a exportar
         * @param titulo Título de la hoja
         * @param ruta Archivo .xlsx de destino
         *
         * Las ventas anuladas aparecen tachadas y no suman en los totales.
         */
        public void ExportarVentas(IEnumerable<Venta> ventas, string titulo, string ruta)
        {
            var lista = ventas.ToList();
            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Ventas");
            string[] columnas = { "N°", "Fecha", "Cliente", "Placa", "Vendió", "Pago", "Unidades", "Total", "Ganancia", "Estado" };
            double[] anchos = { 8, 18, 30, 11, 20, 18, 10, 15, 15, 11 };
            Encabezado(hoja, titulo + " · Taller Moreno", columnas, anchos);

            int fila = FilaEncabezado + 1;
            foreach (var v in lista)
            {
                hoja.Cell(fila, 1).Value = v.Id;
                hoja.Cell(fila, 2).Value = v.Fecha;
                hoja.Cell(fila, 3).Value = v.Cliente;
                hoja.Cell(fila, 4).Value = v.ClientePlaca;
                hoja.Cell(fila, 5).Value = v.UsuarioNombre;
                hoja.Cell(fila, 6).Value = v.MetodoPago;
                hoja.Cell(fila, 7).Value = v.Unidades;
                hoja.Cell(fila, 8).Value = v.Total;
                hoja.Cell(fila, 9).Value = v.Ganancia;
                hoja.Cell(fila, 10).Value = v.Estado;
                if (v.Anulada)
                {
                    var rango = hoja.Range(fila, 1, fila, columnas.Length).Style;
                    rango.Font.Strikethrough = true;
                    rango.Font.FontColor = XLColor.Gray;
                }
                fila++;
            }

            int ultima = fila - 1;
            hoja.Cell(fila, 3).Value = $"{lista.Count(v => !v.Anulada)} ventas";
            foreach (int col in new[] { 7, 8, 9 })
            {
                string letra = XLHelper.GetColumnLetterFromNumber(col);
                var celda = hoja.Cell(fila, col);
                celda.FormulaA1 = ultima > FilaEncabezado ? $"SUMIFS({letra}{FilaEncabezado + 1}:{letra}{ultima},J{FilaEncabezado + 1}:J{ultima},\"<>Anulada\")" : "0";
                celda.Style.Font.Bold = true;
            }
            hoja.Range(FilaEncabezado + 1, 2, fila, 2).Style.DateFormat.Format = "dd/mm/yyyy hh:mm";
            hoja.Range(FilaEncabezado + 1, 8, fila, 9).Style.NumberFormat.Format = FormatoPesos;
            Cerrar(hoja, fila, columnas.Length);
            libro.SaveAs(ruta);
        }

        /**
         * @brief Exporta los clientes con lo que ha comprado cada uno.
         * @param clientes Clientes a exportar
         * @param ruta Archivo .xlsx de destino
         */
        public void ExportarClientes(IEnumerable<Cliente> clientes, string ruta)
        {
            var lista = clientes.ToList();
            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Clientes");
            string[] columnas = { "Cliente", "Documento", "Teléfono", "Placa", "Vehículo", "Compras", "Total comprado", "Última compra", "Debe" };
            double[] anchos = { 30, 15, 15, 11, 24, 10, 17, 16, 15 };
            Encabezado(hoja, "Clientes · Taller Moreno", columnas, anchos);

            int fila = FilaEncabezado + 1;
            foreach (var c in lista)
            {
                hoja.Cell(fila, 1).Value = c.Nombre;
                hoja.Cell(fila, 2).Value = c.Documento;
                hoja.Cell(fila, 3).Value = c.Telefono;
                hoja.Cell(fila, 4).Value = c.Placa;
                hoja.Cell(fila, 5).Value = c.Vehiculo;
                hoja.Cell(fila, 6).Value = c.NumeroCompras;
                hoja.Cell(fila, 7).Value = c.TotalComprado;
                if (c.UltimaCompra.HasValue)
                    hoja.Cell(fila, 8).Value = c.UltimaCompra.Value;
                hoja.Cell(fila, 9).Value = c.Deuda;
                fila++;
            }

            int ultima = fila - 1;
            hoja.Cell(fila, 1).Value = $"{lista.Count} clientes";
            Total(hoja, fila, 6, ultima, formatoPesos: false);
            Total(hoja, fila, 7, ultima);
            hoja.Range(FilaEncabezado + 1, 7, fila, 7).Style.NumberFormat.Format = FormatoPesos;
            Total(hoja, fila, 9, ultima);
            hoja.Range(FilaEncabezado + 1, 9, fila, 9).Style.NumberFormat.Format = FormatoPesos;
            hoja.Range(FilaEncabezado + 1, 8, fila, 8).Style.DateFormat.Format = "dd/mm/yyyy";
            Cerrar(hoja, fila, columnas.Length);
            libro.SaveAs(ruta);
        }

        /**
         * @brief Escribe el título, la fecha y los encabezados.
         * @param hoja Hoja
         * @param titulo Título
         * @param columnas Encabezados
         * @param anchos Ancho de cada columna
         */
        private static void Encabezado(IXLWorksheet hoja, string titulo, string[] columnas, double[] anchos)
        {
            hoja.Cell(1, 1).Value = titulo;
            hoja.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(16);
            hoja.Cell(2, 1).Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}";
            hoja.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

            for (int i = 0; i < columnas.Length; i++)
            {
                hoja.Cell(FilaEncabezado, i + 1).Value = columnas[i];
                hoja.Column(i + 1).Width = anchos[i];
            }
            var encabezado = hoja.Range(FilaEncabezado, 1, FilaEncabezado, columnas.Length).Style;
            encabezado.Font.Bold = true;
            encabezado.Font.FontColor = XLColor.White;
            encabezado.Fill.BackgroundColor = XLColor.FromHtml("#1C1C1E");
        }

        /**
         * @brief Escribe una suma de columna en la fila de totales.
         * @param hoja Hoja
         * @param fila Fila de totales
         * @param columna Columna a sumar
         * @param ultima Última fila con datos
         * @param formatoPesos true para dar formato de pesos
         */
        private static void Total(IXLWorksheet hoja, int fila, int columna, int ultima, bool formatoPesos = true)
        {
            string letra = XLHelper.GetColumnLetterFromNumber(columna);
            var celda = hoja.Cell(fila, columna);
            celda.FormulaA1 = ultima > FilaEncabezado ? $"SUM({letra}{FilaEncabezado + 1}:{letra}{ultima})" : "0";
            celda.Style.Font.Bold = true;
            if (formatoPesos)
                celda.Style.NumberFormat.Format = FormatoPesos;
        }

        /**
         * @brief Resalta la fila de totales, agrega el filtro y fija los encabezados.
         * @param hoja Hoja
         * @param filaTotal Fila de totales
         * @param columnas Número de columnas
         */
        private static void Cerrar(IXLWorksheet hoja, int filaTotal, int columnas)
        {
            var total = hoja.Range(filaTotal, 1, filaTotal, columnas).Style;
            total.Font.Bold = true;
            total.Border.TopBorder = XLBorderStyleValues.Thin;
            if (filaTotal - 1 > FilaEncabezado)
                hoja.Range(FilaEncabezado, 1, filaTotal - 1, columnas).SetAutoFilter();
            hoja.SheetView.FreezeRows(FilaEncabezado);
        }
    }
}
