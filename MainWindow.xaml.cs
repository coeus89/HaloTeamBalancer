using Halo_Team_Balancer.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Excel = Microsoft.Office.Interop.Excel;


namespace Halo_Team_Balancer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, Player> playerDict;
        Dictionary<string, int> rankDict;
        Dictionary<string, Player> selectedPlayerDict;
        Task loadPlayers;
        Task loadRanks;
        delegate void DelMessageBox(string message);
        DelMessageBox delMsg = null;
        delegate void DelPlayers(List<Player> players);
        DelPlayers delShowPlayers = null;
        DelPlayers delShowSelectedPlayers = null;
        delegate void DelUpdateResults(string txt);
        DelUpdateResults delUpdateResultsTextBox = null;
        delegate void DelUpdateRB(List<Player> Blue, List<Player> Red);
        DelUpdateRB delUpdateRBTextBox = null;
        delegate (string, string) getTxtBoxValues();
        Action delClrTxt;

        public MainWindow()
        {
            InitializeComponent();
            playerDict = new Dictionary<string, Player>();
            rankDict = new Dictionary<string, int>();
            loadPlayers = Task.Run(() => LoadPlayersAsync(playerDict));
            loadRanks = Task.Run(() => LoadRanksAsync(rankDict));
            delShowPlayers = new DelPlayers(UpdatePlayersListBox);
            delUpdateResultsTextBox = new DelUpdateResults(UpdateResultTextBox);
            selectedPlayerDict = new Dictionary<string, Player>();
            delShowSelectedPlayers = new DelPlayers(UpdateSortListBox);
            delMsg = new DelMessageBox(ShowMessageBox);
            delUpdateRBTextBox = new DelUpdateRB(UpdateRBListBoxes);
            delClrTxt = new Action(clear_Teams);
        }



        private async void LoadPlayersAsync(Dictionary<string, Player> pDict)
        {
            pDict.Clear();
            string folder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            //string folder = "D:\\Projects\\Halo_Team_Balancer";
            folder += "\\settings\\Players.csv";
            using (var reader = new StreamReader(folder))
            {
                // Skip the header row
                await reader.ReadLineAsync();

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var values = line.Split(',');

                    Player player = new Player(values[0].ToLower(), Int32.Parse(values[1]));
                    pDict.Add(player._name, player);
                }
                reader.Close();
            }
            this.Dispatcher.Invoke(delShowPlayers, pDict.Values.ToList<Player>());
        }

        private async Task LoadRanksAsync(Dictionary<string, int> rDict)
        {
            string folder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            folder += "\\settings\\Ranks.csv";
            using (var reader = new StreamReader(folder))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var values = line.Split();

                    rDict.Add(values[0], Int32.Parse(values[1]));

                }
                reader.Close();
            }
        }

        private async void SeparateTeamsButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (!loadPlayers.IsCompletedSuccessfully)
            {
                throw new TaskSchedulerException("Not Finished Loading Players");
            }

            // Get input list of numbers from text box
            List<Player> players = selectedPlayerDict.Values.ToList();
            

            // Disable the button and show a "working" message
            SeparateTeamsButton.IsEnabled = false;
            this.Dispatcher.Invoke(delUpdateResultsTextBox, "Working...");
            if (players.Count < 2)
            {
                throw new ApplicationException("You need to have at lease two players " +
                    "listed in order to sort them into teams.");
            }

            try
            {
                // Call the separate lists method on a separate thread
                var (list1, list2) = await Task.Run(() => SeparateLists(players));

                // Calculate the averages of the two lists
                double average1 = (from p in list1 select p._csr).ToList<int>().Average();
                double average2 = (from p in list2 select p._csr).ToList<int>().Average();
                double totalAvg = (from p in players select p._csr).ToList<int>().Average();

                // Display the two lists and their averages in the result text box
                string teamtext = $"Blue Team ({list1.Count}): (average csr: {average1:F2})\n" +
                                      $"Red Team ({list2.Count}): (average csr: {average2:F2})";
                await this.Dispatcher.BeginInvoke(delUpdateResultsTextBox, teamtext);
                await this.Dispatcher.BeginInvoke(delUpdateRBTextBox, list1, list2);
                
            
            }
            catch (TaskSchedulerException tex)
            {
                this.Dispatcher.Invoke(delUpdateResultsTextBox, tex.Message);
                //ResultTextBox.Text = tex.Message;
            }
            catch (ApplicationException aex)
            {
                this.Dispatcher.Invoke(delUpdateResultsTextBox, aex.Message);
                //ResultTextBox.Text = aex.Message;
            }
            catch (Exception ex)
            {
                // Display any errors in the result text box
                //ResultTextBox.Text = $"Error: {ex.Message}";
                this.Dispatcher.Invoke(delUpdateResultsTextBox, ex.Message);
            }

            // Re-enable the button
            SeparateTeamsButton.IsEnabled = true;
        }

        private static (List<Player> list1, List<Player> list2) SeparateLists(List<Player> players)
        {
            // Calculate the total sum and average of the input list
            double average = (from p in players select p._csr).ToList<int>().Average();

            // Sort the numbers in descending order
            players.Sort((a, b) => b.CompareTo(a));

            // Create two new lists and initialize them with the first element
            var list1 = new List<Player> { players[0] };
            var list2 = new List<Player> { players[1] };

            // Distribute the remaining numbers to the two lists
            for (int i = 2; i < players.Count; i++)
            {
                if ((from p in list1 select p._csr).ToList<int>().Sum() < 
                    (from p in list2 select p._csr).ToList<int>().Sum())
                {
                    list1.Add(players[i]);
                }
                else
                {
                    list2.Add(players[i]);
                }
            }

            // Return the two lists
            return (list1, list2);
        }

        private void UpdateRBListBoxes(List<Player> BlueTeam, List<Player> RedTeam)
        {
            BlueTeamListBox.ItemsSource = BlueTeam;
            RedTeamListBox.ItemsSource = RedTeam;
        }

        private void UpdatePlayersListBox(List<Player> players)
        {
            PlayersListBox.ItemsSource = players;

        }

        private void UpdateResultTextBox(string txt)
        {
            ResultTextBox.Text = txt;

        }

        private void UpdateSortListBox(List<Player> players)
        {
            SelectedPlayerListBox.ItemsSource = players;

        }

        private void ShowMessageBox(string msg)
        {
            MessageBox.Show(msg);
        }

        private async Task<bool> AddToSelectedPlayerListAsync(ListBox listBox)
        {
            try
            {
                if (listBox != null)
                {
                    int index = listBox.SelectedIndex;
                    //MessageBox.Show("You clicked on item " + index);
                    string pname = (playerDict.Keys.ToList<string>())[index];
                    selectedPlayerDict.Add(pname, playerDict[pname]);
                    await this.Dispatcher.BeginInvoke(delShowSelectedPlayers, selectedPlayerDict.Values.ToList<Player>());
                }
            }
            catch (ArgumentException aex)
            {
                this.Dispatcher.Invoke(delMsg, "you've already added that player to the teams list.");
                //MessageBox.Show(@"you've already added that player to the teams list.");
            }
            
            return true;
        }

        private async void PlayersListBox_MouseDoubleClick_Async(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            await AddToSelectedPlayerListAsync(listBox);
        }

        private async void PlayersListBox_KeyDown_Async(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter) 
            {
                ListBox listBox = sender as ListBox;
                await AddToSelectedPlayerListAsync(listBox);
            }
        }

        private void clear_Teams()
        {
            selectedPlayerDict = new Dictionary<string, Player>();
            List<Player> emptyTeam = new List<Player>();
            RedTeamListBox.ItemsSource = emptyTeam;
            BlueTeamListBox.ItemsSource = emptyTeam;
            ResultTextBox.Text = string.Empty;
            SelectedPlayerListBox.ItemsSource = emptyTeam;
        }

        private async void ClearTeamsButton_Click_Async(object sender, RoutedEventArgs e)
        {
            await this.Dispatcher.BeginInvoke(delClrTxt);
        }

        private async void OpenCSVButton_Click_Async(object sender, RoutedEventArgs e)
        {
            string file_path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            //string folder = "D:\\Projects\\Halo_Team_Balancer";
            file_path += "\\settings\\Players.csv";

            await Task.Run(() => 
            {
                try
                {
                    Excel.Application objExcel = new Excel.Application();
                    objExcel.Workbooks.OpenText(file_path, Comma: true);
                    objExcel.Visible = true;
                }
                catch (Exception)
                {
                    //this.Dispatcher.Invoke(delMsg, "Something happened and " +
                    //    "you were unable to open the csv file with excel. " +
                    //    "Ensure you have microsoft excel installed and registered." +
                    //    ex.Message);
                    Process.Start("notepad.exe", file_path);
                }
                finally
                {
                    
                }
            });
            
        }

        private void clearInputTextBox()
        {
            // Clear InputBox.
            InputTextBox.Text = String.Empty;
            InputCSRBox.Text = String.Empty;
        }

        private async void RefreshButton_Click_Async(object sender, RoutedEventArgs e)
        {
            loadPlayers = Task.Run(() => LoadPlayersAsync(playerDict));
            await loadPlayers;
        }

        private void ShowAddPlayerTxtBox(object sender, RoutedEventArgs e)
        {
            // CoolButton Clicked! Let's show our InputBox.
            InputBox.Visibility = System.Windows.Visibility.Visible;
        }

        private void hideTextBox()
        {
            // YesButton Clicked! Let's hide our InputBox and handle the input text.
            InputBox.Visibility = System.Windows.Visibility.Collapsed;
            InputTextBox.Focus();
        }

        private void collapseInputBox()
        {
            InputBox.Visibility = System.Windows.Visibility.Collapsed;
        }

        private (string, string) getTxtBoxValue()
        {
            string txt = string.Empty;
            string txt2 = string.Empty;

            try
            {
                if (InputTextBox.Text != string.Empty && InputCSRBox.Text != string.Empty) 
                {
                    txt = InputTextBox.Text;
                    txt2 = InputCSRBox.Text;
                }
                else
                {
                    throw new ArgumentException();
                }
            }
            catch (ArgumentException aex)
            {
                this.Dispatcher.Invoke(delMsg, "The Gamertag and CSR must be filled in." + aex.Message);
                //MessageBox.Show(@"you've already added that player to the teams list.");
            }
            
            return (txt, txt2);
        }

        private async void btnAddPlayer_Click_Async(object sender, RoutedEventArgs e)
        {

            await this.Dispatcher.BeginInvoke(()=> { hideTextBox(); });
            // Do something with the Input
            string gamerTag = string.Empty;
            string csr = string.Empty;

            gamerTag = await InputTextBox.Dispatcher.InvokeAsync(() => InputTextBox.Text.ToLower());
            csr = await InputCSRBox.Dispatcher.InvokeAsync(() => InputCSRBox.Text);
            Player player = new Player(gamerTag.Trim(), Int32.Parse(csr.Trim()));
            string txtToWrite = gamerTag.Trim() + ',' + csr.Trim();

            await Task.Run(async () => 
            {

                try
                {
                    string folder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                    folder += "\\settings\\Players.csv";
                    var pDict = playerDict;

                    if (playerDict.ContainsKey(player._name))
                    {


                        string path = folder;
                        string[] lines = File.ReadAllLines(path);

                        for (int i = 0; i < lines.Count(); i++)
                        {
                            var line = lines[i];
                            var vals = line.Split(',');
                            if (vals[0].ToLower() == player._name)
                            {
                                lines[i] = player._name + "," + player._csr;
                                break;
                            }
                        }
                        File.WriteAllLines(path, lines);

                        ////File.Delete(folder);
                        //Thread.Sleep(100);
                        pDict[player._name] = player;
                        //FileStream fs = new FileStream(folder, FileMode.OpenOrCreate, FileAccess.Write);
                        //long endPoint = fs.Length;
                        //using (var writer = new StreamWriter(folder, false))
                        //{
                        //    await writer.WriteLineAsync("Gamertag,CSR");
                        //    foreach (var player in pDict.Values)
                        //    {
                                
                        //        await writer.WriteLineAsync(player._name + "," + player._csr.ToString());
                        //    }
                        //    writer.Close();

                        //}
                        //fs.Close();
                    }
                    else
                    {
                        playerDict.Add(player._name, player);
                        FileStream fs = new FileStream(folder, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                        long endPoint = fs.Length;
                        fs.Seek(endPoint, SeekOrigin.End);
                        using (var writer = new StreamWriter(fs))
                        {
                            writer.WriteLine(txtToWrite);
                            writer.Close();
                        }
                        fs.Close();
                    }

                    await Dispatcher.BeginInvoke(delShowPlayers, playerDict.Values.ToList<Player>());
                }
                catch (Exception ex)
                {
                    await this.Dispatcher.BeginInvoke(delMsg, ex.Message);

                }
            });
            // Clear InputBox.
            await this.Dispatcher.BeginInvoke(() => { clearInputTextBox(); });
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // NoButton Clicked! Let's hide our InputBox.
            collapseInputBox();

            // Clear InputBox.
            clearInputTextBox();
        }
    }

}
