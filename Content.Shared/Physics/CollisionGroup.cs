using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;

namespace Content.Shared.Physics;

/// <summary>
///     Defined collision groups for the physics system.
///     Mask is what it collides with when moving. Layer is what CollisionGroup it is part of.
/// </summary>
[Flags, PublicAPI]
[FlagsFor(typeof(CollisionLayer)), FlagsFor(typeof(CollisionMask))]
public enum CollisionGroup
{
    None               = 0,
    Opaque             = 1 << 0, // 1 Blocks light, can be hit by lasers
    Impassable         = 1 << 1, // 2 Walls, objects impassable by any means
    MidImpassable      = 1 << 2, // 4 Mobs, players, crabs, etc
    HighImpassable     = 1 << 3, // 8 Things on top of tables and things that block tall/large mobs.
    LowImpassable      = 1 << 4, // 16 For things that can fit under a table or squeeze under an airlock
    GhostImpassable    = 1 << 5, // 32 Things impassible by ghosts/observers, ie blessed tiles or forcefields
    BulletImpassable   = 1 << 6, // 64 Can be hit by bullets
    InteractImpassable = 1 << 7, // 128 Blocks interaction/InRangeUnobstructed
    // Y dis door passable when all the others impassable / collision.
    DoorPassable       = 1 << 8, // 256 Allows door to close over top, Like blast doors over conveyors for disposals rooms/cargo.
    BlobImpassable     = 1 << 9, // 512 Blob Tiles // backmen: blob

    MapGrid = MapGridHelpers.CollisionGroup, // Map grids, like shuttles. This is the actual grid itself, not the walls or other entities connected to the grid.

    // 32 possible groups
    // Why dis exist
    AllMask = -1,

    SingularityLayer = Opaque | Impassable | MidImpassable | HighImpassable | LowImpassable | BulletImpassable | InteractImpassable | DoorPassable,

    // Humanoids, etc.
    MobMask = Impassable | HighImpassable | MidImpassable | LowImpassable | BlobImpassable, // backmen: blob
    MobLayer = Opaque | BulletImpassable,
    // Mice, drones
    SmallMobMask = Impassable | LowImpassable | BlobImpassable, // backmen: blob
    SmallMobLayer = Opaque | BulletImpassable,
    // Birds/other small flyers
    FlyingMobMask = Impassable | HighImpassable | BlobImpassable, // backmen: blob
    FlyingMobLayer = Opaque | BulletImpassable,

    // start-backmen: blob
    BlobMobMask = Impassable | HighImpassable | MidImpassable | LowImpassable,
    BlobMobLayer = Opaque | BulletImpassable,

    FlyingBlobMobMask = Impassable | HighImpassable,
    FlyingBlobMobLayer = Opaque | BulletImpassable,
    // end-backmen: blob

    // Mechs
    LargeMobMask = Impassable | HighImpassable | MidImpassable | LowImpassable | BlobImpassable, // backmen: blob
    LargeMobLayer = Opaque | HighImpassable | MidImpassable | LowImpassable | BulletImpassable,

    // Machines, computers
    MachineMask = Impassable | MidImpassable | LowImpassable | BlobImpassable, // backmen: blob
    MachineLayer = Opaque | MidImpassable | LowImpassable | BulletImpassable,
    ConveyorMask = Impassable | MidImpassable | LowImpassable | DoorPassable,

    // Crates
    CrateMask = Impassable | HighImpassable | LowImpassable,

    // Tables that SmallMobs can go under
    TableMask = Impassable | MidImpassable | BlobImpassable, // backmen: blob
    TableLayer = MidImpassable,

    // Tabletop machines, windoors, firelocks
    TabletopMachineMask = Impassable | HighImpassable | BlobImpassable, // backmen: blob
    // Tabletop machines
    TabletopMachineLayer = Opaque | BulletImpassable,

    // Airlocks, windoors, firelocks
    GlassAirlockLayer = HighImpassable | MidImpassable | BulletImpassable | InteractImpassable,
    AirlockLayer = Opaque | GlassAirlockLayer,

    // Airlock assembly
    HumanoidBlockLayer = HighImpassable | MidImpassable,

    // Soap, spills
    SlipLayer = MidImpassable | LowImpassable,
    ItemMask = Impassable | HighImpassable | BlobImpassable, // backmen: blob
    ThrownItem = Impassable | HighImpassable | BulletImpassable | BlobImpassable, // backmen: blob
    WallLayer = Opaque | Impassable | HighImpassable | MidImpassable | LowImpassable | BulletImpassable | InteractImpassable,
    BlobTileLayer = Opaque | BlobImpassable | BulletImpassable, // backmen: blob
    GlassLayer = Impassable | HighImpassable | MidImpassable | LowImpassable | BulletImpassable | InteractImpassable,
    HalfWallLayer = MidImpassable | LowImpassable,

    // Statue, monument, airlock, window
    FullTileMask = Impassable | HighImpassable | MidImpassable | LowImpassable | InteractImpassable,
    // FlyingMob can go past
    FullTileLayer = Opaque | HighImpassable | MidImpassable | LowImpassable | BulletImpassable | InteractImpassable,

    SubfloorMask = Impassable | LowImpassable
}
