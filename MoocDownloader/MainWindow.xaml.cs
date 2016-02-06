using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;

namespace MoocDownloader
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            bgLoadingWorker.WorkerReportsProgress = true;
            bgLoadingWorker.DoWork += bgLoadingWorker_DoWork;
            bgLoadingWorker.ProgressChanged += bgLoadingWorker_ProgressChanged;
            bgLoadingWorker.RunWorkerCompleted += bgLoadingWorker_RunWorkerCompleted;

            bgDownloaderWorker.WorkerReportsProgress = true;
            bgDownloaderWorker.DoWork += bgDownloaderWorker_DoWork;
            bgDownloaderWorker.ProgressChanged += bgDownloaderWorker_ProgressChanged;
            bgDownloaderWorker.RunWorkerCompleted += bgDownloaderWorker_RunWorkerCompleted;

        }

        #region 变量
        List<CourseInfo> courselist = new List<CourseInfo>();
        List<CourseInfo> downloadcourselist = new List<CourseInfo>();

        BackgroundWorker bgLoadingWorker = new BackgroundWorker();
        BackgroundWorker bgDownloaderWorker = new BackgroundWorker();

        string DownloadCourseID = "";
        int NowPercentage = 0;
        string SaveFolderPath = "";

        #endregion

        #region 后台
        void bgLoadingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            GetCourseList();
        }

        void bgLoadingWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pbLoading.Value = e.ProgressPercentage;
            tbProcessAll.Text = pbLoading.Value + "%";
            spPanelTop.IsEnabled = false;
        }

        void bgLoadingWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            gridCourse.DataContext = courselist;
            spPanelTop.IsEnabled = true;
        }

        void bgDownloaderWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            for (int x = 0; x < downloadcourselist.Count; x++)
            {
                string courseid = downloadcourselist[x].CourseID;

                var downloadurllist = GetDownLoadList(courseid);

                string savefolder = SaveFolderPath + downloadcourselist[x].CourseName;
                // Determine whether the directory exists.
                if (!Directory.Exists(savefolder))
                {
                    // Create the directory it does not exist.
                    Directory.CreateDirectory(savefolder);
                }

                for (int y = 0; y < downloadurllist.Count; y++)
                {
                    DownloadCourseID = courseid;
                    string url = downloadurllist.ElementAt(y).Value;
                    string title = downloadurllist.ElementAt(y).Key;

                    //去除非法文件名字符
                    foreach (char rInvalidChar in Path.GetInvalidFileNameChars())
                        title = title.Replace(rInvalidChar.ToString(), string.Empty);

                    string filename = savefolder + "\\" + title + ".mp4";


                    downLoadFromUrl(url, filename);
                    //Thread.Sleep(100);
                    bgDownloaderWorker.ReportProgress((y + 1) * 100 / downloadurllist.Count);

                    //Console.WriteLine(DownloadCourseID + "---" + "y---" + (y + 1) + "---count---" + downloadurllist.Count + "---progress---" + ((y + 1) * 100 / downloadurllist.Count));
                }


            }
        }

        void bgDownloaderWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            List<CourseInfo> nowcourse = (List<CourseInfo>)gridCourse.DataContext;

            courselist.Where(a => a.CourseID == DownloadCourseID).FirstOrDefault().DownloadPercentage = e.ProgressPercentage;

            gridCourse.DataContext = nowcourse.ToList();

            int finishcoursenum = nowcourse.Where(a => a.DownloadPercentage == 100).Count();
            int totalnum = nowcourse.Count();
            pbLoading.Value = finishcoursenum * 100 / totalnum;
            tbProcessAll.Text = pbLoading.Value + "%";
            spPanelTop.IsEnabled = false;
        }

        void bgDownloaderWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            System.Windows.MessageBox.Show("下载完成");
            this.Close();
        }

        #endregion

        #region 自定义方法
        private void GetCourseList()
        {
            string url = "http://www.imooc.com/course/list?page=";

            string listurl = "http://www.imooc.com/course/list";

            var listhtml = GetStrHtml(listurl);

            var pagetotal = Regex.Match(listhtml, @"\d+(?=</em>)").Groups[0].ToString();

            int pagecount = Convert.ToInt32(pagetotal);

            for (int i = 0; i < pagecount; i++)
            {
                string strHtml = GetStrHtml(url + (i + 1));
                var lessonblock = Regex.Matches(strHtml, @"<li class=""course-one"">([\s\S]*?)</li>");



                for (int a = 0; a < lessonblock.Count; a++)
                {
                    CourseInfo ci = new CourseInfo();
                    string html = lessonblock[a].ToString();
                    ci.CourseID = Regex.Match(html, @"(?<=/view/)\d+").Groups[0].ToString().Trim();
                    ci.CourseName = Regex.Match(html, @"(?<=<span>).+(?=</span>)").Groups[0].ToString().Trim();
                    ci.LastUpdate = Regex.Match(html, @"更新.+(?=</span>)").Groups[0].ToString().Trim();
                    ci.LearnPeopleNums = Regex.Match(html, @"(?<=\s)\d+(?=\s)").Groups[0].ToString().Trim();

                    var timeandlevel = Regex.Match(html, @"(?<=\s).+[|].+(?=\s)").Groups[0].ToString().Trim().Split('|');
                    ci.LessonPeriod = timeandlevel[0].ToString().Trim();
                    ci.Level = timeandlevel[1].ToString().Trim();
                    //ci.DownloadPercentage = 60;
                    courselist.Add(ci);
                }

                NowPercentage = (i + 1) * 100 / pagecount;
                bgLoadingWorker.ReportProgress(NowPercentage);
            }
        }

        private string GetStrHtml(string url)
        {
            System.Net.WebRequest webRequest = System.Net.WebRequest.Create(url);
            System.Net.WebResponse webResponse = webRequest.GetResponse();
            System.IO.StreamReader sr = new System.IO.StreamReader(webResponse.GetResponseStream(), System.Text.Encoding.GetEncoding("UTF-8"));
            string strHtml = sr.ReadToEnd();
            sr.Close();

            return strHtml;
        }

        //同步下载
        private void downLoadFromUrl(string url, string filename)
        {
            try
            {
                using (var client = new System.Net.WebClient())
                {
                    client.DownloadFile(new Uri(url), filename);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "下载异常:"+ex.StackTrace);
                return;
            }
        }

        private Dictionary<string, string> GetDownLoadList(string courseid)
        {
            string url = "http://www.imooc.com/learn/" + courseid;

            string strHtml = GetStrHtml(url);

            var videomatches = Regex.Matches(strHtml, @"(?<=/video/).+(?=')");
            var titlematches = Regex.Matches(strHtml, @"(?<=studyvideo"">).+");

            List<string> titlelist = new List<string>();

            foreach (var item in titlematches)
            {
                string title = item.ToString();
                title = title.Substring(0, title.IndexOf('(')).Trim();
                titlelist.Add(title);
            }

            List<string> videolinklist = new List<string>();

            foreach (var item in videomatches)
            {
                videolinklist.Add("http://www.imooc.com/course/ajaxmediainfo/?mid=" + item.ToString() + "&mode=flash");
            }

            List<string> fileurllist = new List<string>();

            for (int i = 0; i < videolinklist.Count; i++)
            {
                string learnhtml = GetStrHtml(videolinklist[i]);
                var linkurl = Regex.Match(learnhtml, @"http.+H.mp4").Groups[0].ToString().Replace(@"\", "");
                fileurllist.Add(linkurl);
            }

            Dictionary<string, string> downloaddic = new Dictionary<string, string>();

            if (fileurllist.Count == titlelist.Count)
            {
                for (int i = 0; i < fileurllist.Count; i++)
                {
                    downloaddic.Add(titlelist[i], fileurllist[i]);
                }
            }

            return downloaddic;
        }

        #endregion

        #region 窗体控件事件

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bgLoadingWorker.RunWorkerAsync();
        }
        private void cbAll_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox ck1 = (System.Windows.Controls.CheckBox)sender;

            List<CourseInfo> nowcourse = (List<CourseInfo>)gridCourse.DataContext;

            gridCourse.DataContext = null;

            if (ck1.IsChecked == true)
            {
                for (int i = 0; i <= nowcourse.Count - 1; i++)
                {
                    nowcourse[i].Selected = true;
                }
            }
            else
            {
                for (int i = 0; i <= nowcourse.Count - 1; i++)
                {
                    nowcourse[i].Selected = false;
                }


            }
            gridCourse.DataContext = nowcourse.ToList();
        }

        private void tbKeyWord_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = tbKeyWord.Text.Trim();

            var fiteredlist = courselist.Where(a => a.CourseName.ToLower().Contains(keyword.ToLower())).OrderBy(a => a.CourseName).ToList();

            gridCourse.DataContext = fiteredlist.ToList();
        }

        private void btnDownLoad_Click(object sender, RoutedEventArgs e)
        {
            downloadcourselist.Clear();
            SaveFolderPath = tbSaveFolder.Text.Trim();
            pbLoading.Value = 0;
            tbProcessAll.Text = "0%";

            foreach (var item in gridCourse.Items)
            {
                CourseInfo ci = (CourseInfo)item;
                if (ci.Selected)
                {
                    ci.Selected = false;
                    downloadcourselist.Add(ci);
                }
            }

            gridCourse.DataContext = downloadcourselist.ToList();

            bgDownloaderWorker.RunWorkerAsync();
        }

        private void cbSelect_Click(object sender, RoutedEventArgs e)
        {
            List<CourseInfo> nowcourse = (List<CourseInfo>)gridCourse.DataContext;

            System.Windows.Controls.CheckBox clickedcb = (System.Windows.Controls.CheckBox)sender;

            if (clickedcb.IsChecked.HasValue)
            {
                string courseid = clickedcb.Tag.ToString();
                courselist.Where(a => a.CourseID == courseid).FirstOrDefault().Selected = (bool)((System.Windows.Controls.CheckBox)sender).IsChecked;
            }

            gridCourse.DataContext = nowcourse.ToList();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dilog = new FolderBrowserDialog();
            dilog.Description = "请选择文件夹";
            if (dilog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (dilog.SelectedPath.LastIndexOf("\\") != dilog.SelectedPath.Length)
                {
                    dilog.SelectedPath += "\\";
                }

                tbSaveFolder.Text = dilog.SelectedPath;
            }


        }

        private void gridCourse_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key.ToString() == "Space")
            {
                List<CourseInfo> nowcourse = (List<CourseInfo>)gridCourse.DataContext;

                for (int i = 0; i < gridCourse.SelectedItems.Count; i++)
                {
                    string courseid = ((CourseInfo)gridCourse.SelectedItems[i]).CourseID;
                    bool nowSelected = courselist.Where(a => a.CourseID == courseid).FirstOrDefault().Selected;
                    courselist.Where(a => a.CourseID == courseid).FirstOrDefault().Selected = !nowSelected;
                }
                gridCourse.DataContext = nowcourse.ToList();
            }
        }

        #endregion

    }
}
