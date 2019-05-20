namespace QConQuantifier

open System
open System.IO
open MzLite.Model
open MzLite.SQL
open BioFSharp.Mz
open Newtonsoft.Json
open Parameters.DTO

module IO = 

            
    module Reader = 

        let createReader mzLiteFilePath = 
            if Path.GetExtension mzLiteFilePath = ".mzlite" then
                new MzLiteSQL(mzLiteFilePath)
            else failwith "only mzLite files are allowed as input. Reader could not be initialized."

        let getMassSpectra (reader:MzLiteSQL) = 
            reader.ReadMassSpectra("sample=0")

    module MassSpectrum = 

        /// Returns the ID of the MassSpectrum
        let getID (massSpectrum: MassSpectrum) =
            massSpectrum.ID  

        /// Returns the MsLevel of the MassSpectrum 
        let getMsLevel (massSpectrum: MassSpectrum) = 
            if massSpectrum.CvParams.Contains("MS:1000511") then 
                (massSpectrum.CvParams.["MS:1000511"].Value) |> Convert.ToInt32
            else 
                -1

        /// Returns the ScanTime (formerly: RetentionTime) of the MassSpectrum
        let getScanTime (massSpectrum: MassSpectrum) =  
            if massSpectrum.Scans.[0].CvParams.Contains("MS:1000016") then
                massSpectrum.Scans.[0].CvParams.["MS:1000016"].Value |> Convert.ToDouble        
            else 
                -1.    
    
        /// Returns PrecursorMZ of MS2 spectrum
        let getPrecursorMZ (massSpectrum: MassSpectrum) =
            if massSpectrum.Precursors.[0].SelectedIons.[0].CvParams.Contains("MS:1002234") then
                massSpectrum.Precursors.[0].SelectedIons.[0].CvParams.["MS:1002234"].Value:?> float  // |> Convert.ToInt32        
            else 
                -1.  
        
        let getPeaks (reader:MzLiteSQL) (ms:MassSpectrum) = 
            reader.ReadSpectrumPeaks(ms.ID).Peaks
            |> Seq.map (fun p-> Peak(p.Mz,p.Intensity))
            |> Array.ofSeq

    module XIC = 

        open MzLite.Processing

        /// Quantification
        let createRangeQuery v offset =
            new RangeQuery(v, offset)

        ///
        let getRetentionTimeIdx (reader:MzLiteSQL) = reader.BuildRtIndex("sample=0")

        /// 
        let getXICBy (reader:MzLiteSQL) (rtIdx:MzLite.Commons.Arrays.IMzLiteArray<MzLiteLinq.RtIndexEntry>) (rtQuery:RangeQuery) (mzQuery:RangeQuery) = 
            reader.RtProfile(rtIdx, rtQuery, mzQuery) 
        
        /// 
        let initGetXIC (reader:MzLiteSQL) (rtIdx:MzLite.Commons.Arrays.IMzLiteArray<MzLiteLinq.RtIndexEntry>) rtOffset mzOffset tarRT tarMz  = 
            let rtQuery = createRangeQuery tarRT rtOffset
            let mzQuery = createRangeQuery tarMz mzOffset
            reader.RtProfile(rtIdx, rtQuery, mzQuery)
            
     
     
     