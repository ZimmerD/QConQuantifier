(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.

#load @"../../src/QConQuantifier\Scripts\references.fsx"
open System
open System.IO
open Deedle
open FSharpAux
open BioFSharp.Mz
open QConQuantifier
open Parameters.Domain
open Parameters.DTO
(**
QConQuant parameters explained
========================

We have already encountered an of the parameters needed to run this tool going through the example on the landing page. This tutorial aims to 
go through this monstrosity one by one and get a little more into detail. 

*)
let qConQuantifierParams = 
    {
    // This will be the name of the peptide data base that is created upon application launch.
    Name                            = "ChlamyTruncDB"
    // This is the directory were the data base will be located. Whenever
    // the application is started the tool looks for data bases in this directory and retrieves their
    // creation parameters. If they are identical to the once in use, the data base will be reused.
    // Note: this also means that this tool will not delete a data base ;) 
    DbFolder                        =  @"C:\Users\david\Source\Repos\netCoreRepos\QConcatifier_Samples\db"
    // This collection of file paths should point to your qConCAT fastas of interest. It is possible to use more than one QconCAT .fasta
    // in one experiment
    QConCatFastaPaths               = [@"C:\Users\david\Source\Repos\netCoreRepos\QConcatifier_Samples\fasta\PS QconCAT.fasta"]
    // The QConCATifier uses the method of target-decoy competition distinct true postive peptide spectrum matches (PSMs) from false positive ones. This data base
    // serves as a codon-unbiased target and decoy source. 
    OrganismFastaPath               =  @"C:\Users\david\Source\Repos\netCoreRepos\QConcatifier_Samples\fasta\Chlamy_JGI5_trunc.fasta"
    // Since .fasta headers are a the wild west of bioinformatics you can specify a regex pattern to parse your protein identifier - if left blank the complete
    // fasta Header will be used in data base creation
    ParseProteinIDRegexPattern      = "id"
    // The protease used for in silico digestion of QConCAT and organism proteins. Note: Should match to the protease used to design the QConCAT. 
    Protease                        = Protease.Trypsin
    // Minimum number of missed cleavages to scan for.
    MinMissedCleavages              = 0
    // Minimum numer of missed cleavages to scan for.
    MaxMissedCleavages              = 1
    // Maximum peptide mass allowed in peptide data base.
    MaxMass                         = 15000.
    // Minimum peptide length allowed in peptide data base.
    MinPepLength                    = 5
    // Maximum peptide length  allowed in peptide data base.
    MaxPepLength                    = 50
    // Isotopic modification used to lable the experiment.
    IsotopicMod                     = [IsotopicMod.N15]
    // Selected mass mode. High accuracy mass spectrometers deliver a resolution that makes it possible to resolve monoisotopic peaks. 
    // If an old instrument is used, the mass mode can be switched to average mass. This is then needed to simulate low resolution theoretical spectra during peptide spectrum
    // matching.
    MassMode                        = MassMode.Monoisotopic
    // If the experimental design led to a permanent modification of a certain amino acid (e.g. carbamidomethylation), this has to be accounted for when computing
    // in silico spectra and retrieving look ups. Here you can specify you modifications.
    FixedMods                       = []            
    // Sometimes the experimental design leads to a variable modification of a certain amino acid (e.g. methionin oxidation) or one is 
    // interested in post translational modification (e.g. phosphorylations). Here you can specify your variable modifications.
    VariableMods                    = []
    // This threshold determines how many variable modifications the user allows to be used for one amino acid. It is not recommended to rise this number to much
    // because it is likely to result in a combinatorial explosion.
    VarModThreshold                 = 3
    // Expected minimum charge, important for calculating theoretically possible mzs of qConCATs
    ExpectedMinCharge               = 2
    // Expected maximum charge, important for calculating theoretically possible mzs of qConCATs
    ExpectedMaxCharge               = 3
    // When a ms2 is recorded, this decision is based on a ion observed in a ms1 scan. The charge and the m/z value of this precursor ion
    // determine the search space. This search window is determined by the theoretical mass +/- x. The x is typically calculated as parts per million
    // of the mass.
    LookUpPPM                       = 30.
    // Determines the scan range when calculating in silico spectra
    ScanRange                       = 100.0,1600.0
    // Determines if the target-decoy competition is used (PepValue) or if the spectra should be filtered solely on the sequest score. 
    PSMThreshold                    = PSMThreshold.PepValue 0.05
    // Determines the scan time window in minutes when extracting a XIC.
    ScanTimeWindow                  = 2.5
    // Determines the m/z window in dalton when extracting a XIC.
    MzWindow_Da                     = 0.07
    // Determines the nterminal series considered when calculating in silico spectra
    NTerminalSeries                 = NTerminalSeries.B
    // Determines the cterminal series considered when calculating in silico spectra
    CTerminalSeries                 = CTerminalSeries.Y
    } 
