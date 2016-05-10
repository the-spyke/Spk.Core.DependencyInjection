using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spk.Core.DependencyInjection.Tests.Components
{
	public interface IMyService1
	{
		string GetSeting();
	}

	public sealed class MyService1 : IMyService1
	{
		public string GetSeting()
		{
			return "This is MyService #1";
		}
	}

	public interface IMyService2
	{
		string GetSeting();
	}

	public sealed class MyService2 : IMyService2
	{
		public string GetSeting()
		{
			return "This is MyService #2";
		}
	}
}
