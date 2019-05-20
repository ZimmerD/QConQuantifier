namespace QConQuantifier

open System
open System.IO
open System.Data.SQLite
open Parameters.Domain
open BioFSharp
open BioFSharp.IO
open BioFSharp.Mz
open FSharpAux.IO
open FSharp.Stats
open BioFSharp.Mz.Quantification.MyQuant
open PeptideLookUp
open SearchEngineResult

module Quantification = 

    ///
    type averagePSM = {
        MeanPrecMz   : float 
        MeanScanTime : float
        WeightedAvgScanTime:float
        MeanScore   : float
        X_Xic         : float []
        Y_Xic         : float []
        }

    let createAveragePSM meanPrecMz meanScanTime weightedAvgScanTime meanScore xXic yXic = {
        MeanPrecMz    = meanPrecMz   
        MeanScanTime  = meanScanTime 
        WeightedAvgScanTime= weightedAvgScanTime
        MeanScore = meanScore
        X_Xic         = xXic         
        Y_Xic         = yXic         
        }    

    ///
    type QuantifiedPeptide = {
        StringSequence      : string
        GlobalMod           : int
        ScanTime            : float
        AverageSequestScore : float
        Charge              : int
        ExperimentalMass    : float 
        N14MZ               : float
        N14Quant            : float
        N15MZ               : float
        N15Quant            : float
        N15Minus1MZ         : float
        N15Minus1Quant      : float
        }

    let createQuantifiedPeptide stringSequence globalMod scanTime avgSequestScore charge experimentalMass n14MZ n14Quant n15MZ n15Quant n15Minus1MZ n15Minus1Quant= {        
        StringSequence      = stringSequence  
        GlobalMod           = globalMod       
        ScanTime            = scanTime  
        AverageSequestScore = avgSequestScore
        Charge              = charge          
        ExperimentalMass    = experimentalMass            
        N14MZ               = n14MZ           
        N14Quant            = n14Quant        
        N15MZ               = n15MZ           
        N15Quant            = n15Quant        
        N15Minus1MZ         = n15Minus1MZ     
        N15Minus1Quant      = n15Minus1Quant  
        }     
        

    ///
    let initGetIsotopicVariant (qConCatPeps:seq<QConcatPeptide> ) =
        let labeled = 
            qConCatPeps
            |> Seq.filter (fun x -> x.GlobalMod = 1)
            |> Seq.map (fun x -> x.ModPeptideSequence,x)
            |> Map.ofSeq
        let unlabeled = 
            qConCatPeps
            |> Seq.filter (fun x -> x.GlobalMod = 0)
            |> Seq.map (fun x -> x.ModPeptideSequence,x)
            |> Map.ofSeq
        fun sequence globMod  -> 
            if globMod = 0 then 
                labeled.[sequence]
            else 
                unlabeled.[sequence]

    ///
    let average reader rtIndex qConQuantifierParams (psms:SearchEngineResult<float> seq) =
            let meanPrecMz            = psms |> Seq.meanBy (fun x -> x.PrecursorMZ)
            let meanScanTime          = psms |> Seq.meanBy (fun x -> x.ScanTime)           
            let scanTimeCenter,scanTimeWindow = 
                let minScanTime = psms |> Seq.minBy (fun x -> x.ScanTime) |> fun x -> x.ScanTime
                let maxScanTime = psms |> Seq.maxBy (fun x -> x.ScanTime) |> fun x -> x.ScanTime
                let c = (minScanTime + maxScanTime) / 2.
                let w = (maxScanTime - c) + qConQuantifierParams.ScanTimeWindow
                c, w
            let (retData,itzData)   =
                let rtQuery = IO.XIC.createRangeQuery scanTimeCenter scanTimeWindow
                let mzQuery = IO.XIC.createRangeQuery meanPrecMz qConQuantifierParams.MzWindow_Da
                let retData',itzData' =
                    IO.XIC.getXICBy reader rtIndex rtQuery mzQuery
                    |> Array.map (fun p -> p.Rt , p.Intensity)
                    |> Array.unzip
                retData',itzData'
            let ms2s = psms |> Seq.map (fun x -> x.ScanTime,x.Score)
            let meanScore = psms |> Seq.averageBy (fun x -> x.Score)
            let weightedAvgScanTime =
                let scanTimes = 
                    psms 
                    |> Seq.map (fun x -> x.ScanTime)
                let weights =
                    scanTimes
                    |> Seq.map (fun scanTime -> 
                                    let i = FSharp.Stats.Signal.PeakDetection.idxOfClosestPeakBy retData itzData scanTime
                                    itzData.[i]
                               )
                Stats.weightedMean weights scanTimes 
            createAveragePSM meanPrecMz meanScanTime weightedAvgScanTime meanScore retData itzData

    ///                
    let quantifyBy reader rtIndex qConQuantifierParams targetMz targetScanTime =
        let (retData,itzData)   =
            let rtQuery = IO.XIC.createRangeQuery targetScanTime qConQuantifierParams.ScanTimeWindow
            let mzQuery = IO.XIC.createRangeQuery targetMz qConQuantifierParams.MzWindow_Da
            let retData',itzData' =
                IO.XIC.getXICBy reader rtIndex rtQuery mzQuery
                |> Array.map (fun p -> p.Rt , p.Intensity)
                |> Array.unzip
            retData',itzData'
        let peaks          = Signal.PeakDetection.SecondDerivative.getPeaks 0.1 2 13 retData itzData
        let peakToQuantify = getPeakBy peaks targetScanTime
        let quantP         = quantifyPeak peakToQuantify  
        quantP,retData,itzData, peakToQuantify.XData

             
    ///
    let quantifyPSMs plotDirectory reader rtIndex qConQuantifierParams getIsotopicVariant (psms:SearchEngineResult<float> list) = 
        psms
        |> List.groupBy (fun x -> x.StringSequence, x.GlobalMod,x.PrecursorCharge)
        |> List.choose (fun ((sequence,globMod,ch),psms) ->
                        try
                        let ms2s = psms |> Seq.map (fun x -> x.ScanTime,x.Score)
                        let averagePSM = average reader rtIndex qConQuantifierParams psms
                        let avgMass = Mass.ofMZ (averagePSM.MeanPrecMz) (ch |> float)
                        let peaks          = Signal.PeakDetection.SecondDerivative.getPeaks 0.1 2 13 averagePSM.X_Xic averagePSM.Y_Xic
                        let peakToQuantify = getPeakBy peaks averagePSM.WeightedAvgScanTime
                        let quantP = quantifyPeak peakToQuantify 
                        let searchScanTime = 
                            if quantP.EstimatedParams |> Array.isEmpty then
                                averagePSM.WeightedAvgScanTime
                            else 
                                quantP.EstimatedParams.[1] 
                        if globMod = 0 then 
                            let labeled        = getIsotopicVariant sequence globMod
                            let n15mz          = Mass.toMZ (labeled.ModMass) (ch|> float)
                            let n15Quant,rt,itz,rtP = quantifyBy reader rtIndex qConQuantifierParams n15mz searchScanTime
                            let n15Minus1Mz    = n15mz - (Mass.Table.NMassInU / (ch|> float))
                            let n15Minus1Quant,_,_,_ = quantifyBy reader rtIndex qConQuantifierParams n15Minus1Mz searchScanTime
                            let chart = Charting.saveChart plotDirectory sequence globMod ch averagePSM.WeightedAvgScanTime ms2s averagePSM.X_Xic averagePSM.Y_Xic   
                                            peakToQuantify.XData peakToQuantify.YData quantP.YPredicted rt itz rtP n15Quant.YPredicted
                                
                        
                            createQuantifiedPeptide sequence globMod averagePSM.WeightedAvgScanTime averagePSM.MeanScore
                                    ch avgMass averagePSM.MeanPrecMz quantP.Area n15mz n15Quant.Area n15Minus1Mz n15Minus1Quant.Area
                            |> Some
                        else
                            let labeled        = getIsotopicVariant sequence globMod
                            let n14mz          = Mass.toMZ (labeled.ModMass) (ch|> float)
                            let n14Quant,rt,itz,rtP      = quantifyBy reader rtIndex qConQuantifierParams n14mz searchScanTime
                            let n15Minus1Mz    = averagePSM.MeanPrecMz - (Mass.Table.NMassInU / (ch|> float))
                            let n15Minus1Quant,_,_,_ = quantifyBy reader rtIndex qConQuantifierParams n15Minus1Mz searchScanTime
                            let chart = Charting.saveChart plotDirectory sequence globMod ch averagePSM.WeightedAvgScanTime ms2s averagePSM.X_Xic averagePSM.Y_Xic  
                                            peakToQuantify.XData peakToQuantify.YData quantP.YPredicted rt itz rtP n14Quant.YPredicted
                                
                            createQuantifiedPeptide sequence globMod averagePSM.WeightedAvgScanTime averagePSM.MeanScore
                                    ch avgMass n14mz n14Quant.Area averagePSM.MeanPrecMz quantP.Area n15Minus1Mz n15Minus1Quant.Area
                            |> Some
                        with
                        | _ -> Option.None
                        )

