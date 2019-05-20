namespace QConQuantifier


open FSharp.Plotly
open System.IO

module Charting = 
  
    let saveChart plotDirectory sequence globalMod ch avgScanTime ms2s 
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
        |> Chart.withSize(1500.,800.)
        |> Chart.SaveHtmlAs(path)
