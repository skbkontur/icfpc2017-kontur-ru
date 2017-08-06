using System.Collections.Generic;
using lib.GraphImpl;
using lib.StateImpl;

namespace lib.Strategies.EdgeWeighting
{
    public class ConnectedComponentsService : IService
    {
        private Graph Graph { get; set; }
        private IDictionary<int, List<ConnectedComponent>> Cache { get; } = new Dictionary<int, List<ConnectedComponent>>();

        public void Setup(State state, IServices services)
        {
            services.Setup<GraphService>(state);
        }

        public void ApplyNextState(State state, IServices services)
        {
            Graph = services.Get<GraphService>(state).Graph;
        }

        public List<ConnectedComponent> For(int punterId)
        {
            return Cache.GetOrCreate(punterId, key => ConnectedComponent.GetComponents(Graph, punterId));
        }
    }
}