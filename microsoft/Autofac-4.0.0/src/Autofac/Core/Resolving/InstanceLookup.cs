// This software is part of the Autofac IoC container
// Copyright © 2011 Autofac Contributors
// http://autofac.org
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Autofac.Core.Resolving
{
    // Is a component context that pins resolution to a point in the context hierarchy
    [SuppressMessage("Microsoft.ApiDesignGuidelines", "CA2213", Justification = "The instance lookup activation scope gets disposed of by the creator of the scope.")]
    internal class InstanceLookup : IComponentContext, IInstanceLookup
    {
        private readonly IResolveOperation _context;
        private readonly ISharingLifetimeScope _activationScope;
        private object _newInstance;
        private bool _executed;

        public InstanceLookup(
            IComponentRegistration registration,
            IResolveOperation context,
            ISharingLifetimeScope mostNestedVisibleScope,
            IEnumerable<Parameter> parameters)
        {
            Parameters = parameters;
            ComponentRegistration = registration;
            _context = context;

            try
            {
                _activationScope = ComponentRegistration.Lifetime.FindScope(mostNestedVisibleScope);
            }
            catch (DependencyResolutionException ex)
            {
                var services = new StringBuilder();
                foreach (var s in registration.Services)
                {
                    services.Append("- ");
                    services.AppendLine(s.Description);
                }

                var message = String.Format(CultureInfo.CurrentCulture, ComponentActivationResources.UnableToLocateLifetimeScope, registration.Activator.LimitType, services);
                throw new DependencyResolutionException(message, ex);
            }
        }

        public object Execute()
        {
            if (_executed)
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, ComponentActivationResources.ActivationAlreadyExecuted, this.ComponentRegistration));

            _executed = true;

            object instance = ComponentRegistration.Sharing == InstanceSharing.None
                ? Activate(Parameters)
                : _activationScope.GetOrCreateAndShare(ComponentRegistration.Id, () => Activate(Parameters));

            var handler = InstanceLookupEnding;
            handler?.Invoke(this, new InstanceLookupEndingEventArgs(this, NewInstanceActivated));

            return instance;
        }

        private bool NewInstanceActivated => _newInstance != null;

        private object Activate(IEnumerable<Parameter> parameters)
        {
            ComponentRegistration.RaisePreparing(this, ref parameters);

            try
            {
                _newInstance = ComponentRegistration.Activator.ActivateInstance(this, parameters);
            }
            catch (Exception ex)
            {
                throw new DependencyResolutionException(String.Format(CultureInfo.CurrentCulture, ComponentActivationResources.ErrorDuringActivation, this.ComponentRegistration), ex);
            }

            if (ComponentRegistration.Ownership == InstanceOwnership.OwnedByLifetimeScope)
            {
                // The fact this adds instances for disposal agnostic of the activator is
                // important. The ProvidedInstanceActivator will NOT dispose of the provided
                // instance once the instance has been activated - assuming that it will be
                // done during the lifetime scope's Disposer executing.
                var instanceAsDisposable = _newInstance as IDisposable;
                if (instanceAsDisposable != null)
                    _activationScope.Disposer.AddInstanceForDisposal(instanceAsDisposable);
            }

            ComponentRegistration.RaiseActivating(this, parameters, ref _newInstance);

            return _newInstance;
        }

        public void Complete()
        {
            if (!NewInstanceActivated) return;

            var beginningHandler = CompletionBeginning;
            beginningHandler?.Invoke(this, new InstanceLookupCompletionBeginningEventArgs(this));

            ComponentRegistration.RaiseActivated(this, Parameters, _newInstance);

            var endingHandler = CompletionEnding;
            endingHandler?.Invoke(this, new InstanceLookupCompletionEndingEventArgs(this));
        }

        public IComponentRegistry ComponentRegistry => _activationScope.ComponentRegistry;

        public object ResolveComponent(IComponentRegistration registration, IEnumerable<Parameter> parameters)
        {
            return _context.GetOrCreateInstance(_activationScope, registration, parameters);
        }

        public IComponentRegistration ComponentRegistration { get; }

        public ILifetimeScope ActivationScope => _activationScope;

        public IEnumerable<Parameter> Parameters { get; }

        public event EventHandler<InstanceLookupEndingEventArgs> InstanceLookupEnding;

        public event EventHandler<InstanceLookupCompletionBeginningEventArgs> CompletionBeginning;

        public event EventHandler<InstanceLookupCompletionEndingEventArgs> CompletionEnding;
    }
}
