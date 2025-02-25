
namespace MyHdfViewer.Models
{
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
        /// 해당 요소에 속한 어트리뷰트 목록
        /// </summary>
        public List<Hdf5Attribute> Attributes { get; set; } = new List<Hdf5Attribute>();
    }

    // 그룹 노드: 재귀적으로 자식 노드를 가질 수 있음
    public class Hdf5Group : Hdf5Node
    {
        /// <summary>
        /// 이 그룹 내부에 포함된 하위 그룹 또는 데이터셋 등
        /// </summary>
        public List<Hdf5Node> Children { get; set; } = new List<Hdf5Node>();
    }

    // 데이터셋 노드: 실제 데이터를 포함하는 노드
    public class Hdf5Dataset : Hdf5Node
    {
        /// <summary>
        /// 데이터 타입 (예: "int", "double", "string" 등)
        /// </summary>
        public required string DataType { get; set; }

        /// <summary>
        /// 데이터셋의 차원 정보
        /// </summary>
        public required int[] Dimensions { get; set; }

        /// <summary>
        /// 실제 데이터. 필요에 따라 적절한 타입(또는 제네릭 타입)으로 변경 가능
        /// </summary>
        public object Data { get; set; }
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
