using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Montecarlo;

public class formMain
{
	private void Button1_Click(object sender, EventArgs e)
	{
		lblProgress.Text = "Progreso: 0%";
		PSMontecarlo simulacion = new PSMontecarlo(numAlternativas.Value, 1, 5000);

		simulacion.ArbolJerarquico = ConfeccionarArbol();

		simulacion.PSMontecarloProgressChanged += progress_changed;
		simulacion.PSMontecarloCompleted += simulation_completed;
		simulacion.ComenzarSimulacion();
	}

	private void progress_changed(object sender, PSMontecarloProgressChangedEventArgs e)
	{
		lblProgress.Text = "Progreso: " + e.Progress.ToString() + "% {" + formatStage(e.Etapa) + "}";
	}

	private string formatStage(MonteCarloEtapas value)
	{
		switch (value) {
			case MonteCarloEtapas.EnIteraciones:
				return "En Iteraciones";
			case MonteCarloEtapas.EnTablaFrecuencias:
				return "Calculando Tabla de Frecuencias";
			case MonteCarloEtapas.EnValoresP:
				return "Calculando Valores-P";
			default:
				return "";
		}
	}

	private void simulation_completed(PSMontecarlo sender, PSMontecarloCompletedEventArgs e)
	{
		lblProgress.Text = "Simulación Finalizada";
		ShowResult(ref e.Resultado);
	}

	private void ShowResult(ref PSMontecarloResult result)
	{
		txtResult.Text = "";
		//reiniciar

		addline("Cantidad de Alternativas: " + result.CantidadAlternativas.ToString());

		for (i = 0; i <= result.ValoresMinMax.Count - 1; i++) {
			addline("Mín. Alternativa " + (i + 1).ToString() + ": " + result.ValoresMinMax(i)(0).ToString("F3"));
			addline("Máx. Alternativa " + (i + 1).ToString() + ": " + result.ValoresMinMax(i)(1).ToString("F3"));
		}
		addline("======= Valores-P =======");
		dynamic listPValues = PSMontecarloUtils.GetListaValoresP();
		for (i = 0; i <= listPValues.Count - 1; i++) {
			decimal[] values = result.ValoresP.ValorP(listPValues(i));
			string formatted = arrayToString(values, "F3");
			addline((listPValues(i) / 100f).ToString("P1") + " : " + formatted);
		}

		Chart1.Series.Clear();

		for (i = 0; i <= result.CantidadAlternativas - 1; i++) {
			Chart1.Series.Add(new DataVisualization.Charting.Series());
			Chart1.Series.Last.Name = "Alternativa " + (i + 1).ToString();
			for (j = 0; j <= result.TablaFrecuencias.Count - 1; j++) {
				double x = Convert.ToDouble(Convert.ToDecimal(result.TablaFrecuencias(j)(1)));
				double y = Convert.ToDouble(Convert.ToDecimal(result.TablaFrecuencias(j)(2 + i)));
				Chart1.Series.Last.Points.AddXY(x, y);
			}
		}
	}

	private void addline(string linea)
	{
		txtResult.Text += linea + ControlChars.NewLine;
	}

	private object arrayToString(decimal[] array, string format = "")
	{
		string final = "";
		if (format.Length == 0) {
			foreach (void v_loopVariable in array) {
				v = v_loopVariable;
				final += v.ToString().ToString + ",";
			}
		} else {
			foreach (void v_loopVariable in array) {
				v = v_loopVariable;
				final += v.ToString(format) + ",";
			}
		}
		return "{" + final.Substring(0, final.Length - 1) + "}";
	}

	private TreeNode2 ConfeccionarArbol()
	{

		TreeNode2 arbol = new TreeNode2(1, ModoMontecarlo.Triangular, TipoCriterio.Pares);

		for (i = 0; i <= numCriterios.Value - 1; i++) {
			//crear nuevo nodo, 1 alternativa, modo triangular y tipo directo
			TreeNode2 t = new TreeNode2(1, ModoMontecarlo.Triangular, TipoCriterio.Directo);
			//añadir nuevo criterio al árbol
			t.Padre = arbol;
		}

		arbol.PrepararArbol(true);
		//son 3 criterios hijos
		//discretos
		arbol.ValoresPares(0, 1) = 1.0 / 2.0;
		arbol.ValoresPares(0, 2) = 2.0;
		arbol.ValoresPares(1, 2) = 2.0;
	
		decimal ratio = arbol.RatioInconsistencia;
		if ((decimal.Compare(ratio, 0.1m) > 0)) {
			Interaction.MsgBox("es inconsistente");
		}
	
		//de riesgo colocar los mismos valores originales para que no varíe
		arbol.ValoresParesRiesgo(0, 1, Indice.Triangular_Izquierda) = 1.0 / 2.0;
		arbol.ValoresParesRiesgo(0, 1, Indice.Triangular_Centro) = 1.0 / 2.0;
		arbol.ValoresParesRiesgo(0, 1, Indice.Triangular_Derecha) = 1.0 / 2.0;
	
		arbol.ValoresParesRiesgo(0, 2, Indice.Triangular_Izquierda) = 2.0;
		arbol.ValoresParesRiesgo(0, 2, Indice.Triangular_Centro) = 2.0;
		arbol.ValoresParesRiesgo(0, 2, Indice.Triangular_Derecha) = 2.0;
	
		arbol.ValoresParesRiesgo(1, 2, Indice.Triangular_Izquierda) = 2.0;
		arbol.ValoresParesRiesgo(1, 2, Indice.Triangular_Centro) = 2.0;
		arbol.ValoresParesRiesgo(1, 2, Indice.Triangular_Derecha) = 2.0;


		TreeNode2 node = null;

		node = arbol.Nodos(0);
		//discretos
		node.ValoresDirecto(0) = 88.0;
		//de riesgo
		node.ValoresDirectoRiesgo(0, Indice.Triangular_Izquierda) = 84.0;
		node.ValoresDirectoRiesgo(0, Indice.Triangular_Centro) = 89.0;
		node.ValoresDirectoRiesgo(0, Indice.Triangular_Derecha) = 93.0;

		node = arbol.Nodos(1);
		//discretos
		node.ValoresDirecto(0) = 42.0;
		//de riesgo
		node.ValoresDirectoRiesgo(0, Indice.Triangular_Izquierda) = 38.0;
		node.ValoresDirectoRiesgo(0, Indice.Triangular_Centro) = 43.0;
		node.ValoresDirectoRiesgo(0, Indice.Triangular_Derecha) = 45.0;

		node = arbol.Nodos(2);
		//discretos
		node.ValoresDirecto(0) = 63.5;
		//de riesgo
		node.ValoresDirectoRiesgo(0, Indice.Triangular_Izquierda) = 60.0;
		node.ValoresDirectoRiesgo(0, Indice.Triangular_Centro) = 62.0;
		node.ValoresDirectoRiesgo(0, Indice.Triangular_Derecha) = 66.0;

		return arbol;
	}
}

//=======================================================
//Service provided by Telerik (www.telerik.com)
//Conversion powered by NRefactory.
//Twitter: @telerik
//Facebook: facebook.com/telerik
//=======================================================
