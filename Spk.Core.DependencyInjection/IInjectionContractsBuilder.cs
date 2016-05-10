using System;
using System.Collections.Generic;
using System.Reflection;

namespace Spk.Core.DependencyInjection
{
	/// <summary>
	/// Contains a set of methods to build injections contracts from its definitions.
	/// </summary>
	public interface IInjectionContractsBuilder
	{
		/// <summary>
		/// Builds injection contracts.
		/// </summary>
		/// <param name="assemblyToSearchIn">The assembly to search for injections contracts in.</param>
		/// <returns>
		/// A dictionary of injection contracts and its implementations.
		/// </returns>
		IDictionary<Type, Type> BuildInjectionContracts(Assembly assemblyToSearchIn);

		/// <summary>
		/// Builds injection contracts.
		/// </summary>
		/// <param name="assemblyNamePrefix">The name prefix for injections contracts assembly.</param>
		/// <param name="injectionContracts">Injection contracts.</param>
		/// <returns>
		/// A dictionary of injection contracts and its implementations.
		/// </returns>
		IDictionary<Type, Type> BuildInjectionContracts(string assemblyNamePrefix, IEnumerable<Type> injectionContracts);
	}
}
