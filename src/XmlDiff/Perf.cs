//------------------------------------------------------------------------------
// <copyright file="Perf.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

#define MEASURE_PERF

namespace Microsoft.XmlDiffPatch {

#if MEASURE_PERF
    public class XmlDiffPerf 
    {
        public int _loadTime = 0;
        public int _hashValueComputeTime = 0;
        public int _identicalOrNoDiffWriterTime = 0;
        public int _matchTime = 0;
        public int _preprocessTime = 0;
        public int _treeDistanceTime = 0;
        public int _diffgramGenerationTime = 0;
        public int _diffgramSaveTime = 0;

        public int TotalTime { 
            get { 
                return _loadTime + _hashValueComputeTime + _identicalOrNoDiffWriterTime + _matchTime + _preprocessTime +
                    _treeDistanceTime + _diffgramGenerationTime + _diffgramSaveTime; 
            } 
        }

        public void Clean() 
        {
            _loadTime = 0;
            _hashValueComputeTime = 0;
            _identicalOrNoDiffWriterTime = 0;
            _matchTime = 0;
            _preprocessTime = 0;
            _treeDistanceTime = 0;
            _diffgramGenerationTime = 0;
            _diffgramSaveTime = 0;
        }
    }
#endif
}