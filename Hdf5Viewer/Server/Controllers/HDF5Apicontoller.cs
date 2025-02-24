using Microsoft.AspNetCore.Mvc;
using Hdf5Viewer.Server.Services; // HDF5Service 사용
using Hdf5Viewer.Shared; // Shared 데이터 모델 사용

namespace Hdf5Viewer.Server.Controllers
{
    [ApiController]
    [Route("api/hdf5")]
    public class HDF5ApiController : ControllerBase
    {
        private readonly HDF5Service _hdf5Service;
        private static string _filePath = string.Empty;

        public HDF5ApiController(HDF5Service hdf5Service)
        {
            _hdf5Service = hdf5Service;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile()
        {
            try
            {
                var file = Request.Form.Files[0];
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded.");
                }

                var tempFilePath = Path.Combine(Path.GetTempPath(), file.FileName);
                _filePath = tempFilePath; // static 변수에 저장

                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new { message = "File uploaded successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Upload failed: {ex.Message}");
            }
        }


        [HttpGet("structure")]
        public ActionResult<Hdf5TreeNode> GetStructure()
        {
            if (string.IsNullOrEmpty(_filePath) || !System.IO.File.Exists(_filePath))
            {
                return BadRequest("No HDF5 file loaded.");
            }

            try
            {
                var treeNode = _hdf5Service.GetHdf5TreeStructure(_filePath);
                return Ok(treeNode);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error getting HDF5 structure: {ex.Message}");
            }
        }


        [HttpGet("dataset")]
        public ActionResult<Hdf5TableData> GetDatasetData(string datasetPath)
        {
             if (string.IsNullOrEmpty(_filePath) || !System.IO.File.Exists(_filePath))
            {
                return BadRequest("No HDF5 file loaded.");
            }

            if (string.IsNullOrEmpty(datasetPath))
            {
                return BadRequest("Dataset path is required.");
            }

            try
            {
                var tableData = _hdf5Service.GetTableData(_filePath, datasetPath);
                return Ok(tableData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error getting dataset data: {ex.Message}");
            }
        }
    }
}