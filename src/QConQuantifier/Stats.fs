namespace QConQuantifier

open BioFSharp
open BioFSharp.Mz.SearchDB

module Stats = 
      
    let weightedMean (weights:seq<float>) (items:seq<float>) =
        let sum,n = Seq.fold2 (fun (sum,n) w i -> w*i+sum,n+w ) (0.,0.) weights items 
        sum / n 