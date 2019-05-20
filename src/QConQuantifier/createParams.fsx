#r @"C:\Users\david\Source\Repos\netCoreRepos\QConQuantifier\src\QConQuantifier\bin\Release\net47\BioFSharp.Mz.dll"
#r @"C:\Users\david\Source\Repos\netCoreRepos\QConQuantifier\src\QConQuantifier\bin\Release\net47\BioFSharp.dll"
#r @"C:\Users\david\Source\Repos\netCoreRepos\QConQuantifier\bin\QConQuantifier\netstandard2.0\QConQuantifier.dll"
#r @"C:\Users\david\Source\Repos\netCoreRepos\QConQuantifier\src\QConQuantifier\bin\Release\net47\Newtonsoft.Json.dll"

open BioFSharp.Mz
open SearchDB
open QConQuantifier
open Parameters.Domain
open Parameters.DTO

let standardQConQuantifierParams :Parameters.DTO.QConQuantifierParams = 
        {
        Name                            = "ChlamyDB"
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


System.IO.File.WriteAllText(@"C:\Users\david\Source\Repos\netCoreRepos\QConQuantifier\src\QConQuantifierConsole\params.Json",Newtonsoft.Json.JsonConvert.SerializeObject standardQConQuantifierParams)
