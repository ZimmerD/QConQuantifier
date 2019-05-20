namespace QConQuantifier

open System
open System.IO
open System.Data.SQLite
open Parameters.Domain
open BioFSharp
open BioFSharp.IO
open BioFSharp.Mz
open FSharpAux.IO

module PeptideLookUp = 

    ///
    type QConcatPeptide = {
        PeptideSequence     : string
        ModPeptideSequence  : string
        ModMass             : float
        MissCleavages       : int 
        GlobalMod           : int 
        }

    ///
    let createQConcatPeptide peptideSequence modPeptideSequence modMass missCleavages globalMod = {
        PeptideSequence     = peptideSequence     
        ModPeptideSequence  = modPeptideSequence  
        ModMass             = modMass 
        MissCleavages       = missCleavages
        GlobalMod           = globalMod
        }   
        
    ///
    let peptideDBParamsOf (qConCatParams:QConQuantifierParams) =
        let Ofasta = FastA.fromFile id qConCatParams.OrganismFastaPath
        let Qfasta = Seq.collect (FastA.fromFile id) qConCatParams.QConCatFastaPaths
        let nQs    = 
            List.map Path.GetFileNameWithoutExtension (qConCatParams.OrganismFastaPath::qConCatParams.QConCatFastaPaths)
            |> String.concat "_" 
        let fPath = 
            let dir   = Path.GetDirectoryName qConCatParams.OrganismFastaPath
            let newName = sprintf "%s.fasta" nQs
            let newPath = Path.Combine [|dir;newName|] 
            if FileIO.fileExists newPath then 
                FileIO.DeleteFile newPath 
            Seq.append Ofasta Qfasta
            |> FastA.write id newPath   
            newPath
        BioFSharp.Mz.SearchDB.createSearchDbParams qConCatParams.Name qConCatParams.DbFolder fPath qConCatParams.FastaHeaderToName
             qConCatParams.Protease qConCatParams.MinMissedCleavages qConCatParams.MaxMissedCleavages qConCatParams.MaxMass
                qConCatParams.MinPepLength qConCatParams.MaxPepLength qConCatParams.IsotopicMod qConCatParams.MassMode
                    qConCatParams.MassFunction qConCatParams.FixedMods qConCatParams.VariableMods qConCatParams.VarModThreshold

    ///
    let dbLookUpCn peptideDBParamsOf (qConCatParams:QConQuantifierParams) = 
        let dbParams = peptideDBParamsOf qConCatParams
        SearchDB.connectOrCreateDB dbParams
 
    /// Prepares statement to select a ModSequence entry by Mass
    let initSelectQConcatPeptideByProtAccession (cn:SQLiteConnection) =
        let querystring = 
            "SELECT 
                PepSequence.Sequence,ModSequence.Sequence,ModSequence.RealMass,CleavageIndex.MissCleavages,ModSequence.GlobalMod
                FROM 
                ModSequence 
                INNER JOIN PepSequence on Pepsequence.ID = ModSequence.PepSequenceID
                INNER JOIN CleavageIndex on CleavageIndex.PepSequenceID = Pepsequence.ID
                INNER JOIN Protein on Protein.ID = CleavageIndex.ProteinID
                WHERE Protein.Accession = @protAccession"
        let cmd = new SQLiteCommand(querystring, cn) 
        cmd.Parameters.Add("@protAccession", Data.DbType.String) |> ignore
        let rec readerloop (reader:SQLiteDataReader) (acc:(string*string*float*int*int) list) =
                match reader.Read() with 
                | true  -> readerloop reader (( reader.GetString(0),reader.GetString(1), reader.GetDouble(2),reader.GetInt32(3),reader.GetInt32(4)) :: acc)
                | false ->  acc 
        fun (protAccession:string) ->
            cmd.Parameters.["@protAccession"].Value <- protAccession
            use reader = cmd.ExecuteReader()            
            readerloop reader [] 
            |> Seq.map (fun (pS,mS,mM,mC,gM) -> createQConcatPeptide pS mS mM mC gM)

    ///
    let copyDBIntoMemory (cn:SQLiteConnection) = SearchDB.copyDBIntoMemory cn

    ///
    let initSelectModPeptideByMassRange (qConCatParams:QConQuantifierParams)  (cn:SQLiteConnection) =  
        let parseAAString = SearchDB.initOfModAminoAcidString qConCatParams.IsotopicMod (qConCatParams.FixedMods@qConCatParams.VariableMods)
        let selectModsequenceByMassRange = SearchDB.Db.SQLiteQuery.prepareSelectModsequenceByMassRange cn 
        (fun lowerMass upperMass  -> 
                let lowerMass' = Convert.ToInt64(lowerMass*1000000.)
                let upperMass' = Convert.ToInt64(upperMass*1000000.)
                selectModsequenceByMassRange lowerMass' upperMass'
                |> List.map (SearchDB.createLookUpResultBy parseAAString)
        )   


    ///
    let initIsValidMz qConQuantParams qConCatPeps = 
        let qConCatMzs = 
            [qConQuantParams.ExpectedMinCharge.. qConQuantParams.ExpectedMaxCharge]
            |> Seq.collect (fun ch -> 
                                qConCatPeps
                                |> Seq.map (fun (qp) -> BioFSharp.Mass.toMZ qp.ModMass (float ch))
                            )
            |> Set.ofSeq
        fun precursorMz ->  
            let minMass,maxMass = Mass.rangePpm qConQuantParams.LookUpPPM precursorMz
            Set.exists (fun x -> x > minMass && x < maxMass) qConCatMzs