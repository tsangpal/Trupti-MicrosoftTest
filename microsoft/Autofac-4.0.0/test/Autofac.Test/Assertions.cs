using System;
using System.Collections.Generic;
using System.Linq;
using Autofac.Core;
using Xunit;

namespace Autofac.Test
{
    internal static class Assertions
    {
        public static void AssertRegistered<TService>(this IComponentContext context)
        {
            Assert.True(context.IsRegistered<TService>());
        }

        public static void AssertNotRegistered<TService>(this IComponentContext context)
        {
            Assert.False(context.IsRegistered<TService>());
        }

        public static void AssertRegistered<TService>(this IComponentContext context, string service)
        {
            Assert.True(context.IsRegisteredWithName<TService>(service));
        }

        public static void AssertNotRegistered<TService>(this IComponentContext context, string service)
        {
            Assert.False(context.IsRegisteredWithName<TService>(service));
        }

        public static void AssertSharing<TComponent>(this IComponentContext context, InstanceSharing sharing)
        {
            var cr = context.RegistrationFor<TComponent>();
            Assert.Equal(sharing, cr.Sharing);
        }

        public static void AssertLifetime<TComponent, TLifetime>(this IComponentContext context)
        {
            var cr = context.RegistrationFor<TComponent>();
            Assert.IsType<TLifetime>(cr.Lifetime);
        }

        public static void AssertOwnership<TComponent>(this IComponentContext context, InstanceOwnership ownership)
        {
            var cr = context.RegistrationFor<TComponent>();
            Assert.Equal(ownership, cr.Ownership);
        }

        public static IComponentRegistration RegistrationFor<TComponent>(this IComponentContext context)
        {
            IComponentRegistration r;
            Assert.True(context.ComponentRegistry.TryGetRegistration(new TypedService(typeof(TComponent)), out r));
            return r;
        }

        /// <summary>
        /// Looks at all registrations for <typeparamref name="TService"/> and validates that <typeparamref name="TFirstComponent"/> is not overridden by <typeparamref name="TLastComponent"/>.
        /// </summary>
        public static void AssertComponentRegistrationOrder<TService, TFirstComponent, TLastComponent>(this IComponentContext context)
        {
            var forService = new TypedService(typeof(TService));
            var firstComponent = typeof(TFirstComponent);
            var lastComponent = typeof(TLastComponent);

            var registrations = context.ComponentRegistry.RegistrationsFor(forService);

            var types = registrations.LookForComponents(new[] { firstComponent, lastComponent }).ToArray();
            Assert.True(types.Length == 2);

            var foundFirst = types[0] == firstComponent;
            var foundLast = types[1] == lastComponent;

            Assert.True(foundFirst);
            Assert.True(foundLast);
        }

        private static IEnumerable<Type> LookForComponents(this IEnumerable<IComponentRegistration> registrations, IEnumerable<Type> types)
        {
            return registrations
                .Select(registration => types.FirstOrDefault(type => registration.Activator.LimitType == type))
                .Where(foundType => foundType != null);
        }
    }
}
