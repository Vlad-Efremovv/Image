using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;

namespace Image
{
    [ApiController]
    [Route("[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly ILogger<FilesController> _logger;

        public FilesController(ILogger<FilesController> logger)
        {
            _logger = logger;
        }

        [HttpGet("download")]
        public ActionResult DownloadFile(string smbPath = @"\\tvr-vsr-fs2.ksc.local\1c_base_shares", string remoteFilePath = @"\erp_zgt_div\20241204\�������.jpg", string username = "1s-dev", string password = "Mbf2urf2903e8")
        {
            string tempFilePath = Path.GetTempFileName(); // ��������� ���� ��� �������� ������������ �����

            try
            {
                // ����������� � SMB-�������
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", $"/c net use {smbPath} /user:{username} {password}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        return StatusCode((int)HttpStatusCode.InternalServerError, "������ ����������� � SMB: �� ������� ������������ � �������.");
                    }
                }

                // �������� ������� �����
                ProcessStartInfo checkFilePsi = new ProcessStartInfo("cmd.exe", $"/c dir {smbPath}{remoteFilePath}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(checkFilePsi))
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        return NotFound("���� �� ������.");
                    }
                }

                // ����������� ����� �� ��������� �����
                System.IO.File.Copy(smbPath + remoteFilePath, tempFilePath, true);

                var fileBytes = System.IO.File.ReadAllBytes(tempFilePath);
                var contentType = "application/octet-stream";
                return File(fileBytes, contentType, Path.GetFileName(remoteFilePath));
            }
            catch (UnauthorizedAccessException uaEx)
            {
                return StatusCode((int)HttpStatusCode.Forbidden, "������ ��������: " + uaEx.Message);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "������: " + ex.Message);
            }
            finally
            {
                // ���������� �� SMB-�������
                try
                {
                    Process.Start("cmd.exe", $"/c net use {smbPath} /delete").WaitForExit();
                }
                catch (Exception delEx)
                {
                    Console.WriteLine($"������ ��� ���������� �� SMB: {delEx.Message}");
                }

                // �������� ���������� �����, ���� �� ����������
                if (System.IO.File.Exists(tempFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                    catch (Exception delEx)
                    {
                        // ����������� ������ ������
                        Console.WriteLine($"������ ��� �������� �����: {delEx.Message}");
                    }
                }
            }
        }

    }
}
