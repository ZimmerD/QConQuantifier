namespace QConQuantifier

open BioFSharp
open BioFSharp.Mz.SearchDB

module Stats = 
      
    /// Returns the weighted mean of the values in the items sequence. 
    /// The contribution of each item is given by the weight assigned in the sequence weights. 
    let weightedMean (weights:seq<float>) (items:seq<float>) =
        let sum,n = Seq.fold2 (fun (sum,n) w i -> w*i+sum,n+w ) (0.,0.) weights items 
        sum / n 