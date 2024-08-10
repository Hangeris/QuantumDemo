using Photon.Deterministic;

namespace Quantum
{
    partial class RuntimePlayer
    {
        public AssetRefEntityPrototype CharacterEntityPrototype;
        
        partial void SerializeUserData(BitStream stream)
        {
            stream.Serialize(ref CharacterEntityPrototype.Id);
        }
    }
}