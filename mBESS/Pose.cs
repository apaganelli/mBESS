using Microsoft.Kinect;

namespace mBESS
{
    abstract class Pose : Movement
    {
        protected int IdentificationFrame { get; set; }

        public override Util.TrackingState Tracker(Body body)
        {
            Util.TrackingState newState;

            if(body != null && ValidPosition(body))
            {
                if(IdentificationFrame == FrameCounter)
                {
                    newState = Util.TrackingState.Identified;
                } else
                {
                    newState = Util.TrackingState.Ongoing;
                    FrameCounter += 1;
                }
            } else
            {
                newState = Util.TrackingState.NotIdentified;
                FrameCounter = 0;
            }

            return newState;
        }


        public int Progress
        {
            get
            {
                return FrameCounter * 100 / IdentificationFrame;
            }
        }

    }
}
