using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using FileReport47.Models;
using FileReport47.Services;
using System.Runtime.Serialization;
using System.Xml;

namespace FileReport47
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0)
            {
                // Mode ligne de commande
                var form = new MainForm();
                form.LoadSettingsAndSearch(args[0]);
            }
            else
            {
                // Mode GUI normal
                Application.Run(new MainForm());
            }
        }
    }
}
