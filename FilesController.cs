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
        public ActionResult DownloadFile(string remoteFilePath, string smbPath, string username, string password)
        {
            string tempFilePath = Path.GetTempFileName(); // Временный файл для хранения загруженного файла

            try
            {
                // Подключение к SMB-серверу
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
                        return StatusCode((int)HttpStatusCode.InternalServerError, "Ошибка подключения к SMB: не удалось подключиться к серверу.");
                    }
                }

                // Проверка наличия файла
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
                        return NotFound("Файл не найден.");
                    }
                }

                // Копирование файла во временное место
                System.IO.File.Copy(smbPath + remoteFilePath, tempFilePath, true);

                var fileBytes = System.IO.File.ReadAllBytes(tempFilePath);
                var contentType = "application/octet-stream";
                return File(fileBytes, contentType, Path.GetFileName(remoteFilePath));
            }
            catch (UnauthorizedAccessException uaEx)
            {
                return StatusCode((int)HttpStatusCode.Forbidden, "Доступ запрещен: " + uaEx.Message);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "Ошибка: " + ex.Message);
            }
            finally
            {
                // Отключение от SMB-сервера
                try
                {
                    Process.Start("cmd.exe", $"/c net use {smbPath} /delete").WaitForExit();
                }
                catch (Exception delEx)
                {
                    Console.WriteLine($"Ошибка при отключении от SMB: {delEx.Message}");
                }

                // Удаление временного файла, если он существует
                if (System.IO.File.Exists(tempFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                    catch (Exception delEx)
                    {
                        // Логирование других ошибок
                        Console.WriteLine($"Ошибка при удалении файла: {delEx.Message}");
                    }
                }
            }
        }

    }
}
