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
        let manufacturerOutput = results.TryGetResult MzLiteFileDirectory
        let outputDir = results.TryGetResult OutputDirectory
        let paramF = results.TryGetResult ParamFile
        let numberOfCores = 
            match results.TryGetResult NumberOfCores with 
            | Some c -> if c > 0 then c else 1
            | None   -> 1 
        match manufacturerOutput, outputDir, paramF with 
        | Some i, Some o , Some p -> 
            ///
            let processParams: Parameters.Domain.QConQuantifierParams = 
                System.IO.File.ReadAllText p
                |> JsonConvert.DeserializeObject<Parameters.DTO.QConQuantifierParams> 
                |> Parameters.DTO.QConQuantifierParams.toDomain
            ///
            let inputFiles = DirectoryInfo(i).GetFiles("*.mzlite")
            ///            
            let peptideDB = PeptideLookUp.dbLookUpCn processParams
            ///
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

            ///
            let outFilePath = Path.Combine [|o;"QuantifiedPeptides.txt"|]
            res.SaveCsv(outFilePath,includeRowKeys=true,separator='\t',keyNames=["StringSequence";"GlobalMod";"Charge"])
            printfn "Done."
        | _ -> failwith "Error parsing provided Parameters. Type --h for help."
        System.Console.ReadKey() |> ignore
        printfn "Hit any key to exit."
        0