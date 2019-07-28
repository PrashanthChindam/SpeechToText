using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinSpeechToText
{
    static class Program
    {
        private static int homePage = Convert.ToInt32(ConfigurationManager.AppSettings["HomePage"]);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SetStartupForm();
        }

        static void SetStartupForm()
        {
            switch(homePage)
            {
                case 1:
                    Application.Run(new FormProcessAudio());
                    break;
                case 2:
                    Application.Run(new FormProcessAudioFile());
                    break;
                default:
                    Application.Run(new FormProcessAudio());
                    break;
            }

        }
    }
}
