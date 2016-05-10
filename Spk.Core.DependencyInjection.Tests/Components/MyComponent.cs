using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spk.Core.DependencyInjection.Tests.Components
{
	public sealed class MyComponent : MyComponentBase
	{
		private readonly IMyService2 _myService2;

		public MyComponent(IMyComponentInjectionContract injectionContract) 
			: base(injectionContract)
		{
			_myService2 = injectionContract.MyService2;
		}

		public IMyService2 MyService2
		{
			get
			{
				return _myService2;
			}
		}

		public interface IMyComponentInjectionContract : IMyComponentBaseInjectionContract
		{
			IMyService2 MyService2 { get; }
		}
	}
}
