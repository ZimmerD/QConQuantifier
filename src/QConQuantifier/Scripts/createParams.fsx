#load "references.fsx"

open System.IO
open BioFSharp.Mz
open SearchDB
open QConQuantifier
open Parameters.Domain
open Parameters.DTO

let standardQConQuantifierParams :Parameters.DTO.QConQuantifierParams = 
    {
    Name                            = "ChlamyTruncDB"
    DbFolder                        =  @"C:\Users\david\Source\Repos\netCoreRepos\QConcatifier_Samples\db"
    QConCatFastaPaths               = [@"C:\Users\david\Source\Repos\netCoreRepos\QConcatifier_Samples\fasta\PS QconCAT.fasta"]
    OrganismFastaPath               =  @"C:\Users\david\Source\Repos\netCoreRepos\QConcatifier_Samples\fasta\Chlamy_JGI5_trunc.fasta"
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

let sourceD = __SOURCE_DIRECTORY__

System.IO.File.WriteAllText(Path.Combine [|sourceD;"sampleParams.Json"|],Newtonsoft.Json.JsonConvert.SerializeObject standardQConQuantifierParams)
