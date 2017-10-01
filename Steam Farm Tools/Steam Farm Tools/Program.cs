using System;
using System.Windows.Forms;

namespace Shatulsky_Farm {
    static class Program {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main() {
            GetForm.Start();
            GetForm.NewForm();
            Application.Run(GetForm.MyMainForm);
        }

        public static class GetForm {
            public static void Start() {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
            public static MainForm MyMainForm;

            public static void NewForm() {
                MyMainForm = new MainForm();
            }

        }
    }
}
