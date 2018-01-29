using Microsoft.Kinect;
using System;

namespace mBESS
{
    class Util
    {
        public enum TrackingState
        {
            NotIdentified, Ongoing, Identified
        }

        public static bool CompareWithErrorMargin(double margin, double value1, double value2)
        {
            return value1 >= value2 - margin && value1 <= value1 + margin;
        }

        public static Vector4 CreateVectorTwoPoints(Joint joint1, Joint joint2)
        {
            Vector4 result = new Vector4();

            result.X = Convert.ToSingle(Math.Sqrt(Math.Pow(joint1.Position.X - joint2.Position.X, 2)));
            result.Y = Convert.ToSingle(Math.Sqrt(Math.Pow(joint1.Position.Y - joint2.Position.Y, 2)));
            result.Z = Convert.ToSingle(Math.Sqrt(Math.Pow(joint1.Position.Z - joint2.Position.Z, 2)));
            return result;
        }

        public static double VectorProduct(Vector4 v, Vector4 w)
        {
            return v.X * w.X + v.Y * w.Y + v.Z * w.Z;
        }

        public static double VectorModulusProduct(Vector4 v, Vector4 w)
        {
            return Math.Sqrt((Math.Pow(v.X, 2) + Math.Pow(v.Y, 2) + Math.Pow(v.Z, 2)) * 
                (Math.Pow(w.X, 2) + Math.Pow(w.Y, 2) + Math.Pow(w.Z, 2)));
        }

        /// <summary>
        /// Calculate angle between joints
        /// </summary>
        /// <param name="joint1">Distal joint</param>
        /// <param name="joint2">Middle joint</param>
        /// <param name="joint3">Proximal joint</param>
        /// <returns>Angle between joints in degrees (double)</returns>
        public static double ScalarProduct(Joint joint1, Joint joint2, Joint joint3)
        {
            Vector4 v = CreateVectorTwoPoints(joint1, joint2);
            Vector4 w = CreateVectorTwoPoints(joint2, joint3);

            double radians = Math.Acos(VectorProduct(v, w) / VectorModulusProduct(v, w));

            // return the result in degrees.
            return radians * 180 / Math.PI;
        }



    }
}
