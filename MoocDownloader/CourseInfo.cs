using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoocDownloader
{
    public class CourseInfo
    {
        public bool Selected { get; set; }
        public string CourseName { get; set; }
        //简介
        //public string CourseTip { get; set; }
        public string LastUpdate { get; set; }
        public string LearnPeopleNums { get; set; }
        //时长
        public string LessonPeriod { get; set; }
        public string Level { get; set; }
        public string CourseID { get; set; }

        public int DownloadPercentage { get; set; }
    }
}
