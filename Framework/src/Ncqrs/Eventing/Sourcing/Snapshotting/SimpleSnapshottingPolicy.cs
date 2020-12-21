using System;
using Ncqrs.Domain;

namespace Ncqrs.Eventing.Sourcing.Snapshotting
{
    /// <summary>
    /// Default implementation of snapshotting policy using one interval value for all aggregates.
    /// </summary>
    public class SimpleSnapshottingPolicy : ISnapshottingPolicy
    {
        private const int DefaultSnapshotIntervalInEvents = 3;
        private readonly int _snapshotIntervalInEvents = DefaultSnapshotIntervalInEvents;

        public SimpleSnapshottingPolicy()
        {            
        }

        public SimpleSnapshottingPolicy(int snapshotIntervalInEvents)
        {
            _snapshotIntervalInEvents = snapshotIntervalInEvents;
        }

        public bool ShouldCreateSnapshot(AggregateRoot aggregateRoot)
        {
	        if (SupportsSnapshot(aggregateRoot.GetType()))
	        {
		        if (!aggregateRoot.RestoredFromSnapshot)
		        {
			        return true;
		        }

		        for (var i = aggregateRoot.InitialVersion + 1; i <= aggregateRoot.Version; i++)
		        {
			        if (i%_snapshotIntervalInEvents == 0)
			        {
				        return true;
			        }
		        }
	        }
	        return false;
        }

	    public bool SupportsSnapshot(Type aggregateRootType)
	    {
			return aggregateRootType.GetSnapshotInterfaceType() != null;
	    }
    }
}