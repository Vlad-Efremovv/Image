using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Net;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Image.Controllers
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

        // Метод для подключения к SMB и получения списка файлов
        [HttpGet("list")]
        public ActionResult<IEnumerable<string>> ListFiles(string smbPath = @"\\tvr-vsr-fs2.ksc.local\1c_base_shares", string username = "1s-dev", string password = "Mbf2urf2903e8")
        {
            string localPath = @"C:\Down\шпилька.jpg";
            string remoteFilePath = @"\erp_zgt_div\20241204\шпилька.jpg"; // Путь к файлу на SMB
            string localFilePath = @"C:\Down\шпилька.jpg"; // Локальный путь для сохранения

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
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        Console.WriteLine("Ошибка подключения к SMB: " + error);
                        return null;
                    }
                }

                // Теперь можно копировать файл
                System.IO.File.Copy(smbPath + remoteFilePath, localFilePath, true);
                Console.WriteLine("Файл успешно скачан по пути: " + localFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }
            finally
            {
                // Отключение от SMB-сервера (не обязательно, но рекомендуется)
                Process.Start("cmd.exe", $"/c net use {smbPath} /delete");
            }
            return null;
        }
    }
}
