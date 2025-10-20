using System;
using System.Collections.Generic;

namespace OpenTrader
{
    public class Profile
    {
        public string ClassName;
        public string MethodName;
        public int Calls;
        public TimeSpan Ticks; // in seconds
        private Profiles mProfiles;
        public DateTime Started;

        public Profile(Profiles profiles, string className, string methodName)
        {
            mProfiles = profiles;
            ClassName = className;
            MethodName = methodName;
            Ticks = new TimeSpan(0);
            Calls = 0;
        }

        public void AddTicks(TimeSpan ticks)
        {
            Ticks += ticks;
        }

        public void Start()
        {
            Started = DateTime.Now;
        }

        public void Stop()
        {
            AddTicks(DateTime.Now - Started);
        }
    }

    public class Profiles : List<Profile>
    {


        public Profile Add(string className, string methodName)
        {
            Profile profile = new Profile(this, className, methodName);
            if( Find(profile) == null)
            {
                Add(profile);
            }
            return profile;
        }

        public Profile Find(Profile find)
        {

            foreach (Profile profile in this)
            {
                if( profile.ClassName == find.ClassName && profile.MethodName == find.MethodName)
                {
                    return profile;
                }
            }
            return null;
        }

        public Profile Find(string className, string methodName)
        {

            foreach (Profile profile in this)
            {
                if (profile.ClassName == className && profile.MethodName == methodName)
                {
                    return profile;
                }
            }
            return null;
        }

        public void AddTicks(string @class, string @method, TimeSpan ticks)
        {
            Profile profile = Find(@class, @method);
            if(profile != null)
            {
                profile.AddTicks(ticks);
            }
        }
    }

    public class ProfileStack : List<Profile>
    {
        Profile mCurrentProfile;
        public delegate void ChangeDelegate(Profile profile);
        public ChangeDelegate OnChange;

        static public Profiles mProfiles = new Profiles();


        public Profiles Profiles
        {
            get { return mProfiles;  }
        }

        public void Push(string className, string methodName)
        {
            if (mCurrentProfile != null)
            {
                mCurrentProfile.Stop();
            }
            mCurrentProfile = mProfiles.Find(className, methodName);
            if(mCurrentProfile == null)
            {
                mCurrentProfile = new Profile(mProfiles,className, methodName);
                mProfiles.Add(mCurrentProfile);
            }
            this.Add(mCurrentProfile);
            mCurrentProfile.Start();
            mCurrentProfile.Calls += 1;
        }

        public void Pop()
        {
            int last = this.Count - 1;
            if (last > 0)
            {
                mCurrentProfile.Stop();
                if (OnChange != null)
                    OnChange(mCurrentProfile);
                this.RemoveAt(last);
                last = last - 1;
                if (last >= 0)
                {
                    mCurrentProfile = this[last];
                    mCurrentProfile.Start();
                }
            }
        }

        public void AddTicks(TimeSpan ticks)
        {
            if(mCurrentProfile != null)
            { 
                mCurrentProfile.AddTicks(ticks);
                if(OnChange != null)
                    OnChange(mCurrentProfile);
            }
        }
    }
}
