using Photon.Deterministic;
using Quantum.Player;

namespace Quantum.PlayerSpawner
{
    public unsafe class SpawnSystem : SystemMainThreadFilter<PlayerSystem.Filter>, ISignalOnPlayerDataSet
    {
        public override void Update(Frame frame, ref PlayerSystem.Filter filter) { }
        
        public void OnPlayerDataSet(Frame f, PlayerRef player)
        {
            var data = f.GetPlayerData(player);
            if (data == null)
                return;
            
            var prototypeEntity = f.FindAsset<EntityPrototype>(data.CharacterEntityPrototype.Id);
            var createdEntity = f.Create(prototypeEntity);
            
            if (f.Unsafe.TryGetPointer<PlayerLink>(createdEntity, out var playerLink))
            {
                playerLink->Player = player;
            }
            
            if (f.Unsafe.TryGetPointer<Transform3D>(createdEntity, out var transform))
            {
                transform->Position = GetSpawnPosition(player);
            }
        }

        private FPVector3 GetSpawnPosition(int playerNumber)
        {
            return new FPVector3(playerNumber * 2, 0, 0);
        }
    }
}
