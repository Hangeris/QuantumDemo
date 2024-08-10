namespace Quantum
{
    public static class SystemSetup
    {
        public static SystemBase[] CreateSystems(RuntimeConfig gameConfig, SimulationConfig simulationConfig)
        {
            return
            [
                // pre-defined core systems
                new Core.CullingSystem2D(),
                new Core.CullingSystem3D(),

                new Core.PhysicsSystem2D(),
                new Core.PhysicsSystem3D(),

                Core.DebugCommand.CreateSystem(),

                new Core.NavigationSystem(),
                new Core.EntityPrototypeSystem(),
                new Core.PlayerConnectedSystem(),

                // user systems go here 
                new Systems.Movement.MovementSystem(),
                new Systems.PlayerSpawner.SpawnSystem(),
                new Systems.PlayerSpawner.DespawnSystem()
            ];
        }
    }
}