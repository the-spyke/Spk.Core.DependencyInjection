using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spk.Core.DependencyInjection.Tests.Components
{
	public abstract class MyComponentBase
	{
		private readonly IMyService1 _myService1;

		protected MyComponentBase(IMyComponentBaseInjectionContract injectionContract)
		{
			_myService1 = injectionContract.MyService1;
		}

		public IMyService1 MyService1
		{
			get
			{
				return _myService1;
			}
		}

		public interface IMyComponentBaseInjectionContract : IInjectionContract
		{
			IMyService1 MyService1 { get; }
		}
	}
}
