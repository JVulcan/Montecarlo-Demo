Imports Montecarlo

Public Class formMain
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        lblProgress.Text = "Progreso: 0%"
        Dim simulacion As New PSMontecarlo(numAlternativas.Value, 1, 5000)

        simulacion.ArbolJerarquico = ConfeccionarArbol()

        AddHandler simulacion.PSMontecarloProgressChanged, AddressOf progress_changed
        AddHandler simulacion.PSMontecarloCompleted, AddressOf simulation_completed
        simulacion.ComenzarSimulacion()
    End Sub

    Private Sub progress_changed(sender As Object, e As PSMontecarloProgressChangedEventArgs)
        lblProgress.Text = "Progreso: " & e.Progress.ToString() & "% {" & formatStage(e.Etapa) & "}"
    End Sub

    Private Function formatStage(value As MonteCarloEtapas) As String
        Select Case value
            Case MonteCarloEtapas.EnIteraciones
                Return "En Iteraciones"
            Case MonteCarloEtapas.EnTablaFrecuencias
                Return "Calculando Tabla de Frecuencias"
            Case MonteCarloEtapas.EnValoresP
                Return "Calculando Valores-P"
            Case Else
                Return ""
        End Select
    End Function

    Private Sub simulation_completed(sender As PSMontecarlo, e As PSMontecarloCompletedEventArgs)
        lblProgress.Text = "Simulación Finalizada"
        ShowResult(e.Resultado)
    End Sub

    Private Sub ShowResult(ByRef result As PSMontecarloResult)
        txtResult.Text = "" 'reiniciar

        addline("Cantidad de Alternativas: " & result.CantidadAlternativas.ToString())
        Dim minmax As String = ""
        For i = 0 To result.ValoresMinMax.Count - 1
            addline("Mín. Alternativa " & (i + 1).ToString() & ": " & result.ValoresMinMax(i)(0).ToString("F3"))
            addline("Máx. Alternativa " & (i + 1).ToString() & ": " & result.ValoresMinMax(i)(1).ToString("F3"))
        Next
        addline("======= Valores-P =======")
        Dim listPValues = PSMontecarloUtils.GetListaValoresP()
        For i = 0 To listPValues.Count - 1
            Dim values As Decimal() = result.ValoresP.ValorP(listPValues(i))
            Dim formatted As String = arrayToString(values, "F3")
            'Dim pval As String = listPValues(i).ToString("P1")
            addline((listPValues(i) / 100.0F).ToString("P1") & " : " & formatted)
        Next

        Chart1.Series.Clear()

        For i = 0 To result.CantidadAlternativas - 1
            Chart1.Series.Add(New DataVisualization.Charting.Series)
            Chart1.Series.Last.Name = "Alternativa " & (i + 1).ToString()
            For j = 0 To result.TablaFrecuencias.Count - 1
                Dim x As Double = Convert.ToDouble(CDec(result.TablaFrecuencias(j)(1)))
                Dim y As Double = Convert.ToDouble(CDec(result.TablaFrecuencias(j)(2 + i)))
                Chart1.Series.Last.Points.AddXY(x, y)
            Next
        Next
    End Sub

    Private Sub addline(linea As String)
        txtResult.Text &= linea & ControlChars.NewLine
    End Sub

    Private Function arrayToString(array As Decimal(), Optional format As String = "")
        Dim final As String = ""
        If format.Length = 0 Then
            For Each v In array
                final &= v.ToString().ToString & ","
            Next
        Else
            For Each v In array
                final &= v.ToString(format) & ","
            Next
        End If
        Return "{" & final.Substring(0, final.Length - 1) & "}"
    End Function

    Private Function ConfeccionarArbol() As TreeNode2

        Dim arbol As New TreeNode2(1, ModoMontecarlo.Triangular, TipoCriterio.Pares)

        For i = 0 To numCriterios.Value - 1
            'crear nuevo nodo, 1 alternativa, modo triangular y tipo directo
            Dim t As New TreeNode2(1, ModoMontecarlo.Triangular, TipoCriterio.Directo)
            'añadir nuevo criterio al árbol
            arbol.Nodes.Add(t)
        Next

        arbol.PrepararArbol(True)
        'son 3 criterios hijos
        'discretos
        arbol.ValoresPares(0, 1) = 1.0R / 2.0R
        arbol.ValoresPares(0, 2) = 1.0R
        arbol.ValoresPares(1, 2) = 1.0R

        If (Decimal.Compare(arbol.RatioInconsistencia, 0.1D) > 0) Then
            MsgBox("es inconsistente")
        End If

        'de riesgo
        arbol.ValoresParesRiesgo(0, 1, Indice.Triangular_Izquierda) = 1.0R / 3.0R
        arbol.ValoresParesRiesgo(0, 1, Indice.Triangular_Centro) = 1.0R / 2.0R
        arbol.ValoresParesRiesgo(0, 1, Indice.Triangular_Derecha) = 1.0R

        arbol.ValoresParesRiesgo(0, 2, Indice.Triangular_Izquierda) = 2.0R
        arbol.ValoresParesRiesgo(0, 2, Indice.Triangular_Centro) = 2.0R
        arbol.ValoresParesRiesgo(0, 2, Indice.Triangular_Derecha) = 1.0R

        arbol.ValoresParesRiesgo(1, 2, Indice.Triangular_Izquierda) = 1.0R
        arbol.ValoresParesRiesgo(1, 2, Indice.Triangular_Centro) = 1.0R
        arbol.ValoresParesRiesgo(1, 2, Indice.Triangular_Derecha) = 1.0R


        Dim node As TreeNode2 = Nothing

        node = arbol.Nodes(0)
        'discretos
        node.ValoresDirecto(0) = 88.0R
        'de riesgo
        node.ValoresDirectoRiesgo(0, Indice.Triangular_Izquierda) = 84.0R
        node.ValoresDirectoRiesgo(0, Indice.Triangular_Centro) = 89.0R
        node.ValoresDirectoRiesgo(0, Indice.Triangular_Derecha) = 93.0R

        node = arbol.Nodes(1)
        'discretos
        node.ValoresDirecto(0) = 42.0R
        'de riesgo
        node.ValoresDirectoRiesgo(0, Indice.Triangular_Izquierda) = 38.0R
        node.ValoresDirectoRiesgo(0, Indice.Triangular_Centro) = 43.0R
        node.ValoresDirectoRiesgo(0, Indice.Triangular_Derecha) = 45.0R

        node = arbol.Nodes(2)
        'discretos
        node.ValoresDirecto(0) = 63.5R
        'de riesgo
        node.ValoresDirectoRiesgo(0, Indice.Triangular_Izquierda) = 60.0R
        node.ValoresDirectoRiesgo(0, Indice.Triangular_Centro) = 62.0R
        node.ValoresDirectoRiesgo(0, Indice.Triangular_Derecha) = 66.0R

        Return arbol
    End Function
End Class
