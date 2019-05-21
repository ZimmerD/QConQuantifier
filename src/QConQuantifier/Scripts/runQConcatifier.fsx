#load "references.fsx"

open System
open System.IO
open Deedle
open FSharpAux
open BioFSharp.Mz
open QConQuantifier
open Parameters.Domain
open Parameters.DTO

let qConQuantifierParams = 
    {
    Name                            = "ChlamyTruncDB"
    DbFolder                        = ""
    QConCatFastaPaths               = [""]
    OrganismFastaPath               = ""
    ParseProteinIDRegexPattern      = "id"
    Protease                        = Protease.Trypsin
    MinMissedCleavages              = 0
    MaxMissedCleavages              = 1
    MaxMass                         = 15000.
    MinPepLength                    = 5
    MaxPepLength                    = 50
    IsotopicMod                     = [IsotopicMod.N15]
    MassMode                        = MassMode.Monoisotopic
    FixedMods                       = []            
    VariableMods                    = []
    VarModThreshold                 = 3
    ExpectedMinCharge               = 2
    ExpectedMaxCharge               = 3
    LookUpPPM                       = 30.
    ScanRange                       = 100.0,1600.0
    PSMThreshold                    = PSMThreshold.PepValue 0.05
    ScanTimeWindow                  = 2.5
    MzWindow_Da                     = 0.07
    NTerminalSeries                 = NTerminalSeries.B
    CTerminalSeries                 = CTerminalSeries.Y
    } 
    |> QConQuantifierParams.toDomain
        

let inputDirectory = ""
let outputDirectory = ""
let numberOfCores = 4

///
let inputFiles = DirectoryInfo(inputDirectory).GetFiles("*.mzlite")
///            
let peptideDB = PeptideLookUp.dbLookUpCn qConQuantifierParams
///
let res = 
    inputFiles 
    |> PSeq.map (fun f -> 
                    printfn "Start analyzing: %s" f.FullName
                    let res = Pipeline.analyzeFile peptideDB qConQuantifierParams outputDirectory f.FullName
                    printfn "Finished analyzing: %s" f.FullName
                    res
                )
    |> PSeq.withDegreeOfParallelism numberOfCores
    |> List.ofSeq
    |> Pipeline.mergeFrames

///
let outFilePath = Path.Combine [|outputDirectory;"QuantifiedPeptides.txt"|]
res.SaveCsv(outFilePath,includeRowKeys=true,separator='\t',keyNames=["StringSequence";"GlobalMod";"Charge"])

