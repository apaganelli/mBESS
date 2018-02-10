using System;
using System.Collections.Generic;
using System.Windows.Media;

using Microsoft.Kinect;
using Microsoft.Kinect.Face;

using System.Windows;
using System.Globalization;

namespace mBESS
{
    class KinectBodyView : ObservableObject
    {
        private KinectSensor _sensor = null;

        /// <summary> Reader for body frames </summary>
        private BodyFrameReader _bodyFrameReader = null;

        /// <summary> Array for the bodies (Kinect will track up to 6 people simultaneously) </summary>
        private Body[] bodies = null;

        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 10;

        private const double CoMSize = 5;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        // Face recognition attributes and variables
        FaceFrameSource _faceSource = null;            
        FaceFrameReader _faceReader = null;
        FaceFrameResult _faceResult = null;

        ApplicationViewModel _app;

        /// <summary>
        /// Checks if a pose is stable.
        /// </summary>
        public PoseDoubleStance poseDoubleStance;

        CenterOfMass _CoM;

        public KinectBodyView(ApplicationViewModel app)
        {
            // Gets application pointer.
            _app = app;

            _CoM = new CenterOfMass(_app);

            // Gets Kinect sensor reference.
            _sensor = KinectSensor.GetDefault();

            // If there is an active kinect / of accessible studio library.
            if (_sensor != null)
            {
                // Opens the sensor.
                _sensor.Open();

                // open the reader for the body frames
                _bodyFrameReader = _sensor.BodyFrameSource.OpenReader();
                _bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;

                // get the coordinate mapper
                this.coordinateMapper = _sensor.CoordinateMapper;

                // get the depth (display) extents
                FrameDescription frameDescription = _sensor.DepthFrameSource.FrameDescription;

                // get size of joint space
                this.displayWidth = frameDescription.Width;
                this.displayHeight = frameDescription.Height;

                _faceSource = new FaceFrameSource(_sensor, 0, FaceFrameFeatures.LeftEyeClosed | FaceFrameFeatures.RightEyeClosed);
                _faceReader = _faceSource.OpenReader();

                _faceReader.FrameArrived += FaceReader_FrameArrived;
            }

            // Sets flag for recording DoubleStance position references to false
            RecordDoubleStance = false;
            ExecuteDoubleStanceTest = false;

            poseDoubleStance = new PoseDoubleStance(_app);

            CreateBones();
        }


        bool _addEyeError = true;

        private void FaceReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using(var frame = e.FrameReference.AcquireFrame())
            {
                if(frame != null)
                {
                    _faceResult = frame.FaceFrameResult;
                }
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor and updates the associated gesture detector object for each body
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        // creates an array of 6 bodies, which is the max number of bodies that Kinect can track simultaneously
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                // visualize the new body data
                this.UpdateBodyFrame(this.bodies);
            }
        }

        int _dsCounter = 0;
        int _dsPoseErrorCounter = 0;
        int _dsEyesErrorCounter = 0;
        int _NumFramesThresholdError = 15;
        int _lastNOK_Type = 0;
        double _frameNumId = 0;
        double _skipFrames = 0;

        /// <summary>
        /// Updates the body array with new information from the sensor
        /// Should be called whenever a new BodyFrameArrivedEvent occurs
        /// </summary>
        /// <param name="bodies">Array of bodies to update</param>
        public void UpdateBodyFrame(Body[] bodies)
        {
            if (bodies != null)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                    int penIndex = 0;

                    foreach (Body body in bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            _faceSource.TrackingId = body.TrackingId;
                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // Calculate Center of Mass using segment method.
                            CameraSpacePoint CoM = _CoM.CalculateCoM(joints);

                            if (CoM.Z < 0)
                            {
                                CoM.Z = InferredZPositionClamp;
                            }

                            DepthSpacePoint Location_CoM = coordinateMapper.MapCameraPointToDepthSpace(CoM);
                            Point CoM_Point = new Point(Location_CoM.X, Location_CoM.Y);

                            // UpdateJointPosition(joints);

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;

                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }


                            // Pose calibration, recording positions to get a reference of them when they are theoretically stable.
                            if(RecordDoubleStance)
                            { 
                                if(_dsCounter < ApplicationViewModel.MaxFramesReference)
                                {
                                    _app.DSFL_X.Add(joints[JointType.FootLeft].Position.X);
                                    _app.DSFL_Y.Add(joints[JointType.FootLeft].Position.Y);
                                    _app.DSFL_Z.Add(joints[JointType.FootLeft].Position.Z);

                                    _app.DSFR_X.Add(joints[JointType.FootRight].Position.X);
                                    _app.DSFR_Y.Add(joints[JointType.FootRight].Position.Y);
                                    _app.DSFR_Z.Add(joints[JointType.FootRight].Position.Z);

                                    _app.DSHL_X.Add(joints[JointType.HandLeft].Position.X);
                                    _app.DSHL_Y.Add(joints[JointType.HandLeft].Position.Y);
                                    _app.DSHL_Z.Add(joints[JointType.HandLeft].Position.Z);

                                    _app.DSHR_X.Add(joints[JointType.HandRight].Position.X);
                                    _app.DSHR_Y.Add(joints[JointType.HandRight].Position.Y);
                                    _app.DSHR_Z.Add(joints[JointType.HandRight].Position.Z);

                                    _app.DSHE_X.Add(joints[JointType.Head].Position.X);
                                    _app.DSHE_Y.Add(joints[JointType.Head].Position.Y);
                                    _app.DSHE_Z.Add(joints[JointType.Head].Position.Z);

                                    _app.DSSM_X.Add(joints[JointType.SpineMid].Position.X);
                                    _app.DSSM_Y.Add(joints[JointType.SpineMid].Position.Y);
                                    _app.DSSM_Z.Add(joints[JointType.SpineMid].Position.Z);

                                    _app.DSSB_X.Add(joints[JointType.SpineBase].Position.X);
                                    _app.DSSB_Y.Add(joints[JointType.SpineBase].Position.Y);
                                    _app.DSSB_Z.Add(joints[JointType.SpineBase].Position.Z);

                                    _dsCounter++;
                                    // Adds 50 ms to counter.
                                    _app.PoseCalibrationCounter += 50;
                                }    
                            } else
                            {
                                if(_dsCounter > 0)
                                {
                                    poseDoubleStance.CalculatedStandardJoints();
                                }

                                _dsCounter = 0;
                            }

                            // Checks if user is in the right pose (balanced) or unbalanced
                            if(ExecuteDoubleStanceTest)
                            {
                                Util.TrackingState trackingState = poseDoubleStance.Tracker(body);

                                _frameNumId++;
                                _skipFrames--;          // delay after counting an error to be back to position without counting new errors.

                                switch(trackingState)
                                {
                                    case Util.TrackingState.Identified:             // the test is over
                                        ExecuteDoubleStanceTest = false;
                                        ((DoubleCalibrationViewModel)_app.CurrentPageViewModel).StatusText = "Finished sucessfully";
                                        _dsPoseErrorCounter = 0;
                                        break;

                                    case Util.TrackingState.NotIdentified:          // a pose error
                                        ((DoubleCalibrationViewModel)_app.CurrentPageViewModel).StatusText = "Double stance pose with error. # of errors: " + ((DoubleCalibrationViewModel)_app.CurrentPageViewModel).TotalDoubleStanceError;

                                        if (poseDoubleStance.NOK_Type != _lastNOK_Type)
                                        {
                                            if(_skipFrames < 0) _dsPoseErrorCounter++;
                                        }

                                        if(_dsPoseErrorCounter > _NumFramesThresholdError)
                                        {
                                            _dsPoseErrorCounter = 0;
                                            _lastNOK_Type = poseDoubleStance.NOK_Type;
                                            ((DoubleCalibrationViewModel)_app.CurrentPageViewModel).TotalDoubleStanceError++;

                                            if (_lastNOK_Type > 7) // foot displacement in general causes hand and trunk displacement, give 1 second to return to position.
                                                _skipFrames = 30;
                                        }

                                        break;

                                    case Util.TrackingState.Ongoing:                // it is okay, keep going.
                                        ((DoubleCalibrationViewModel)_app.CurrentPageViewModel).StatusText = "Double stance pose is okay. # errors: " + ((DoubleCalibrationViewModel)_app.CurrentPageViewModel).TotalDoubleStanceError;
                                        _dsPoseErrorCounter = 0;
                                        _lastNOK_Type = 0;
                                        _skipFrames = 0;
                                        break;
                                }
                            }   // end execute double stance test - analyze posture.


                            // Text to show eyes status on drawing space.
                            string faceText = "EYES: ";

                            // Only analyze eyes if it has a tracked faced.
                            if (_faceResult != null)
                            {
                                var eyeLeftClosed = _faceResult.FaceProperties[FaceProperty.LeftEyeClosed];
                                var eyeRightClosed = _faceResult.FaceProperties[FaceProperty.RightEyeClosed];

                                if (eyeLeftClosed == DetectionResult.No || eyeRightClosed == DetectionResult.No)
                                {
                                    if (eyeLeftClosed == DetectionResult.No) faceText += "LEFT ";
                                    if (eyeRightClosed == DetectionResult.No) faceText += "RIGHT ";
                                    faceText += "OPEN";

                                    if (_addEyeError)
                                    {
                                        // Only update error and status messages if test is on.
                                        if (ExecuteDoubleStanceTest)
                                        {

                                            if (_dsEyesErrorCounter > _NumFramesThresholdError)
                                            {
                                                ((DoubleCalibrationViewModel)_app.CurrentPageViewModel).TotalDoubleStanceError++;
                                                ((DoubleCalibrationViewModel)_app.CurrentPageViewModel).StatusText = "Eye(s) opened error";
                                                _dsEyesErrorCounter = 0;

                                                _addEyeError = false;
                                            }
                                            else
                                            {
                                                _dsEyesErrorCounter++;
                                            }
                                        }                                        
                                    }
                                }
                                else
                                {
                                    faceText += "CLOSED";
                                    _dsEyesErrorCounter = 0;

                                    if (!_addEyeError)
                                    {
                                        _addEyeError = true;

                                    }
                                }

                                Point textLocation = new Point(10, 10);

                                dc.DrawText(new FormattedText(
                                            faceText,
                                            CultureInfo.GetCultureInfo("en-us"),
                                            FlowDirection.LeftToRight,
                                            new Typeface("Georgia"),
                                            30,                                        // Font size 
                                            Brushes.White),
                                            textLocation);
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen, true);
                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                            dc.DrawEllipse(Brushes.White, null, CoM_Point, CoMSize, CoMSize);
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
                
            }
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen, bool trackState)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen, trackState);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen, bool track)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (track && (
                joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked))
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;
                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;
                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Create drawing group and body colors, and body segments.
        /// </summary>
        public void CreateBones()
        {
            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));
            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);
        }

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get { return this.imageSource; }
        }

#region Properties
        public bool RecordDoubleStance { get; set; }
        public bool ExecuteDoubleStanceTest { get; set; }
#endregion
    }
}
