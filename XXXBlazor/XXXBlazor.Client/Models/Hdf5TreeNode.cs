
namespace XXXBlazor.Client.Models
{
    /// <summary>
    /// HDF5 파일 내의 객체 타입을 나타냅니다.
    /// </summary>
    public enum Hdf5NodeType
    {
        Group,
        Dataset,
        Attribute,
        Unknown
    }

    /// <summary>
    /// HDF5 파일의 그룹, 데이터셋 등을 표현하는 기본 클래스입니다.
    /// </summary>
    public class Hdf5TreeNode
    {
        /// <summary>
        /// 노드 이름
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 전체 경로
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// 노드 타입 (그룹, 데이터셋 등)
        /// </summary>
        public Hdf5NodeType NodeType { get; set; }

        /// <summary>
        /// 부모 노드
        /// </summary>
        public Hdf5TreeNode? Parent { get; set; }

        /// <summary>
        /// 자식 노드 목록 (그룹인 경우)
        /// </summary>
        public List<Hdf5TreeNode> Children { get; set; }

        /// <summary>
        /// 속성 목록
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; }

        /// <summary>
        /// 데이터셋인 경우 차원 정보
        /// </summary>
        public ulong[]? Dimensions { get; set; }

        /// <summary>
        /// 데이터셋의 데이터 타입
        /// </summary>
        public Type? DataType { get; set; }

        /// <summary>
        /// 데이터셋 값 (데이터셋인 경우에만 유효)
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public Hdf5TreeNode()
        {
            Children = new List<Hdf5TreeNode>();
            Attributes = new Dictionary<string, object>();
        }

        /// <summary>
        /// 노드 정보를 문자열로 출력하는 메서드
        /// </summary>
        public override string ToString()
        {
            return $"{NodeType}: {Path}";
        }

        /// <summary>
        /// 전체 트리 구조를 문자열로 출력하는 메서드
        /// </summary>
        public string ToTreeString(int indent = 0)
        {
            var indentStr = new string(' ', indent * 2);
            var result = $"{indentStr}{ToString()}";

            if (NodeType == Hdf5NodeType.Dataset && Dimensions != null)
            {
                result += $" [{string.Join(", ", Dimensions)}]";
                if (DataType != null)
                {
                    result += $" ({DataType.Name})";
                }
            }

            result += Environment.NewLine;

            if (Attributes.Count > 0)
            {
                result += $"{indentStr}  [Attributes: {string.Join(", ", Attributes.Keys)}]{Environment.NewLine}";
            }

            foreach (var child in Children)
            {
                result += child.ToTreeString(indent + 1);
            }

            return result;
        }

        /// <summary>
        /// 데이터셋 노드의 데이터를 다차원 배열로 변환하여 반환
        /// </summary>
        /// <typeparam name="T">데이터 타입</typeparam>
        /// <returns>다차원 배열</returns>
        public Array? GetDataAsMultidimensional<T>() where T : unmanaged
        {
            if (NodeType != Hdf5NodeType.Dataset)
                throw new InvalidOperationException("이 메서드는 데이터셋 노드에서만 사용할 수 있습니다.");

            if (Data is T[] typedArray && Dimensions != null)
            {
                return Hdf5ArrayHelper.ConvertToMultidimensionalArray(typedArray, Dimensions);
            }

            throw new InvalidOperationException($"데이터를 {typeof(T).Name} 타입으로 변환할 수 없습니다.");
        }
    }
}
