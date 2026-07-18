using System.Runtime.CompilerServices;

// Exposes internal helpers to the test assemblies so manager bookkeeping and the
// view/transition seams can be exercised directly with fakes.
[assembly: InternalsVisibleTo("KidzDev.Unity.Toast.Tests.Editor")]
[assembly: InternalsVisibleTo("KidzDev.Unity.Toast.Tests.Runtime")]
