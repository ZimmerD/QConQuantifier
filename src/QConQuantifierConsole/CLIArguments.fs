namespace QConQuantifier

open System
open Argu

module CLIArgumentParsing = 

    open System.IO
    type CLIArguments =
        |[<Unique>] [<AltCommandLine("-i")>] MzLiteFile of filePath:string
        |[<Unique>] [<AltCommandLine("-f")>] OrganismFasta of filePath :string
        |[<AltCommandLine("-f")>] QConcatFasta of filePath :string        
        |[<Unique>] [<AltCommandLine("-o")>] OutputDirectory  of directoryPath:string 
        |[<Unique>] [<AltCommandLine("-p")>] ParamFile of path:string
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | MzLiteFile _ -> "specify mass spectrometry data in mzlite format."
                | OrganismFasta _ -> "specify the whole proteom fasta."
                | QConcatFasta _ -> "specify fasta of QConcat protein."
                | OutputDirectory  _ -> "specify output directory."
                | ParamFile _        -> "specify param file."