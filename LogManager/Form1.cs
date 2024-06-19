using LogFileMonitorService;
using System;
using System.ServiceProcess;
namespace LogManager
{
    public partial class Form1 : Form
    {
        System.Timers.Timer timer1 = new  System.Timers.Timer();
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string serviceName = "MyWindowsService";
            ServiceController service = new ServiceController(serviceName);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Interval = (1 * 60 * 1000); // 1 min
            timer1.Tick += new EventHandler(Service1);
            timer1.Start();
        }
    }
}
