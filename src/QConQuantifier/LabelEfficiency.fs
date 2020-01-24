namespace QConQuantifier

open BioFSharp
open BioFSharp.Elements
open BioFSharp.Mz
open Quantification
open FSharp.Stats


module LabelEfficiency = 

    type LabelEfficiencyPredictor =
        {
            StringSequence              : string
            GlobalMod                   : int
            PredictedDistribution       : (float*float) list
            Charge                      : int
            PredictedLabelEfficiency    : float
            ExperimentalDistribution    : (float*float) list
        }

    let createLabelEfficiencyPredictor stringSequence charge globalMod predictedDistribution predictedLabelEfficiency experimentalDistribution =
        {
            StringSequence              = stringSequence          
            Charge                      = charge                  
            GlobalMod                   = globalMod               
            PredictedDistribution       = predictedDistribution   
            PredictedLabelEfficiency    = predictedLabelEfficiency
            ExperimentalDistribution    = experimentalDistribution
        }

    type LabelEfficiencyResult =
        {
            StringSequence          : string
            GlobalMod               : int
            Charge                  : int
            PredictedLabelEfficiency: float
            PredictedPattern        : (float*float) list
            ActualPattern           : (float*float) list
            MedianLabelEfficiency   : float
            MedianPattern           : (float*float) list
            FullLabeledPattern      : (float*float) list
            CorrectionFactor        : float
        }

    let createLabelEfficiencyResult stringSequence globalMod charge predictedLabelEfficiency predictedPattern  actualPattern medianLabelEfficiency medianPattern fullLabeledPattern correctionFactor =
        {
            StringSequence              = stringSequence          
            GlobalMod                   = globalMod               
            Charge                      = charge                  
            PredictedLabelEfficiency    = predictedLabelEfficiency
            PredictedPattern            = predictedPattern        
            ActualPattern               = actualPattern           
            MedianLabelEfficiency       = medianLabelEfficiency   
            MedianPattern               = medianPattern           
            FullLabeledPattern          = fullLabeledPattern      
            CorrectionFactor            = correctionFactor        
        }

    let initlabelN15Partial n15Prob =
        ///Diisotopic representation of nitrogen with abundancy of N14 and N15 swapped
        let n14Prob = 1. - n15Prob
        let N15 = Di (createDi "N15" (Isotopes.Table.N15,n15Prob) (Isotopes.Table.N14,n14Prob) )
        fun f -> Formula.replaceElement f Elements.Table.N N15

    let labelFullN15 =
        let N15 = Elements.Table.Heavy.N15
        fun f -> Formula.replaceElement f Elements.Table.N N15

    let generateIsotopicDistributionOfFormulaBySum (charge:int) (f:Formula.Formula) =
        IsotopicDistribution.MIDA.ofFormula 
            IsotopicDistribution.MIDA.normalizeByProbSum
            0.01
            0.001
            charge
            f

    let generateIsotopicDistributionOfFormulaByMax (charge:int) (f:Formula.Formula) =
        IsotopicDistribution.MIDA.ofFormula 
            IsotopicDistribution.MIDA.normalizeByMaxProb
            0.01
            0.001
            charge
            f

    let predictLabelEfficiency (qP : QuantifiedPeptide) =
        if (nan.Equals(qP.N15Minus1Quant)) || (nan.Equals(qP.N15Quant)) then

            createLabelEfficiencyPredictor
                qP.StringSequence
                qP.Charge
                qP.GlobalMod
                []
                nan
                [(qP.N15Minus1MZ,qP.N15Minus1Quant);(qP.N15MZ,qP.N15Quant)]
        else
            let peptide =
                qP.StringSequence
                |> BioArray.ofAminoAcidString
                |> BioSeq.toFormula

            let peakRatio = qP.N15Minus1Quant / qP.N15Quant

            let theoreticalIsotopicDistributions =
                [for i in 0.5 .. 0.001 .. 0.999 do
                    //if ((int (i*1000.)) % 100) = 0 then
                    //    printfn "%.3f" i
                    yield
                        i,
                        peptide
                        |> initlabelN15Partial i
                        |> Formula.add Formula.Table.H2O
                        |> generateIsotopicDistributionOfFormulaByMax qP.Charge
                ]


            let theoreticalRatios =
                theoreticalIsotopicDistributions
                |> List.map 
                    (fun (le,dist) ->
                        let n15Prob = 
                            dist
                            |> List.minBy 
                                (fun (mz,prob) ->
                                    abs (qP.N15MZ - mz)
                                )
                            |> snd
                        
                        let n15Minus1Prob = 
                            dist
                            |> List.minBy 
                                (fun (mz,prob) ->
                                    abs (qP.N15Minus1MZ - mz)
                                )
                            |> snd
                        le,(n15Minus1Prob / n15Prob), dist
                    )

            let predictedLE, ratio, predictedDist = 
                theoreticalRatios
                |> List.minBy
                    (fun (le,ratio,dist) ->
                        abs (peakRatio - ratio)
                    )

            createLabelEfficiencyPredictor
                qP.StringSequence
                qP.Charge
                qP.GlobalMod
                predictedDist
                predictedLE
                [(qP.N15Minus1MZ,qP.N15Minus1Quant);(qP.N15MZ,qP.N15Quant)]

    let estimateCorrectionFactors (predictors: LabelEfficiencyPredictor []) : LabelEfficiencyResult [] =

        let lEDist =
            predictors
            |> Array.map (fun p -> p.PredictedLabelEfficiency)
            |> Array.filter (fun x -> not (nan.Equals(x)))

        let outlierBorders = FSharp.Stats.Testing.Outliers.tukey 3. lEDist
            
        let filteredMedianPredictedLabelEfficiency =
            lEDist
            |> Array.filter (fun eff -> eff < outlierBorders.Upper && eff > outlierBorders.Lower)
            |> Seq.median 

        predictors
        |> Array.map 
            (fun lePredictor ->
                if nan.Equals(lePredictor.PredictedLabelEfficiency) then
                    createLabelEfficiencyResult
                        lePredictor.StringSequence
                        lePredictor.GlobalMod
                        lePredictor.Charge
                        lePredictor.PredictedLabelEfficiency
                        lePredictor.PredictedDistribution
                        (
                            lePredictor.ExperimentalDistribution
                            |> List.unzip
                            |> fun (x,y) ->
                                List.zip
                                    x
                                    (
                                        y
                                        |> fun vals -> 
                                            let max = List.max vals
                                            vals
                                            |> List.map (fun v -> v / max)
                                    )
                        )
                        filteredMedianPredictedLabelEfficiency
                        []
                        []
                        nan
                else
                    let n15Minus1Mz, n15Minus1Quant =
                        lePredictor.ExperimentalDistribution.[0]

                    let n15Mz, n15Quant =
                        lePredictor.ExperimentalDistribution.[1]

                    let formulaWithH2O =
                        lePredictor.StringSequence
                        |> BioArray.ofAminoAcidString
                        |> BioSeq.toFormula
                        |> Formula.add Formula.Table.H2O

                    let predictedWithMedianLE =
                        formulaWithH2O
                        |> initlabelN15Partial filteredMedianPredictedLabelEfficiency
                        |> generateIsotopicDistributionOfFormulaBySum lePredictor.Charge

                    let predictedWithMedianLENorm = 
                        formulaWithH2O
                        |> initlabelN15Partial filteredMedianPredictedLabelEfficiency
                        |> generateIsotopicDistributionOfFormulaByMax lePredictor.Charge

                    let predictedWithFullLE = 
                        formulaWithH2O
                        |> initlabelN15Partial 0.99999
                        |> generateIsotopicDistributionOfFormulaBySum lePredictor.Charge

                    let predictedWithFullLENorm =
                        formulaWithH2O
                        |> initlabelN15Partial 0.99999
                        |> generateIsotopicDistributionOfFormulaByMax lePredictor.Charge

                    let n15ProbWithMedianLE =
                        predictedWithMedianLE
                        |> List.minBy   
                            (fun (mz,prob) -> abs (mz - n15Mz))

                    let n15ProbWithFullLE =
                        predictedWithFullLE
                        |> List.minBy   
                            (fun (mz,prob) -> abs (mz - n15Mz))

                    let correctionFactor = 
                        snd n15ProbWithFullLE / snd n15ProbWithMedianLE

                    createLabelEfficiencyResult
                        lePredictor.StringSequence
                        lePredictor.GlobalMod
                        lePredictor.Charge
                        lePredictor.PredictedLabelEfficiency
                        lePredictor.PredictedDistribution
                        (
                            lePredictor.ExperimentalDistribution
                            |> List.unzip
                            |> fun (x,y) ->
                                List.zip
                                    x
                                    (
                                        y
                                        |> fun vals -> 
                                            let max = List.max vals
                                            vals
                                            |> List.map (fun v -> v / max)
                                    )
                        )
                        filteredMedianPredictedLabelEfficiency
                        predictedWithMedianLENorm
                        predictedWithFullLENorm
                        correctionFactor
            )
