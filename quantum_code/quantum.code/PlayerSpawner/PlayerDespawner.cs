using Quantum.Movement;

namespace Quantum.PlayerSpawner
{
    public unsafe class PlayerDespawner : SystemMainThreadFilter<MovementSystem.Filter>, ISignalOnPlayerDisconnected
    {
        public override void Update(Frame frame, ref MovementSystem.Filter filter) { }
        
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
