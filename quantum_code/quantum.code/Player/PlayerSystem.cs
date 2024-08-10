namespace Quantum.Player;

public unsafe class PlayerSystem : SystemMainThreadFilter<PlayerSystem.Filter>
{
    public struct Filter
    {
        public EntityRef Entity;
        public CharacterController3D* CharacterController;
        public Transform3D* Transform;
        public PlayerLink* Link;
    }
    
    public override void Update(Frame frame, ref Filter filter) { }
}