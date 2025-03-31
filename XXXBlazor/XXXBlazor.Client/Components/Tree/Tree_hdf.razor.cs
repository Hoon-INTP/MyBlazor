
using Microsoft.AspNetCore.Components;
using XXXBlazor.Client.Models;
using DevExpress.Blazor;

namespace XXXBlazor.Client.Pages
{
    public class Hdf5TreeBase : ComponentBase
    {
        [Parameter]
        public Hdf5TreeNode? selectedNode { get; set; }

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
            if (selectedNode != null)
            {

                if(nodesList.Count > 0)
                {
                    nodesList.Clear();
                }

                nodesList.Add(PrepareNodeForTreeView(selectedNode));

                if (currentNode == null)
                {
                    currentNode = selectedNode;
                }

                if(nodePathMap.Count > 0)
                {
                    nodePathMap.Clear();
                }

                BuildNodePathMap(selectedNode);

            }
            else
            {
                currentNode = null;
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

        protected string GetArrayPreview(object array)
        {
            var arrayObj = array as Array;
            if (arrayObj == null) return "잘못된 배열";

            // 작은 배열인 경우 전체 표시
            if (arrayObj.Length <= 10)
            {
                return FormatArray(arrayObj);
            }

            // 큰 배열은 처음 5개와 마지막 5개만 표시
            var preview = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                preview.Add(FormatValue(arrayObj.GetValue(i)));
            }
            preview.Add("...");
            for (int i = Math.Max(5, arrayObj.Length - 5); i < arrayObj.Length; i++)
            {
                preview.Add(FormatValue(arrayObj.GetValue(i)));
            }

            return $"[{string.Join(", ", preview)}]";
        }

        protected string FormatArray(Array array)
        {
            var values = new List<string>();
            for (int i = 0; i < array.Length; i++)
            {
                values.Add(FormatValue(array.GetValue(i)));
            }
            return $"[{string.Join(", ", values)}]";
        }

        protected string FormatValue(object? value)
        {
            if (value == null) return "null";
            if (value is float f) return f.ToString("0.###");
            if (value is double d) return d.ToString("0.###");
            return value.ToString() ?? "";
        }

        protected string FormatAttributeValue(object? value)
        {
            if (value == null) return "null";

            // 에러 메시지인 경우 구분
            if (value is string str && str.StartsWith("Error reading attribute"))
            {
                return str;
            }

            // 숫자 포맷팅
            if (value is float f) return f.ToString("0.###");
            if (value is double d) return d.ToString("0.###");

            // 배열 포맷팅
            if (value.GetType().IsArray)
            {
                return GetArrayPreview(value);
            }

            return value.ToString() ?? "";
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
