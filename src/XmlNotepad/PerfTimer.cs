//---------------------------------------------------------------------------
// File: PerfTimer.cs
//
// A basic class for inheriting Tests that want to get timing information.
//---------------------------------------------------------------------------

#if PERFTIMER
using System;
using System.Runtime.InteropServices;

namespace XmlNotepad {

    internal class PerfTimer {
        long m_Start;
        long m_End;
        long m_Freq;
        long m_Min;
        long m_Max;
        long m_Count;
        long m_Sum;

		[DllImport("KERNEL32.DLL", EntryPoint = "QueryPerformanceCounter", SetLastError = true,
					CharSet = CharSet.Unicode, ExactSpelling = true,
					CallingConvention = CallingConvention.StdCall)]
		public static extern int QueryPerformanceCounter(ref long time);

		[DllImport("KERNEL32.DLL", EntryPoint = "QueryPerformanceFrequency", SetLastError = true,
			 CharSet = CharSet.Unicode, ExactSpelling = true,
			 CallingConvention = CallingConvention.StdCall)]
		public static extern int QueryPerformanceFrequency(ref long freq);


        public PerfTimer() {
            m_Start = m_End = 0;
            QueryPerformanceFrequency(ref m_Freq);
            m_Min = m_Max = m_Count = m_Sum = 0;
        }

        public void Start() {
            m_Start = GetTime();
            m_End = m_Start;
        }

        public void Stop() {
            m_End = GetTime();
        }

        public long GetDuration() { // in milliseconds.            
            return GetMilliseconds(GetTicks());
        }

        public long GetMilliseconds(long ticks) {
            return (ticks * (long)1000) / m_Freq;
        }

        public long GetTicks() {
            return (m_End - m_Start);
        }

        static public long GetTime() { // in nanoseconds.
            long i = 0;
            QueryPerformanceCounter(ref i);
            return i;
        }

        // These methods allow you to count up multiple iterations and
        // then get the median, average and percent variation.
        public void Count(long ms) {
            if (m_Min == 0) m_Min = ms;
            if (ms < m_Min) m_Min = ms;
            if (ms > m_Max) m_Max = ms;
            m_Sum += ms;
            m_Count++;
        }

        public long Min() {
            return m_Min;
        }

        public long Max() {
            return m_Max;
        }

        public double Median() {
            return TwoDecimals(m_Min + ((m_Max - m_Min) / 2.0));
        }

        public double PercentError() {
            double spread = (m_Max - m_Min) / 2.0;
            double percent = TwoDecimals((double)(spread * 100.0) / (double)(m_Min));
            return percent;
        }

        static public double TwoDecimals(double i) {
            return Math.Round(i * 100) / 100;
        }

        public long Average() {
            if (m_Count == 0) return 0;
            return m_Sum / m_Count;
        }

        public void Clear() {
            m_Start = m_End = m_Min = m_Max = m_Sum = m_Count = 0;
        }
    }
}
#endif