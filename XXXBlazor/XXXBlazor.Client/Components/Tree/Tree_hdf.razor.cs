
using Microsoft.AspNetCore.Components;
using XXXBlazor.Client.Models;
using DevExpress.Blazor;

namespace XXXBlazor.Client.Pages
{
    public class Hdf5TreeBase : ComponentBase
    {
        [Parameter]
        public Hdf5TreeNode? selectedFile { get; set; }

        [Parameter]
        public EventCallback<Hdf5TreeNode> OnNodeClicked { get; set; }

        protected Hdf5TreeNode? currentNode;

        // 트리뷰에 바인딩할 데이터 소스
        protected List<Hdf5TreeNode> nodesList = new List<Hdf5TreeNode>();

        // 노드 경로를 키로 사용하는 딕셔너리
        protected Dictionary<string, Hdf5TreeNode> nodePathMap = new Dictionary<string, Hdf5TreeNode>();

        protected bool ShowAttribute = false;

        protected override void OnParametersSet()
        {
            if (selectedFile != null)
            {

                if(nodesList.Count > 0)
                {
                    nodesList.Clear();
                }

                nodesList.Add(PrepareNodeForTreeView(selectedFile));

                if (currentNode == null)
                {
                    currentNode = selectedFile;
                }

                if(nodePathMap.Count > 0)
                {
                    nodePathMap.Clear();
                }

                BuildNodePathMap(selectedFile);

            }
            else
            {
                currentNode = new Hdf5TreeNode();
            }
        }

        protected Hdf5TreeNode PrepareNodeForTreeView(Hdf5TreeNode node)
        {
            // DisplayName 속성을 동적으로 추가
            if (node.NodeType == Hdf5NodeType.Dataset && node.Dimensions != null)
            {
                node.GetType().GetProperty("DisplayName")?.SetValue(node, $"{node.Name} [{string.Join(", ", node.Dimensions)}]");
            }
            else if (node.NodeType == Hdf5NodeType.Group)
            {
                node.GetType().GetProperty("DisplayName")?.SetValue(node, $"{node.Name} (그룹)");
            }
            else
            {
                node.GetType().GetProperty("DisplayName")?.SetValue(node, node.Name);
            }

            return node;
        }

        // 노드 경로 맵 구축
        protected void BuildNodePathMap(Hdf5TreeNode node)
        {
            if (node == null) return;

            if (!string.IsNullOrEmpty(node.Path))
            {
                nodePathMap[node.Path] = node;
            }

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    // 트리뷰에 표시하기 위해 노드 준비
                    PrepareNodeForTreeView(child);
                    // 재귀적으로 맵 구축
                    BuildNodePathMap(child);
                }
            }
        }

        protected async Task OnNodeSelected(TreeViewNodeEventArgs args)
        {
            try
            {
                if (args.NodeInfo == null)
                {
                    return;
                }

                string? nodePath = args.NodeInfo.Name;

                if (string.IsNullOrEmpty(nodePath))
                {
                    return;
                }

                if (nodePathMap.TryGetValue(nodePath, out var node))
                {
                    currentNode = node;
                    await OnNodeClicked.InvokeAsync(currentNode);
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"노드 선택 처리 중 오류: {ex.Message}");
                Console.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
        }

        protected void CheckedChanged(bool bMode)
        {
            ShowAttribute = bMode;
        }
    }
}
