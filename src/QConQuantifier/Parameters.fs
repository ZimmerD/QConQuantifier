namespace QConQuantifier

open BioFSharp
open BioFSharp.Mz
open BioFSharp.Mz.SearchDB

module Parameters = 
    
    module Domain =
        
        type NTerminalSeries = ((IBioItem -> float) -> AminoAcids.AminoAcid list -> PeakFamily<TaggedMass.TaggedMass> list)
        type CTerminalSeries = ((IBioItem -> float) -> AminoAcids.AminoAcid list -> PeakFamily<TaggedMass.TaggedMass> list)

        type PSMThreshold = 
            | PepValue of float
            | SequestScore of float 

        type QConQuantifierParams = {
                // name of database i.e. Creinhardtii_236_protein_full_labeled
                Name                : string
                // path of db storage folder
                DbFolder            : string
                QConCatFastaPaths   : string list 
                OrganismFastaPath   : string
                FastaHeaderToName   : string -> string
                Protease            : Digestion.Protease
                MinMissedCleavages  : int
                MaxMissedCleavages  : int
                MaxMass             : float
                MinPepLength        : int
                MaxPepLength        : int
                // valid symbol name of isotopic label in label table i.e. #N15
                IsotopicMod         : SearchInfoIsotopic list 
                MassMode            : MassMode
                MassFunction        : IBioItem -> float  
                FixedMods           : SearchModification list            
                VariableMods        : SearchModification list
                VarModThreshold     : int
                ExpectedMinCharge   : int
                ExpectedMaxCharge   : int 
                LookUpPPM           : float
                ScanRange           : float*float 
                PSMThreshold        : PSMThreshold
                /// Xic time window
                ScanTimeWindow      : float
                /// Xic mz window
                MzWindow_Da         : float
                ///
                NTerminalSeries     : NTerminalSeries
                CTerminalSeries     : CTerminalSeries
                } 
    
    module DTO = 
        open BioFSharp.Elements

        type MassMode = SearchDB.MassMode

        module MassMode =
   
            let toDomain (massMode: MassMode) =
                match massMode with
                | MassMode.Monoisotopic -> BioItem.initMonoisoMassWithMemP
                | MassMode.Average      -> BioItem.initAverageMassWithMemP


        type Protease =
            | Trypsin

        module Protease = 

            let toDomain protease = 
                match protease with
                | Trypsin -> Digestion.Table.getProteaseBy "Trypsin"

        type IsotopicMod =
            | N15
            | C13

        module IsotopicMod =
            let toDomain isoMod = 
                match isoMod with
                | N15 -> 
                    createSearchInfoIsotopic "N15" Elements.Table.N Elements.Table.Heavy.N15
                | C13 -> 
                    let C13 = Element.Di (createDi "C13" (Isotopes.Table.C13,Isotopes.Table.C12.NatAbundance) (Isotopes.Table.C12,Isotopes.Table.C13.NatAbundance) )
                    createSearchInfoIsotopic "C15" Elements.Table.C C13
        
        type Modification =
            | Acetylation'ProtNTerm'
            | Carbamidomethyl'Cys'
            | Oxidation'Met'
            | Phosphorylation'Ser'Thr'Tyr'
    
        module Modification  =

            let toDomain modification = 
                match modification with
                | Acetylation'ProtNTerm'        -> Table.acetylation'ProtNTerm'
                | Carbamidomethyl'Cys'          -> Table.carbamidomethyl'Cys'
                | Oxidation'Met'                -> Table.oxidation'Met'
                | Phosphorylation'Ser'Thr'Tyr'  -> Table.phosphorylation'Ser'Thr'Tyr'

        let parseProteinIdUsing regex =
            match regex with
            | "ID" | "id" | "Id" | "" -> 
                id
            | pattern ->        
                (fun (inp : string)  -> System.Text.RegularExpressions.Regex.Match(inp,pattern).Value)

        type NTerminalSeries = 
            | A
            | B
            | C
            | AB
            | AC
            | BC
            | ABC
        
        module NTerminalSeries = 
            let toDomain nTermSeries =
                match nTermSeries with
                | A   -> Fragmentation.Series.aOfBioList
                | B   -> Fragmentation.Series.bOfBioList
                | C   -> Fragmentation.Series.cOfBioList
                | AB  -> Fragmentation.Series.abOfBioList
                | AC  -> Fragmentation.Series.acOfBioList
                | BC  -> Fragmentation.Series.bcOfBioList
                | ABC -> Fragmentation.Series.abcOfBioList


        type CTerminalSeries = 
            | X
            | Y
            | Z
            | XY
            | XZ
            | YZ
            | XYZ
        
        module CTerminalSeries = 
            let toDomain nTermSeries =
                match nTermSeries with
                | X   -> Fragmentation.Series.xOfBioList
                | Y   -> Fragmentation.Series.yOfBioList
                | Z   -> Fragmentation.Series.zOfBioList
                | XY  -> Fragmentation.Series.xyOfBioList
                | XZ  -> Fragmentation.Series.xzOfBioList
                | YZ  -> Fragmentation.Series.yzOfBioList
                | XYZ -> Fragmentation.Series.xyzOfBioList

        type QConQuantifierParams = {
                // name of database i.e. Creinhardtii_236_protein_full_labeled
                Name                : string
                // path of db storage folder
                DbFolder            : string
                QConCatFastaPaths   : string list 
                OrganismFastaPath   : string
                ParseProteinIDRegexPattern   : string 
                Protease            : Protease
                MinMissedCleavages  : int
                MaxMissedCleavages  : int
                MaxMass             : float
                MinPepLength        : int
                MaxPepLength        : int
                // valid symbol name of isotopic label in label table i.e. #N15
                IsotopicMod         : IsotopicMod list 
                MassMode            : MassMode
                FixedMods           : Modification list            
                VariableMods        : Modification list
                VarModThreshold     : int
                ExpectedMinCharge   : int
                ExpectedMaxCharge   : int 
                LookUpPPM           : float
                ScanRange           : float*float 
                PSMThreshold        : Domain.PSMThreshold
                /// Xic time window
                ScanTimeWindow      : float
                /// Xic mz window
                MzWindow_Da         : float
                ///
                NTerminalSeries     : NTerminalSeries
                CTerminalSeries     : CTerminalSeries
                } 

        module QConQuantifierParams = 

            let toDomain (dtoQParams:QConQuantifierParams) :Domain.QConQuantifierParams =

                {
                Name                = dtoQParams.Name               
                DbFolder            = dtoQParams.DbFolder            
                QConCatFastaPaths   = dtoQParams.QConCatFastaPaths   
                OrganismFastaPath   = dtoQParams.OrganismFastaPath   
                FastaHeaderToName   = parseProteinIdUsing dtoQParams.ParseProteinIDRegexPattern   
                Protease            = Protease.toDomain dtoQParams.Protease
                MinMissedCleavages  = dtoQParams.MinMissedCleavages  
                MaxMissedCleavages  = dtoQParams.MaxMissedCleavages   
                MaxMass             = dtoQParams.MaxMass             
                MinPepLength        = dtoQParams.MinPepLength        
                MaxPepLength        = dtoQParams.MaxPepLength        
                IsotopicMod         = dtoQParams.IsotopicMod |> List.map IsotopicMod.toDomain      
                MassMode            = dtoQParams.MassMode            
                MassFunction        = MassMode.toDomain dtoQParams.MassMode 
                FixedMods           = dtoQParams.FixedMods  |> List.map Modification.toDomain          
                VariableMods        = dtoQParams.VariableMods    |> List.map Modification.toDomain    
                VarModThreshold     = dtoQParams.VarModThreshold     
                ExpectedMinCharge   = dtoQParams.ExpectedMinCharge   
                ExpectedMaxCharge   = dtoQParams.ExpectedMaxCharge   
                LookUpPPM           = dtoQParams.LookUpPPM           
                ScanRange           = dtoQParams.ScanRange                
                PSMThreshold        = dtoQParams.PSMThreshold      
                ScanTimeWindow      = dtoQParams.ScanTimeWindow      
                MzWindow_Da         = dtoQParams.MzWindow_Da       
                NTerminalSeries     = NTerminalSeries.toDomain dtoQParams.NTerminalSeries
                CTerminalSeries     = CTerminalSeries.toDomain dtoQParams.CTerminalSeries
                }                     