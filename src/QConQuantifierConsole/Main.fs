namespace QConQuantifier

open System
open Argu
open CLIArgumentParsing

module QConQuantifier =

    [<EntryPoint>]
    let main argv = 
        printfn "%A" argv

        let parser = ArgumentParser.Create<CLIArguments>(programName =  (System.Reflection.Assembly.GetExecutingAssembly().GetName().Name)) 
        let usage  = parser.PrintUsage()
        printfn "%s" usage
        let results = parser.Parse argv
        printfn "Hit any key to exit."
        System.Console.ReadKey() |> ignore
        0
