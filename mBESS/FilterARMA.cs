using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mBESS
{
    class FilterARMA
    {
        private int idx;
        private int N;
        private float[] history;

        /// <summary>
        /// Constructor of the filter ARMA
        /// </summary>
        /// <param name="n">Size of the average moved window of historical position data</param>
        public FilterARMA(int n)
        {
            idx = 0;
            N = n;
            history = new float[N];
        }

        /// <summary>
        /// Stores a new position and get rid of the oldest one.
        /// </summary>
        /// <param name="point"></param>
        public float GetPoint(float point)
        {
            UpdateSerie(point);
            return history.Average();
        }

        public void ClearSerie()
        {
            history = null;
            history = new float[N];          
        }


        private void UpdateSerie(float point)
        {
            // Update history for smoothing not tracked points
            if (idx < N)
            {
                history[idx] = point;
                idx++;
            }
            else
            {
                // history is full, then shift last N-1 elements to left
                // last position receives new point.
                Array.Copy(history, 1, history, 0, N - 1);
                history[N - 1] = point;
            }
        }
    }
}
