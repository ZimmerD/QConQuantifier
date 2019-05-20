namespace QConQuantifier

module CreateParams = 

    open Parameters.DTO
    open Parameters.Domain

    open BioFSharp.Mz.SearchDB
    open Newtonsoft.Json

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


    System.IO.File.WriteAllText(@"C:\Users\david\Source\Repos\netCoreRepos\QConQuantifier\src\QConQuantifierConsole\params.Json",JsonConvert.SerializeObject standardQConQuantifierParams)
