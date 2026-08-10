using System.Runtime.CompilerServices;

// Make internals visible to the Editor assembly
[assembly: InternalsVisibleTo("EventBus.Editor")]

// Make internals visible to the test assemblies
[assembly: InternalsVisibleTo("EventBus.Tests")]
