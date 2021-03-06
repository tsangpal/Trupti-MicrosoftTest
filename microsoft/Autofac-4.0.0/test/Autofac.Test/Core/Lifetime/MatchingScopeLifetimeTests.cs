using System;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Xunit;

namespace Autofac.Test.Core.Lifetime
{
    public class MatchingScopeLifetimeTests
    {
        [Fact]
        public void WhenNoMatchingScopeIsPresent_TheExceptionMessageIncludesTheTag()
        {
            var container = new Container();
            const string tag = "abcdefg";
            var msl = new MatchingScopeLifetime(tag);
            var rootScope = (ISharingLifetimeScope)container.Resolve<ILifetimeScope>();

            var ex = Assert.Throws<DependencyResolutionException>(() => msl.FindScope(rootScope));
            Assert.True(ex.Message.Contains(tag));
        }

        [Fact]
        public void WhenNoMatchingScopeIsPresent_TheExceptionMessageIncludesTheTags()
        {
            var container = new Container();
            const string tag1 = "abc";
            const string tag2 = "def";
            var msl = new MatchingScopeLifetime(tag1, tag2);
            var rootScope = (ISharingLifetimeScope)container.Resolve<ILifetimeScope>();

            var ex = Assert.Throws<DependencyResolutionException>(() => msl.FindScope(rootScope));
            Assert.True(ex.Message.Contains(string.Format("{0}, {1}", tag1, tag2)));
        }

        [Fact]
        public void WhenTagsToMatchIsNull_ExceptionThrown()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new MatchingScopeLifetime(null));

            Assert.Equal("lifetimeScopeTagsToMatch", exception.ParamName);
        }

        [Fact]
        public void MatchesAgainstSingleTaggedScope()
        {
            const string tag = "Tag";
            var msl = new MatchingScopeLifetime(tag);
            var container = new Container();
            var lifetimeScope = (ISharingLifetimeScope)container.BeginLifetimeScope(tag);

            Assert.Equal(lifetimeScope, msl.FindScope(lifetimeScope));
        }

        [Fact]
        public void MatchesAgainstMultipleTaggedScopes()
        {
            const string tag1 = "Tag1";
            const string tag2 = "Tag2";

            var msl = new MatchingScopeLifetime(tag1, tag2);
            var container = new Container();

            var tag1Scope = (ISharingLifetimeScope)container.BeginLifetimeScope(tag1);
            Assert.Equal(tag1Scope, msl.FindScope(tag1Scope));

            var tag2Scope = (ISharingLifetimeScope)container.BeginLifetimeScope(tag2);
            Assert.Equal(tag2Scope, msl.FindScope(tag2Scope));
        }
    }
}
