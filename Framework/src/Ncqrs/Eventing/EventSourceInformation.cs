using System;

namespace Ncqrs.Eventing
{
    /// <summary>
    /// Contains information about an event source.
    /// </summary>
    public class EventSourceInformation
    {
        private readonly Guid _id;
        private readonly Type _type;
        private readonly long _initialVersion;
        private readonly long _currentVersion;

        public EventSourceInformation(Guid id, Type type, long initialVersion, long currentVersion)
        {
            _id = id;
            _type = type;
            _currentVersion = currentVersion;
            _initialVersion = initialVersion;
        }

        public long CurrentVersion
        {
            get { return _currentVersion; }
        }

        public long InitialVersion
        {
            get { return _initialVersion; }
        }

        public Guid Id
        {
            get { return _id; }
        }

        public Type Type
        {
            get { return _type; }
        }
    }
}