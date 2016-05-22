using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spk.Core.DependencyInjection.Tests.Prototypes
{
	public interface IServiceForBase
	{
		string GetString();
	}

	public class ServiceForBase : IServiceForBase
	{
		public string GetString()
		{
			return "ServiceForBase => GetString";
		}
	}
}
