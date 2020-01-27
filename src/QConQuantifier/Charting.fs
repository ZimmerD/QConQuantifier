namespace QConQuantifier


open FSharp.Plotly
open System.IO

module Charting = 
    
    let xAxis showGrid title titleSize tickSize = Axis.LinearAxis.init(Title=title,Showgrid=showGrid,Showline=true,Mirror=StyleParam.Mirror.All,Zeroline=false,Tickmode=StyleParam.TickMode.Auto,Ticks= StyleParam.TickOptions.Inside, Tickfont=Font.init(StyleParam.FontFamily.Arial,Size=tickSize),Titlefont=Font.init(StyleParam.FontFamily.Arial,Size=titleSize))
    let yAxis showGrid title titleSize tickSize (range:float*float)= Axis.LinearAxis.init(Title=title,Showgrid=showGrid,Showline=true,Mirror=StyleParam.Mirror.All,Tickmode=StyleParam.TickMode.Auto,Ticks= StyleParam.TickOptions.Inside,Tickfont=Font.init(StyleParam.FontFamily.Arial,Size=tickSize),Titlefont=Font.init(StyleParam.FontFamily.Arial,Size=titleSize),Range=StyleParam.Range.MinMax range)
    
    let saveMedianLabelEfficiencyChart (allPredictedLE:float[]) (correctedDist:float[]) (tuckeyD : float) (upperBorder:float) (lowerBorder:float) plotDirectory =
        
        let path = Path.Combine [|plotDirectory;"0_OverallMedianLabelEfficiency"|]
        [
            Chart.BoxPlot(x=allPredictedLE,Jitter = 0.1,Boxpoints=StyleParam.Boxpoints.All,Name="All Predicted <br></br>label efficiencies")
            |> Chart.withShapes 
                [
                    (Shape.init(StyleParam.ShapeType.Line, X0 = upperBorder, X1 = upperBorder, Y0 = -0.4, Y1 = 0.4))
                    (Shape.init(StyleParam.ShapeType.Line, X0 = lowerBorder, X1 = lowerBorder, Y0 = -0.4, Y1 = 0.4))
                ]
        
            Chart.BoxPlot(x=correctedDist,Jitter = 0.1,Boxpoints=StyleParam.Boxpoints.All,Name="Filtered Predicted <br></br>label efficiencies")
        ]
        |> Chart.Combine
        |> Chart.withX_Axis (yAxis false "Label Efficiency" 20 16 (0.925, 1.075))
        |> Chart.withY_Axis (xAxis false "" 20 16)
        |> Chart.withTitle (sprintf "Label efficiency - Outlier detection (tuckeyC = %f, upper=%f, lower=%f)" tuckeyD upperBorder lowerBorder)
        |> Chart.withMarginSize 200.
        |> Chart.withSize (800.,400.)
        |> Chart.SaveHtmlAs(path)

    let saveLabelEfficiencyChart plotDirectory sequence globalMod ch 
        (fullLabeledPattern:(float*float) list)
        (medianPattern:(float*float) list)
        (predictedPattern:(float*float) list)
        (actualPattern:(float*float) list)
        (predictedLabelEfficiency:float)
        (medianLabelEfficiency:float)
        =

        let title = sprintf "%s_GMod_%i_Charge_%i" sequence globalMod ch

        let path = 
            let sequence = sequence |> String.filter (fun x -> x <> '*')
            Path.Combine [|plotDirectory;title|]

        [
            fullLabeledPattern
            |> List.map (fun (x,y) -> [(x,0.);(x,y);(x,0.)])
            |> List.concat
            |> Chart.Line
            |> Chart.withTraceName ("Fully Labeled Pattern")
            |> Chart.withLineStyle(Color="lightgray",Width = 20)

            medianPattern
            |> List.map (fun (x,y) -> [(x,0.);(x,y);(x,0.)])
            |> List.concat
            |> Chart.Line
            |> Chart.withTraceName (sprintf "CorrectedPattern @ Median LE of %.3f" medianLabelEfficiency)
            |> Chart.withLineStyle(Width = 10,Color="lightgreen")

            predictedPattern
            |> List.map (fun (x,y) -> [(x,0.);(x,y);(x,0.)])
            |> List.concat
            |> Chart.Line
            |> Chart.withTraceName (sprintf "PredictedPattern @ %.3f LE" predictedLabelEfficiency)
            |> Chart.withLineStyle(Color="orange",Width = 5)

            Chart.Point(actualPattern,Name="Experimental Values")
            |> Chart.withMarkerStyle(Size = 15,Symbol = StyleParam.Symbol.Square, Color = "lightred")
            


        ]
        |> Chart.Combine
        |> Chart.withTitle title
        |> Chart.withX_Axis (xAxis false "m/z" 20 16)
        |> Chart.withY_Axis (yAxis false "normalized probability" 20 16 (0.,1.1))
        |> Chart.withSize(1500.,800.)
        |> Chart.SaveHtmlAs(path)

    
    let savePSMChart plotDirectory sequence globalMod ch avgScanTime ms2s 
        (xXic:float[]) (yXic:float[])  (xToQuantify:float[]) (ypToQuantify:float[]) (fitY:float[]) 
            (xXicInferred:float[]) (yXicinferred:float[]) (xInferred:float[]) (inferredFit:float[]) =
        let path = 
            let sequence = sequence |> String.filter (fun x -> x <> '*')
            let title = sprintf "%s_GMod_%i_Charge_%i" sequence globalMod ch
            Path.Combine [|plotDirectory;title|]
        [
        Chart.Point(xXic, yXic)                     |> Chart.withTraceName "Target XIC"
        Chart.Point(ms2s)                           |> Chart.withTraceName "MS2s with scores"
        Chart.Point([avgScanTime],[1.])             |> Chart.withTraceName "Weighted Mean of Ms2 scan times"
        Chart.Point((xToQuantify), (ypToQuantify))  |> Chart.withTraceName "Identified Target Peak"
        Chart.Line(xToQuantify,fitY)                |> Chart.withTraceName "Fit of target Peak"
        Chart.Point(xXicInferred, yXicinferred)     |> Chart.withTraceName "Inferred XIC"
        Chart.Line(xInferred,inferredFit)           |> Chart.withTraceName "Fit of inferred Peak"
    
        ]
        |> Chart.Combine
        |> Chart.withTitle(sprintf "Sequence= %s,GlobalMod = %i, Charge State = %i" sequence globalMod ch)
        |> Chart.withX_Axis (xAxis false "Intensity" 20 16)
        |> Chart.withY_Axis (xAxis false "Retention Time" 20 16)
        |> Chart.withSize(1500.,800.)
        |> Chart.SaveHtmlAs(path)
