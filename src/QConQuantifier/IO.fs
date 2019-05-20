namespace QConQuantifier

open System
open System.IO
open BioFSharp.Mz

open MzLite.Model
open MzLite.SQL
open MzLite.Processing

module IO = 
            
    module Reader = 

        /// Creates an mzLite reader. This object is used to access data stored in the mzLite format.
        let createReader mzLiteFilePath = 
            if Path.GetExtension mzLiteFilePath = ".mzlite" then
                new MzLiteSQL(mzLiteFilePath)
            else failwith "only mzLite files are allowed as input. Reader could not be initialized."
        
        /// Returns all mass spectra present in a mzLite file. 
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
        
        /// Returns a Peak array containing all peaks of a given mass spectrum.
        let getPeaks (reader:MzLiteSQL) (ms:MassSpectrum) = 
            reader.ReadSpectrumPeaks(ms.ID).Peaks
            |> Seq.map (fun p-> Peak(p.Mz,p.Intensity))
            |> Array.ofSeq

    module XIC = 

        /// Creates a range query used to define look up dimensions, e.g. in rt
        let createRangeQuery v offset =
            new RangeQuery(v, offset)

        /// Creates an retention time (scan time) index. This data structure preserves the order of all ms1s to facilitate a fast XIC extraction.
        let getRetentionTimeIdx (reader:MzLiteSQL) = reader.BuildRtIndex("sample=0")

        /// Returns a XIC from a file (reader). A retention time index (rtIdx) has to be created beforhand. The rt and mz dimensions of the XIC are defined using range queries.
        let getXICBy (reader:MzLiteSQL) (rtIdx:MzLite.Commons.Arrays.IMzLiteArray<MzLiteLinq.RtIndexEntry>) (rtQuery:RangeQuery) (mzQuery:RangeQuery) = 
            reader.RtProfile(rtIdx, rtQuery, mzQuery) 
        
        /// Returns a XIC from a file (reader). A retention time index (rtIdx) has to be created beforhand. 
        /// given rt and mz values along with their offsets this function will internally create Query items.
        let initGetXIC (reader:MzLiteSQL) (rtIdx:MzLite.Commons.Arrays.IMzLiteArray<MzLiteLinq.RtIndexEntry>) rtOffset mzOffset tarRT tarMz  = 
            let rtQuery = createRangeQuery tarRT rtOffset
            let mzQuery = createRangeQuery tarMz mzOffset
            reader.RtProfile(rtIdx, rtQuery, mzQuery)
            
     
     
     