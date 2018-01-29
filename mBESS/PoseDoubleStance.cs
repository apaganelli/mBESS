using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace mBESS
{
    class PoseDoubleStance : Pose
    {
        ApplicationViewModel _app;

        public PoseDoubleStance(ApplicationViewModel app)
        {
            _app = app;
            this.Name = "PoseDoubleStance";
            this.FrameCounter = 10;
        }

        protected override bool ValidPosition(Body body)
        {
            Joint footLeft = body.Joints[JointType.FootLeft];
            Joint footRight = body.Joints[JointType.FootRight];

            Joint handLeft = body.Joints[JointType.HandLeft];
            Joint handRight = body.Joints[JointType.HandRight];

            Joint spineBase = body.Joints[JointType.SpineBase];
            Joint spineMid = body.Joints[JointType.SpineMid];
            Joint head = body.Joints[JointType.Head];

            return true;
        }
    }
}
