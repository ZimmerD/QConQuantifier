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
QConQuantifier
======================

Setup
-------

1. The QConQuantifier library can be downloaded from <a href="https://github.com/ZimmerD/QConQuantifier">github</a>. 
2. To build the tool please make sure that you have installed:  
	- 2.1 the latest  <a href="https://dotnet.microsoft.com/download">.NET Core SDK</a> AND <a href="https://dotnet.microsoft.com/downloadr">.NET Framework Dev Pack</a>.   
	- 2.2 the fake cli via "dotnet tool install fake-cli -g", build by calling "fake build" in the root of the project. For details regarding projects based uppon the project scaffold please visit <a href="https://github.com/fsprojects/ProjectScaffold">ProjectScaffold</a> or <a href="https://github.com/CSBiology/CsbScaffold">CsbScaffold</a>. 
3. Once everything is set up you can download the <a href="https://1drv.ms/u/s!Ak2uNQ51QZNO00VztxLIcEIKTZpi">sample datasets</a> and get started! 

Example
-------

After downloading the sample data set and building the project you can approach to analyze the data, by executing the QConQuantifier console tool.

### Running the tool.
Navigate to ..\QConQuantifier\src\QConQuantifier\Scripts and open createParams.fsx with your favorite IDE. As you can see I already referenced 
paths fitting to the structure of the sample data. You will probably have to refine the paths a bit. Now we can use the help of intellisense to create a parameter
type. In the end we will end up with a human readable Json file that we can exchange with colleagues and reuse if we want to start the console tool again.
*)


let qConQuantifierParams = 
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
    } 

(***do-not-eval***)  
System.IO.File.WriteAllText("C:\Users\david\Source\Repos\netCoreRepos\QConcatifier_Samples\params.Json",Newtonsoft.Json.JsonConvert.SerializeObject qConQuantifierParams)


(**
After creating this parameter set and writing it to a file we have everything to start the console tool. 
To see which parameters the tool expects we can start it with the --help parameter.

![cmd_helpOut](img/cmd_helpOut.PNG)

As you can see there are 4 parameters to provide:

1. -i MzLiteFileDirectory: The path of the directory containing the files to analyze
2. -o OutputDirectory    : The path of the directory containing the files to analyze
3. -p ParamFile          : The path of the param file we just created
4. -c NumberOfCores      : The number of cores you want to use. Each File will be analyzed on a different Core and the results will be combined.

If I now add the correct paths the programm will start and finish after aprox. 15 minutes.:

![cmd_Run](img/cmd_Run.PNG)

If you now navigate to your designated output folder you will find the data in a tab seperated format as "QuantifiedPeptides.txt" and a folder containing .html
graphs showing the quantification results for each peptide grouped by the respective raw file. 
*)

(**
Samples & documentation
-----------------------

 * On the right side you will find further tutorials.

 * [API Reference](reference/index.html) contains automatically generated documentation for all types, modules
   and functions in the library. This includes additional brief samples on using most of the
   functions.
 
*)
