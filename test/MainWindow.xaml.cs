using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Threading;

namespace test
{

    public partial class MainWindow : Window
    {

        private Boolean IsLogin = false;

        private static readonly HttpClient client = new HttpClient();

        private string base_url = "https://uxcandy.com/~shapoval/test-task-backend/v2";
        private string developer_key = "Nikolay";

        private string? Token = null;

        private string[] sortFilds = new string[] { "Имя", "Email", "Статус" };
        private string[] sortDir = new string[] { "Asc", "Deac" };

        private int Pages = 0;
        private int CurrntPage = 1; // пагинация с 1
        private double MaxRow = 3.0;


        private Timer _timer;

        private Login Tab_Login = new Login();
        private List Tab_List = new List();



        public MainWindow()
        {
            InitializeComponent();

            SortBy.ItemsSource = sortFilds;
            SortBy.SelectedIndex = 0;

            SortDir.ItemsSource = sortDir;
            SortDir.SelectedIndex = 0;

            SortBy.SelectionChanged += OnSelected;
            SortDir.SelectionChanged += OnSelected;
            
            Tab_Login.BtnLogin.Click += OnLogin;

            Tab_List.BtnCreate.Click += OnCreate;
            Tab_List.BtnPrev.Click += OnPrev;
            Tab_List.BtnNext.Click += OnNext;

            DataContext = this;

            Frame.Content = Tab_List;

            UpdateTasks();
        }


        void UpdateTask(object sender, PropertyChangedEventArgs e)
        {
            Task item = (Task)sender;

            if(e.PropertyName == "Text")
            {
                item.IsAdm = true;
            }

            UpdateTask(item);
        }

        async private void UpdateTask(Task item)
        {
            var values = new Dictionary<string, string>{
                { "token", Token },
                { "text", item.Text },
                { "status", item.Status.ToString() }
            };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync(String.Format(base_url + "/edit/{0}?developer=" + developer_key, item.Id), content);
            var responseString = await response.Content.ReadAsStringAsync();

            Response_EditTask result = JsonSerializer.Deserialize<Response_EditTask>(responseString);

            if (result.Status == "ok")
            {
                ShowStasut(false, "Успешно отредактировано");
            }
            else
            {
                ShowStasut(true, result.Message.Token);
            }

            UpdateTasks();
        }



        async private void UpdateTasks()
        {
            Tab_List.BtnPrev.IsEnabled = Tab_List.BtnNext.IsEnabled = false;

            var param = String.Format("/?developer={0}", developer_key);

            string sort = "";
            switch (SortBy.SelectedIndex)
            {
                case 0: sort = "username"; break;
                case 1: sort = "email"; break;
                case 2: sort = "status"; break;
            }
            param += "&sort_field=" + sort;

            param += "&sort_direction=" + (SortDir.SelectedIndex == 0 ? "asc" : "desc");
            param += "&page=" + CurrntPage;

            var response = await client.GetStringAsync(base_url + param);
            Response_Tasks result = JsonSerializer.Deserialize<Response_Tasks>(response);

            var ItemsSource = result.Message.Tasks;
            Pages = (int)Math.Ceiling(int.Parse(result.Message.TotalTaskCount) / MaxRow);

            Tab_List.TaskList.ItemsSource = null;
            Tab_List.TaskList.ItemsSource = ItemsSource;

            ItemsSource.ForEach(t => t.PropertyChanged += UpdateTask );

            UpdatePages();
        }




        private void UpdatePages() {
            if (Pages > 1)
            {

                if (CurrntPage > Pages)
                {
                    CurrntPage = 1;
                }

                Tab_List.BtnPrev.IsEnabled = CurrntPage != 1;
                Tab_List.BtnNext.IsEnabled = CurrntPage != Pages;

                Tab_List.PageLabel.Content = CurrntPage + " - " + Pages;

                Tab_List.PagesControl.Visibility = Visibility.Visible;
            }
            else
            {
                Tab_List.PagesControl.Visibility = Visibility.Hidden;
            }
        }
        private void SetIsLogin(Boolean b)
        {
            IsLogin = b;

            Tab_List.TaskList.IsReadOnly = !b;
        }




        private void OnAuth(object sender, RoutedEventArgs e)
        {
            if(Frame.Content == Tab_Login)
            {
                Frame.Content = Tab_List;
                BtnAuth.Content = "Авторизация";
            }
            else
            {
                if (IsLogin)
                {
                    SetIsLogin(false);
                    BtnAuth.Content = "Авторизация";
                }
                else
                {
                    Frame.Content = Tab_Login;
                    BtnAuth.Content = "К списку задач";
                }
            }
        }

 
        async private void GetToken()
         {
            Tab_Login.status.Content = "";

            var values = new Dictionary<string, string>{
                { "username", Tab_Login.username.Text },
                { "password", Tab_Login.password.Text }
            };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync(base_url + "/login?developer=" + developer_key, content);
            var responseString = await response.Content.ReadAsStringAsync();

            Response_Token result = JsonSerializer.Deserialize<Response_Token>(responseString);
               
            if(result.Status == "ok")
            {
                SetIsLogin(true);
                Token = result.Message.Token;
                Frame.Content = Tab_List;
                BtnAuth.Content = "Выйти";
            }
            else
            {
                Tab_Login.status.Content = result.Message.Password.Equals(null) ? result.Message.Username : result.Message.Password;
            }
         }

        async private void CreateTask()
        {
            Tab_List.CreateStatus.Content = "";

            var values = new Dictionary<string, string>{
                { "username", Tab_List.NewUserName.Text },
                { "email", Tab_List.NewEmail.Text },
                { "text", Tab_List.NewText.Text }
            };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync(base_url + "/create?developer=" + developer_key, content);
            var responseString = await response.Content.ReadAsStringAsync();

            Response_Create result = JsonSerializer.Deserialize<Response_Create>(responseString);

            if (result.Status == "ok")
            {
                ShowStasut(false, "Успешно добавлено");

                UpdateTasks();
            }
            else
            {
                var ms = result.Message;

                var a = !String.IsNullOrEmpty(ms.Email) ? result.Message.Email :
                                                          String.IsNullOrEmpty(ms.Text) ? ms.UserName : result.Message.Text;
                ShowStasut(true, a);
            }
        }


        private void ShowStasut (bool isAlarm, string msg)
        {
            Tab_List.CreateStatus.Foreground = new System.Windows.Media.SolidColorBrush(isAlarm ? Colors.Red : Colors.Green);
            Tab_List.CreateStatus.Content = msg;

            if (_timer != null)
            {
                _timer.Dispose();
            }
            
            _timer = new Timer( state => {
                Dispatcher.BeginInvoke(new ThreadStart(delegate { Tab_List.CreateStatus.Content = ""; }));

                _timer.Dispose();
                _timer = null;
            }, null, 3000, 0);
        }



        private void OnLogin(object sender, RoutedEventArgs e)
        {
            GetToken();
        }
        private void OnCreate(object sender, RoutedEventArgs e)
        {
            CreateTask();
        }
        private void OnPrev(object sender, RoutedEventArgs e)
        {
            --CurrntPage;
            UpdateTasks();
        }
        private void OnNext(object sender, RoutedEventArgs e)
        {
            ++CurrntPage;
            UpdateTasks();
        }
        private void OnSelected(object sender, RoutedEventArgs e)
        {
            UpdateTasks();
        }

    }







    public class Response_Tasks
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("message")]
        public Response_Message Message { get; set; }



        public class Response_Message
        {
            [JsonPropertyName("tasks")]
            public List<Task> Tasks { get; set; }

            [JsonPropertyName("total_task_count")]
            public string TotalTaskCount { get; set; }
        }
    }

    public class Response_EditTask
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("message")]
        public Response_Message Message { get; set; }



        public class Response_Message
        {
            [JsonPropertyName("token")]
            public string Token { get; set; }
        }
    }

    public class Response_Token
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("message")]
        public Response_Message Message { get; set; }



        public class Response_Message
        {
            [JsonPropertyName("token")]
            public string Token { get; set; }

            [JsonPropertyName("username")]
            public string Username { get; set; }

            [JsonPropertyName("password")]
            public string Password { get; set; }
        }
    }


    public class Response_Create
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("message")]
        public Response_Message Message { get; set; }

        public class Response_Message
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("username")]
            public string UserName { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; }

            [JsonPropertyName("text")]
            public string Text { get; set; }

            [JsonPropertyName("status")]
            public int Status { get; set; }
        }
    }

    public class Task : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, e);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }




        [JsonConstructor]
        public Task(int id, string username, string email, string text, int status)
        {
            this.Id = id;
            this.UserName = username;
            this.Email = email;
            this.Text = text;
            this.Status = status;
        }

        public Task(Response_Create.Response_Message ms)
        {
            this.Id = ms.Id;
            this.UserName = ms.UserName;
            this.Email = ms.Email;
            this.Text = ms.Text;
            this.Status = ms.Status;
        }


        public void SetIsAdm(bool IsAdm)
        {
            this.Status = (this.IsReady ? 10 : 0) + (IsAdm ? 1 : 0);
        }
        public void SetIsReady(bool IsReady)
        {
            this.Status = (IsReady ? 10 : 0) + (this.IsAdm ? 1 : 0);
        }





        private string text;


        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("username")]
        public string UserName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("text")]
        public string Text {
            get
            {
                return this.text;
            }
            set
            {
                this.text = value;
                OnPropertyChanged("Text");
            }
        }

        [JsonPropertyName("status")]
        public int Status { get; set; }


        public Boolean IsAdm {
            get
            {
                return Status % 10 == 1;
            }
            set
            {
                Status = (Status / 10 == 1 ? 10 : 0) + (value ? 1 : 0);
            }
        }

        public Boolean IsReady
        {
            get
            {
                return Status / 10 == 1;
            }
            set
            {
                Status = (Status % 10 == 1 ? 1 : 0) + (value ? 10 : 0);
                OnPropertyChanged("IsReady");
            }
        }

        enum TaskStatus
        {
            notReady = 0,
            notReady_adm = 1,
            ready = 10,
            ready_adm = 11
        }
    }
}
