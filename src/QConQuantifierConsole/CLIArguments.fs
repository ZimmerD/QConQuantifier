namespace QConQuantifier

open System
open Argu

module CLIArgumentParsing = 

    open System.IO
    type CLIArguments =
        |[<Unique>] [<AltCommandLine("-i")>] MzLiteFileDirectory of directoryPath:string
        |[<Unique>] [<AltCommandLine("-i")>] MzLiteFile  of filePath:string
        |[<Unique>] [<AltCommandLine("-o")>] OutputDirectory  of directoryPath:string 
        |[<Unique>] [<AltCommandLine("-p")>] ParamFile of path:string
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | MzLiteFile _ -> "specify mass spectrometry data in mzlite format."
                | OutputDirectory  _ -> "specify output directory."
                | ParamFile _        -> "specify param file."