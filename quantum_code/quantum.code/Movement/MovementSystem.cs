using Photon.Deterministic;

namespace Quantum.Movement
{
    public unsafe class MovementSystem : SystemMainThreadFilter<MovementSystem.Filter>
    {
        public const int LerpRotationSpeed = 15;
        
        public struct Filter
        {
            public EntityRef Entity;
            public CharacterController3D* CharacterController;
            public Transform3D* Transform;
            public PlayerLink* Link;
        }
        
        public override void Update(Frame frame, ref Filter filter)
        {
            var input = frame.GetPlayerInput(filter.Link->Player);
            var inputVector = new FPVector2((FP)input->X / 10, (FP)input->Y / 10);

            if (inputVector.SqrMagnitude > 1)
                inputVector = inputVector.Normalized;
            
            filter.CharacterController->Move(frame, filter.Entity, inputVector.XOY);
            
            if (input->Jump.WasPressed)
                filter.CharacterController->Jump(frame);

            if (inputVector.SqrMagnitude != default)
                filter.Transform->Rotation = FPQuaternion.Lerp(filter.Transform->Rotation, FPQuaternion.LookRotation(inputVector.XOY), frame.DeltaTime * LerpRotationSpeed);
        }
    }
}
