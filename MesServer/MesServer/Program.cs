using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MessengerServer
{
    public class ClientInfo
    {
        public string Name { get; set; }
        public TcpClient Client { get; set; }
        public string UniqueId { get; set; }
    }

    public class ServerForm : Form
    {
        private Button btnStart;
        private Button btnStop;
        private TextBox txtLog;
        private ListBox lstClients;
        private Label lblStatus;
        private TextBox txtMessage;
        private Button btnSend;
        private Label lblClientsCount;
        private ComboBox cmbReceiver;
        private Label lblReceiver;

        private TcpListener server;
        private List<ClientInfo> clients = new List<ClientInfo>();
        private bool isRunning = false;
        private int clientCounter = 1;
        private object lockObj = new object();

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ServerForm());
        }

        public ServerForm()
        {
            InitializeForm();
            CreateControls();
        }

        private void InitializeForm()
        {
            this.Text = "Сервер";
            this.Size = new Size(850, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
        }

        private void CreateControls()
        {
            Panel topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 50;
            topPanel.BackColor = Color.SteelBlue;

            btnStart = new Button();
            btnStart.Text = "Запустить";
            btnStart.Size = new Size(120, 30);
            btnStart.Location = new Point(10, 10);
            btnStart.BackColor = Color.Green;
            btnStart.ForeColor = Color.White;
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.Click += BtnStart_Click;

            btnStop = new Button();
            btnStop.Text = "Остановить";
            btnStop.Size = new Size(120, 30);
            btnStop.Location = new Point(140, 10);
            btnStop.BackColor = Color.Red;
            btnStop.ForeColor = Color.White;
            btnStop.FlatStyle = FlatStyle.Flat;
            btnStop.Enabled = false;
            btnStop.Click += BtnStop_Click;

            lblStatus = new Label();
            lblStatus.Text = "Сервер остановлен";
            lblStatus.Size = new Size(200, 30);
            lblStatus.Location = new Point(270, 10);
            lblStatus.ForeColor = Color.White;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            topPanel.Controls.Add(btnStart);
            topPanel.Controls.Add(btnStop);
            topPanel.Controls.Add(lblStatus);

            Panel mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Padding = new Padding(10);

            GroupBox logGroup = new GroupBox();
            logGroup.Text = "Лог сервера";
            logGroup.Size = new Size(500, 350);
            logGroup.Location = new Point(0, 0);

            txtLog = new TextBox();
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Dock = DockStyle.Fill;
            txtLog.Font = new Font("Consolas", 9);
            txtLog.BackColor = Color.Black;
            txtLog.ForeColor = Color.Lime;

            logGroup.Controls.Add(txtLog);

            GroupBox clientsGroup = new GroupBox();
            clientsGroup.Text = "Подключенные клиенты";
            clientsGroup.Size = new Size(300, 350);
            clientsGroup.Location = new Point(510, 0);

            lstClients = new ListBox();
            lstClients.Dock = DockStyle.Fill;
            lstClients.Font = new Font("Segoe UI", 9);
            lstClients.SelectionMode = SelectionMode.MultiSimple;
            lstClients.SelectedIndexChanged += LstClients_SelectedIndexChanged;

            clientsGroup.Controls.Add(lstClients);

            lblClientsCount = new Label();
            lblClientsCount.Text = "Клиентов: 0";
            lblClientsCount.Size = new Size(100, 20);
            lblClientsCount.Location = new Point(650, 360);
            lblClientsCount.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblClientsCount.ForeColor = Color.Blue;

            Panel sendPanel = new Panel();
            sendPanel.Size = new Size(820, 100);
            sendPanel.Location = new Point(0, 370);
            sendPanel.BackColor = Color.LightGray;
            sendPanel.BorderStyle = BorderStyle.FixedSingle;

            lblReceiver = new Label();
            lblReceiver.Text = "Кому:";
            lblReceiver.Size = new Size(70, 20);
            lblReceiver.Location = new Point(10, 15);
            lblReceiver.Font = new Font("Segoe UI", 9);

            cmbReceiver = new ComboBox();
            cmbReceiver.Size = new Size(200, 25);
            cmbReceiver.Location = new Point(85, 12);
            cmbReceiver.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbReceiver.Items.Add("Всем");

            txtMessage = new TextBox();
            txtMessage.Size = new Size(600, 25);
            txtMessage.Location = new Point(10, 45);
            txtMessage.Font = new Font("Segoe UI", 9);
            txtMessage.Multiline = false;

            btnSend = new Button();
            btnSend.Text = "Отправить";
            btnSend.Size = new Size(150, 25);
            btnSend.Location = new Point(620, 45);
            btnSend.BackColor = Color.SteelBlue;
            btnSend.ForeColor = Color.White;
            btnSend.Click += BtnSend_Click;

            sendPanel.Controls.Add(lblReceiver);
            sendPanel.Controls.Add(cmbReceiver);
            sendPanel.Controls.Add(txtMessage);
            sendPanel.Controls.Add(btnSend);

            mainPanel.Controls.Add(logGroup);
            mainPanel.Controls.Add(clientsGroup);
            mainPanel.Controls.Add(lblClientsCount);
            mainPanel.Controls.Add(sendPanel);

            this.Controls.Add(topPanel);
            this.Controls.Add(mainPanel);
        }

        private void LstClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateReceiverComboBox();
        }

        private void UpdateReceiverComboBox()
        {
            cmbReceiver.Items.Clear();
            cmbReceiver.Items.Add("Всем");

            if (lstClients.SelectedItems.Count > 0)
            {
                foreach (var selectedItem in lstClients.SelectedItems)
                {
                    cmbReceiver.Items.Add(selectedItem.ToString());
                }
            }
            else if (lstClients.Items.Count > 0)
            {
                foreach (var item in lstClients.Items)
                {
                    cmbReceiver.Items.Add(item.ToString());
                }
            }

            if (cmbReceiver.Items.Count > 0)
            {
                cmbReceiver.SelectedIndex = 0;
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                server = new TcpListener(IPAddress.Any, 8888);
                server.Start();
                isRunning = true;

                btnStart.Enabled = false;
                btnStop.Enabled = true;
                lblStatus.Text = "Сервер запущен (порт 8888)";
                lblStatus.ForeColor = Color.LightGreen;

                LogMessage("Сервер запущен");
                LogMessage("IP адрес: " + GetLocalIP());
                LogMessage("Порт: 8888");
                LogMessage("Ожидание подключений...");
                LogMessage("");

                Thread acceptThread = new Thread(AcceptClients);
                acceptThread.IsBackground = true;
                acceptThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка запуска сервера: " + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            StopServer();
        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                string receiver = cmbReceiver.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(receiver))
                {
                    MessageBox.Show("Выберите получателя", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string message = "[Сервер]: " + txtMessage.Text;

                if (receiver == "Всем")
                {
                    Broadcast(message);
                    LogMessage("Отправлено всем: " + txtMessage.Text);
                }
                else
                {
                    SendToSpecificClient(receiver, message);
                    LogMessage("Отправлено клиенту " + receiver + ": " + txtMessage.Text);
                }

                txtMessage.Clear();
                txtMessage.Focus();
            }
        }

        private void AcceptClients()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient client = server.AcceptTcpClient();

                    string clientName = "Клиент " + clientCounter++;
                    string uniqueId = Guid.NewGuid().ToString();

                    var clientInfo = new ClientInfo
                    {
                        Name = clientName,
                        Client = client,
                        UniqueId = uniqueId
                    };

                    lock (lockObj)
                    {
                        clients.Add(clientInfo);
                    }

                    this.Invoke(new Action(() =>
                    {
                        lstClients.Items.Add(clientName);
                        UpdateClientsCount();
                        UpdateReceiverComboBox();
                        LogMessage("Подключился: " + clientName);
                    }));

                    SendToClient(clientInfo, "Добро пожаловать, " + clientName + "!");
                    SendToClient(clientInfo, "Ваш ID: " + uniqueId);

                    Thread clientThread = new Thread(() => HandleClient(clientInfo));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
                catch
                {
                    break;
                }
            }
        }

        private void HandleClient(ClientInfo clientInfo)
        {
            NetworkStream stream = clientInfo.Client.GetStream();
            byte[] buffer = new byte[4096];

            try
            {
                while (isRunning && clientInfo.Client.Connected)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (message.StartsWith("/to:"))
                    {
                        string[] parts = message.Split(':');
                        if (parts.Length >= 3)
                        {
                            string targetClientName = parts[1];
                            string actualMessage = parts[2];

                            this.Invoke(new Action(() =>
                            {
                                LogMessage(clientInfo.Name + " -> " + targetClientName + ": " + actualMessage);
                            }));

                            SendToSpecificClient(targetClientName, clientInfo.Name + ": " + actualMessage);
                        }
                    }
                    else
                    {
                        this.Invoke(new Action(() =>
                        {
                            LogMessage(clientInfo.Name + ": " + message);
                        }));

                        BroadcastFromClient(clientInfo.Name, message);
                    }
                }
            }
            catch
            {
            }
            finally
            {
                lock (lockObj)
                {
                    clients.Remove(clientInfo);
                }

                this.Invoke(new Action(() =>
                {
                    lstClients.Items.Remove(clientInfo.Name);
                    UpdateClientsCount();
                    UpdateReceiverComboBox();
                    LogMessage("Отключился: " + clientInfo.Name);
                }));

                Broadcast(clientInfo.Name + " вышел из чата");
                clientInfo.Client.Close();
            }
        }

        private void Broadcast(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);

            lock (lockObj)
            {
                foreach (var clientInfo in clients)
                {
                    try
                    {
                        if (clientInfo.Client.Connected)
                        {
                            NetworkStream stream = clientInfo.Client.GetStream();
                            stream.Write(data, 0, data.Length);
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void BroadcastFromClient(string senderName, string message)
        {
            string fullMessage = senderName + ": " + message;
            byte[] data = Encoding.UTF8.GetBytes(fullMessage);

            lock (lockObj)
            {
                foreach (var clientInfo in clients)
                {
                    try
                    {
                        if (clientInfo.Client.Connected)
                        {
                            NetworkStream stream = clientInfo.Client.GetStream();
                            stream.Write(data, 0, data.Length);
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void SendToSpecificClient(string clientName, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);

            lock (lockObj)
            {
                var clientInfo = clients.Find(c => c.Name == clientName);
                if (clientInfo != null && clientInfo.Client.Connected)
                {
                    try
                    {
                        NetworkStream stream = clientInfo.Client.GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                    catch
                    {
                        this.Invoke(new Action(() =>
                        {
                            LogMessage("Ошибка отправки клиенту: " + clientName);
                        }));
                    }
                }
                else
                {
                    this.Invoke(new Action(() =>
                    {
                        LogMessage("Клиент не найден или отключен: " + clientName);
                    }));
                }
            }
        }

        private void SendToClient(ClientInfo clientInfo, string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                NetworkStream stream = clientInfo.Client.GetStream();
                stream.Write(data, 0, data.Length);
            }
            catch
            {
            }
        }

        private void LogMessage(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + "\r\n");
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private void UpdateClientsCount()
        {
            lblClientsCount.Text = "Клиентов: " + lstClients.Items.Count;
        }

        private void StopServer()
        {
            isRunning = false;

            server?.Stop();

            lock (lockObj)
            {
                foreach (var clientInfo in clients)
                {
                    try { clientInfo.Client.Close(); } catch { }
                }
                clients.Clear();
            }

            btnStart.Enabled = true;
            btnStop.Enabled = false;
            lblStatus.Text = "Сервер остановлен";
            lblStatus.ForeColor = Color.White;

            lstClients.Items.Clear();
            UpdateClientsCount();
            cmbReceiver.Items.Clear();
            cmbReceiver.Items.Add("Всем");

            LogMessage("Сервер остановлен");
        }

        private string GetLocalIP()
        {
            try
            {
                string hostName = Dns.GetHostName();
                IPAddress[] addresses = Dns.GetHostAddresses(hostName);

                foreach (var addr in addresses)
                {
                    if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return addr.ToString();
                    }
                }
            }
            catch { }

            return "127.0.0.1";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (isRunning)
            {
                var result = MessageBox.Show(
                    "Сервер еще работает. Остановить перед выходом?",
                    "Подтверждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    StopServer();
                }
                else
                {
                    e.Cancel = true;
                }
            }

            base.OnFormClosing(e);
        }
    }
}