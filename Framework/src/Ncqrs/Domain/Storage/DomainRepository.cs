using System;
using Ncqrs.Eventing;
using Ncqrs.Eventing.Sourcing.Snapshotting;

namespace Ncqrs.Domain.Storage
{
    public class DomainRepository : IDomainRepository
    {
        private readonly IAggregateRootCreationStrategy _aggregateRootCreator;
        private readonly IAggregateSnapshotter _aggregateSnapshotter;

        public DomainRepository(IAggregateRootCreationStrategy aggregateRootCreationStrategy, IAggregateSnapshotter aggregateSnapshotter)
        {
            _aggregateRootCreator = aggregateRootCreationStrategy;
            _aggregateSnapshotter = aggregateSnapshotter;
        }

        public AggregateRoot Load(Type aggreateRootType, Snapshot snapshot, CommittedEventStream eventStream)
        {
            AggregateRoot aggregate;
	        if (!_aggregateSnapshotter.TryLoadFromSnapshot(aggreateRootType, snapshot, eventStream, out aggregate))
	        {
		        aggregate = GetByIdFromScratch(aggreateRootType, eventStream);
	        }
	        return aggregate;
        }

        public Snapshot TryTakeSnapshot(AggregateRoot aggregateRoot)
        {
            Snapshot snapshot;
            _aggregateSnapshotter.TryTakeSnapshot(aggregateRoot, out snapshot);
            return snapshot;
        }

        protected AggregateRoot GetByIdFromScratch(Type aggregateRootType, CommittedEventStream committedEventStream)
        {
	        if (committedEventStream.IsEmpty)
	        {
		        return null;
	        }
	        AggregateRoot aggregateRoot = _aggregateRootCreator.CreateAggregateRoot(aggregateRootType);
            aggregateRoot.InitializeFromHistory(committedEventStream);
            return aggregateRoot;
        }
    }
}
