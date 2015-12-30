using System;
using Ncqrs.Eventing.Sourcing.Snapshotting;

namespace Ncqrs.Domain.Storage
{
    public class AggregateSupportsSnapshotValidator : IAggregateSupportsSnapshotValidator
    {
        public bool DoesAggregateSupportsSnapshot(Type aggregateType, Type snapshotType)
        {
            Type memType = aggregateType.GetSnapshotInterfaceType();
            Type expectedType = typeof(ISnapshotable<>).MakeGenericType(snapshotType);
            return memType == expectedType;
        }
    }
}
