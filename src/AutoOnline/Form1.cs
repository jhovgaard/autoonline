using System;
using System.Deployment.Application;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace AutoOnline
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private bool activated = false;

        private void UpdateStatus(object source = null, EventArgs eventArgs = null)
        {
            var isRunning = IsPoeRunning();
            PoeStatusLabel.Text = isRunning  ? "Running" : "Not running";
            PoeStatusLabel.ForeColor = isRunning ? Color.Green : Color.Red;

            if (isRunning)
            {
                if (activated)
                {
                    SendUpdateToXyz();
                }
            }
            else
            {
                // Reset timer status if PoE was running earlier
                poeStatusTimer.Interval = 1000;
            }
        }

        bool SendUpdateToXyz()
        {
            var url = new Uri(textBox1.Text);
            var req = (HttpWebRequest) WebRequest.Create(url);
            req.Method = "POST";
            req.AllowAutoRedirect = false;
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)req.GetResponse();
            }
            catch (WebException ex)
            {
                MessageBox.Show("Connection failed. poe.xyz.is returned: " + ex.Message);
                return false;    
            }

            if (response.Headers["Location"].ToLower() != url.ToString().ToLower())
            {
                MessageBox.Show("Connection failed. Wrong poe.xyz.is online link.");
                return false;    
            }
            req.Abort(); // Abort to be sure requests don't hang

            ShopIndexerStatusLabel.Text = DateTime.Now.ToShortTimeString() + " (Next: " + DateTime.Now.AddHours(1).ToShortTimeString() + ")";
            poeStatusTimer.Interval = 60 * 60 * 1000; // Set timer to only check PoE status every 1 hour
            return true;
        }

        bool IsPoeRunning()
        {
            return Process.GetProcesses().Any(
                process => process.ProcessName.ToLower().Contains("pathofexile"));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = Properties.Settings.Default.OnlineLink;
            UpdateStatus();
            poeStatusTimer.Tick += UpdateStatus;
            poeStatusTimer.Start();
            
        }

        private void ActivateButton_Click(object sender, EventArgs e)
        {
            if (!activated)
            {
                if (!IsPoeRunning())
                {
                    MessageBox.Show("Path of Exile is not running.");
                    return;
                }
                try
                {
                    new Uri(textBox1.Text);
                    textBox1.Enabled = false;
                }
                catch (Exception)
                {
                    textBox1.BackColor = Color.LightPink;
                    return;
                }

                ShopIndexerStatusLabel.Text = "Connecting...";

                var couldSendOnlineUpdate = SendUpdateToXyz();
                if (!couldSendOnlineUpdate)
                {
                    activated = false;
                    ShopIndexerStatusLabel.Text = "N/A";
                    ActivateButton.Text = "Activate";
                    textBox1.Enabled = true;
                    return;
                }
                
                Properties.Settings.Default.OnlineLink = textBox1.Text;
                Properties.Settings.Default.Save();
            }
            else
            {
                textBox1.Enabled = true;
                ShopIndexerStatusLabel.Text = "N/A";
            }

            activated = !activated;           
            textBox1.ResetBackColor();
            
            ActivateButton.Text = !activated ? "Activate" : "Deactive";
        }
    }
}
