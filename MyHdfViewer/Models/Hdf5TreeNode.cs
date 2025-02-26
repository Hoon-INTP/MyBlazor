
namespace MyHdfViewer.Models
{
    /// <summary>
    /// Enum for HDF5 node type
    /// </summary>
    public enum Hdf5NodeType
    {
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
        /// 파일의 전체 경로
        /// </summary>
        public required string FilePath { get; set; }

        /// <summary>
        /// 파일 이름
        /// </summary>
        public required string FileName { get; set; }

        /// <summary>
        /// HDF5 파일의 루트 그룹 (내부 구조의 최상위 노드)
        /// </summary>
        public required Hdf5Group RootGroup { get; set; }
    }

    // HDF5 파일 내부의 모든 요소에 대한 기본 클래스
    public abstract class Hdf5Node
    {
        /// <summary>
        /// 그룹, 데이터셋 등 요소의 이름
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// 노드의 전체 경로 (예: "/Group1/Dataset1")
        /// </summary>
        public required string Path { get; set; }

        /// <summary>
        /// 노드의 유형
        /// </summary>
        public Hdf5NodeType NodeType { get; set; }

        /// <summary>
        /// 부모 노드에 대한 참조
        /// </summary>
        public Hdf5Node? Parent { get; set; }

        /// <summary>
        /// 해당 요소에 속한 어트리뷰트 목록
        /// </summary>
        public List<Hdf5Attribute> Attributes { get; set; } = new List<Hdf5Attribute>();
    }

    // 그룹 노드: 재귀적으로 자식 노드를 가질 수 있음
    public class Hdf5Group : Hdf5Node
    {
        public Hdf5Group()
        {
            NodeType = Hdf5NodeType.Group;
        }

        /// <summary>
        /// 이 그룹 내부에 포함된 하위 그룹 또는 데이터셋 등
        /// </summary>
        public List<Hdf5Node> Children { get; set; } = new List<Hdf5Node>();

        /// <summary>
        /// 자식 노드 추가 시 부모-자식 관계 설정
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
        public required string DataType { get; set; }

        /// <summary>
        /// 데이터셋의 차원 정보
        /// </summary>
        public required int[] Dimensions { get; set; }

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
        public string GetPreview()
        {
            if (IsDataLoaded && Data != null)
            {
                // 데이터가 이미 로드된 경우, 간략한 미리보기 반환
                return "데이터 로드됨: " + GetDataSummary();
            }
            else
            {
                // 데이터가 로드되지 않은 경우, 크기 정보만 반환
                return $"데이터셋 크기: {string.Join(" x ", Dimensions)}";
            }
        }

        /// <summary>
        /// 데이터 요약 정보 생성 (타입에 따라 다르게 처리)
        /// </summary>
        private string GetDataSummary()
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
                    int previewCount = Math.Min(5, array.Length);
                    var preview = new object[previewCount];
                    Array.Copy(array, preview, previewCount);
                    
                    string result = $"[{string.Join(", ", preview)}";
                    if (array.Length > previewCount)
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
