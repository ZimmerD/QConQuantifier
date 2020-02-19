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
open BioFSharp.Mz.Quantification.HULQ
open PeptideLookUp
open SearchEngineResult
open MzIO.Processing

module Quantification = 

    ///
    type averagePSM = {
        MeanPrecMz          : float 
        MeanScanTime        : float
        WeightedAvgScanTime :float
        MeanScore           : float
        X_Xic               : float []
        Y_Xic               : float []
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
        

    /// Returns a function that given an isotopic variant of a qConcat peptide returns the respective labled/unlabeled version. 
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

    /// Given a list of psms mapping to the same ion species this function estimates an average psm.
    /// For this various estimators of central tendency are computed e.g the mean of the precursorMz, 
    /// the mean ScanTime as well as an precursor intensity weighted scan time mean. This psm average
    /// reduces the risk of picking a wrong XIC peak.
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
                let rtQuery = Query.createRangeQuery scanTimeCenter scanTimeWindow
                let mzQuery = Query.createRangeQuery meanPrecMz qConQuantifierParams.MzWindow_Da
                let retData',itzData' =
                    Query.getXIC reader rtIndex rtQuery mzQuery
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

    /// Performs XIC extraction at a given targetMz and scan Time. Offsets are defined in qConQuantifierParams.
    /// Subsequently, peak detection is performed and a levenberg marquardt routine (FSharp.Stats) is employed to fit a gaussian, or in case tailed
    /// peak is observed, an exponentially modified gaussian to the selected peak.  
    let quantifyBy reader rtIndex qConQuantifierParams targetMz targetScanTime =
        let (retData,itzData)   =
            let rtQuery = Query.createRangeQuery targetScanTime qConQuantifierParams.ScanTimeWindow
            let mzQuery = Query.createRangeQuery targetMz qConQuantifierParams.MzWindow_Da
            let retData',itzData' =
                Query.getXIC reader rtIndex rtQuery mzQuery
                |> Array.map (fun p -> p.Rt , p.Intensity)
                |> Array.unzip
            retData',itzData'
        let peaks          = Signal.PeakDetection.SecondDerivative.getPeaks 0.1 2 13 retData itzData
        let peakToQuantify = getPeakBy peaks targetScanTime
        let quantP         = quantifyPeak peakToQuantify  
        quantP,retData,itzData, peakToQuantify.XData

             
    /// Quantifies every Ion identified by at least one psm. Given a collection of PSMs this function first performs an aggregation, grouping all psms mapping
    /// to the same ion species. Afterwards the average PSM is computed. Based uppon this, XIC extraction and quantification is performed. Additionally, a paired
    /// quantification is performed. This means that if a light variant (e.g a N14 labeled peptide) was identified, the XIC corresponding to the
    /// heavy variant is extracted and quantified. This does not only enlarge the fraction of quantified peptides it also alows to control for the quality of 
    /// the quantification if an ion is quantified from both perspectives (in case of both, Heavy-To-Light and Light-To-Heavy inference).
    /// Besides the monoisotopic peak this function also quantifies the N15Minus1 peak this allows to calculate the labeling efficiency. 
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
                        // The target PSM was not global modified -> a light peptide
                        if globMod = 0 then 
                            let labeled        = getIsotopicVariant sequence globMod
                            let n15mz          = Mass.toMZ (labeled.ModMass) (ch|> float)
                            let n15Quant,rt,itz,rtP = quantifyBy reader rtIndex qConQuantifierParams n15mz searchScanTime
                            let n15Minus1Mz    = n15mz - (Mass.Table.NMassInU / (ch|> float))
                            let n15Minus1Quant,_,_,_ = quantifyBy reader rtIndex qConQuantifierParams n15Minus1Mz searchScanTime
                            let chart = Charting.savePSMChart plotDirectory sequence globMod ch averagePSM.WeightedAvgScanTime ms2s averagePSM.X_Xic averagePSM.Y_Xic   
                                            peakToQuantify.XData peakToQuantify.YData quantP.YPredicted rt itz rtP n15Quant.YPredicted
      
                            createQuantifiedPeptide sequence globMod averagePSM.WeightedAvgScanTime averagePSM.MeanScore
                                    ch avgMass averagePSM.MeanPrecMz quantP.Area n15mz n15Quant.Area n15Minus1Mz n15Minus1Quant.Area
                            |> Some
                        // The target PSM was global modified -> a heavy peptide
                        else
                            let labeled        = getIsotopicVariant sequence globMod
                            let n14mz          = Mass.toMZ (labeled.ModMass) (ch|> float)
                            let n14Quant,rt,itz,rtP      = quantifyBy reader rtIndex qConQuantifierParams n14mz searchScanTime
                            let n15Minus1Mz    = averagePSM.MeanPrecMz - (Mass.Table.NMassInU / (ch|> float))
                            let n15Minus1Quant,_,_,_ = quantifyBy reader rtIndex qConQuantifierParams n15Minus1Mz searchScanTime
                            let chart = Charting.savePSMChart plotDirectory sequence globMod ch averagePSM.WeightedAvgScanTime ms2s averagePSM.X_Xic averagePSM.Y_Xic  
                                            peakToQuantify.XData peakToQuantify.YData quantP.YPredicted rt itz rtP n14Quant.YPredicted
                                
                            createQuantifiedPeptide sequence globMod averagePSM.WeightedAvgScanTime averagePSM.MeanScore
                                    ch avgMass n14mz n14Quant.Area averagePSM.MeanPrecMz quantP.Area n15Minus1Mz n15Minus1Quant.Area
                            |> Some
                        with
                        | e as exn -> 
                            //printfn "%s" e.Message
                            Option.None
                        )

