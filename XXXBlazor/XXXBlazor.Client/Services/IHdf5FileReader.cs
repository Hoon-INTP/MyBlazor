using XXXBlazor.Client.Models;

namespace XXXBlazor.Client.Services
{
    /// <summary>
    /// HDF5 파일 읽기 인터페이스
    /// </summary>
    public interface IHdf5FileReader
    {
        /// <summary>
        /// 파일 경로에서 HDF5 파일을 로드하여 루트 노드를 반환합니다.
        /// </summary>
        /// <param name="filePath">HDF5 파일 경로</param>
        /// <returns>HDF5 파일의 루트 노드</returns>
        Hdf5TreeNode LoadFromFile(string filePath);

        /// <summary>
        /// 메모리 스트림에서 HDF5 파일을 로드하여 루트 노드를 반환합니다.
        /// </summary>
        /// <param name="stream">HDF5 데이터가 포함된 메모리 스트림</param>
        /// <returns>HDF5 파일의 루트 노드</returns>
        Hdf5TreeNode LoadFromStream(MemoryStream stream, string fileName);

        /// <summary>
        /// 데이터셋에서 특정 부분만 읽습니다.
        /// </summary>
        /// <typeparam name="T">데이터 타입</typeparam>
        /// <param name="filePath">HDF5 파일 경로</param>
        /// <param name="datasetPath">데이터셋 경로</param>
        /// <param name="start">시작 인덱스 배열</param>
        /// <param name="count">요소 개수 배열</param>
        /// <returns>읽은 데이터 배열</returns>
        T[] ReadDatasetRegion<T>(string filePath, string datasetPath, ulong[] start, ulong[] count) where T : unmanaged;

        /// <summary>
        /// 데이터셋을 다차원 배열로 반환합니다.
        /// </summary>
        /// <typeparam name="T">데이터 타입</typeparam>
        /// <param name="filePath">HDF5 파일 경로</param>
        /// <param name="datasetPath">데이터셋 경로</param>
        /// <returns>차원 정보에 맞게 변환된 다차원 배열</returns>
        Array ReadDatasetAsMultidimensional<T>(string filePath, string datasetPath) where T : unmanaged;

        /// <summary>
        /// 데이터셋의 부분 영역을 다차원 배열로 반환합니다.
        /// </summary>
        /// <typeparam name="T">데이터 타입</typeparam>
        /// <param name="filePath">HDF5 파일 경로</param>
        /// /// <param name="datasetPath">데이터셋 경로</param>
        /// <param name="start">시작 인덱스 배열</param>
        /// <param name="count">요소 개수 배열</param>
        /// <returns>차원 정보에 맞게 변환된 다차원 배열</returns>
        Array ReadDatasetRegionAsMultidimensional<T>(string filePath, string datasetPath, ulong[] start, ulong[] count) where T : unmanaged;
    }
}
