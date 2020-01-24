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
        let plotDirectory =
            let path = Path.Combine [|outputDir;"plots";rawFileName|] 
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
            |> Seq.filter (fun ms -> IO.MassSpectrum.getMsLevel ms = 2  && IO.MassSpectrum.getPrecursorMZ ms |> isValidMz )
            |> Array.ofSeq
            
        /// All peptide spectrum matches.
        let psms = Identification.calcPeptideSpectrumMatches inReader selectModPeptideByMassRange calcIonSeries qConQuantParams possibleMs2s
            
        /// Get all peptide spectrum matches above a use defined threshold.        
        let thresholdedPsms = Identification.thresholdPSMs qConQuantParams psms        
        //////////////////
        //Quantification
        //////////////////

        /// 
        let rtIndex = IO.XIC.getRetentionTimeIdx inReader 

        /// Given an isotopic variant of a qConcat peptide this function returns the respective labled/unlabeled version. 
        let getIsotopicVariant = Quantification.initGetIsotopicVariant qConCatPeps
 
        ///
        let quantifiedPSMs = Quantification.quantifyPSMs plotDirectory inReader rtIndex qConQuantParams getIsotopicVariant thresholdedPsms

        ///
        let results: Frame<string*bool*int,string>  = 
            if qConQuantParams.EstimateLabelEfficiency then
                quantifiedPSMs
                |> fun qpsms ->
                    qpsms
                    |> Array.ofList
                    |> Array.map predictLabelEfficiency
                    |> estimateCorrectionFactors
                    |> Deedle.Frame.ofRecords  
                    |> Frame.indexRowsUsing (fun x -> x.GetAs<string>("StringSequence"), x.GetAs<bool>("GlobalMod"),x.GetAs<int>("Charge"))
                    |> Frame.dropCol "StringSequence"
                    |> Frame.dropCol "GlobalMod"
                    |> Frame.dropCol "Charge"
                    |> Frame.merge 
                        (
                            qpsms 
                            |> Deedle.Frame.ofRecords  
                            |> Frame.indexRowsUsing (fun x -> x.GetAs<string>("StringSequence"), x.GetAs<bool>("GlobalMod"),x.GetAs<int>("Charge"))
                            |> Frame.dropCol "StringSequence"
                            |> Frame.dropCol "GlobalMod"
                            |> Frame.dropCol "Charge"
                        )
                    |> Frame.mapColKeys (fun x -> x + "_" + rawFileName )
            else
                quantifiedPSMs 
                |> Deedle.Frame.ofRecords  
                |> Frame.indexRowsUsing (fun x -> x.GetAs<string>("StringSequence"), x.GetAs<bool>("GlobalMod"),x.GetAs<int>("Charge"))
                |> Frame.dropCol "StringSequence"
                |> Frame.dropCol "GlobalMod"
                |> Frame.dropCol "Charge"
                |> Frame.mapColKeys (fun x -> x + "_" + rawFileName )
        results

    ///
    let mergeFrames (frames:Frame<_,string> list) = 
        frames |> List.reduce (Frame.join JoinKind.Outer) 
         