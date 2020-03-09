// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace TestKitchen
{
	public sealed class TestFixture : IServiceCollection, IServiceProvider, IDisposable
	{
		private readonly IServiceCollection _perTestScope = new ServiceCollection();
		private readonly IServiceCollection _services;
		private IServiceProvider _perTestProvider;
		private IServiceProvider _serviceProvider;
		private IServiceScope _serviceScope;

		public TestFixture(IServiceCollection services) => _services = services;

		public void Dispose() { }

		public object GetService(Type serviceType)
		{
			if (serviceType == typeof(TestFixture) ||
			    serviceType == typeof(IServiceCollection) ||
			    serviceType == typeof(IServiceProvider))
				return this;

			var service = _perTestProvider?.GetService(serviceType);
			if (service != null)
				return service;

			service = _serviceProvider?.GetService(serviceType);
			return service;
		}

		public TestFixture AddPerTestSingleton<T>(Func<IServiceProvider, T> implementationFactory) where T : class
		{
			_perTestScope.AddSingleton(implementationFactory);
			return this;
		}

		internal void Begin()
		{
			_serviceScope = _services.BuildServiceProvider().CreateScope();
			_serviceProvider = _serviceScope.ServiceProvider;
		}

		internal void BeginTest()
		{
			_perTestProvider = _perTestScope.BuildServiceProvider();
		}

		internal void EndTest()
		{
			_perTestProvider = null;
		}

		internal void End()
		{
			_serviceScope?.Dispose();
			_serviceProvider = null;
		}

		#region IServiceCollection

		public IEnumerator<ServiceDescriptor> GetEnumerator()
		{
			return _services.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable) _services).GetEnumerator();
		}

		public void Add(ServiceDescriptor item)
		{
			_services.Add(item);
		}

		public void Clear()
		{
			_services.Clear();
		}

		public bool Contains(ServiceDescriptor item)
		{
			return _services.Contains(item);
		}

		public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
		{
			_services.CopyTo(array, arrayIndex);
		}

		public bool Remove(ServiceDescriptor item)
		{
			return _services.Remove(item);
		}

		public int Count => _services.Count;

		public bool IsReadOnly => _services.IsReadOnly;

		public int IndexOf(ServiceDescriptor item)
		{
			return _services.IndexOf(item);
		}

		public void Insert(int index, ServiceDescriptor item)
		{
			_services.Insert(index, item);
		}

		public void RemoveAt(int index)
		{
			_services.RemoveAt(index);
		}

		public ServiceDescriptor this[int index]
		{
			get => _services[index];
			set => _services[index] = value;
		}

		#endregion
	}
}