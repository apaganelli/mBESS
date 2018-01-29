using Microsoft.Kinect;

namespace mBESS
{
    abstract class Movement
    {
        protected int FrameCounter { get; set; }
        public string Name { get; set; }
        public abstract Util.TrackingState Tracker(Body body);
        protected abstract bool ValidPosition(Body body);
    }
}
