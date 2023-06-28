using MQTTnet;
using MQTTnet.Client;
using System;
using System.Globalization;
using System.Windows.Forms;
using System.Threading;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using System.Collections.Generic;
using System.Text;

namespace HelloWorld
{
    public partial class MqttTestWindow : Form
    {
        private IMqttClient _mqttClient;
        public MqttTestWindow()
        {
            InitializeComponent();
        }

        private MqttClientOptions GetClientOptions()
        {
            string ipValue = tbIp.Text.ToString();
            int portValue = int.Parse(tbPort.Text.Trim().ToString());
            if (ipValue == null || portValue == 0)
            {
                PrintLogs("ip端口号不能为空");
                return null;
            }
            string clientId = Guid.NewGuid().ToString().Replace("-", "").ToUpper();
            int secondValue = (int)nudTimeOut.Value;
            TimeSpan timeOut = TimeSpan.FromSeconds(secondValue);
            var options = new MqttClientOptionsBuilder()
             .WithTcpServer(ipValue, portValue) // 设置Mqtt服务器的IP地址和端口号
             .WithClientId(clientId) // 设置一个客户端ID，该ID必须唯一
             .WithCleanSession()
             .WithTimeout(timeOut)
             .Build();
            return options;
        }

        //连接客户端
        private async void GetClientConnect()
        {
            _mqttClient = new MqttFactory().CreateMqttClient();
            var options = GetClientOptions();
            await _mqttClient.ConnectAsync(options, CancellationToken.None).ContinueWith(task =>
           {
               if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
               {
                   PrintLogs("已连接到Mqtt服务器！");
               }
               else
               {
                   PrintLogs("连接Mqtt服务器失败！");
               }
           });
        }

        //断开客户端连接
        public async void GetDisconnected()
        {
            await _mqttClient.DisconnectAsync();
            PrintLogs("Mqtt客户端断开连接");
        }

        //Topic订阅
        public async void SubscribeTopic()
        {
            string topic1 = txTopic1.Text.ToString().Trim();
            string topic2 = txTopic2.Text.ToString().Trim();


            var topicFilter1 = new MqttTopicFilterBuilder().WithTopic(topic1).WithQualityOfServiceLevel(GetQos()).Build();
            var topicFilter2 = new MqttTopicFilterBuilder().WithTopic(topic2).WithQualityOfServiceLevel(GetQos()).Build();
            var topicFilters = new List<MqttTopicFilter> { topicFilter1, topicFilter2 };

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder();
            foreach (MqttTopicFilter topicFilter in topicFilters)
            {
                subscribeOptions.WithTopicFilter(topicFilter);
            }
            var options = subscribeOptions.
             WithTopicFilter(topicFilter1).
             WithTopicFilter(topicFilter2).Build();



            await _mqttClient.SubscribeAsync(options);
            
            foreach (var topicFilter in topicFilters)
            {
                PrintLogs("Topic:" + topicFilter.Topic.ToString() + "订阅成功");
            }

            ReceiveMsg();

        }
        //接收订阅的消息
        private void ReceiveMsg()
        {
            _mqttClient.ApplicationMessageReceivedAsync += delegate (MqttApplicationMessageReceivedEventArgs args) {
                if (args.ApplicationMessage.Payload is null)
                {
                    PrintLogs("Empty message");
                }
                else
                {
                    PrintLogs(Encoding.UTF8.GetString(args.ApplicationMessage.Payload));
                }

                return System.Threading.Tasks.Task.CompletedTask;
            };

        }

        private MqttQualityOfServiceLevel GetQos() 
        {
            var qosValue = new MqttQualityOfServiceLevel();
            string value = ""; // 存储 RadioButton 的值
            foreach (Control control in groupBox2.Controls)
            {
                if (control is RadioButton)
                {
                    RadioButton radioButton = (RadioButton)control;
                    if (radioButton.Checked)
                    {
                        value = radioButton.Text;
                        break;
                    }
                }
            }
            switch (value)
            {
                case "0":
                    qosValue = MqttQualityOfServiceLevel.AtMostOnce; 
                    break;
                case "1":
                    qosValue = MqttQualityOfServiceLevel.AtLeastOnce;
                    break;
                case "2":
                    qosValue = MqttQualityOfServiceLevel.ExactlyOnce;
                    break;
            }
            return qosValue;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //string time = DateTime.Now.ToString("HH:mm:ss.fff",new CultureInfo("zh-cn"));
            //tbLog.AppendText(time+"\r\n");
            GetClientConnect();
        }

        private void btDisconnect_Click(object sender, EventArgs e)
        {
            GetDisconnected();
        }

        private void btClearLog_Click(object sender, EventArgs e)
        {
            tbLog.Clear();
        }

        private void PrintLogs(string content)
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff", new CultureInfo("zh-cn"));
            tbLog.Invoke(new Action(() =>
            {
                tbLog.AppendText(time + ":  " + content + "\r\n");
            }));
            
        }

        private void btSubscripte_Click(object sender, EventArgs e)
        {
            SubscribeTopic();
        }
    }
}
