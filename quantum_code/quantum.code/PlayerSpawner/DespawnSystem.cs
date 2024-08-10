using Quantum.Player;

namespace Quantum.PlayerSpawner
{
    public unsafe class DespawnSystem : SystemMainThreadFilter<PlayerSystem.Filter>, ISignalOnPlayerDisconnected
    {
        public override void Update(Frame frame, ref PlayerSystem.Filter filter) { }
        
        public void OnPlayerDisconnected(Frame f, PlayerRef player)
        {
            foreach (var playerLink in f.GetComponentIterator<PlayerLink>())
            {
                if (playerLink.Component.Player != player)
                    continue;
                
                f.Destroy(playerLink.Entity);
            }
        }
    }
}
