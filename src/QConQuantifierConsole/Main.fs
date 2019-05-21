namespace QConQuantifier

open System
open Argu
open CLIArgumentParsing
open Newtonsoft.Json
open FSharpAux
open Deedle

module QConQuantifier =
    open System.IO

    [<EntryPoint>]
    let main argv = 
        printfn "%A" argv
        let parser = ArgumentParser.Create<CLIArguments>(programName = (System.Reflection.Assembly.GetExecutingAssembly().GetName().Name)) 
        let usage  = parser.PrintUsage()
        printfn "%s" usage
        let results = parser.Parse argv
        let mzLiteFileDirectory = results.TryGetResult MzLiteFileDirectory
        let outputDir = results.TryGetResult OutputDirectory
        let paramF = results.TryGetResult ParamFile
        let numberOfCores = 
            match results.TryGetResult NumberOfCores with 
            | Some c -> if c > 0 then c else 1
            | None   -> 1 
        match mzLiteFileDirectory, outputDir, paramF with 
        | Some i, Some o , Some p ->     
        
            /// Parsed process params.
            let processParams: Parameters.Domain.QConQuantifierParams = 
                System.IO.File.ReadAllText p
                |> JsonConvert.DeserializeObject<Parameters.DTO.QConQuantifierParams> 
                |> Parameters.DTO.QConQuantifierParams.toDomain           
            /// All mzLite files in the input directory.
            let inputFiles = 
                match DirectoryInfo(i).GetFiles("*.mzlite") with 
                | x when Array.isEmpty x -> failwith "provided input directory (-i) does not contain mzLiteFiles."
                | x -> x            
            /// Creates PeptideDB and/or returns connection.           
            let peptideDB = PeptideLookUp.dbLookUpCn processParams            
            
            /// Joined result tables. 
            let res = 
                inputFiles 
                |> PSeq.map (fun f -> 
                                printfn "Start analyzing: %s" f.FullName
                                let res = Pipeline.analyzeFile peptideDB processParams o f.FullName
                                printfn "Finished analyzing: %s" f.FullName
                                res
                            )
                |> PSeq.withDegreeOfParallelism numberOfCores
                |> List.ofSeq
                |> Pipeline.mergeFrames

            /// Saves joined result table to provided output directory.
            let outFilePath = Path.Combine [|o;"QuantifiedPeptides.txt"|]
            res.SaveCsv(outFilePath,includeRowKeys=true,separator='\t',keyNames=["StringSequence";"GlobalMod";"Charge"])
            printfn "Done."
        
        | _ -> failwith "Error parsing provided Parameters. Type --h for help."
        System.Console.ReadKey() |> ignore
        printfn "Hit any key to exit."
        0