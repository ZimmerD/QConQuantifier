namespace QConQuantifier

module Quantify = 

    open System.Data.SQLite
    open Parameters.Domain
    open BioFSharp.Mz
    open System.IO
    open FSharp.Stats
    open BioFSharp.IO

    ///
    let analyzeFile (peptideDB:SQLiteConnection) (qConQuantParams:QConQuantifierParams) outputDir mzLiteFilePath = 

        ///
        let rawFileName = Path.GetFileNameWithoutExtension mzLiteFilePath
        
        ///
        let inReader = IO.Reader.createReader mzLiteFilePath

        ///
        let tr = inReader.BeginTransaction()

        ///
        let plotDirectory =
            let path = Path.Combine [|outputDir;"plots"|] 
            Directory.CreateDirectory(path) |> ignore
            path            
        
        ///copy peptideDB to memory to facilitate a fast look up
        printfn "Copy peptide DB into Memory"
        let memoryDB = PeptideLookUp.copyDBIntoMemory peptideDB 
        printfn "Copy peptide DB into Memory: finished"

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
            
        ///
        let psms = 
            Identification.calcPeptideSpectrumMatches inReader selectModPeptideByMassRange calcIonSeries qConQuantParams possibleMs2s
            
        /// 
        let thresholdedPsms = Identification.thresholdPSMs qConQuantParams psms
 
        /// 
        let rtIndex = IO.XIC.getRetentionTimeIdx inReader 

        ///
        let getIsotopicVariant = Quantification.initGetIsotopicVariant qConCatPeps
 
        ///
        let quantifiedPSMs = Quantification.quantifyPSMs inReader rtIndex qConQuantParams getIsotopicVariant thresholdedPsms

        quantifiedPSMs 
        //|> Deedle.Frame.ofRecords  
        //|> Frame.groupRowsUsing (fun k x -> x.Get("StringSequence"), x.Get("GlobalMod"),x.Get("Charge"))
        //|> Frame.mapRowKeys fst
        //|> Frame.dropCol "StringSequence"
        //|> Frame.dropCol "GlobalMod"
        //|> Frame.dropCol "Charge"
        //|> Frame.mapColKeys (fun x -> x + "_" + rawFileName )