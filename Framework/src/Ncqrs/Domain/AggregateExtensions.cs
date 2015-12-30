using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Ncqrs.Eventing.Sourcing.Snapshotting;

namespace Ncqrs.Domain
{
	internal static class AggregateRootExtensions
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static readonly ConcurrentDictionary<Type, Type> SnapshotInterfaceTypes = new ConcurrentDictionary<Type, Type>();
		private static readonly ConcurrentDictionary<Type, SnapshotMethods> SnapshotCreateMethods = new ConcurrentDictionary<Type, SnapshotMethods>();

		public static Type GetSnapshotInterfaceType(this Type aggregateType)
		{
			Type snapshotInterfaceType;
			if (!SnapshotInterfaceTypes.TryGetValue(aggregateType, out snapshotInterfaceType))
			{
				Type[] snapshotables = (from i in aggregateType.GetInterfaces()
					where i.IsGenericType && i.GetGenericTypeDefinition() == typeof (ISnapshotable<>)
					select i).ToArray();
				if (snapshotables.Any())
				{
					if (snapshotables.Count() > 1)
					{
						Log.WarnFormat("Aggregate root {0} contains multiple snapshot interfaces while only one is allowed.",
							aggregateType.FullName);
					}
					else
					{
						snapshotInterfaceType = snapshotables[0];
					}
				}
				SnapshotInterfaceTypes[aggregateType] = snapshotInterfaceType;
			}
			return snapshotInterfaceType;
		}

		public static MethodInfo GetSnapshotCreateMethod(this Type aggregateType)
		{
			SnapshotMethods snapshotMethods = getSnapshotMethods(aggregateType);
			return snapshotMethods.CreateMethod;
		}

		public static MethodInfo GetSnapshotRestoreMethod(this Type aggregateType)
		{
			SnapshotMethods snapshotMethods = getSnapshotMethods(aggregateType);
			return snapshotMethods.RestoreMethod;
		}

		private static SnapshotMethods getSnapshotMethods(Type aggregateType)
		{
			Type snapshotInterfaceType = aggregateType.GetSnapshotInterfaceType();
			if (snapshotInterfaceType != null)
			{
				SnapshotMethods snapshotMethods;
				if (!SnapshotCreateMethods.TryGetValue(snapshotInterfaceType, out snapshotMethods))
				{
					MethodInfo createMethod = snapshotInterfaceType.GetMethod("CreateSnapshot");
					MethodInfo restoreMethod = snapshotInterfaceType.GetMethod("RestoreFromSnapshot");
					snapshotMethods = new SnapshotMethods(createMethod, restoreMethod);
					SnapshotCreateMethods[snapshotInterfaceType] = snapshotMethods;
				}
				return snapshotMethods;
			}
			return SnapshotMethods.NotSnapshot;
		}

		private class SnapshotMethods
		{
			public static readonly SnapshotMethods NotSnapshot = new SnapshotMethods(null, null);

			public SnapshotMethods(MethodInfo createMethod, MethodInfo restoreMethod)
			{
				CreateMethod = createMethod;
				RestoreMethod = restoreMethod;
			}

			public MethodInfo CreateMethod { get; private set; }
			public MethodInfo RestoreMethod { get; private set; }
		}
	}
}