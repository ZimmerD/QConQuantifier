namespace QConQuantifier

open System
open Argu

module CLIArgumentParsing = 

    open System.IO
    type CLIArguments =
        | [<Unique>] [<AltCommandLine("-i")>] MzLiteFileDirectory of directoryPath:string
        | [<Unique>] [<AltCommandLine("-o")>] OutputDirectory  of directoryPath:string 
        | [<Unique>] [<AltCommandLine("-p")>] ParamFile of filePath:string
        | [<Unique>] [<AltCommandLine("-c")>] NumberOfCores of integer:int
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | MzLiteFileDirectory _ -> "Specify directory to browse for mass spectrometry data (.mzlite)."
                | OutputDirectory  _    -> "Specify output directory."
                | ParamFile _           -> "Specify param file."
                | NumberOfCores _       -> "Specify how many cores the application can use."
