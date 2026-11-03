/**
 * @file FotoReferencia.cs
 * @brief Elige la foto de referencia de un repuesto sin foto propia.
 * @author Santiago Caicedo
 */
using System.Globalization;

namespace TallerMoreno.Core
{
    /**
     * @brief Elige la foto de referencia mirando primero el nombre del repuesto y después su categoría.
     *
     * La categoría sola no basta: una "Batería" guardada en "Eléctricos" mostraba un alternador.
     * El nombre dice mejor qué es, así que se buscan palabras como "batería", "filtro" o "pastillas"
     * y solo si no aparece ninguna se usa la foto de la categoría.
     */
    public static class FotoReferencia
    {
        /** Opciones que ignoran mayúsculas y tildes. */
        private const CompareOptions Opciones = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

        /** Foto para lo que no se reconoce. */
        public const string Otros = "otros";

        /** Todas las fotos que puede devolver Elegir; cada una debe existir en Assets/Categorias. */
        public static IReadOnlyCollection<string> Todas =>
            PorNombre.Select(p => p.Foto).Concat(PorCategoria.Values).Append(Otros).Distinct().ToList();

        /**
         * Palabras del nombre y la foto que les toca, en orden: la primera que aparezca gana.
         * Lo más específico va antes ("filtro de aceite" es un filtro, "líquido de frenos" es de frenos).
         * Cada palabra coincide con el comienzo de una palabra del nombre ("pastilla" encuentra "pastillas").
         */
        private static readonly (string Foto, string[] Palabras)[] PorNombre =
        {
            ("filtros", ["filtro"]),
            ("baterias", ["bateria", "acumulador"]),
            ("llantas", ["llanta", "neumatico", "rin"]),
            ("frenos", ["freno", "pastilla", "zapata", "campana", "caliper", "mordaza"]),
            ("suspension", ["amortiguador", "suspension", "espiral", "resorte", "tijera", "rotula", "buje", "terminal de direccion", "axial", "barra estabilizadora"]),
            ("correas", ["correa", "distribucion", "tensor"]),
            ("refrigeracion", ["radiador", "refrigerante", "termostato", "bomba de agua", "ventilador", "anticongelante", "manguera"]),
            // Antes que motor: "Aceite de motor 20W50" es un aceite.
            ("aceites", ["aceite", "lubricante", "grasa", "liquido", "valvulina", "aditivo"]),
            ("luces", ["bombillo", "farola", "faro", "luz", "luces", "stop", "direccional", "exploradora", "led"]),
            ("electricos", ["alternador", "arranque", "burro", "fusible", "rele", "sensor", "cable", "regulador", "switch", "pito", "bobina"]),
            ("motor", ["bujia", "piston", "empaque", "culata", "valvula", "motor", "anillo", "carburador", "inyector", "ciguenal", "embrague", "clutch"]),
            ("accesorios", ["tapete", "forro", "ambientador", "kit de carretera", "extintor", "llavero", "plumilla", "limpiaparabrisas"]),
        };

        /** Foto de cada categoría, para cuando el nombre no dice qué es. */
        private static readonly Dictionary<string, string> PorCategoria = new()
        {
            ["Baterías"] = "baterias",
            ["Llantas"] = "llantas",
            ["Aceites y lubricantes"] = "aceites",
            ["Filtros"] = "filtros",
            ["Frenos"] = "frenos",
            ["Suspensión"] = "suspension",
            ["Motor"] = "motor",
            ["Eléctricos"] = "electricos",
            ["Luces"] = "luces",
            ["Correas"] = "correas",
            ["Refrigeración"] = "refrigeracion",
            ["Accesorios"] = "accesorios",
        };

        /**
         * @brief Nombre del archivo de la foto de referencia (sin extensión).
         * @param nombre Nombre del repuesto, como "Batería 12V 45Ah"
         * @param categoria Categoría del repuesto
         * @return Uno de baterias, llantas, aceites, filtros, frenos, suspension, motor, electricos,
         *         luces, correas, refrigeracion, accesorios u otros
         */
        public static string Elegir(string nombre, string categoria)
        {
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                // Signos como paréntesis, guiones o barras separan palabras: "Batería-12V", "(Freno)", "Filtro/aire".
                string limpio = new(nombre.Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray());
                string texto = " " + string.Join(' ', limpio.Split(' ', StringSplitOptions.RemoveEmptyEntries)) + " ";
                foreach (var (foto, palabras) in PorNombre)
                    if (palabras.Any(p => EmpiezaPalabra(texto, p)))
                        return foto;
            }
            return categoria != null && PorCategoria.TryGetValue(categoria, out string porCategoria) ? porCategoria : Otros;
        }

        /**
         * @brief Indica si alguna palabra del texto empieza por la palabra buscada, sin importar tildes ni mayúsculas.
         * @param texto Nombre con un espacio al comienzo y al final
         * @param palabra Palabra o frase buscada
         * @return true si aparece al comienzo de una palabra
         */
        private static bool EmpiezaPalabra(string texto, string palabra) =>
            Formato.Cultura.CompareInfo.IndexOf(texto, " " + palabra, Opciones) >= 0;
    }
}
