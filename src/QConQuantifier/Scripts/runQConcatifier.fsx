#load "references.fsx"
#load @"C:\Users\kevin\source\repos\kMutagene\QConQuantifier\packages\Deedle\Deedle.fsx"

open System
open System.IO
open Deedle
open FSharpAux
open BioFSharp.Mz
open QConQuantifier
open Parameters.Domain
open Parameters.DTO

let sourceD = __SOURCE_DIRECTORY__

let qConQuantifierParams = 
    {
    Name                            = "ChlamyTruncDB"
    DbFolder                        =  Path.Combine(sourceD,"../../../tests/TestBox")
    QConCatFastaPaths               = [Path.Combine(sourceD,"../../../tests/TestBox/PS_QconCAT.fasta")]
    OrganismFastaPath               =  Path.Combine(sourceD,"../../../tests/TestBox/Chlamy_JGI5_5.fasta")
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
    EstimateLabelEfficiency         = true
    } 

System.IO.File.WriteAllText(Path.Combine(sourceD,"../../../tests/TestBox/sampleParams.json"),Newtonsoft.Json.JsonConvert.SerializeObject qConQuantifierParams)
    

let qparams = qConQuantifierParams  |> QConQuantifierParams.toDomain
let inputDirectory  = Path.Combine(sourceD,"../../../tests/TestBox/Samples")
let outputDirectory = Path.Combine(sourceD,"../../../tests/TestBox/Out")
let numberOfCores = 4

///
let inputFiles = DirectoryInfo(inputDirectory).GetFiles("*.mzlite")
///            
let peptideDB = PeptideLookUp.dbLookUpCn qparams
///
let res = 
    inputFiles 
    |> PSeq.map (fun f -> 
                    printfn "Start analyzing: %s" f.FullName
                    let res = Pipeline.analyzeFile peptideDB qparams outputDirectory f.FullName
                    printfn "Finished analyzing: %s" f.FullName
                    res
                )
    |> PSeq.withDegreeOfParallelism numberOfCores
    |> List.ofSeq
    |> Pipeline.mergeFrames

///
let outFilePath = Path.Combine [|outputDirectory;"QuantifiedPeptides.txt"|]
res.SaveCsv(outFilePath,includeRowKeys=true,separator='\t',keyNames=["StringSequence";"GlobalMod";"Charge"])

