using System.Runtime.CompilerServices;

// Grants the Blip EditMode test assembly access to internal members of TerritoryDeveloper.Game.
// Required for BlipBaker internals (DebugTailKey + TryEvictHead) exercised in BlipBakerCacheTests.
[assembly: InternalsVisibleTo("Blip.Tests.EditMode")]

// Grants the Blip PlayMode test assembly access to internal members (BlipCatalog.MixerRouter +
// BlipCatalog.PatchHash) exercised in Play_AllMvpIds_ResolvesAndRoutes.
[assembly: InternalsVisibleTo("Blip.Tests.PlayMode")]
