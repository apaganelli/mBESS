using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;


namespace mBESS
{
    class CenterOfMass
    {
        readonly float CoGTrunk = 0.438f;
        readonly float CoGUpperarm = 0.491f;
        readonly float CoGForearm = 0.418f;
        readonly float CoGHand = 0.82f;
        readonly float CoGThigh = 0.4f;
        readonly float CoGCalf = 0.418f;
        readonly float CoGFoot = 0.449f;

        readonly float HeadWeightRatio = 0.073f;
        readonly float TrunkWeightRatio = 0.507f;
        readonly float UpperarmWeightRatio = 0.026f;
        readonly float ForearmWeightRatio = 0.016f;
        readonly float HandWeightRatio = 0.007f;
        readonly float ThighWeightRatio = 0.103f;
        readonly float CalfWeightRatio = 0.043f;
        readonly float FootWeightRatio = 0.015f;

        JointType[] ListProx = {
            JointType.Head,
            JointType.SpineShoulder,
            JointType.ShoulderLeft, JointType.ShoulderRight,
            JointType.ElbowLeft, JointType.ElbowRight,
            JointType.WristLeft, JointType.WristRight,
            JointType.HipLeft, JointType.HipRight,
            JointType.KneeLeft, JointType.KneeRight,
            JointType.AnkleLeft, JointType.AnkleRight };
            
        JointType[] ListDist = {
            JointType.Neck,
            JointType.SpineBase,
            JointType.ElbowLeft, JointType.ElbowRight,
            JointType.WristLeft, JointType.WristRight,
            JointType.HandLeft, JointType.HandRight,
            JointType.KneeLeft, JointType.KneeRight,
            JointType.AnkleLeft, JointType.AnkleRight,
            JointType.FootLeft, JointType.FootRight };

        ApplicationViewModel _app;
        CameraSpacePoint _planePosition;

        public CenterOfMass(ApplicationViewModel app)
        {
            _app = app;
            _planePosition = new CameraSpacePoint();

        }

        public CameraSpacePoint CalculateCoM(IReadOnlyDictionary<JointType, Joint> joints)
        {
            CameraSpacePoint CoM = new CameraSpacePoint();

            CoM.X = 0f;
            CoM.Y = 0f;
            CoM.Z = 0f;

            float ratio = 0f;
            CameraSpacePoint segment = new CameraSpacePoint();

            _planePosition = joints[JointType.SpineBase].Position;

            // Calculate CoM
            // Calculate distance to plane of eache segment CoM.
            // Calculate moments about axis of each segment CoM.
            // Sum up moments about each axis.
            // for each segment
            // Sum of moments = momemnt of the resultant.

            for (int i = 0; i < ListProx.Length; i++)
            {
                segment = CalculateSegmentCoMPosition(joints[ListProx[i]].Position, joints[ListDist[i]].Position, ListProx[i], out ratio);
                segment = Sub3DPosition(segment, _planePosition);
                segment.X = segment.X * ratio;
                segment.Y = segment.Y * ratio;
                segment.Z = segment.Z * ratio;
                CoM = Add3DPosition(CoM, segment);
            }

            // CoM is calculated based on the moments of inertia related to a reference plane.
            // then the accumulated CoM is related to the coordinates of that plane.
            CoM.X = CoM.X + _planePosition.X;
            CoM.Y = CoM.Y + _planePosition.Y;
            CoM.Z = CoM.Z + _planePosition.Z;

            return CoM;
        }

        /// <summary>
        /// Gets the difference of each coordinate.
        /// </summary>
        /// <param name="proximal">Proximal joint 3D position</param>
        /// <param name="distal">Distal joint 3D position</param>
        /// <returns>3D differences between proximal and distal joints</returns>
        private CameraSpacePoint CalculateSegmentPointDistance(CameraSpacePoint proximal, CameraSpacePoint distal)
        {
            CameraSpacePoint distances = new CameraSpacePoint();
            distances.X = proximal.X - distal.X;
            distances.Y = proximal.Y - distal.Y;
            distances.Z = proximal.Z - distal.Z;
            return distances;
        }

        private CameraSpacePoint CalculateSegmentCoMPosition(CameraSpacePoint proximal, CameraSpacePoint distal, JointType segment, out float weightRatio)
        {

            // Calculate distances of x,y,z coordinates of proximal to distal joint.
            CameraSpacePoint distances = new CameraSpacePoint();
            distances = CalculateSegmentPointDistance(proximal, distal);

            CameraSpacePoint segmentCoM = new CameraSpacePoint();

            // Sets the correct weight ratio for segment CoM location.
            weightRatio = 0f;
            float segmentCoG = 0f;

            switch (segment)
            {
                case JointType.Head:
                    // It is not possible to calculate segment CoM location, because kinect returns center of head position.
                    // The proportion got by Clauser et al. (1969) gives us the relative distance from vertex to chin-neck interscet.
                    // Then, in this case, we keep whatever position Kinect is providing.
                    break;

                case JointType.SpineShoulder:
                    // TRUNK
                    segmentCoG = CoGTrunk;
                    weightRatio = TrunkWeightRatio;
                    break;

                case JointType.ShoulderLeft:
                case JointType.ShoulderRight:
                    // UPPERARMS
                    segmentCoG = CoGUpperarm;
                    weightRatio =  UpperarmWeightRatio;
                    break;

                case JointType.ElbowLeft:
                case JointType.ElbowRight:
                    // FOREARMS
                    segmentCoG = CoGForearm;
                    weightRatio =  ForearmWeightRatio;
                    break;

                case JointType.WristLeft:
                case JointType.WristRight:
                    // HANDS
                    segmentCoG = CoGHand;
                    weightRatio = HandWeightRatio;
                    break;

                case JointType.HipLeft:
                case JointType.HipRight:
                    // Thighs
                    segmentCoG = CoGThigh;
                    weightRatio = ThighWeightRatio;
                    break;

                case JointType.KneeLeft:
                case JointType.KneeRight:
                    // Calves
                    segmentCoG = CoGCalf;
                    weightRatio = CalfWeightRatio;
                    break;

                case JointType.AnkleLeft:
                case JointType.AnkleRight:
                    // Feet
                    segmentCoG = CoGFoot;
                    weightRatio = FootWeightRatio;
                    break;
            }

            // Center of Gravity based on Clauser et al. (1969) innertial data and proportional to segment length (distances between joints).
            segmentCoM.X = distances.X * segmentCoG;
            segmentCoM.Y = distances.Y * segmentCoG;
            segmentCoM.Z = distances.Z * segmentCoG;

            // Get segment CoM position
            // Distances based on proximal to distal direction

            CameraSpacePoint segmentCoMPosition = new CameraSpacePoint();            
            segmentCoMPosition = Sub3DPosition(proximal, segmentCoM);

            return segmentCoMPosition;
        }


        private CameraSpacePoint Add3DPosition(CameraSpacePoint p1, CameraSpacePoint p2)
        {
            CameraSpacePoint result = new CameraSpacePoint();
            result.X = p1.X + p2.X;
            result.Y = p1.Y + p2.Y;
            result.Z = p1.Z + p2.Z;

            return (result);
        }


        /// <summary>
        /// Subtract p1 from p2 (p1 - p2)
        /// </summary>
        /// <param name="p1">Minuend</param>
        /// <param name="p2">Subtrahend</param>
        /// <returns></returns>
        private CameraSpacePoint Sub3DPosition(CameraSpacePoint p1, CameraSpacePoint p2)
        {
            CameraSpacePoint result = new CameraSpacePoint();
            result.X = p1.X - p2.X;
            result.Y = p1.Y - p2.Y;
            result.Z = p1.Z - p2.Z;

            return (result);
        }
    }
}
