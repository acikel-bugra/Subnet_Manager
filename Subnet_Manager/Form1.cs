using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.NetworkInformation;// mac adresleri lazm olcak ağda islem yapacağız
using System.Threading;//eş zamanlı işlem yapmak için
using System.Net;// ping vs internet gerektiren kodlarımız için
using System.Management;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;

namespace Subnet_Manager
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            lblStatus.ForeColor = System.Drawing.Color.Red;
            //boş olanlar kırmızı renk alacak sistemsel olarak
            lblStatus.Text = "BOŞ";
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        Thread myThread = null;

        public void scan(string subnet)
        {
            Ping myPing;
            PingReply reply;
            IPAddress addr;
            IPHostEntry host;
            //gerekli değişkenlerimizi aldık
            progressBar1.Maximum = 254;
            progressBar1.Value = 0;
            //ipler 0dan başlar 255 kadardır bu nedenle progressbar değerlerini 
            //0 254 arasında değiştirdim
            listVAddr.Items.Clear();
            //listbox temizliyoruz her taramada

            for (int i = 1; i < 255; i++)
            {
                //programımızda alt alan adı yazdırıyoruz bu nedir örneğin 192.168.1
                //ancak klasik bi ip 192.168.1 ile 192.168.1.255 arasındadır
                //bu sebeple döngü ile her ağa ping atarak bu alandan geri dönüş olup
                //olmadığına bakacağız for 1 ile 255 arası çalıştırdım
                string subnetn = "." + i.ToString();
                //örnek 192.168.1 değerine . ekledik daha sonra i değerini verdim
                //ilk dönmesinde değer 192.168.1.1 oldu
                myPing = new Ping();
                reply = myPing.Send(subnet + subnetn, 900);
                //daha sonra oluşturdumuz ipye ping attim

                lblStatus.ForeColor = System.Drawing.Color.Green;
                lblStatus.Text = "Taranıyor: " + subnet + subnetn;
                //taranıyor yazısı ve ip gösterdik lbl rengi bu sürede yesil

                if (reply.Status == IPStatus.Success)
                {//eğer olumlu dönerse listboxa eklicek hostu ve ipimizi kaydedecek
                    try
                    {
                        addr = IPAddress.Parse(subnet + subnetn);
                        host = Dns.GetHostEntry(addr);

                        listVAddr.Items.Add(new ListViewItem(new String[] { subnet + subnetn, host.HostName, "Hazır" }));
                    }
                    catch { listBox1.Items.Add("" + subnet + subnetn + " için ana makine adı alınamadı"); }
                }
                progressBar1.Value += 1;
            }
            //dönmezse bişe yapmasına gerek yok işlemlere devam
            cmdScan.Enabled = true;
            cmdStop.Enabled = false;
            txtIP.Enabled = true;
            lblStatus.Text = "Bitti!";
            int count = listVAddr.Items.Count;
            MessageBox.Show("Tarama Tamamlandı!\n " + count.ToString() + " Host Bulundu.", "Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void query(string host)
        {

            string temp = null;
            string[] _searchClass = { "Win32_ComputerSystem", "Win32_OperatingSystem", "Win32_BaseBoard", "Win32_BIOS" };
            string[] param = { "UserName", "Caption", "Product", "Description" };

            lblStatus.ForeColor = System.Drawing.Color.Green;

            for (int i = 0; i <= _searchClass.Length - 1; i++)
            {
                lblStatus.Text = "Bilgi Alınıyor.";
                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("\\\\" + host + "\\root\\CIMV2", "SELECT *FROM " + _searchClass[i]);
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        lblStatus.Text = "Bilgi Alınıyor. .";

                        temp += obj.GetPropertyValue(param[i]).ToString() + "\n";
                        if (i == _searchClass.Length - 1)
                        {
                            lblStatus.Text = "Tamamlandı!";
                            MessageBox.Show(temp, "Host Bilgisi: " + host, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            break;
                        }
                        lblStatus.Text = "Bilgi Alınıyor. . .";
                    }
                }
                catch (Exception ex) { MessageBox.Show("WMI sorgusunda hata.\n\n" + ex.ToString(), "HATA", MessageBoxButtons.OK, MessageBoxIcon.Error); break; }
            }
        }

        public void controlSys(string host, int flags)
        {
            #region 
            /*
             *Flags:
             *  0 (0x0)Log Off
             *  4 (0x4)Forced Log Off (0 + 4)
             *  1 (0x1)Shutdown
             *  5 (0x5)Forced Shutdown (1 + 4)
             *  2 (0x2)Reboot
             *  6 (0x6)Forced Reboot (2 + 4)
             *  8 (0x8)Power Off
             *  12 (0xC)Forced Power Off (8 + 4)
             */
            #endregion

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("\\\\" + host + "\\root\\CIMV2", "SELECT *FROM Win32_OperatingSystem");

                foreach (ManagementObject obj in searcher.Get())
                {
                    ManagementBaseObject inParams = obj.GetMethodParameters("Win32Shutdown");

                    inParams["Flags"] = flags;

                    ManagementBaseObject outParams = obj.InvokeMethod("Win32Shutdown", inParams, null);
                }
            }
            catch (ManagementException manex) { MessageBox.Show("Error:\n\n" + manex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            catch (UnauthorizedAccessException unaccex) { MessageBox.Show("Yetkili?\n\n" + unaccex.ToString(), "HATA", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void cmdScan_Click(object sender, EventArgs e)
        {
            if (txtIP.Text == string.Empty)
            {
                MessageBox.Show("IP Adresi Girilmedi.", "HATA", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                myThread = new Thread(() => scan(txtIP.Text));
                myThread.Start();

                if (myThread.IsAlive == true)
                {
                    cmdStop.Enabled = true;
                    cmdScan.Enabled = false;
                    txtIP.Enabled = false;
                }
            }
        }

        private void cmdStop_Click(object sender, EventArgs e)
        {
            myThread.Suspend();
            cmdScan.Enabled = true;
            cmdStop.Enabled = false;
            txtIP.Enabled = true;
            lblStatus.ForeColor = System.Drawing.Color.Red;
            lblStatus.Text = "Boş";
        }

        private void listVAddr_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (listVAddr.FocusedItem.Bounds.Contains(e.Location) == true)
                {
                    conMenuStripIP.Show(Cursor.Position);
                }
            }
        }

        private void cmdScan_Click_1(object sender, EventArgs e)
        {
            if (txtIP.Text == string.Empty)
            {
                listBox1.Items.Add("IP Adresi Girilmedi.");
            }
            else
            {
                myThread = new Thread(() => scan(txtIP.Text));
                myThread.Start();

                if (myThread.IsAlive == true)
                {
                    cmdStop.Enabled = true;
                    cmdScan.Enabled = false;
                    txtIP.Enabled = false;
                }
            }
        }

        private void cmdStop_Click_1(object sender, EventArgs e)
        {
            myThread.Suspend();
            cmdScan.Enabled = true;
            cmdStop.Enabled = false;
            txtIP.Enabled = true;
            lblStatus.ForeColor = System.Drawing.Color.Red;
            lblStatus.Text = "BOŞ";
        }

        private void showInfoToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            try
            {
                string host = listVAddr.SelectedItems[0].Text.ToString();
                Thread qThread = new Thread(() => query(host));
                qThread.Start();
            }
            catch (Exception hata)
            {

                MessageBox.Show("Hata Aldınız IP adresi seçtiğinize emin olun." + hata);
            }

        }

        private void kapatToolStripMenuItem_Click(object sender, EventArgs e)
        {

            string host = listVAddr.SelectedItems[0].Text.ToString();
            controlSys(host, 5);
        }

        private void yenidenBaşlatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string host = listVAddr.SelectedItems[0].Text.ToString();
            controlSys(host, 6);
        }

        private void gücüKapatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string host = listVAddr.SelectedItems[0].Text.ToString();
            controlSys(host, 12);
        }

        private void yardımToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Program birkaç şekilde kullanılabilir ilk olarak LAN daki diğer bilgisayarları bulup yetkiniz dahilinde bunlar üzerinde kapat yeniden başlat sistem bilgisi öğrenebilirsizin ikinci olaraktan ise alt ağlarında hangi siteler bulunduğunu merak ettiğiniz sunucuların alt ağlarına bakabilirsiniz.");
        }

        private void hakkımdaToolStripMenuItem_Click(object sender, EventArgs e)
        {


        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.cimer.gov.tr");
        }
    }
}
