using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace M3U8_Downloader_Web.Models
{
    public class download_model
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// 保存文件名
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 下载地址
        /// </summary>
        public string m3u8_url { get; set; }
        /// <summary>
        /// 下载路径
        /// </summary>
        public string download_path { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime create_time { get; set; }
        /// <summary>
        /// 任务id
        /// </summary>
        public int ffmpeg_id { get; set; }
        /// <summary>
        /// 下载进度
        /// </summary>
        public double progress { get; set; }
        /// <summary>
        /// 总时长
        /// </summary>
        public string duration { get; set; }
        /// <summary>
        /// 已下载大小
        /// </summary>
        public string download_size { get; set; }
        /// <summary>
        /// 状态 0 等待下载，1下载中，2下载完成
        /// </summary>
        public int status { get; set; }
    }
}
