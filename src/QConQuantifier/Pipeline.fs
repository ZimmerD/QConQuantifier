namespace QConQuantifier

module Pipeline = 

    open System.Data.SQLite
    open Parameters.Domain
    open BioFSharp.Mz
    open System.IO
    open FSharp.Stats
    open BioFSharp.IO
    open Deedle
    open LabelEfficiency
    open MzIO.Processing

    /// Performs identifaction and quantification of mzLite File. Results are written to outputDir  
    let analyzeFile (peptideDB:SQLiteConnection) (qConQuantParams:QConQuantifierParams) outputDir mzLiteFilePath = 

        //////////////////
        //IO preparation
        ////////////////// 
        ///
        let rawFileName = Path.GetFileNameWithoutExtension mzLiteFilePath
        
        ///
        let inReader = IO.Reader.createReader mzLiteFilePath

        ///
        let tr = inReader.BeginTransaction()

        ///
        let psmPlotDirectory =
            let path = Path.Combine [|outputDir;"plots";rawFileName;"PSM"|] 
            Directory.CreateDirectory(path) |> ignore
            path            

        let lePlotDirectory =
            let path = Path.Combine [|outputDir;"plots";rawFileName;"LabelEfficiency"|] 
            Directory.CreateDirectory(path) |> ignore
            path            

        //////////////////
        //Identification
        //////////////////        
        ///copy peptideDB to memory to facilitate a fast look up
        let memoryDB = PeptideLookUp.copyDBIntoMemory peptideDB 

        /// Gets all modified peptide between first and second mass.
        let selectModPeptideByMassRange = PeptideLookUp.initSelectModPeptideByMassRange qConQuantParams memoryDB
        
        /// Gets all modified peptides by protein accession.
        let selectQConcatPeptideByProtAccession = PeptideLookUp.initSelectQConcatPeptideByProtAccession memoryDB

        /// Given a AminoAcid list this function will compute N- and C-terminal ion ladders.
        let calcIonSeries = Identification.initCalcIonSeries qConQuantParams
               
        /// All peptides of qConcat heritage.
        let qConCatPeps = 
            let protHeader = 
                Seq.collect (FastA.fromFile id) qConQuantParams.QConCatFastaPaths
                |> Seq.map (fun prot -> prot.Header)
            Seq.collect selectQConcatPeptideByProtAccession protHeader
            |> Seq.filter (fun x -> x.MissCleavages = 0)

        
        /// Returns true if mz is theoretically mapping to an ion originating from a QConcat peptide.
        let isValidMz = PeptideLookUp.initIsValidMz qConQuantParams qConCatPeps 

        /// Filter all measured ms2s for those with a precursor mz theoretically mapping to an ion originating from a QConcat peptide.
        let possibleMs2s =
            inReader
            |> IO.Reader.getMassSpectra
            |> Seq.filter (fun ms -> MassSpectrum.getMsLevel ms = 2  && MassSpectrum.getPrecursorMZ ms |> isValidMz )
            |> Array.ofSeq
            
        /// All peptide spectrum matches.
        let psms = Identification.calcPeptideSpectrumMatches inReader selectModPeptideByMassRange calcIonSeries qConQuantParams possibleMs2s
            
        /// Get all peptide spectrum matches above a use defined threshold.        
        let thresholdedPsms = Identification.thresholdPSMs qConQuantParams psms        

        //printfn "%A" thresholdedPsms

        //////////////////
        //Quantification
        //////////////////

        /// 
        let rtIndex = IO.XIC.getRetentionTimeIdx inReader 

        /// Given an isotopic variant of a qConcat peptide this function returns the respective labled/unlabeled version. 
        let getIsotopicVariant = Quantification.initGetIsotopicVariant qConCatPeps
 
        ///
        let quantifiedPSMs = Quantification.quantifyPSMs psmPlotDirectory inReader rtIndex qConQuantParams getIsotopicVariant thresholdedPsms
        ///
        let results: Result<Frame<string*bool*int,string>,exn> = 
            if qConQuantParams.EstimateLabelEfficiency && quantifiedPSMs.Length > 1 then
                quantifiedPSMs
                |> fun qpsms ->
                    let labelEfficiencyResults =
                        qpsms
                        |> Array.ofList
                        |> Array.map predictLabelEfficiency
                        |> fun predictors ->
                            let medianLE = 
                                predictors
                                |> getFilteredMedianLabelEfficiency lePlotDirectory 3.
                            predictors
                            |> estimateCorrectionFactors medianLE

                    labelEfficiencyResults
                    |> Array.iter
                        (fun leRes ->
                            Charting.saveLabelEfficiencyChart 
                                lePlotDirectory 
                                leRes.StringSequence
                                leRes.GlobalMod
                                leRes.Charge
                                (leRes.FullLabeledPattern   |> LabelEfficiency.patternOfString)
                                (leRes.MedianPattern        |> LabelEfficiency.patternOfString)
                                (leRes.PredictedPattern     |> LabelEfficiency.patternOfString)
                                (leRes.ActualPattern        |> LabelEfficiency.patternOfString)
                                leRes.PredictedLabelEfficiency
                                leRes.MedianLabelEfficiency
                        )

                    labelEfficiencyResults
                    |> Deedle.Frame.ofRecords  
                    |> Frame.indexRowsUsing (fun x -> x.GetAs<string>("StringSequence"), x.GetAs<bool>("GlobalMod"),x.GetAs<int>("Charge"))
                    |> Frame.dropCol "StringSequence"
                    |> Frame.dropCol "GlobalMod"
                    |> Frame.dropCol "Charge"
                    |> Frame.join JoinKind.Outer
                        (
                            qpsms 
                            |> Deedle.Frame.ofRecords  
                            |> Frame.indexRowsUsing (fun x -> x.GetAs<string>("StringSequence"), x.GetAs<bool>("GlobalMod"),x.GetAs<int>("Charge"))
                            |> Frame.dropCol "StringSequence"
                            |> Frame.dropCol "GlobalMod"
                            |> Frame.dropCol "Charge"
                        )
                    |> Frame.mapColKeys (fun x -> x + "_" + rawFileName )
                    |> Ok
            elif quantifiedPSMs.Length > 0 then
                quantifiedPSMs 
                |> Deedle.Frame.ofRecords  
                |> Frame.indexRowsUsing (fun x -> x.GetAs<string>("StringSequence"), x.GetAs<bool>("GlobalMod"),x.GetAs<int>("Charge"))
                |> Frame.dropCol "StringSequence"
                |> Frame.dropCol "GlobalMod"
                |> Frame.dropCol "Charge"
                |> Frame.mapColKeys (fun x -> x + "_" + rawFileName )
                |> Ok
            else
                Result.Error (System.Exception("File does not contain any peptides from the input QConCATemer"))
        results

    ///
    let mergeFrames (frames:Frame<_,string> list) = 
        frames |> List.reduce (Frame.join JoinKind.Outer) 
         