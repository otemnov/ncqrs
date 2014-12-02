using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Transactions;

namespace Ncqrs.Eventing.ServiceModel.Bus
{
	public class InProcessEventBus : IEventBus
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly Dictionary<Type, List<Action<PublishedEvent>>> _handlerRegister =
			new Dictionary<Type, List<Action<PublishedEvent>>>();

		private readonly bool _useTransactionScope;
		private static readonly Action<PublishedEvent>[] NoHandlersForEvent = new Action<PublishedEvent>[0];

		/// <summary>
		///     Creates new <see cref="InProcessEventBus" /> instance that wraps publishing to
		///     handlers into a <see cref="TransactionScope" />.
		/// </summary>
		public InProcessEventBus()
			: this(true)
		{
		}

		/// <summary>
		///     Creates new <see cref="InProcessEventBus" /> instance.
		/// </summary>
		/// <param name="useTransactionScope">Use transaction scope?</param>
		public InProcessEventBus(bool useTransactionScope)
		{
			_useTransactionScope = useTransactionScope;
		}

		public void Publish(IPublishableEvent eventMessage)
		{
			Type eventMessageType = eventMessage.GetType();

			Log.InfoFormat("Started publishing event {0}.", eventMessageType.FullName);

			IReadOnlyCollection<Action<PublishedEvent>> handlers = GetHandlersForEvent(eventMessage);

			if (handlers.Count == 0)
			{
				Log.WarnFormat("Did not found any handlers for event {0}.", eventMessageType.FullName);
			}
			else
			{
				if (_useTransactionScope)
				{
					TransactionallyPublishToHandlers(eventMessage, eventMessageType, handlers);
				}
				else
				{
					PublishToHandlers(eventMessage, eventMessageType, handlers);
				}
			}
		}

		public void Publish(IEnumerable<IPublishableEvent> eventMessages)
		{
			foreach (IPublishableEvent eventMessage in eventMessages)
			{
				Publish(eventMessage);
			}
		}

		private static void TransactionallyPublishToHandlers(IPublishableEvent eventMessage, Type eventMessageType, IReadOnlyCollection<Action<PublishedEvent>> handlers)
		{
			Contract.Requires<ArgumentNullException>(handlers != null);

			using (var transaction = new TransactionScope())
			{
				PublishToHandlers(eventMessage, eventMessageType, handlers);
				transaction.Complete();
			}
		}

		private static void PublishToHandlers(IPublishableEvent eventMessage, Type eventMessageType, IReadOnlyCollection<Action<PublishedEvent>> handlers)
		{
			Log.DebugFormat("Found {0} handlers for event {1}.", handlers.Count, eventMessageType.FullName);

			Type publishedEventClosedType = typeof (PublishedEvent<>).MakeGenericType(eventMessage.Payload.GetType());
			var publishedEvent = (PublishedEvent) Activator.CreateInstance(publishedEventClosedType, eventMessage);

			foreach (var handler in handlers)
			{
				Log.DebugFormat("Calling handler {0} for event {1}.", handler.GetType().FullName, eventMessageType.FullName);

				handler(publishedEvent);

				Log.DebugFormat("Call finished.");
			}
		}

		protected IReadOnlyCollection<Action<PublishedEvent>> GetHandlersForEvent(IPublishableEvent eventMessage)
		{
			Type dataType = eventMessage.Payload.GetType();
			List<Action<PublishedEvent>> handlersForEvent;
			if (_handlerRegister.TryGetValue(dataType, out handlersForEvent))
			{
				return handlersForEvent.AsReadOnly();
			}
			return NoHandlersForEvent;
		}

		public void RegisterHandler<TEvent>(IEventHandler<TEvent> handler)
		{
			Type eventDataType = typeof(TEvent);
			Action<PublishedEvent> act = evnt => handler.Handle((IPublishedEvent<TEvent>) evnt);
			RegisterHandler(eventDataType, act);
		}

		public void RegisterHandler(Type eventDataType, Action<PublishedEvent> handler)
		{
			List<Action<PublishedEvent>> handlers;
			if (!_handlerRegister.TryGetValue(eventDataType, out handlers))
			{
				handlers = new List<Action<PublishedEvent>>(1);
				_handlerRegister.Add(eventDataType, handlers);
			}

			handlers.Add(handler);
		}
	}
}