using System;
using Microsoft.Kinect;

namespace mBESS
{
    class Tracker<T> : ITracker where T: Movement, new()
    {
        private T moviment;
        Util.TrackingState CurrentState { get; set; }
        
        public event EventHandler MovimentIdentified;
        public event EventHandler MovimentOngoing;

        public Tracker()
        {
            CurrentState = Util.TrackingState.NotIdentified;
            moviment = Activator.CreateInstance<T>();
        }

        void ITracker.Tracker(Body body)
        {
            Util.TrackingState newState = moviment.Tracker(body);

            if (newState == Util.TrackingState.Identified && CurrentState != Util.TrackingState.Identified)
            {
                CallEvent(MovimentIdentified); 
            }

            if(newState == Util.TrackingState.Ongoing && (newState == Util.TrackingState.Ongoing || CurrentState == Util.TrackingState.NotIdentified))
            {
                CallEvent(MovimentOngoing);
            }

            CurrentState = newState;
        }

        void CallEvent(EventHandler newEvent)
        {
            if (newEvent != null)
                newEvent(moviment, new EventArgs());
        }
    }
}
