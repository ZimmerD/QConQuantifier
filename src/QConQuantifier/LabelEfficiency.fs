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
            PredictedPattern        : string
            ActualPattern           : string
            MedianLabelEfficiency   : float
            MedianPattern           : string
            FullLabeledPattern      : string
            CorrectionFactor        : float
        }

    ///C0nverts a float tuple list to its full String representation. Sadly needed as Deedle does use ToString() for patterns otherwise
    let stringOfPattern (p: (float*float) list) =
        p
        |> List.fold (fun acc (mz,prob) -> if acc = "" then (sprintf "(%f,%f)" mz prob) else sprintf "%s;(%f,%f)" acc mz prob) ""
        |> fun x -> sprintf "[%s]" x
    
    ///C0nverts a float list tuple String representation to the list type. Sadly needed as Deedle does use ToString() for patterns otherwise
    let patternOfString (patternString:string) =
        if patternString = "[]" then 
            []
        else
            patternString
                .Replace("[","")
                .Replace("]","")
                .Split(';')
            |> Array.map 
                (fun mzprob ->
                    mzprob
                        .Replace("(","")
                        .Replace(")","")
                        .Split(',')
                    |> fun x -> 
                        (float x.[0], float x.[1])
                )
            |> Array.toList

    let createLabelEfficiencyResult 
        stringSequence 
        globalMod charge 
        predictedLabelEfficiency 
        (predictedPattern   : (float*float) list)
        (actualPattern      : (float*float) list)
        medianLabelEfficiency 
        (medianPattern      : (float*float) list)
        (fullLabeledPattern : (float*float) list)
        correctionFactor =
        {
            StringSequence              = stringSequence
            GlobalMod                   = globalMod     
            Charge                      = charge        
            PredictedLabelEfficiency    = predictedLabelEfficiency
            PredictedPattern            = predictedPattern  |> stringOfPattern
            ActualPattern               = actualPattern     |> stringOfPattern
            MedianLabelEfficiency       = medianLabelEfficiency   
            MedianPattern               = medianPattern     |> stringOfPattern
            FullLabeledPattern          = fullLabeledPattern|> stringOfPattern
            CorrectionFactor            = correctionFactor 
        }

    ///returns a function that replaces the nitrogen atoms in a formula with a nitrogen with the given probability of being the 15N isotope
    let initlabelN15Partial n15Prob =
        ///Diisotopic representation of nitrogen with abundancy of N14 and N15 swapped
        let n14Prob = 1. - n15Prob
        let N15 = Di (createDi "N15" (Isotopes.Table.N15,n15Prob) (Isotopes.Table.N14,n14Prob) )
        fun f -> Formula.replaceElement f Elements.Table.N N15

    ///Predicts an isotopic distribution of the given formula at the given charge, normalized by the sum of probabilities, using the MIDAs algorithm
    let generateIsotopicDistributionOfFormulaBySum (charge:int) (f:Formula.Formula) =
        IsotopicDistribution.MIDA.ofFormula 
            IsotopicDistribution.MIDA.normalizeByProbSum
            0.01
            0.001
            charge
            f

    ///Predicts an isotopic distribution of the given formula at the given charge, normalized by the maximum probability, using the MIDAs algorithm
    let generateIsotopicDistributionOfFormulaByMax (charge:int) (f:Formula.Formula) =
        IsotopicDistribution.MIDA.ofFormula 
            IsotopicDistribution.MIDA.normalizeByMaxProb
            0.01
            0.001
            charge
            f

    ///Predicts the Label efficiency of the given Peptide by finding the best fitting Isotopic distribution amongst distributions generated at various label efficiencies.
    let predictLabelEfficiency (qP : QuantifiedPeptide) =

        //no need to predict anything if one of the peaks is missing.
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

            //Generate "testing" distributions (patterns at various label efficiencies)
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

            //for each predicted pattern, couple it with label efficiency and n15 / n15-1 ratio
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
            
            //find best fitting pattern by minimizing the absolute difference between the N15/N15-1 ratios
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

    ///Calculates median label efficiency after filtering the label efficiency distribution using the tukey interquartile range criterion, initialized with C=3
    let getFilteredMedianLabelEfficiency plotDirectory tukeyC (predictors: LabelEfficiencyPredictor []) =

        let lEDist =
            predictors
            |> Array.map (fun p -> p.PredictedLabelEfficiency)
            |> Array.filter (fun x -> not (nan.Equals(x)))

        let outlierBorders = FSharp.Stats.Testing.Outliers.tukey 3. lEDist
            
        let filtered = 
            lEDist
            |> Array.filter (fun eff -> eff < outlierBorders.Upper && eff > outlierBorders.Lower)

        Charting.saveMedianLabelEfficiencyChart lEDist filtered tukeyC outlierBorders.Upper outlierBorders.Lower plotDirectory

        filtered
        |> Seq.median 

    ///For all given LabelEfficiency predictors, adds the correction factor needed to correct the N15 value as if the label efficiency would be 100%. 
    ///This is calculated by predicting the isotopic patterns at median label (the proxy for the label efficiency of the qconcat protein) efficiency and at 100% label efficiency. The correction Factor is the 
    ///is the probability peak of 15N at median LE / probability peak of N15 at 100%Le
    let estimateCorrectionFactors (filteredMedianPredictedLabelEfficiency:float) (predictors: LabelEfficiencyPredictor []) : LabelEfficiencyResult [] =
        predictors
        |> Array.map 
            (fun lePredictor ->
                //if we cannot predict LE, return an uncorrected result.
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
