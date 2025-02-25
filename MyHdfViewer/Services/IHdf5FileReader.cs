
using MyHdfViewer.Models;

namespace MyHdfViewer.Services
{
    /// <summary>
    /// HDF5 파일에서 기본 메타데이터와 루트 노드 구조를 읽어 Hdf5FileModel을 생성
    /// </summary>
    public interface IHdf5FileReader
    {
        /// <summary>
        /// 주어진 파일 경로의 HDF5 파일을 읽어 Hdf5FileModel을 반환
        /// </summary>
        /// <param name="filePath">HDF5 파일 경로</param>
        /// <returns>파일 메타데이터와 기본 노드 구조를 포함하는 모델</returns>
        Hdf5FileModel ReadFile(string filePath);
    }
}
