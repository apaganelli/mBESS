using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Kinect;

namespace mBESS
{
    class PoseDoubleStance : Pose
    {
        ApplicationViewModel _app;
        double _errorAllowed;                    // Precision for noticing differences in any axis of analysed joints
        double _stdAngle;                        // Average angle of head, spine_mid, spine_base recorded during body calibration.
        double _errorAllowedAngle;               // Precision for noticing difference in standard angle of trunk in degrees.

        public Stopwatch StopWatch;

        public string FrameStatus;              // Gets all status messages to be saved.

        public int NOK_Type;                   // Stores the NOK types: 16 LeftFoot, 8 RightFoot, 4 LeftHand, 2 RightHand, 1 Trunk.


        // Auxiliary variables to get joint axis position
        CameraSpacePoint _head;
        CameraSpacePoint _spineMid;
        CameraSpacePoint _spineBase;
        CameraSpacePoint _handLeft;
        CameraSpacePoint _handRight;
        CameraSpacePoint _footLeft;
        CameraSpacePoint _footRight;

        /// <summary>
        /// Constructor. Instantiate local variables and initialize parameters
        /// </summary>
        /// <param name="app">Pointer to main application</param>
        public PoseDoubleStance(ApplicationViewModel app)
        {
            _app = app;

            _errorAllowed = _app.JointAxisPrecision;
            _errorAllowedAngle = _app.AnglePrecision;

            _head = new CameraSpacePoint();
            _spineBase = new CameraSpacePoint();
            _spineMid = new CameraSpacePoint();
            _handLeft = new CameraSpacePoint();
            _handRight = new CameraSpacePoint();
            _footLeft = new CameraSpacePoint();
            _footRight = new CameraSpacePoint();

            StopWatch = new Stopwatch();

            this.Name = "PoseDoubleStance";
            this.FrameCounter = 0;
            this.IdentificationFrame = 30 * 30;                         // Number of frames to be analysed, based on 30 frames per second.
        }

        /// <summary>
        /// Checks if the pose of the body is stable.
        /// </summary>
        /// <param name="body"></param>
        /// <returns>True if pose is stable, false if not.</returns>
        protected override bool ValidPosition(Body body)
        {
            string status = String.Format("{0:00000}", StopWatch.ElapsedMilliseconds) + " /  ";
            string frameStatus = "";

            // Get current positions
            CameraSpacePoint footLeft = body.Joints[JointType.FootLeft].Position;
            CameraSpacePoint footRight = body.Joints[JointType.FootRight].Position;
            CameraSpacePoint handLeft = body.Joints[JointType.HandLeft].Position;
            CameraSpacePoint handRight = body.Joints[JointType.HandRight].Position;
            CameraSpacePoint spineBase = body.Joints[JointType.SpineBase].Position;
            CameraSpacePoint spineMid = body.Joints[JointType.SpineMid].Position;
            CameraSpacePoint head = body.Joints[JointType.Head].Position;

            // Compare current positions to standard position
            bool leftFootOk  = Util.CompareJointsWithErrorMargin(_errorAllowed, footLeft, _footLeft);
            bool rightFootOk = Util.CompareJointsWithErrorMargin(_errorAllowed, footRight, _footRight);
            bool leftHandOk  = Util.CompareJointsWithErrorMargin(_errorAllowed, handLeft, _handLeft);
            bool rightHandOk = Util.CompareJointsWithErrorMargin(_errorAllowed, handRight, _handRight);

            double currAngle = Util.ScalarProduct(head, spineMid, spineBase);
            bool tiltBodyOk  = Util.CompareWithErrorMargin(_errorAllowedAngle, _stdAngle, currAngle);

            // Print and store status.
            status += leftFootOk ? "OK" : "NOK X " + footLeft.X.ToString("N2") + " : " + _footLeft.X.ToString("N2") +
                " Y " + footLeft.Y.ToString("N2") + " : " + _footLeft.Y.ToString("N2") + 
                " Z " + footLeft.Z.ToString("N2") + " : " + _footLeft.Z.ToString("N2");

            ((DoubleCalibrationViewModel)_app.CurrentPageViewModel).LeftFoot = status;
            frameStatus = status + "\n";

            status = rightFootOk ? "OK" : "NOK X " + footRight.X.ToString("N2") + " : " + _footRight.X.ToString("N2") +
                " Y " + footRight.Y.ToString("N2") + " : " + _footRight.Y.ToString("N2") +
                " Z " + footRight.Z.ToString("N2") + " : " + _footRight.Z.ToString("N2");

            ((DoubleCalibrationViewModel)_app.CurrentPageViewModel).RightFoot = status;
            frameStatus += status + "\n";

            status = leftHandOk ? "OK" : "NOK X " + handLeft.X.ToString("N2") + " : " + _handLeft.X.ToString("N2") +
                " Y " + handLeft.Y.ToString("N2") + " : " + _handLeft.Y.ToString("N2") +
                " Z " + handLeft.Z.ToString("N2") + " : " + _handLeft.Z.ToString("N2");

            ((DoubleCalibrationViewModel)_app.CurrentPageViewModel).LeftHand = status;
            frameStatus += status + "\n";

            status = rightHandOk ? "OK" : "NOK X " + handRight.X.ToString("N2") + " : " + _handRight.X.ToString("N2") +
                " Y " + handRight.Y.ToString("N2") + " : " + _handRight.Y.ToString("N2") +
                " Z " + handRight.Z.ToString("N2") + " : " + _handRight.Z.ToString("N2");

            ((DoubleCalibrationViewModel)_app.CurrentPageViewModel).RightHand = status;
            frameStatus += status + "\n";

            status = tiltBodyOk ? "OK" : "NOK " + currAngle.ToString("N2") + " : " + _stdAngle.ToString("N2");

            ((DoubleCalibrationViewModel)_app.CurrentPageViewModel).TrunkSway = status;
            
            NOK_Type = 0;
            if (!leftFootOk) NOK_Type = 16;
            if (!rightFootOk) NOK_Type += 8;
            if (!leftHandOk) NOK_Type += 4;
            if (!rightHandOk) NOK_Type += 2;
            if (!tiltBodyOk) NOK_Type += 1;

            frameStatus += status + "\n" + "NOK_type: " + NOK_Type +  " Computated errors: " + ((DoubleCalibrationViewModel)_app.CurrentPageViewModel).TotalDoubleStanceError;

            Console.WriteLine(frameStatus);

            FrameStatus += frameStatus;


            return leftFootOk && rightFootOk && leftHandOk && rightHandOk && tiltBodyOk;
        }

        /// <summary>
        /// Gets the average values of calibration joint positions and sets up the spine angle with the head.
        /// </summary>
        public void CalculatedStandardJoints()
        {
            // Debugging
            //foreach(float x in _app.DSFL_X) { Console.WriteLine(x); }

            List<double> l_scalarProduct = new List<double>();

            _footLeft.X = _app.DSFL_X.Average();
            _footLeft.Y = _app.DSFL_Y.Average();
            _footLeft.Z = _app.DSFL_Z.Average();

            _footRight.X = _app.DSFR_X.Average();
            _footRight.Y = _app.DSFR_Y.Average();
            _footRight.Z = _app.DSFR_Z.Average();

            _handLeft.X = _app.DSHL_X.Average();
            _handLeft.Y = _app.DSHL_Y.Average();
            _handLeft.Z = _app.DSHL_Z.Average();

            _handRight.X = _app.DSHR_X.Average();
            _handRight.Y = _app.DSHR_Y.Average();
            _handRight.Z = _app.DSHR_Z.Average();

            for(int i = 0; i < _app.DSHE_X.Count; i++)
            {
                _head.X = _app.DSHE_X[i];
                _head.Y = _app.DSHE_Y[i];
                _head.Z = _app.DSHE_Z[i];

                _spineMid.X = _app.DSSM_X[i];
                _spineMid.Y = _app.DSSM_Y[i];
                _spineMid.Z = _app.DSSM_Z[i];

                _spineBase.X = _app.DSSB_X[i];
                _spineBase.Y = _app.DSSB_Y[i];
                _spineBase.Z = _app.DSSB_Z[i];

                _stdAngle = Util.ScalarProduct(_head, _spineMid, _spineBase);

                // Console.WriteLine("Scalar Product-" + i + " : " + _stdAngle.ToString("N2"));

                l_scalarProduct.Add(_stdAngle);
            }

            _stdAngle = l_scalarProduct.Average();

            Console.WriteLine("Scalar Product Average: " + _stdAngle.ToString("N2") + " degrees.");
        }
    }
}
