using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace Ncqrs.Eventing.Sourcing.Mapping
{
	/// <summary>
	///     A internal event handler mapping strategy that maps methods as an event handler based on method name and parameter
	///     type.
	///     <remarks>
	///         All method that match the following requirements are mapped as an event handler:
	///         <list type="number">
	///             <item>
	///                 <value>
	///                     Method name should start with <i>On</i> or <i>on</i>. Like: <i>OnProductAdded</i> or
	///                     <i>onProductAdded</i>.
	///                 </value>
	///             </item>
	///             <item>
	///                 <value>
	///                     The method should only accept one parameter.
	///                 </value>
	///             </item>
	///             <item>
	///                 <value>
	///                     The parameter must be, or implemented from, the type specified by the <see cref="EventBaseType" />
	///                     property. Which is <see cref="object" /> by default.
	///                 </value>
	///             </item>
	///         </list>
	///     </remarks>
	/// </summary>
	public class ConventionBasedEventHandlerMappingStrategy : IEventHandlerMappingStrategy
	{
		private static readonly ConcurrentDictionary<Type, ICollection<ConventionMatchedMethod>> ConventionMatchedMethods =
			new ConcurrentDictionary<Type, ICollection<ConventionMatchedMethod>>();

		private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public IEnumerable<ISourcedEventHandler> GetEventHandlers(object target)
		{
			Contract.Requires<ArgumentNullException>(target != null, "The target cannot be null.");
			Contract.Ensures(Contract.Result<IEnumerable<ISourcedEventHandler>>() != null, "The result should never be null.");

			ICollection<ConventionMatchedMethod> matchedMethods = getConventionMatchedMethods(target);

			var handlers = new List<ISourcedEventHandler>(matchedMethods.Count);
			foreach (ConventionMatchedMethod method in matchedMethods)
			{
				MethodInfo methodCopy = method.MethodInfo;
				Type parameterType = method.EventType.ParameterType;

				Logger.DebugFormat("Created event handler for method {0} based on convention.", methodCopy.Name);
				Action<object> invokeAction = e => methodCopy.Invoke(target, new[] {e});
				var handler = new TypeThresholdedActionBasedDomainEventHandler(invokeAction, parameterType, methodCopy.Name, true);
				handlers.Add(handler);
			}
			return handlers;
		}

		private ICollection<ConventionMatchedMethod> getConventionMatchedMethods(object target)
		{
			Type targetType = target.GetType();
			Logger.DebugFormat("Trying to get all event handlers based by convention for {0}.", targetType);
			if (!ConventionMatchedMethods.ContainsKey(targetType))
			{
				ConventionMatchedMethods[targetType] = loadUpMethods(targetType);
			}
			return ConventionMatchedMethods[targetType];
		}

		private ConventionMatchedMethod[] loadUpMethods(Type targetType)
		{
			MethodInfo[] methodsToMatch = targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			return (from method in methodsToMatch
				let parameters = method.GetParameters()
				where
					parameters.Length == 1
					&& method.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase)
					&& !method.GetCustomAttributes(typeof (NoEventHandlerAttribute), true).Any()
				select new ConventionMatchedMethod {MethodInfo = method, EventType = parameters[0]}).ToArray();
		}

		private class ConventionMatchedMethod
		{
			public MethodInfo MethodInfo { get; set; }
			public ParameterInfo EventType { get; set; }
		}
	}
}