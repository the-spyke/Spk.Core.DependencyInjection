using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using LightInject;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Spk.Core.DependencyInjection.Tests.Components;

namespace Spk.Core.DependencyInjection.Implementations.Tests
{
	[TestClass]
	public class InjectionContractsBuilderTests
	{
		public InjectionContractsBuilder _builder;

		[TestInitialize]
		public void Initialize()
		{
			_builder = new InjectionContractsBuilder();
		}

		[TestMethod]
		public void FullIntegrationTest()
		{
			Stopwatch stopWatch = new Stopwatch();

			Assembly currentAssembly = Assembly.GetExecutingAssembly();

			stopWatch.Start();
			IDictionary<Type, Type> services = _builder.BuildInjectionContracts(currentAssembly);
			stopWatch.Stop();

			Console.WriteLine("Execution time [ms]: " + stopWatch.ElapsedMilliseconds);

			ServiceContainer container = new ServiceContainer();

			foreach (KeyValuePair<Type, Type> servicePair in services)
			{
				Console.WriteLine();
				Console.WriteLine("Service: " + servicePair.Key.FullName);
				Console.WriteLine("Implementation: " + servicePair.Value.FullName);

				container.Register(servicePair.Key, servicePair.Value, new PerContainerLifetime());
			}

			container.Register<IMyService1, MyService1>(new PerContainerLifetime());
			container.Register<IMyService2, MyService2>(new PerContainerLifetime());

			container.Register<MyComponent>(new PerContainerLifetime());

			MyComponent myComponent = container.GetInstance<MyComponent>();

			Assert.AreEqual(myComponent.MyService1.GetSeting(), "This is MyService #1", "IMyService1 wrong result.");
			Assert.AreEqual(myComponent.MyService2.GetSeting(), "This is MyService #2", "IMyService2 wrong result.");
		}
	}
}