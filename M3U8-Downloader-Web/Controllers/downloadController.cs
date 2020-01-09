using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using M3U8_Downloader_Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace M3U8_Downloader_Web.Controllers
{
    public class downloadController : Controller
    {
        private static Dictionary<string, string> download_dic = new Dictionary<string, string> {
            { ".mp4", " -bsf:a aac_adtstoasc -movflags +faststart " }, { ".mkv", " -bsf:a aac_adtstoasc " }, { ".flv", " -f f4v -bsf:a aac_adtstoasc " }, { ".ts", " -f mpegts " }
        };
        private static List<download_model> download_list = new List<download_model>();
        public IActionResult index()
        {
            return View();
        }
        [HttpPost]
        public object add(download_model model)
        {
            model.id = Guid.NewGuid().ToString("N");
            model.create_time = DateTime.Now;
            model.download_path = AppContext.BaseDirectory + "wwwroot/download/";
            if (!Directory.Exists(model.download_path))
            {
                Directory.CreateDirectory(model.download_path);
            }
            download_list.Add(model);
            down();
            return new { code = 200, msg = "成功", date = model };
        }
        private void down()
        {
            if (download_list.Count() > 0 && download_list.Where(a => a.status == 1).Count() < 1)
            {
                var model = download_list.Where(a => a.status == 0).FirstOrDefault();
                if (model != null)
                {
                    var ml = "-threads 0 -i " + "\"" + model.m3u8_url + "\"" + " -c copy -y" + download_dic[Path.GetExtension(model.name).ToLower()] + "\"" + model.download_path + model.name + "\"";
                    model.ffmpeg_id = RealAction(ml);
                    model.status = 1;
                }
            }
        }
        [HttpPost]
        public object delete(string id)
        {
            var model = download_list.Where(a => a.id == id).FirstOrDefault();
            if (model != null && Process.GetProcessById(model.ffmpeg_id) != null)  //如果进程还存在就强制结束它
            {
                Process.GetProcessById(model.ffmpeg_id).Kill();
            }
            download_list.Remove(model);
            if (model.progress != 100)
            {
                if (System.IO.File.Exists(model.download_path + model.name))
                {
                    System.IO.File.Delete(model.download_path + model.name);
                }
            }
            down();
            return new { code = 200, msg = "成功" };
        }
        [HttpGet]
        public object list()
        {
            return new { code = 200, msg = "成功", data = download_list };
        }
        [HttpPost]
        public object suspend(string id)
        {
            return new { code = 200, msg = "成功" };
        }

        private int RealAction(string StartFileArg)
        {
            var ffmpeg = @"/app/ffmpeg-4.2.2/ffmpeg";
            Process CmdProcess = new Process();
            CmdProcess.StartInfo.FileName = ffmpeg;      // 命令  
            CmdProcess.StartInfo.Arguments = StartFileArg;      // 参数  
            CmdProcess.StartInfo.CreateNoWindow = true;         // 不创建新窗口  
            CmdProcess.StartInfo.UseShellExecute = false;
            CmdProcess.StartInfo.RedirectStandardInput = true;  // 重定向输入  
            CmdProcess.StartInfo.RedirectStandardOutput = true; // 重定向标准输出  
            CmdProcess.StartInfo.RedirectStandardError = true;  // 重定向错误输出  

            //CmdProcess.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
            //在ffmpeg的执行过程输出数据全部以错误输出产生的
            CmdProcess.ErrorDataReceived += new DataReceivedEventHandler(p_ErrorDataReceived);

            CmdProcess.EnableRaisingEvents = true;// 启用Exited事件  
            CmdProcess.Exited += new EventHandler(CmdProcess_Exited);   // 注册进程结束事件  

            CmdProcess.Start();
            CmdProcess.BeginOutputReadLine();
            CmdProcess.BeginErrorReadLine();
            // 如果打开注释，则以同步方式执行命令，此例子中用Exited事件异步执行。  
            // CmdProcess.WaitForExit();  
            return CmdProcess.Id;//获取ffmpeg.exe的进程ID

        }
        private void CmdProcess_Exited(object sender, EventArgs e)
        {
            //命令结束调用
            var ff_pr = (Process)sender;
            var model = download_list.Where(a => a.ffmpeg_id == ff_pr.Id).FirstOrDefault();
            if (model != null)
            {
                model.status = 2;
                model.ffmpeg_id = 0;
                model.progress = 100;
            }
            down();
        }
        private void p_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                var ff_pr = (Process)sender;
                var model = download_list.Where(a => a.ffmpeg_id == ff_pr.Id).FirstOrDefault();
                if (model != null)
                {
                    //输出
                    if (e.Data.Contains("Duration"))
                    {
                        Regex duration = new Regex(@"Duration: (\d\d[.:]){3}\d\d", RegexOptions.Compiled | RegexOptions.Singleline);//取总视频时长
                        model.duration = duration.Match(e.Data).Value.Replace("Duration: ", "");
                    }
                    if (e.Data.Contains("frame"))
                    {
                        Regex regex = new Regex(@"(\d\d[.:]){3}\d\d", RegexOptions.Compiled | RegexOptions.Singleline);//取视频时长以及Time属性
                        var time = regex.Matches(e.Data).OfType<Match>().Last().Value;
                        Regex size = new Regex(@"[1-9][0-9]{0,}kB time", RegexOptions.Compiled | RegexOptions.Singleline);//取已下载大小
                        var sizekb = size.Matches(e.Data).OfType<Match>().Last().ToString().Replace("kB time", "");
                        var Formatsize = FormatFileSize(Convert.ToDouble(sizekb) * 1024);
                        model.download_size = Formatsize;

                        double All = Convert.ToDouble(Convert.ToDouble(model.duration.Split(':')[0]) * 60 * 60 + Convert.ToDouble(model.duration.Split(':')[1]) * 60
                       + Convert.ToDouble(model.duration.Split(':')[2]));
                        double Downloaded = Convert.ToDouble(Convert.ToDouble(time.Split(':')[0]) * 60 * 60 + Convert.ToDouble(time.Split(':')[1]) * 60
                        + Convert.ToDouble(time.Split(':')[2]));
                        if (All == 0) All = 1;  //防止被除数为零导致程序崩溃
                        double Progress = ((int)(Downloaded / All * 10000)) / 100d;
                        if (Progress > 100)  //防止进度条超过百分之百
                            Progress = 100;
                        if (Progress < 0)  //防止进度条小于零……
                            Progress = 0;
                        model.progress = Progress;
                    }
                }
            }
        }
        //格式化大小输出
        public static string FormatFileSize(double fileSize)
        {
            if (fileSize < 0)
            {
                throw new ArgumentOutOfRangeException("fileSize");
            }
            else if (fileSize >= 1024 * 1024 * 1024)
            {
                return string.Format("{0:########0.00} GB", ((double)fileSize) / (1024 * 1024 * 1024));
            }
            else if (fileSize >= 1024 * 1024)
            {
                return string.Format("{0:####0.00} MB", ((double)fileSize) / (1024 * 1024));
            }
            else if (fileSize >= 1024)
            {
                return string.Format("{0:####0.00} KB", ((double)fileSize) / 1024);
            }
            else
            {
                return string.Format("{0} bytes", fileSize);
            }
        }
    }
}