namespace QConQuantifier

open System
open System.IO
open BioFSharp.Mz

open MzIO
open MzIO.Model
open MzIO.MzSQL
open MzIO.Processing
open MzIO.Processing.MzIOLinq
open MzIO.IO

module IO = 
            
    module Reader = 

        /// Creates an mzLite reader. This object is used to access data stored in the mzLite format.
        let createReader mzLiteFilePath = 
            if Path.GetExtension mzLiteFilePath = ".mzlite" then
                let mzLiteReader = new MzSQL(mzLiteFilePath)
                mzLiteReader :> IMzIODataReader
            else failwith "only mzLite files are allowed as input. Reader could not be initialized."
        
        /// Returns all mass spectra present in a mzLite file. 
        let getMassSpectra (reader: IMzIODataReader) = 
            reader.ReadMassSpectra("sample=0")

    module MassSpectrum = 
        
        /// Returns a Peak array containing all peaks of a given mass spectrum.
        let getPeaks (reader: IMzIODataReader) (ms: MassSpectrum) = 
            reader.ReadSpectrumPeaks(ms.ID).Peaks
            |> Seq.map (fun p-> Peak(p.Mz,p.Intensity))
            |> Array.ofSeq

    module XIC = 

        /// Creates an retention time (scan time) index. This data structure preserves the order of all ms1s to facilitate a fast XIC extraction.
        let getRetentionTimeIdx (reader: IMzIODataReader) = reader.BuildRtIndex("sample=0")

        /// Returns a XIC from a file (reader). A retention time index (rtIdx) has to be created beforhand. 
        /// given rt and mz values along with their offsets this function will internally create Query items.
        let initGetXIC (reader: IMzIODataReader) (rtIdx: Commons.Arrays.IMzIOArray<MzIOLinq.RtIndexEntry>) rtOffset mzOffset tarRT tarMz  = 
            let rtQuery = Query.createRangeQuery tarRT rtOffset
            let mzQuery = Query.createRangeQuery tarMz mzOffset
            reader.RtProfile(rtIdx, rtQuery, mzQuery)
            
     
     
     