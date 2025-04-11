using XXXBlazor.Client.Models;

namespace XXXBlazor.Client.Services
{
    public class Hdf5StateService
    {
        public event Action<Hdf5TreeNode, List<string>> TreeNodeChanged;
        
        public void NotifyTreeNodeChanged(Hdf5TreeNode node, List<string> newLegendData)
        {
            TreeNodeChanged?.Invoke(node, newLegendData);
        }

    }
}
