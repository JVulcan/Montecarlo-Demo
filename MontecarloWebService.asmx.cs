using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

using Montecarlo; // <- Importar clases del módulo
using System.Windows.Forms; // <- es requisito añadir esta referencia para poder manejar la clase TreeNode2

namespace WebServiceMontecarlo
{
    
    /// <summary>
    /// Descripción breve de Montecarlo
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // Para permitir que se llame a este servicio web desde un script, usando ASP.NET AJAX, quite la marca de comentario de la línea siguiente. 
    // [System.Web.Script.Services.ScriptService]
    public class MontecarloWS : System.Web.Services.WebService
    {
        public class Data
        {
            public string Token; // ticket de la simulación
            public PSMontecarlo Instancia; // instancia de la simulación
            public bool Preparado; // indica si el modelo de la simulación está preparado para ingresar valores
            public PSMontecarloResult Resultado; // referencia al resultado
            public bool ResultadoListo; // indica si el resultado está listo (simulación terminada)

            public Data(string tkn, PSMontecarlo inst)
            {
                if (inst == null)
                    return; // error, no puede ser null
                if (tkn == null)
                    return; // error, no puede ser null
                this.Token = tkn;
                this.Instancia = inst;
                this.Preparado = false;
                // resultado se colocará cuando termine la simulación
                this.ResultadoListo = false;
                this.Resultado = null;

                inst.PSMontecarloCompleted += SimulationCompleted; // enlazar evento de término de simulación
            }
            // cuando la simulación haya acabado, mandar el resultado a la variable de esta clase.
            private void SimulationCompleted(object sender, PSMontecarloCompletedEventArgs e)
            {
                this.ResultadoListo = true;
                this.Resultado = e.Resultado;
            }
        } // end Data

        // Mensaje de error predeterminado al no encontrar la simulación con el Ticket dado
        private const string TokenNotFound = "No se pudo encontrar la simulación asociada al Ticket";

        /// <summary>
        /// Lista de simulaciones actualmente en uso
        /// </summary>
        private static List<Data> simulaciones = new List<Data>();

        //////////////////////////////////////////////////////////
        // LISTA DE FUNCIONES QUE SE EXPONEN EN EL WEB SERVICE
        /////////////////////////////////////////////////////////

        /// <summary>
        /// Se reserva espacio para una nueva Simulación.
        /// </summary>
        /// <returns>Ticket que se asocia a la simulación creada. Se utiliza para las siguientes funciones</returns>
        [WebMethod]
        public string NuevaSimulacion()
        {
            string token = generateNewToken();

            PSMontecarlo inst = new PSMontecarlo();
            // predeterminado utilizar 1 hilo/procesador
            inst.NumeroProcesadores = 1;

            simulaciones.Add( new Data(token, inst) );

            return token;
        }
        /// <summary>
        /// Después de haber terminado la simulación y guardado los resultados, asegúrese de eliminar la instancia llamando a esta función
        /// </summary>
        /// <param name="token">Ticket de la simulación a Terminar</param>
        /// <returns>'ok' si fue exitoso, de lo contrario es un error</returns>
        [WebMethod]
        public string Terminar(string token)
        {
            int index = getIndexOf(token);
            if (index == -1)
                return TokenNotFound;

            simulaciones.RemoveAt(index);
            return "ok";
        }
        /// <summary>
        /// La primera función que se debe llamar después de obtener un ticket. PreRequisito: NuevaSimulacion
        /// </summary>
        /// <param name="token">Ticket de la simulación</param>
        /// <param name="cantidadAlts">Cantidad de Alternativas (Productos). Mínimo 1</param>
        /// <returns>'ok' si fue exitoso, de lo contrario es un error</returns>
        [WebMethod]
        public string setCantidadAlternativas(string token, int cantidadAlts)
        {
            PSMontecarlo inst = getInstance(token);
            if (inst == null)
                return TokenNotFound;
            if (cantidadAlts < 1)
                return "Alternativas debe ser mayor a cero";
            inst.CantidadAlternativas = cantidadAlts;
            return "ok";
        }
        /// <summary>
        /// Se establece la cantidad de Iteraciones de la simulación. PreRequisito: NuevaSimulacion
        /// </summary>
        /// <param name="token">Ticket de la simulación</param>
        /// <param name="cantidadIter">Cantidad/Número de Iteraciones para la simulación. Mínimo 1</param>
        /// <returns>'ok' si fue exitoso, de lo contrario es un error</returns>
        [WebMethod]
        public string setIteraciones(string token, int cantidadIter)
        {
            PSMontecarlo inst = getInstance(token);
            if (inst == null)
                return TokenNotFound;
            if (cantidadIter < 1)
                return "Iteraciones debe ser mayor a cero";
            inst.NumeroIteraciones = cantidadIter;
            return "ok";
        }
        /// <summary>
        /// Colocar los pesos de cada Criterio. PreRequisito: setCantidadAlternativas
        /// </summary>
        /// <param name="token">Ticket de la simulación</param>
        /// <param name="pesos">Lista de pesos. Se infiere que el valor 1.0 significa 100%. Mínimo 2 valores y la suma de ellos debería dar 1.0</param>
        /// <returns>'ok' si fue exitoso, de lo contrario es un error</returns>
        [WebMethod]
        public string setPesosCriterios(string token, List<double> pesos)
        {
            PSMontecarlo inst = getInstance(token);
            if (inst == null)
                return TokenNotFound;
            if (pesos == null || pesos.Count < 2)
                return "La cantidad de pesos no es válida";
            // la cantidad de valores en 'pesos' nos dirá automáticamente
            // cuántos criterios debemos colocar
            TreeNode2 root = createTree(pesos.Count, inst.CantidadAlternativas);
            root.Tag2 = pesos; // <- guardar pesos para llenarlos después
            inst.ArbolJerarquico = root;
            return "ok";
        }
        /// <summary>
        /// Coloca para un Criterio su modo de cálculo Montecarlo (Uniforme, Triangular). PreRequisito: setPesosCriterios
        /// </summary>
        /// <param name="token">Ticket de la simulación</param>
        /// <param name="IndiceCriterio">Índice del Criterio en base cero</param>
        /// <param name="modo">Uniforme o Triangular (0 o 1)</param>
        /// <returns>'ok' si fue exitoso, de lo contrario es un error</returns>
        [WebMethod]
        public string setModoMontecarloCriterio(string token, int IndiceCriterio, int modo)
        {
            PSMontecarlo inst = getInstance(token);
            if (inst == null)
                return TokenNotFound;
            if (inst.ArbolJerarquico == null)
                return "PreRequisito no ha sido llamado";
            if (inst.ArbolJerarquico.Nodos.Count - 1 < IndiceCriterio || IndiceCriterio < 0)
                return "El índice de criterio no es válido";
            if (modo != 0 && modo != 1) // <- el modo no es un valor válido.
                return "El modo Montecarlo entregado no es válido";
            TreeNode2 criterio = (TreeNode2)inst.ArbolJerarquico.Nodos[IndiceCriterio];
            criterio.ModoMontecarlo = (ModoMontecarlo)modo;
            return "ok";
        }

        /// <summary>
        /// Prepara el árbol para ingresar sus valores. PreRequisito: setModoMontecarloCriterio en cada Criterio
        /// </summary>
        /// <param name="token">Ticket de la simulación</param>
        /// <returns>'ok' si fue exitoso, de lo contrario es un error</returns>
        [WebMethod]
        public string prepararArbol(string token)
        {
            int index = getIndexOf(token);
            if (index == -1)
                return TokenNotFound;
            Data data = simulaciones[index];
            PSMontecarlo inst = data.Instancia;
            if (inst == null)
                return TokenNotFound;
            if (inst.ArbolJerarquico == null)
                return "PreRequisito no ha sido llamado";

            // cada vez que se llama a preparar se resetean los valores.
            // sólo preparar cuando no lo estén.
            // De esta manera, se puede llamar a esta función cada
            // vez que se ingresarán valores a los criterios sin preocupación
            // de que los valores se reseteen.
            if (!data.Preparado)
            {
                try
                {
                    inst.ArbolJerarquico.PrepararArbol(true);
                }
                catch(Exception e)
                {
                    return e.Message.ToString();
                }
                
                data.Preparado = true;

                // ingresar los pesos de cada criterio que habíamos guardado:
                TreeNode2 arbol = inst.ArbolJerarquico;
                List<double> pesos = (List<double>)arbol.Tag2; // ver línea 149 de este mismo archivo
                for (int i = 0; i < pesos.Count; i++)
                {
                    // valor discreto
                    arbol.ValoresDirecto[i] = pesos[i];
                    // mismo valor para riesgos para que no haya variabilidad
                    arbol.ValoresDirectoRiesgo[i, (int)Indice.Uniforme_Izquierda] = pesos[i]; // 0
                    arbol.ValoresDirectoRiesgo[i, (int)Indice.Uniforme_Derecha] = pesos[i]; // 1
                }
            }
            
            return "ok";
        }
        /// <summary>
        /// Coloca el valor discreto para un Criterio específico. PreRequisito: prepararArbol
        /// </summary>
        /// <param name="token">Ticket de la simulación</param>
        /// <param name="IndiceCriterio">Índice del Criterio a establecer valor</param>
        /// <param name="valor">Valor discreto a colocar. 1.0 significa 100%.
        /// <param name="IndiceAlternativa">Índice de la Alternativa/Producto en base cero</param>
        /// Se infiere que el valor 1.0 significa 100%.</param>
        /// <returns>'ok' si fue exitoso, de lo contrario es un error</returns>
        [WebMethod]
        public string setValorDiscretoCriterio(string token, int IndiceCriterio, double valor, int IndiceAlternativa)
        {
            int index = getIndexOf(token);
            if (index == -1)
                return TokenNotFound;
            Data data = simulaciones[index];
            PSMontecarlo inst = data.Instancia;
            if (inst == null)
                return TokenNotFound;
            if (inst.ArbolJerarquico == null)
                return "PreRequisito no ha sido llamado";
            if (!data.Preparado)
                return "El modelo no ha sido preparado aún. Llame a prepararArbol primero.";
            if (inst.ArbolJerarquico.Nodos.Count - 1 < IndiceCriterio || IndiceCriterio < 0)
                return "El índice de criterio no es válido";
            if (inst.CantidadAlternativas - 1 < IndiceAlternativa || IndiceAlternativa < 0)
                return "El índice de alternativa no es válido";

            TreeNode2 criterio = (TreeNode2)inst.ArbolJerarquico.Nodos[IndiceCriterio];
            criterio.ValoresDirecto[IndiceAlternativa] = valor * 100.0;

            return "ok";
        }
        /// <summary>
        /// Coloca los valores de riesgo para el Criterio dado en la Alternativa dada. PreRequisito: prepararArbol
        /// </summary>
        /// <param name="token">Ticket de la simulación</param>
        /// <param name="IndiceCriterio">Índice del Criterio a establecer valor</param>
        /// <param name="valores">Valores de riesgo: 2 valores cuando es Uniforme y 3 valores cuando es Triangular</param>
        /// <param name="IndiceAlternativa">Índice de la Alternativa/Producto para colocar el valor</param>
        /// <returns>'ok' si fue exitoso, de lo contrario es un error</returns>
        [WebMethod]
        public string setValoresRiesgosCriterio(string token, int IndiceCriterio, List<double> valores, int IndiceAlternativa)
        {
            int index = getIndexOf(token);
            if (index == -1)
                return TokenNotFound;
            Data data = simulaciones[index];
            PSMontecarlo inst = data.Instancia;
            if (inst == null)
                return TokenNotFound;
            if (inst.ArbolJerarquico == null)
                return "PreRequisito no ha sido llamado";
            if (!data.Preparado)
                return "El modelo no ha sido preparado aún. Llame a prepararArbol primero.";
            if (inst.ArbolJerarquico.Nodos.Count - 1 < IndiceCriterio)
                return "El índice sobrepasa a la cantidad de Criterios actual";
            if (inst.CantidadAlternativas - 1 < IndiceAlternativa || IndiceAlternativa < 0)
                return "El índice de la Alternativa no es válido";

            TreeNode2 criterio = (TreeNode2)inst.ArbolJerarquico.Nodos[IndiceCriterio];
            if (valores == null || valores.Count < 2 || valores.Count > 3 || // valores no debe ser null y debe ser entre 2 y 3 datos
                (criterio.ModoMontecarlo == ModoMontecarlo.Uniforme && valores.Count != 2) || // si el criterio es uniforme, valores debe posser 2 datos
                (criterio.ModoMontecarlo == ModoMontecarlo.Triangular && valores.Count != 3) || // si es triangular debe posser 3 datos
                (criterio.ModoMontecarlo == ModoMontecarlo.Indefinido)) // no puede ser indefinido
                return "Los valores no coinciden con los datos del Criterio";

            // Como la cantidad de datos en valores coincide con el modo Montecarlo del criterio,
            // se pueden ingresar los valores directamente usando los índices de cada dato en valores.
            // Obviamente tomando en cuenta que el primero representa el rango izquierdo y el último el rango derecho.
            for (int i = 0; i < valores.Count; i++)
                criterio.ValoresDirectoRiesgo[IndiceAlternativa, i] = valores[i] * 100.0;
            
            return "ok";
        }
        /// <summary>
        /// Comienza la simulación. PreRequisito: setValoresRiesgoCriterio en cada Criterio
        /// </summary>
        /// <param name="token">Ticket de la simulación</param>
        /// <returns>'ok' si fue exitoso, de lo contrario es un error</returns>
        [WebMethod]
        public string ComenzarSimulacion(string token)
        {
            PSMontecarlo inst = getInstance(token);
            if (inst == null)
                return TokenNotFound;

            try
            {
                inst.ComenzarSimulacion();
            }
            catch (Exception e)
            {
                return e.Message.ToString();
            }
            
            return "ok";
        }
        /// <summary>
        /// Pregunta si la simulación en relación al Ticket ha terminado
        /// </summary>
        /// <param name="token">Ticket de la simulación</param>
        /// <returns>true si la simulación terminó, de lo contrario false</returns>
        [WebMethod]
        public bool EstaTerminado(string token)
        {
            int index = getIndexOf(token);
            if (index == -1)
                return false;

            if (simulaciones[index].ResultadoListo)
                return true;
            else
                return false;
        }
        /// <summary>
        /// Devuelve los resultados de Simulación. Recuerde llamar a la función Terminar cuando haya acabado con el uso de los datos.
        /// </summary>
        /// <param name="token">Ticket de la simulación</param>
        /// <returns>clase PSMontecarloResult</returns>
        [WebMethod]
        public PSMontecarloResult ObtenerResultado(string token)
        {
            if (EstaTerminado(token))
                return simulaciones[getIndexOf(token)].Resultado;
            else
                return null;
        }

        //////////////////////////////////////////////////////////////
        // FUNCIONES DE UTILIDAD
        //////////////////////////////////////////////////////////////
        private static Random rand = new Random();
        private static string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private static int tokenLength = 35;
        /// <summary>
        /// Genera un nuevo Token. No es 100% seguro, no se recomienda utilizarlo en producción.
        /// </summary>
        /// <returns></returns>
        internal string generateNewToken()
        {
            char[] token = new char[tokenLength];

            for (int i = 0; i < token.Length; i++)
                token[i] = chars[rand.Next(chars.Length)];

            return new string(token);
        }

        /// <summary>
        /// Obtiene el índice en el que se encuentra la simulación en 'instancias'
        /// </summary>
        /// <param name="token">Ticket de la simulación</param>
        /// <returns>Índice. Devuelve -1 si no se encontró.</returns>
        internal int getIndexOf(string token)
        {
            if (simulaciones.Count > 0)
            {
                for (int i = 0; i < simulaciones.Count; i++)
                    if (simulaciones[i].Token == token)
                        return i;
            }
            return -1;
        }
        /// <summary>
        /// Obtiene la instancia de simulación que corresponde al Ticket
        /// </summary>
        /// <param name="token">Ticket de la simulación</param>
        /// <returns>PSMontecarlo. Devuelve null/Nothing si no se encontró</returns>
        internal PSMontecarlo getInstance(string token)
        {
            if (simulaciones.Count > 0)
            {
                foreach (Data item in simulaciones)
                    if (item.Token == token)
                        return item.Instancia;
            }
            return null;
        }

        /// <summary>
        /// Genera un árbol
        /// </summary>
        /// <param name="criteriaNumber">Número de criterios de la simulación</param>
        /// <param name="alternativeCount">Cantidad de Alternativas (Productos) de la simulación</param>
        /// <returns></returns>
        internal TreeNode2 createTree(int criteriaNumber, int alternativeCount)
        {
            try
            {
                // el nodo principal raíz será tipo Directo para poder ingresar sus pesos
                // de manera directa. No es necesario colocarlo en modo Pares porque el
                // cálculo de los pesos en modo Pares ya fue hecho en SharePoint.
                // Da igual el modo montecarlo de este nodo.
                TreeNode2 root = new TreeNode2(alternativeCount, ModoMontecarlo.Uniforme, TipoCriterio.Directo);
                for (int i = 0; i < criteriaNumber; i++)
                {
                    // los hijos también serán en modo Directo;
                    // Nota: es obligatorio si se usa 1 sola Alternativa
                    TreeNode2 child = new TreeNode2(alternativeCount, ModoMontecarlo.Indefinido, TipoCriterio.Directo);
                    
                    child.Padre = root; // <-- de esta manera se deben agregar los hijos al árbol
                }
                return root;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
