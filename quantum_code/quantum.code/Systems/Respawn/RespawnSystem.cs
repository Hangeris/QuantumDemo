using Quantum.Systems.Player;

namespace Quantum.Systems.Respawn
{
    public unsafe class RespawnSystem : SystemMainThreadFilter<PlayerSystem.Filter>
    {
        public const int RespawnHeight = -10;
        
        public override void Update(Frame frame, ref PlayerSystem.Filter filter)
        {
            if (filter.Transform->Position.Y < RespawnHeight)
            {
                filter.Transform->Position = PlayerSpawner.SpawnSystem.GetSpawnPosition(filter.Link->Player);
            }
        }
    }
}