using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spk.Core.DependencyInjection.Tests.Prototypes
{
	public sealed class BaseComponentContract : BaseComponent.IBaseComponentContract
	{
		private readonly IServiceForBase _DependencyForBase;

		public BaseComponentContract(IServiceForBase DependencyForBase)
		{
			if (DependencyForBase == null)
			{
				throw new ArgumentNullException("DependencyForBase");
			}

			_DependencyForBase = DependencyForBase;
		}

		public IServiceForBase DependencyForBase
		{
			get
			{
				return _DependencyForBase;
			}
		}
	}
}
