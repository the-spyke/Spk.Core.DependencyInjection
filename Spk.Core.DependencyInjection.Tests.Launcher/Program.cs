using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spk.Core.DependencyInjection.Implementations.Tests;

namespace Spk.Core.DependencyInjection.Tests.Launcher
{
	class Program
	{
		static void Main(string[] args)
		{
			InjectionContractsBuilderTests tests = new InjectionContractsBuilderTests();

			tests.Initialize();

			Console.WriteLine("### FullIntegrationTest");
			tests.FullIntegrationTest();
			Console.WriteLine("### Done");
		}
	}
}
