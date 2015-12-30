using System;
using System.Reflection;
using Ncqrs.Eventing;
using Ncqrs.Eventing.Sourcing.Snapshotting;

namespace Ncqrs.Domain.Storage
{
	public class DefaultAggregateSnapshotter : IAggregateSnapshotter
	{
		private readonly IAggregateRootCreationStrategy _aggregateRootCreator;
		private readonly IAggregateSupportsSnapshotValidator _snapshotValidator;

		public DefaultAggregateSnapshotter(IAggregateRootCreationStrategy aggregateRootCreationStrategy, IAggregateSupportsSnapshotValidator snapshotValidator)
		{
			_aggregateRootCreator = aggregateRootCreationStrategy;
			_snapshotValidator = snapshotValidator;
		}

		public bool TryLoadFromSnapshot(Type aggregateRootType, Snapshot snapshot, CommittedEventStream committedEventStream, out AggregateRoot aggregateRoot)
		{
			aggregateRoot = null;
			if (snapshot == null)
				return false;

			if (AggregateSupportsSnapshot(aggregateRootType, snapshot.Payload.GetType()))
			{
				aggregateRoot = _aggregateRootCreator.CreateAggregateRoot(aggregateRootType);
				aggregateRoot.InitializeFromSnapshot(snapshot);

				MethodInfo restoreMethod = aggregateRoot.GetType().GetSnapshotRestoreMethod();
				restoreMethod.Invoke(aggregateRoot, new[] {snapshot.Payload});

				aggregateRoot.InitializeFromHistory(committedEventStream);
				return true;
			}
			return false;
		}

		public bool TryTakeSnapshot(AggregateRoot aggregateRoot, out Snapshot snapshot)
		{
			snapshot = null;
			MethodInfo createMethod = aggregateRoot.GetType().GetSnapshotCreateMethod();
			if (createMethod != null)
			{
				object payload = createMethod.Invoke(aggregateRoot, new object[0]);
				snapshot = new Snapshot(aggregateRoot.EventSourceId, aggregateRoot.Version, payload);
				return true;
			}
			return false;
		}

		private bool AggregateSupportsSnapshot(Type aggregateRootType, Type snapshotType)
		{
			return _snapshotValidator.DoesAggregateSupportsSnapshot(aggregateRootType, snapshotType);
		}
	}
}