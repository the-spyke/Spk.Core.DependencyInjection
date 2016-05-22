using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spk.Core.DependencyInjection.Tests.Prototypes
{
	public class BaseComponent
	{
		public BaseComponent(IBaseComponentContract services)
		{
			MyService = services.DependencyForBase;
		}

		private IServiceForBase MyService { get; set; }

		public virtual void Run()
		{
			Console.WriteLine(MyService.GetString());
		}

		public interface IBaseComponentContract : IMarkerInterface
		{
			IServiceForBase DependencyForBase
			{
				get;
			}
		}
	}
}
