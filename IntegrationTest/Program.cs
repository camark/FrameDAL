﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ResumeFactory.Forms;
using ResumeFactory.Entity;
using FrameDAL.Core;
using FrameDAL.Attributes;

namespace ResumeFactory
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            ResumeFactory.Common.Config.DefaultPath
                = FrameDAL.Config.Configuration.DefaultPath
                = Application.StartupPath + @"\ResumeFactory.ini";
            // RunUnitTest();
            RunResumeFactory();
        }

        private static void RunUnitTest()
        {
            LinqTest test = new LinqTest();
            test.Debug(test.TestSkipTake);
            // test.Run();
        }

        private static void RunResumeFactory()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            FormLogin formLogin = new FormLogin();
            Sunisoft.IrisSkin.SkinEngine skin = new Sunisoft.IrisSkin.SkinEngine(formLogin);
            skin.SkinFile = ResumeFactory.Common.Config.SkinFile;
            skin.TitleFont = new System.Drawing.Font("微软雅黑", 10F); 
            skin.SkinScrollBar = false;
            if (formLogin.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new FormMain(formLogin.LoginUser));
            }
        }
    }

    [Table("user")]
    public class User0
    {
        [Id(GeneratorType.Uuid)]
        [Column("id")]
        public string Id { get; set; }

        [Column("user_name")]
        public string UserName { get; set; }

        [Column("user_pwd")]
        public string UserPwd { get; set; }
    }

    [Table("resume")]
    public class Resume0
    {
        [Id(GeneratorType.Uuid)]
        [Column("id")]
        public string Id { get; set; }

        [Column("resume_name")]
        public string ResumeName { get; set; }

        [Column("user_id")]
        public string UserId { get; set; }
    }

    [Table("student")]
    public class Student
    {
        [Id(GeneratorType.Identity)]
        [Column("id")]
        public virtual int Id { get; set; }

        [Column("stu_name")]
        public virtual string StuName { get; set; }

        [Column("stu_age")]
        public virtual int StuAge { get; set; }

        [Column("stu_class")]
        public virtual int StuClass { get; set; }

        [ManyToMany(JoinTable = "stu_course", JoinColumn = "stu_id", InverseJoinColumn = "course_id", LazyLoad = false)]
        public virtual List<Course> Courses { get; set; }
    }

    [Table("course")]
    public class Course
    {
        [Id(GeneratorType.Identity)]
        [Column("id")]
        public virtual int Id { get; set; }

        [Column("course_name")]
        public virtual string CourseName { get; set; }

        [ManyToMany(JoinTable = "stu_course", JoinColumn = "course_id", InverseJoinColumn = "stu_id", LazyLoad = false)]
        public virtual List<Student> Students { get; set; }
    }
}