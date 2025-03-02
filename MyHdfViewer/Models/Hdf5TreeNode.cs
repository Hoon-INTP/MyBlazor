
using PureHDF;

namespace MyHdfViewer.Models
{
    /// <summary>
    /// Enum for HDF5 node type
    /// </summary>
    public enum Hdf5NodeType
    {
        NULL_NODE = -1,
        Group,
        Dataset,
        Reserved01,
        Reserved02,
        Reserved03,
        Reserved04,
        Reserved05,
        Other,
    }

    // 파일 외적 메타데이터와 최상위 내부 구조를 포함하는 클래스
    public class Hdf5FileModel
    {
        /// <summary>
        /// MemoryStream for File
        /// </summary>
        //public required MemoryStream Memorystream { get; set; }

        /// <summary>
        /// Name of the HDF5 file
        /// </summary>
        public required string FileName { get; set; }

        /// <summary>
        /// Root group of the HDF5 file (the top-level group)
        /// </summary>
        public required Hdf5Group RootGroup { get; set; }

        /// <summary>
        /// Test Method : Prtint All Nodes in HDF5 File
        /// </summary>
        /// <param name="node">Hdf5Node</param>
        /// <param name="indent">Indentation</param>
        /// <returns>String</returns>
        public string PrintAllNodes(Hdf5Node node, string indent = "", int depth = 0)
        {
            string result = indent;

            for(int i = 0; i < depth; i++)
            {
                result += "  ";
            }
            result += node.Name + " (" + node.NodeType + ")" + ((node is Hdf5Dataset dataset1) ? (H5DataTypeClass)dataset1.DataType : "") + "\n";

            if (node is Hdf5Group group)
            {
                foreach (var child in group.Children)
                {
                    result += PrintAllNodes(child, indent + "  ", depth + 1);
                }
            }
            else if (node is Hdf5Dataset dataset)
            {
                result += "  " + dataset.GetPreview(depth + 1, false) + "\n";
            }

            return result;
        }
    }

    // HDF5 파일 내부의 모든 요소에 대한 기본 클래스
    public abstract class Hdf5Node
    {
        /// <summary>
        /// Name of the node (example: "Group1", "Dataset2" etc.)
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Full path of the node (including all parent names)
        /// </summary>
        public required string Path { get; set; }

        /// <summary>
        /// Type of the node
        /// </summary>
        public Hdf5NodeType NodeType { get; set; }

        /// <summary>
        /// Reference to parent node
        /// </summary>
        public Hdf5Node? Parent { get; set; }

        /// <summary>
        /// Attribute list of the node
        /// </summary>
        public List<Hdf5Attribute> Attributes { get; set; } = new List<Hdf5Attribute>();

        /// <summary>
        /// Add an attribute to the node
        /// </summary>
        public void AddAttribute(string name, object value)
        {
            Attributes.Add(new Hdf5Attribute { Name = name, Value = value });
        }
    }

    // Group Node : Contains other groups or datasets recursively
    public class Hdf5Group : Hdf5Node
    {
        public Hdf5Group()
        {
            NodeType = Hdf5NodeType.Group;
        }

        /// <summary>
        /// Child nodes of the group (can be groups or datasets, etc.)
        /// </summary>
        public List<Hdf5Node> Children { get; set; } = new List<Hdf5Node>();

        /// <summary>
        /// Add a child node to the group (Connects the parent-child relationship)
        /// </summary>
        /// <param name="node">추가할 자식 노드</param>
        public void AddChild(Hdf5Node node)
        {
            node.Parent = this;
            Children.Add(node);
        }
    }

    // 데이터셋 노드: 실제 데이터를 포함하는 노드
    public class Hdf5Dataset : Hdf5Node
    {
        public Hdf5Dataset()
        {
            NodeType = Hdf5NodeType.Dataset;
        }

        /// <summary>
        /// 데이터 타입 (예: "int", "double", "string" 등)
        /// </summary>
        public required byte DataType { get; set; }

        /// <summary>
        /// 데이터셋의 차원 정보
        /// </summary>
        public required ulong[] Dimensions { get; set; }

        /// <summary>
        /// 데이터가 로드되었는지 여부
        /// </summary>
        public bool IsDataLoaded { get; private set; }

        /// <summary>
        /// 데이터셋 ID (데이터 로드 시 사용)
        /// </summary>
        public long DatasetId { get; set; }

        /// <summary>
        /// 실제 데이터. 필요에 따라 적절한 타입으로 변경 가능
        /// </summary>
        private object? _data;
        public object? Data
        {
            get => _data;
            set
            {
                _data = value;
                IsDataLoaded = value != null;
            }
        }

        /// <summary>
        /// 데이터를 지연 로딩하는 메서드
        /// </summary>
        public void LoadData(Func<Hdf5Dataset, object> dataLoader)
        {
            if (!IsDataLoaded)
            {
                Data = dataLoader(this);
            }
        }

        /// <summary>
        /// 데이터셋의 미리보기 (요약) 제공
        /// </summary>
        /// <returns>데이터 요약 정보</returns>
        public string GetPreview(int depth, bool isPreview = true)
        {
            string result = "";

            for(int i = 0; i < depth; i++)
            {
                result += "  ";
            }

            if (IsDataLoaded && Data != null)
            {
                // 데이터가 이미 로드된 경우, 간략한 미리보기 반환
                return result + "데이터 로드됨: " + GetDataSummary(isPreview);
            }
            else
            {
                // 데이터가 로드되지 않은 경우, 크기 정보만 반환
                return result + $"Dataset [Name : {Name}] [Path: {Path}] [Size: {string.Join(" x ", Dimensions)}] [Type : {DataType}] [Dimension : {Dimensions.Length}]";
            }
        }

        /// <summary>
        /// 데이터 요약 정보 생성 (타입에 따라 다르게 처리)
        /// </summary>
        private string GetDataSummary(bool isPreview)
        {
            if (Data == null) return "null";

            // 배열 타입인 경우
            if (Data.GetType().IsArray)
            {
                Array array = (Array)Data;
                if (array.Length == 0) return "빈 배열";

                // 1차원 배열인 경우
                if (array.Rank == 1)
                {
                    int previewCount = isPreview ? Math.Min(5, array.Length) : array.Length ;
                    var preview = new object[previewCount];
                    Array.Copy(array, preview, previewCount);

                    string result = $"[{string.Join(", ", preview)}";
                    if ( isPreview && array.Length > previewCount)
                        result += ", ...";
                    result += "]";
                    return result;
                }

                // 다차원 배열인 경우
                return $"{array.Rank}차원 배열, 총 요소 수: {array.Length}";
            }

            // 기타 타입
            return Data.ToString() ?? "알 수 없는 데이터";
        }

        /// <summary>
        /// Get Data Array
        /// </summary>
        /// <returns>Array</returns>
        public Array GetDataArray()
        {
            if (Data == null) return null;

            if (Data.GetType().IsArray)
            {
                return (Array)Data;
            }

            return null;
        }
    }

    // 어트리뷰트를 표현하는 클래스
    public class Hdf5Attribute
    {
        /// <summary>
        /// 어트리뷰트 이름
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// 어트리뷰트 값 (다양한 타입을 지원할 수 있도록 object 사용)
        /// </summary>
        public required object Value { get; set; }
    }
}
