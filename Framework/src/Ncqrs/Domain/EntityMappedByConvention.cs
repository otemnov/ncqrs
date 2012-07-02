﻿using System;
using System.Collections.Generic;
using Ncqrs.Eventing.Sourcing;
using Ncqrs.Eventing.Sourcing.Mapping;

namespace Ncqrs.Domain
{
	[Serializable]
	public abstract class EntityMappedByConvention<TAggregateRoot> : Entity<TAggregateRoot> where TAggregateRoot : AggregateRoot
	{
		protected EntityMappedByConvention(TAggregateRoot parent, Guid entityId)
			: base(parent, entityId)
		{
			internalAssignRoot(parent);
		}

		public override void AssignRoot(TAggregateRoot root)
		{
			base.AssignRoot(root);
			internalAssignRoot(root);
		}

		private void internalAssignRoot(TAggregateRoot root)
		{
			var mapping = new ConventionBasedEventHandlerMappingStrategy();
			IEnumerable<ISourcedEventHandler> handlers = mapping.GetEventHandlers(this);
			foreach (var directHandler in handlers)
			{
				var handler = new EntityThresholdedDomainEventHandlerWrapper(EntityId, GetType(), directHandler, directHandler.EventType);
				root.RegisterHandler(handler);
			}
		}
	}
}
