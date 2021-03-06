﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Spk.Core.DependencyInjection.Implementations
{
	/// <summary>
	/// Represents a helper, that can make implementation for dependency list interfaces and register them in container.
	/// </summary>
	public sealed class InjectionContractsBuilder : IInjectionContractsBuilder
	{
		#region Private Fields

		private readonly static Type _rootContract = typeof(IInjectionContract);

		#endregion

		#region IInjectionContractsBuilder Members

		/// <summary>
		/// Builds injection contracts.
		/// </summary>
		/// <param name="assemblyToSearchIn">The assembly to search for injections contracts in.</param>
		/// <returns>
		/// A dictionary of injection contracts and its implementations.
		/// </returns>
		public IDictionary<Type, Type> BuildInjectionContracts(Assembly assemblyToSearchIn)
		{
			Guardian.IsNotNull(assemblyToSearchIn, nameof(assemblyToSearchIn));

			IEnumerable<Type> injectionContracts = FindInjectionContracts(assemblyToSearchIn);
			string nameSpace = assemblyToSearchIn.GetName().Name;

			return BuildInjectionContractsCore(nameSpace, injectionContracts);
		}

		/// <summary>
		/// Builds injection contracts.
		/// </summary>
		/// <param name="assemblyNamePrefix">The name prefix for injections contracts assembly.</param>
		/// <param name="injectionContracts">Injection contracts.</param>
		/// <returns>
		/// A dictionary of injection contracts and its implementations.
		/// </returns>
		public IDictionary<Type, Type> BuildInjectionContracts(string assemblyNamePrefix, IEnumerable<Type> injectionContracts)
		{
			Guardian.IsNotEmpty(assemblyNamePrefix, nameof(assemblyNamePrefix));
			Guardian.IsNotNull(injectionContracts, nameof(injectionContracts));

			CheckInjectionContractsForPurity(injectionContracts);

			return BuildInjectionContractsCore(assemblyNamePrefix, injectionContracts);
		}

		#endregion

		#region Private Implementation

		/// <summary>
		/// Builds injection contracts.
		/// </summary>
		/// <param name="assemblyNamePrefix">The name prefix for injections contracts assembly.</param>
		/// <param name="injectionContracts">Injection contracts.</param>
		/// <returns>
		/// A dictionary of injection contracts and its implementations.
		/// </returns>
		private IDictionary<Type, Type> BuildInjectionContractsCore(string assemblyNamePrefix, IEnumerable<Type> injectionContracts)
		{
			Dictionary<Type, Type> contracts = new Dictionary<Type, Type>();

			if (!injectionContracts.Any())
			{
				return contracts;
			}

			// All implemented contracts will be stored in an assembly with this prefix.
			// GUIDs for multiple invocations during program's lifetime. 
			string rootNameSpace = string.Format(
				"{0}.{1}_{2}",
				assemblyNamePrefix,
				"InjectionContracts",
				Guid.NewGuid().ToString("N"));

			AssemblyName assemblyName = new AssemblyName(rootNameSpace);
			// TODO: Investigate disadvantages of the Collect mode.
			AssemblyBuilderAccess assemblyBuilderAccess = AssemblyBuilderAccess.Run;

#if DEBUG
			// Saving assembly only for testing and debugging.
			assemblyBuilderAccess = AssemblyBuilderAccess.RunAndSave;
#endif

			AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, assemblyBuilderAccess);

			// I don't know, how to choose module names :(
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName + ".dll");

			// For every contract generate its implementation and add it to the dictionary.
			foreach (Type targetInterface in injectionContracts)
			{
				// First 'I' is not removed from implementation's name.
				// GUID is for same named contracts in different name spaces.
				string typeName = string.Format(
					"{0}.{1}_{2}",
					rootNameSpace,
					targetInterface.Name,
					Guid.NewGuid().ToString("N"));

				// A regular public sealed class.
				TypeBuilder typeBuilder = moduleBuilder.DefineType(
					typeName,
					TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout |
					TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);

				// Implements an injection contract.
				typeBuilder.AddInterfaceImplementation(targetInterface);

				List<PropertyInfo> injectionProperties = GetInjectionProperties(targetInterface);

				List<KeyValuePair<string, FieldInfo>> propertiesBackingFields = new List<KeyValuePair<string, FieldInfo>>(injectionProperties.Count);

				foreach (PropertyInfo injectionProperty in injectionProperties)
				{
					FieldInfo propertyBackingField = EmitGetOnlyProperty(typeBuilder, injectionProperty);

					propertiesBackingFields.Add(new KeyValuePair<string, FieldInfo>(injectionProperty.Name, propertyBackingField));
				}

				EmitConstructor(typeBuilder, propertiesBackingFields);

				Type targetImplementation = typeBuilder.CreateType();

				contracts.Add(targetInterface, targetImplementation);
			}

#if DEBUG
			assemblyBuilder.Save(assemblyName + ".dll");
#endif

			return contracts;
		}

		/// <summary>
		/// Finds the injection contracts in an assembly.
		/// </summary>
		/// <param name="assembly">The assembly to search in.</param>
		/// <returns>
		/// The list of injection contracts.
		/// </returns>
		private static IEnumerable<Type> FindInjectionContracts(Assembly assembly)
		{
			List<Type> injectionContracts = new List<Type>();

			foreach (Type currentType in assembly.GetTypes())
			{
				// We need all interfaces extending root Injection Contract and not
				// declared in an open or abstract class.
				if (currentType.IsInterface &&
					_rootContract.IsAssignableFrom(currentType) &&
					(currentType.DeclaringType == null || !currentType.DeclaringType.IsAbstract) &&
					currentType != _rootContract)
				{
					CheckInjectionContractForPurity(currentType);

					injectionContracts.Add(currentType);
				}
			}

			return injectionContracts;
		}

		/// <summary>
		/// Checks the injection contract for purity (no other interfaces in its inheritance chain).
		/// </summary>
		/// <param name="injectionContract">The injection contract to check.</param>
		/// <exception cref="InvalidOperationException">
		/// In case of invalid interface in the inheritance chain.
		/// </exception>
		private static void CheckInjectionContractForPurity(Type injectionContract)
		{
			Type[] baseInterfaces = injectionContract.GetInterfaces();

			// An injection contract must not contain any other interfaces as its parents, but other injection contracts.
			// TODO: Add checks, that there are should be no methods in a contract.
			if (baseInterfaces.Any(bi => !_rootContract.IsAssignableFrom(bi) && bi != _rootContract))
			{
				throw new InvalidOperationException(string.Format(
					"Interface <{0}> contains another interface in its hierarchy, which is not an Injection Contract descendant.",
					injectionContract.FullName));
			}
		}

		/// <summary>
		/// Checks the list of injection contracts for purity (no other interfaces in its inheritance chain).
		/// </summary>
		/// <param name="injectionContracts">The injection contracts to check.</param>
		/// <exception cref="InvalidOperationException">
		/// In case of invalid interface in the inheritance chain.
		/// </exception>
		private static void CheckInjectionContractsForPurity(IEnumerable<Type> injectionContracts)
		{
			foreach (Type injectionContract in injectionContracts)
			{
				CheckInjectionContractForPurity(injectionContract);
			}
		}

		/// <summary>
		/// Gets the injection properties from an interface and check for invalid ones.
		/// </summary>
		/// <param name="injectionContract">The injection contract.</param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">
		/// In case of non-reference return type of a property or property duplication in child interfaces.
		/// </exception>
		private static List<PropertyInfo> GetInjectionProperties(Type injectionContract)
		{
			List<PropertyInfo> properties = new List<PropertyInfo>();

			// Combine properties from all contracts in hierarchy.
			properties.AddRange(injectionContract.GetProperties());

			foreach (Type subInterface in injectionContract.GetInterfaces())
			{
				properties.AddRange(subInterface.GetProperties());
			}

			HashSet<string> propertyNames = new HashSet<string>();

			// Check every property for constraints:
			// 1. We inject only classes and interfaces
			// 2. Do not define same properties in multiple interfaces (will be removed in future)
			foreach (PropertyInfo property in properties)
			{
				if (property.PropertyType.IsValueType)
				{
					throw new InvalidOperationException(string.Format(
						"Injection property with name '{0}' in <{1}> must be of a reference type.",
						property.Name,
						property.DeclaringType.Name));
				}

				if (propertyNames.Contains(property.Name))
				{
					// TODO: Add an option to allow duplicate properties.
					throw new InvalidOperationException(string.Format(
						"Duplicate injection property with name '{0}' in <{1}> for <{2}>.",
						property.Name,
						property.DeclaringType.Name,
						injectionContract.FullName));
				}

				propertyNames.Add(property.Name);
			}

			return properties;
		}

		/// <summary>
		/// Emits a get-only property and adds its backing field to the list.
		/// </summary>
		/// <param name="typeBuilder">The type builder.</param>
		/// <param name="property">The property to add.</param>
		/// <param name="propertiesBackingFields">The properties backing fields add current to.</param>
		/// <returns>
		/// The property builder.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// In case if property declaration doesn't contain getter or does contain a setter.
		/// </exception>
		private static FieldInfo EmitGetOnlyProperty(
			TypeBuilder typeBuilder,
			PropertyInfo property)
		{
			// Private field has the name of the property + '_' symbol at the front (no first letter case lowering).
			FieldInfo backingField = typeBuilder.DefineField(
				"_" + property.Name,
				property.PropertyType,
				FieldAttributes.Private | FieldAttributes.InitOnly);

			MethodInfo definedGetter = property.GetGetMethod();

			// Contract is required to have a getter only property
			if (definedGetter == null)
			{
				throw new InvalidOperationException(string.Format(
					"No property getter defined for {0} in <{1}>.",
					property.Name,
					property.DeclaringType.FullName));
			}

			// Same property methods name generation as in current C# compiler.
			MethodBuilder getterBuilder = typeBuilder.DefineMethod(
				"get_" + property.Name,
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
				MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
				property.PropertyType,
				Type.EmptyTypes);

			ILGenerator getterBody = getterBuilder.GetILGenerator();

			// Just return the backing field.
			getterBody.Emit(OpCodes.Ldarg_0);
			getterBody.Emit(OpCodes.Ldfld, backingField);
			getterBody.Emit(OpCodes.Ret);

			MethodInfo definedSetter = property.GetSetMethod();

			// Contract is required to have a getter only property.
			// All dependencies are injected in constructor and there are should be no way to modify this.
			if (definedSetter != null)
			{
				throw new InvalidOperationException(string.Format(
					"Excess property setter defined for {0} in <{1}>.",
					property.Name,
					property.DeclaringType.FullName));
			}

			PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(
				property.Name,
				PropertyAttributes.None,
				property.PropertyType,
				null);

			propertyBuilder.SetGetMethod(getterBuilder);

			return backingField;
		}

		/// <summary>
		/// Emits the constructor with arguments null checks.
		/// </summary>
		/// <param name="typeBuilder">The type builder.</param>
		/// <param name="propertiesBackingFields">The properties backing fields.</param>
		private static void EmitConstructor(TypeBuilder typeBuilder, List<KeyValuePair<string, FieldInfo>> propertiesBackingFields)
		{
			// Public constructor with a parameter for each property.
			ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
				CallingConventions.Standard,
				propertiesBackingFields.Select(bfp => bfp.Value.FieldType).ToArray());

			// Own parameters start with 1 index.
			short parameterIndex = 1;

			foreach (KeyValuePair<string, FieldInfo> backingFieldPair in propertiesBackingFields)
			{
				string propertyName = backingFieldPair.Key;

				ParameterBuilder parameterBuilder = constructorBuilder.DefineParameter(
					parameterIndex,
					ParameterAttributes.None,
					propertyName);

				parameterIndex += 1;
			}

			// For the first null value parameter there will be an ArgumentNullException with this parameter's name.
			Type exceptionType = typeof(ArgumentNullException);
			ConstructorInfo exceptionConstructor = exceptionType.GetConstructor(new Type[] { typeof(string) });

			ConstructorInfo baseConstructor = typeof(object).GetConstructor(Type.EmptyTypes);

			ILGenerator constructorBody = constructorBuilder.GetILGenerator();

			// Call base class's constructor.
			constructorBody.Emit(OpCodes.Ldarg_0);
			constructorBody.Emit(OpCodes.Call, baseConstructor);

			parameterIndex = 1;

			foreach (KeyValuePair<string, FieldInfo> backingFieldPair in propertiesBackingFields)
			{
				string propertyName = backingFieldPair.Key;
				FieldInfo backingField = backingFieldPair.Value;

				Label afterIfLabel = constructorBody.DefineLabel();

				// Check a parameter for null.
				constructorBody.Emit(OpCodes.Ldarg, parameterIndex);
				constructorBody.Emit(OpCodes.Brtrue_S, afterIfLabel);
				constructorBody.Emit(OpCodes.Ldstr, propertyName);
				constructorBody.Emit(OpCodes.Newobj, exceptionConstructor);
				constructorBody.Emit(OpCodes.Throw);
				constructorBody.MarkLabel(afterIfLabel);

				// Load a parameter into a backing field.
				constructorBody.Emit(OpCodes.Ldarg_0);
				constructorBody.Emit(OpCodes.Ldarg, parameterIndex);
				constructorBody.Emit(OpCodes.Stfld, backingField);

				parameterIndex += 1;
			}

			constructorBody.Emit(OpCodes.Ret);
		}

		#endregion
	}
}
