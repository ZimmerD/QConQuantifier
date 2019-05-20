namespace QConQuantifier

open System
open System.IO
open System.Data.SQLite
open Parameters.Domain
open BioFSharp
open BioFSharp.Mz
open MzLite.Model
open IO
open SearchEngineResult
open Parameters.Domain

module Identification = 

    /// Returns a function to perform a in silico fragmentation of any given aminoAcid list. The computed N- and C-terminal ion ladders are defined in qConQuantParams.
    let initCalcIonSeries (qConQuantParams:QConQuantifierParams) =
        fun aal -> Fragmentation.Series.fragmentMasses qConQuantParams.NTerminalSeries qConQuantParams.CTerminalSeries qConQuantParams.MassFunction aal
   
    /// Maps all fragment spectra (ms2s) and matches their spectra against in silico spectra. The insilico spectra are retrieved based on the precursor mzs of the 
    /// ms2s, user supplied minimal and maximum charge states and user supplied search tolerance in ppm.  
    /// The algorithm used to compare theoretical and recorded spectra is the SEQUEST algorithm.
    let calcPeptideSpectrumMatches reader lookUpF calcIonSeries (qConQuantParams:QConQuantifierParams) (ms2s:MassSpectrum []) = 
        [|qConQuantParams.ExpectedMinCharge.. qConQuantParams.ExpectedMaxCharge|]
        |> Array.collect (fun chargeState ->  
                            ms2s
                            |> Array.choose (fun ms2 -> 
                                            try
                                                let recSpec = MassSpectrum.getPeaks reader ms2
                                                Some(ms2,recSpec)
                                            with
                                            | _ -> 
                                                Option.None
                                            )
                            |> Array.choose (fun (ms2,recSpec) -> 
                                            try
                                            //
                                            let scanTime = MassSpectrum.getScanTime ms2
                                            //
                                            let precMz = MassSpectrum.getPrecursorMZ ms2             
                                            //
                                            let putPutMass = Mass.ofMZ precMz (float chargeState)
                                            //
                                            let lowerMass,upperMass = Mass.rangePpm qConQuantParams.LookUpPPM putPutMass
                                            //
                                            let lookUpResults :SearchDB.LookUpResult<AminoAcids.AminoAcid> list = 
                                                lookUpF lowerMass upperMass
                                            //
                                            let theoSpecs = 
                                                lookUpResults
                                                |> List.map (fun lookUpResult -> 
                                                                let ionSeries = calcIonSeries lookUpResult.BioSequence
                                                                lookUpResult,ionSeries
                                                            )   
                                                |> SequestLike.getTheoSpecs qConQuantParams.ScanRange chargeState
                                            let sequestLikeScores = 
                                                SequestLike.calcSequestScoreParallel qConQuantParams.ScanRange recSpec scanTime chargeState precMz theoSpecs ms2.ID
                                            let bestTargetSequest =
                                                sequestLikeScores
                                                |> List.filter (fun (x:SearchEngineResult.SearchEngineResult<float>) -> x.IsTarget)
                                                |> List.truncate 10
                                            let bestDecoySequest =
                                                sequestLikeScores
                                                |> List.filter (fun x -> not x.IsTarget)
                                                |> List.truncate 10
                                            bestTargetSequest@bestDecoySequest 
                                            |> Some
                                            with
                                            | _ -> Option.None
                                        )
                        )
        |> List.concat
   
    /// Uses the target decoy approach to threshold psms at a given pep value cut off.
    let private filterPSMsByPepValue pepValueCutoff (psms:SearchEngineResult<float> list ) = 
        FDRControl.getPEPValues 1. (fun (x:SearchEngineResult.SearchEngineResult<float>) -> x.Score) (fun x -> not x.IsTarget ) (Array.ofList psms)
        |> List.ofArray
        |> List.map2 (fun x y -> x,y) psms
        |> List.filter (fun x -> snd x < pepValueCutoff)
        |> List.map fst 
        |> List.groupBy (fun x -> x.SpectrumID)
        |> List.map (fun x -> snd x |> List.maxBy (fun x -> x.Score))

    /// Uses an arbitrary SequestScore threshold to filter PSMs.
    let private filterPSMsByScore scoreCutOff (psms:SearchEngineResult<float> list) =                        
        psms
        |> List.filter (fun x -> x.Score > scoreCutOff && x.IsTarget)
        |> List.groupBy (fun x -> x.SpectrumID)
        |> List.map (fun x -> snd x |> List.maxBy (fun x -> x.Score))

    /// Filters PSMs according to the PSMThresholding case definded in qConQuantParams.
    let thresholdPSMs (qConQuantParams:QConQuantifierParams) (psms:SearchEngineResult<float> list) =
        match qConQuantParams.PSMThreshold with 
        | PSMThreshold.PepValue v     -> filterPSMsByPepValue v psms
        | PSMThreshold.SequestScore v -> filterPSMsByScore v psms